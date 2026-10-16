using CSharpFunctionalExtensions;
using ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetAccountLedger;
using ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetTrialBalance;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The two reports anybody checks before they believe a set of books.
    /// <para>
    /// What is covered here is the arithmetic the handlers do on top of the rows: netting an
    /// opening balance out of prior turnover, signing each movement by the account's own nature,
    /// carrying the balance down, and unioning the accounts that moved with the ones that only
    /// carried a balance in.
    /// </para>
    /// <para>
    /// What is <em>not</em> covered is the SQL underneath - the date windows and the exclusion of
    /// drafts. Those live in <c>DocumentRepository</c>, and neither available provider can test
    /// them honestly: the in-memory provider cannot translate a join from a document line to a
    /// filtered document, and SQLite stores decimals as text, so a summed column comes back wrong
    /// rather than failing. They need a real relational database, and belong in an integration
    /// suite rather than being faked into passing here.
    /// </para>
    /// </summary>
    public sealed class LedgerQueryTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static Account CreateAccount(string code, string name, AccountBalance balance) =>
            new(code, name,
                balance == AccountBalance.Debtor ? AccountType.AssetCurrent : AccountType.LiabilityCurrent,
                AccountGroup.LevelFour, balance, FinancialStatement.BalanceSheet, isFinal: true);

        private static DateTimeOffset On(int year, int month, int day) => new(new DateTime(year, month, day), TimeSpan.Zero);

        private static LedgerLineRow Line(DateTimeOffset on, decimal debtor, decimal creditor, string title = "Entry") =>
            new(Guid.NewGuid(), "1001", on, title, null, debtor, creditor);

        private sealed class LedgerHarness
        {
            public Mock<IAccountRepository> Accounts { get; } = new();
            public Mock<IDocumentRepository> Documents { get; } = new();

            public LedgerHarness(Account? account, IEnumerable<AccountTurnoverRow> prior, IEnumerable<LedgerLineRow> lines)
            {
                Accounts.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);

                Documents.Setup(r => r.GetTurnoverByAccountAsync(
                        null, It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([.. prior]);

                Documents.Setup(r => r.GetLedgerLinesAsync(
                        It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                        It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([.. lines]);
            }

            public GetAccountLedgerQueryHandler Build() => new(Accounts.Object, Documents.Object);
        }

        private static GetAccountLedgerQuery LedgerFor(Guid accountId) => new()
        {
            AccountId = accountId,
            From = new DateOnly(2025, 5, 1),
            To = new DateOnly(2025, 5, 31)
        };

        /// <summary>
        /// Everything posted before the window, netted by the account's nature. Without it the
        /// running balance means nothing.
        /// </summary>
        [Fact]
        public async Task The_opening_balance_is_prior_turnover_netted()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(
                cash,
                [new AccountTurnoverRow(cash.Id, "1001", "Cash", 1_500m, 0m)],
                []);

            var result = await harness.Build().Handle(LedgerFor(cash.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1_500m, result.Value.OpeningBalance);
        }

        [Fact]
        public async Task An_account_with_nothing_before_the_window_opens_at_zero()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(cash, [], []);

            var result = await harness.Build().Handle(LedgerFor(cash.Id), Token);

            Assert.Equal(0m, result.Value.OpeningBalance);
            Assert.Equal(0m, result.Value.ClosingBalance);
        }

        /// <summary>
        /// Prior turnover comes back for every account at once, so the handler has to pick out its
        /// own - otherwise one account opens on another's balance.
        /// </summary>
        [Fact]
        public async Task The_opening_balance_ignores_other_accounts()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(
                cash,
                [
                    new AccountTurnoverRow(Guid.NewGuid(), "4001", "Revenue", 0m, 9_999m),
                    new AccountTurnoverRow(cash.Id, "1001", "Cash", 1_500m, 0m)
                ],
                []);

            var result = await harness.Build().Handle(LedgerFor(cash.Id), Token);

            Assert.Equal(1_500m, result.Value.OpeningBalance);
        }

        [Fact]
        public async Task The_running_balance_accumulates_down_the_rows()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(
                cash,
                [new AccountTurnoverRow(cash.Id, "1001", "Cash", 1_500m, 0m)],
                [Line(On(2025, 5, 5), 300m, 0m), Line(On(2025, 5, 20), 0m, 200m)]);

            var result = await harness.Build().Handle(LedgerFor(cash.Id), Token);

            Assert.Equal(1_800m, result.Value.Rows[0].RunningBalance);
            Assert.Equal(1_600m, result.Value.Rows[1].RunningBalance);
            Assert.Equal(1_600m, result.Value.ClosingBalance);
        }

        [Fact]
        public async Task The_totals_are_the_movements_in_the_window()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(
                cash,
                [],
                [Line(On(2025, 5, 5), 300m, 0m), Line(On(2025, 5, 20), 0m, 200m)]);

            var result = await harness.Build().Handle(LedgerFor(cash.Id), Token);

            Assert.Equal(300m, result.Value.TotalDebtor);
            Assert.Equal(200m, result.Value.TotalCreditor);
            Assert.Equal(2, result.Value.Rows.Count);
        }

        /// <summary>
        /// The rule that makes a ledger readable: a debit on a liability reduces what is owed
        /// rather than adding to it, because the account's nature is credit.
        /// </summary>
        [Fact]
        public async Task A_debit_on_a_credit_account_reduces_it()
        {
            var payables = CreateAccount("2001", "Trade payables", AccountBalance.Creditor);
            var harness = new LedgerHarness(
                payables,
                [new AccountTurnoverRow(payables.Id, "2001", "Trade payables", 0m, 1_000m)],
                [Line(On(2025, 5, 20), 200m, 0m)]);

            var result = await harness.Build().Handle(LedgerFor(payables.Id), Token);

            Assert.Equal(1_000m, result.Value.OpeningBalance);
            Assert.Equal(800m, result.Value.ClosingBalance);
        }

        [Fact]
        public async Task A_credit_on_a_debit_account_reduces_it()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(
                cash,
                [new AccountTurnoverRow(cash.Id, "1001", "Cash", 1_000m, 0m)],
                [Line(On(2025, 5, 20), 0m, 200m)]);

            var result = await harness.Build().Handle(LedgerFor(cash.Id), Token);

            Assert.Equal(800m, result.Value.ClosingBalance);
        }

        /// <summary>
        /// A debit-natured account that has been credited beyond what it holds is overdrawn, and
        /// the report has to say so rather than clamping at zero.
        /// </summary>
        [Fact]
        public async Task A_balance_may_go_the_wrong_way()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(cash, [], [Line(On(2025, 5, 20), 0m, 500m)]);

            var result = await harness.Build().Handle(LedgerFor(cash.Id), Token);

            Assert.Equal(-500m, result.Value.ClosingBalance);
        }

        [Fact]
        public async Task A_ledger_for_an_unknown_account_is_refused()
        {
            var harness = new LedgerHarness(null, [], []);

            var result = await harness.Build().Handle(LedgerFor(Guid.NewGuid()), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("AccountNotFound", result.Error.Code);
        }

        /// <summary>
        /// The opening balance is everything up to the day before the window, not up to the first
        /// day of it - otherwise a movement on the opening day would be counted twice.
        /// </summary>
        [Fact]
        public async Task The_opening_balance_is_asked_for_up_to_the_day_before()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var harness = new LedgerHarness(cash, [], []);

            await harness.Build().Handle(LedgerFor(cash.Id), Token);

            harness.Documents.Verify(r => r.GetTurnoverByAccountAsync(
                null, new DateOnly(2025, 4, 30), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private sealed class TrialBalanceHarness
        {
            public Mock<IAccountRepository> Accounts { get; } = new();
            public Mock<IDocumentRepository> Documents { get; } = new();

            public TrialBalanceHarness(
                IEnumerable<Account> postable,
                IEnumerable<AccountTurnoverRow> opening,
                IEnumerable<AccountTurnoverRow> movement)
            {
                Accounts.Setup(r => r.GetPostableAccountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([.. postable]);

                Documents.Setup(r => r.GetTurnoverByAccountAsync(
                        null, It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([.. opening]);

                Documents.Setup(r => r.GetTurnoverByAccountAsync(
                        It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([.. movement]);
            }

            public GetTrialBalanceQueryHandler Build() => new(Accounts.Object, Documents.Object);
        }

        private static GetTrialBalanceQuery TrialBalanceFor(bool includeDormant = false) => new()
        {
            From = new DateOnly(2025, 5, 1),
            To = new DateOnly(2025, 5, 31),
            IncludeAccountsWithNoMovement = includeDormant
        };

        /// <summary>
        /// The whole point of the report: if the two totals differ, the books do not balance.
        /// </summary>
        [Fact]
        public async Task A_trial_balance_balances_when_the_movements_do()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var revenue = CreateAccount("4001", "Revenue", AccountBalance.Creditor);

            var harness = new TrialBalanceHarness(
                [cash, revenue],
                [],
                [
                    new AccountTurnoverRow(cash.Id, "1001", "Cash", 300m, 0m),
                    new AccountTurnoverRow(revenue.Id, "4001", "Revenue", 0m, 300m)
                ]);

            var result = await harness.Build().Handle(TrialBalanceFor(), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(300m, result.Value.TotalDebtor);
            Assert.Equal(300m, result.Value.TotalCreditor);
            Assert.True(result.Value.IsBalanced);
        }

        [Fact]
        public async Task A_trial_balance_says_so_when_it_does_not_balance()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var revenue = CreateAccount("4001", "Revenue", AccountBalance.Creditor);

            var harness = new TrialBalanceHarness(
                [cash, revenue],
                [],
                [
                    new AccountTurnoverRow(cash.Id, "1001", "Cash", 300m, 0m),
                    new AccountTurnoverRow(revenue.Id, "4001", "Revenue", 0m, 250m)
                ]);

            var result = await harness.Build().Handle(TrialBalanceFor(), Token);

            Assert.False(result.Value.IsBalanced);
        }

        [Fact]
        public async Task Each_row_carries_its_opening_and_closing_balance()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);

            var harness = new TrialBalanceHarness(
                [cash],
                [new AccountTurnoverRow(cash.Id, "1001", "Cash", 1_500m, 0m)],
                [new AccountTurnoverRow(cash.Id, "1001", "Cash", 300m, 200m)]);

            var result = await harness.Build().Handle(TrialBalanceFor(), Token);

            var row = Assert.Single(result.Value.Rows);
            Assert.Equal(1_500m, row.OpeningBalance);
            Assert.Equal(300m, row.Debtor);
            Assert.Equal(200m, row.Creditor);
            Assert.Equal(1_600m, row.ClosingBalance);
        }

        /// <summary>
        /// Leaving out what did not move is what keeps the report readable; including it is a
        /// deliberate ask.
        /// </summary>
        [Fact]
        public async Task An_account_that_did_not_move_is_left_out_by_default()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var dormant = CreateAccount("1009", "Petty cash", AccountBalance.Debtor);

            var harness = new TrialBalanceHarness(
                [cash, dormant],
                [new AccountTurnoverRow(dormant.Id, "1009", "Petty cash", 400m, 0m)],
                [new AccountTurnoverRow(cash.Id, "1001", "Cash", 300m, 0m)]);

            var result = await harness.Build().Handle(TrialBalanceFor(), Token);

            Assert.Single(result.Value.Rows);
            Assert.DoesNotContain(result.Value.Rows, r => r.AccountCode == "1009");
        }

        /// <summary>
        /// An account that only carried a balance forward still belongs on the report when asked
        /// for, which is why the opening and movement sets are unioned rather than the movements
        /// alone being walked.
        /// </summary>
        [Fact]
        public async Task An_account_with_only_an_opening_balance_can_be_included()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var dormant = CreateAccount("1009", "Petty cash", AccountBalance.Debtor);

            var harness = new TrialBalanceHarness(
                [cash, dormant],
                [new AccountTurnoverRow(dormant.Id, "1009", "Petty cash", 400m, 0m)],
                [new AccountTurnoverRow(cash.Id, "1001", "Cash", 300m, 0m)]);

            var result = await harness.Build().Handle(TrialBalanceFor(includeDormant: true), Token);

            var carried = result.Value.Rows.Single(r => r.AccountCode == "1009");
            Assert.Equal(400m, carried.OpeningBalance);
            Assert.Equal(0m, carried.Debtor);
            Assert.Equal(400m, carried.ClosingBalance);
        }

        [Fact]
        public async Task Rows_come_back_in_account_code_order()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var payables = CreateAccount("2001", "Trade payables", AccountBalance.Creditor);
            var revenue = CreateAccount("4001", "Revenue", AccountBalance.Creditor);

            var harness = new TrialBalanceHarness(
                [cash, payables, revenue],
                [],
                [
                    new AccountTurnoverRow(revenue.Id, "4001", "Revenue", 0m, 300m),
                    new AccountTurnoverRow(cash.Id, "1001", "Cash", 300m, 0m),
                    new AccountTurnoverRow(payables.Id, "2001", "Trade payables", 0m, 100m)
                ]);

            var result = await harness.Build().Handle(TrialBalanceFor(), Token);

            Assert.Equal(["1001", "2001", "4001"], result.Value.Rows.Select(r => r.AccountCode));
        }

        /// <summary>
        /// Turnover comes back for anything that was ever posted to, including accounts that have
        /// since been closed to new postings. A report has to skip what it cannot describe rather
        /// than fail on it.
        /// </summary>
        [Fact]
        public async Task An_account_no_longer_postable_is_skipped_rather_than_breaking_the_report()
        {
            var cash = CreateAccount("1001", "Cash", AccountBalance.Debtor);

            var harness = new TrialBalanceHarness(
                [cash],
                [],
                [
                    new AccountTurnoverRow(cash.Id, "1001", "Cash", 300m, 0m),
                    new AccountTurnoverRow(Guid.NewGuid(), "9999", "Retired", 0m, 300m)
                ]);

            var result = await harness.Build().Handle(TrialBalanceFor(), Token);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Rows);
        }

        [Fact]
        public async Task A_window_that_ends_before_it_starts_is_refused()
        {
            var harness = new TrialBalanceHarness([], [], []);

            var query = new GetTrialBalanceQuery { From = new DateOnly(2025, 5, 31), To = new DateOnly(2025, 5, 1) };
            var result = await harness.Build().Handle(query, Token);

            Assert.True(result.IsFailure);
        }
    }
}
