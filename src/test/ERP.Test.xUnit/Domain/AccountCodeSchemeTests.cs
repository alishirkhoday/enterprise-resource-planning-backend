using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// The chart's numbering, which used to live inside a command handler as a fourteen case
    /// switch and a hand rolled padding loop.
    /// </summary>
    public sealed class AccountCodeSchemeTests
    {
        private static Account CreateHeading(AccountType type = AccountType.AssetCurrent) =>
            Account.CreateTopLevel("Assets", type, AccountBalance.Debtor, FinancialStatement.BalanceSheet, isFinal: false, existingCountOfType: 0);

        [Theory]
        [InlineData(AccountType.AssetCurrent, "101")]
        [InlineData(AccountType.AssetNonCurrentTangible, "141")]
        [InlineData(AccountType.LiabilityCurrent, "201")]
        [InlineData(AccountType.Equity, "301")]
        [InlineData(AccountType.RevenueOperating, "401")]
        [InlineData(AccountType.ExpenseOperating, "501")]
        [InlineData(AccountType.CostPriceProductSold, "801")]
        [InlineData(AccountType.Other, "901")]
        public void The_first_account_of_a_type_opens_its_block(AccountType type, string expected)
        {
            Assert.Equal(expected, AccountCodeScheme.TopLevelCode(type, existingCountOfType: 0));
        }

        [Fact]
        public void Each_further_account_takes_the_next_number_in_the_block()
        {
            Assert.Equal("102", AccountCodeScheme.TopLevelCode(AccountType.AssetCurrent, 1));
            Assert.Equal("110", AccountCodeScheme.TopLevelCode(AccountType.AssetCurrent, 9));
        }

        /// <summary>
        /// Current assets are given forty numbers before tangible fixed assets begin. Running past
        /// that would hand out a code belonging to another type, which is worse than refusing.
        /// </summary>
        [Fact]
        public void A_block_that_runs_out_is_refused_rather_than_spilling_into_the_next()
        {
            Assert.Equal("139", AccountCodeScheme.TopLevelCode(AccountType.AssetCurrent, 38));

            Assert.Throws<InvalidOperationException>(() =>
                AccountCodeScheme.TopLevelCode(AccountType.AssetCurrent, 39));
        }

        [Fact]
        public void A_child_extends_its_parent_with_four_digits()
        {
            Assert.Equal("1010001", AccountCodeScheme.ChildCode("101", existingChildCount: 0));
            Assert.Equal("1010009", AccountCodeScheme.ChildCode("101", existingChildCount: 8));
        }

        /// <summary>
        /// The bug this scheme replaced. Padding used to be measured from the existing count, so
        /// the tenth child produced a five digit segment - 101 + "000" + "10" - and every code
        /// under that parent stopped lining up from then on.
        /// </summary>
        [Theory]
        [InlineData(9, "1010010")]
        [InlineData(99, "1010100")]
        [InlineData(999, "1011000")]
        public void The_tenth_child_is_still_four_digits_wide(int existingChildCount, string expected)
        {
            var code = AccountCodeScheme.ChildCode("101", existingChildCount);

            Assert.Equal(expected, code);
            Assert.Equal("101".Length + AccountCodeScheme.ChildSequenceLength, code.Length);
        }

        [Fact]
        public void Every_child_of_one_parent_has_the_same_length()
        {
            var lengths = Enumerable.Range(0, 1_200)
                .Select(count => AccountCodeScheme.ChildCode("101", count).Length)
                .Distinct();

            Assert.Single(lengths);
        }

        [Fact]
        public void A_parent_cannot_hold_more_children_than_the_width_allows()
        {
            Assert.Throws<InvalidOperationException>(() =>
                AccountCodeScheme.ChildCode("101", AccountCodeScheme.MaximumChildSequence));
        }

        [Fact]
        public void A_heading_is_always_at_the_first_level()
        {
            Assert.Equal(AccountGroup.LevelOne, CreateHeading().Group);
        }

        [Fact]
        public void A_child_is_numbered_from_its_parent()
        {
            var heading = CreateHeading();

            var child = heading.CreateChild("Cash", AccountGroup.LevelTwo, AccountBalance.Debtor, FinancialStatement.BalanceSheet, isFinal: true, existingChildCount: 0);

            Assert.Equal(heading.Code + "0001", child.Code);
            Assert.Equal(heading.Id, child.ParentId);
            Assert.Contains(child, heading.Children);
        }

        /// <summary>
        /// A liability under an asset still adds up line by line and is wrong in every report that
        /// groups by type, which nobody finds until a statement is already out.
        /// </summary>
        [Fact]
        public void A_child_takes_its_parents_type()
        {
            var heading = CreateHeading(AccountType.LiabilityCurrent);

            var child = heading.CreateChild("Trade payables", AccountGroup.LevelTwo, AccountBalance.Creditor, FinancialStatement.BalanceSheet, true, 0);

            Assert.Equal(AccountType.LiabilityCurrent, child.Type);
        }

        [Fact]
        public void Nothing_may_sit_under_a_final_account()
        {
            var heading = Account.CreateTopLevel("Cash", AccountType.AssetCurrent, AccountBalance.Debtor, FinancialStatement.BalanceSheet, isFinal: true, existingCountOfType: 0);

            Assert.Throws<InvalidOperationException>(() =>
                heading.CreateChild("Petty cash", AccountGroup.LevelTwo, AccountBalance.Debtor, FinancialStatement.BalanceSheet, true, 0));
        }

        [Theory]
        [InlineData(AccountGroup.LevelOne)]
        [InlineData(AccountGroup.LevelTwo)]
        public void A_child_has_to_sit_deeper_than_its_parent(AccountGroup group)
        {
            var heading = CreateHeading();
            var level2 = heading.CreateChild("Cash", AccountGroup.LevelTwo, AccountBalance.Debtor, FinancialStatement.BalanceSheet, isFinal: false, existingChildCount: 0);

            Assert.Throws<InvalidOperationException>(() =>
                level2.CreateChild("Nonsense", group, AccountBalance.Debtor, FinancialStatement.BalanceSheet, true, 0));
        }

        [Fact]
        public void A_branch_of_the_chart_is_answerable_by_prefix()
        {
            var heading = CreateHeading();
            var cash = heading.CreateChild("Cash", AccountGroup.LevelTwo, AccountBalance.Debtor, FinancialStatement.BalanceSheet, false, 0);
            var pettyCash = cash.CreateChild("Petty cash", AccountGroup.LevelThree, AccountBalance.Debtor, FinancialStatement.BalanceSheet, true, 0);

            Assert.StartsWith(heading.Code, cash.Code, StringComparison.Ordinal);
            Assert.StartsWith(cash.Code, pettyCash.Code, StringComparison.Ordinal);
        }
    }
}
