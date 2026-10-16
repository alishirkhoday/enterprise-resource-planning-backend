using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// The rules that keep the ledger trustworthy: an entry must balance before it is posted, and
    /// once posted it cannot be edited.
    /// </summary>
    public sealed class JournalEntryTests
    {
        private static FinancialYear CreateOpenYear()
        {
            var year = new FinancialYear("1404", new DateOnly(2025, 3, 21), new DateOnly(2026, 3, 20));
            var period = new AccountingPeriod(year, 1, "Farvardin", DateRange.Create(new DateOnly(2025, 3, 21), new DateOnly(2025, 4, 20)));
            year.AddPeriod(period);
            year.Open(Guid.NewGuid());
            return year;
        }

        private static Account CreateFinalAccount(string code, string name, AccountBalance balance) =>
            new(code, name, AccountType.AssetCurrent, AccountGroup.LevelFour, balance, FinancialStatement.BalanceSheet, isFinal: true);

        private static Document CreateDraftEntry(FinancialYear year) =>
            new(year, "Cash sale", new DateTimeOffset(new DateTime(2025, 4, 1), TimeSpan.Zero), "Test entry", DocumentType.Receive);

        [Fact]
        public void An_unbalanced_entry_cannot_be_posted()
        {
            var year = CreateOpenYear();
            var entry = CreateDraftEntry(year);
            var cash = CreateFinalAccount("1001", "Cash", AccountBalance.Debtor);
            var revenue = CreateFinalAccount("4001", "Revenue", AccountBalance.Creditor);

            entry.AddItem(new DocumentItem(entry, cash, 1, 1_000m, 0m));
            entry.AddItem(new DocumentItem(entry, revenue, 2, 0m, 900m));

            Assert.False(entry.IsBalanced());
            Assert.Throws<InvalidOperationException>(() => entry.Post());
        }

        [Fact]
        public void A_balanced_entry_posts_and_raises_an_event()
        {
            var year = CreateOpenYear();
            var entry = CreateDraftEntry(year);
            var cash = CreateFinalAccount("1001", "Cash", AccountBalance.Debtor);
            var revenue = CreateFinalAccount("4001", "Revenue", AccountBalance.Creditor);

            entry.AddItem(new DocumentItem(entry, cash, 1, 1_000m, 0m));
            entry.AddItem(new DocumentItem(entry, revenue, 2, 0m, 1_000m));
            entry.Post("tester");

            Assert.True(entry.IsBalanced());
            Assert.Equal(DocumentStatus.Definite, entry.Status);
            Assert.Contains(entry.DomainEvents, e => e is JournalEntryPosted);
        }

        [Fact]
        public void A_posted_entry_cannot_be_edited()
        {
            var year = CreateOpenYear();
            var entry = CreateDraftEntry(year);
            var cash = CreateFinalAccount("1001", "Cash", AccountBalance.Debtor);
            var revenue = CreateFinalAccount("4001", "Revenue", AccountBalance.Creditor);

            entry.AddItem(new DocumentItem(entry, cash, 1, 1_000m, 0m));
            entry.AddItem(new DocumentItem(entry, revenue, 2, 0m, 1_000m));
            entry.Post();

            Assert.Throws<InvalidOperationException>(() =>
                entry.AddItem(new DocumentItem(entry, cash, 3, 50m, 0m)));
        }

        [Fact]
        public void A_line_cannot_carry_both_a_debit_and_a_credit()
        {
            var year = CreateOpenYear();
            var entry = CreateDraftEntry(year);
            var cash = CreateFinalAccount("1001", "Cash", AccountBalance.Debtor);

            Assert.Throws<ArgumentException>(() => new DocumentItem(entry, cash, 1, 100m, 100m));
        }

        [Fact]
        public void A_summary_account_does_not_accept_postings()
        {
            var year = CreateOpenYear();
            var entry = CreateDraftEntry(year);
            var summary = new Account("10", "Current assets", AccountType.AssetCurrent, AccountGroup.LevelOne,
                AccountBalance.Debtor, FinancialStatement.BalanceSheet, isFinal: false);

            Assert.Throws<InvalidOperationException>(() =>
                entry.AddItem(new DocumentItem(entry, summary, 1, 100m, 0m)));
        }

        [Fact]
        public void A_subsidiary_account_from_the_wrong_group_is_refused()
        {
            var year = CreateOpenYear();
            var entry = CreateDraftEntry(year);
            var receivable = CreateFinalAccount("1301", "Trade receivables", AccountBalance.Debtor);

            var customers = new DetailAccountGroup("C", "Customers", "Customer");
            var suppliers = new DetailAccountGroup("S", "Suppliers", "Supplier");
            receivable.RequireDetailGroup(customers, 1);

            var supplier = new DetailAccount(suppliers, "S-001", "A supplier");
            var line = new DocumentItem(entry, receivable, 1, 1_000m, 0m);

            Assert.Throws<InvalidOperationException>(() => line.SetDetailAccounts(supplier));
        }
    }
}
