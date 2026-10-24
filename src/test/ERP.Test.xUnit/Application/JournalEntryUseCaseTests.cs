using CSharpFunctionalExtensions;
using ERP.Application.Common.Interfaces.Identity;
using ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateJournalEntry;
using ERP.Application.UseCases.Modules.FinanceManagement.Commands.PostJournalEntry;
using ERP.Application.UseCases.Modules.FinanceManagement.Commands.RecordChequeOperation;
using ERP.Application.UseCases.Modules.FinanceManagement.Commands.RegisterCheque;
using ERP.Application.UseCases.Modules.FinanceManagement.Commands.ReverseJournalEntry;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The ledger. A posted entry is never edited or removed, which is the whole reason anybody
    /// trusts it, so the interesting rules are all about what is refused.
    /// </summary>
    public sealed class JournalEntryUseCaseTests
    {
        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed record TestUser(Guid? UserId, string? UserName, bool IsAuthenticated) : ICurrentUser;

        private static ICurrentUser Actor => new TestUser(Guid.NewGuid(), "kaveh.ahangar", true);

        private static FinancialYear CreateOpenYear()
        {
            var year = new FinancialYear("1404", new DateOnly(2025, 3, 21), new DateOnly(2026, 3, 20));
            year.AddPeriod(new AccountingPeriod(year, 1, "Farvardin",
                DateRange.Create(new DateOnly(2025, 3, 21), new DateOnly(2025, 4, 20))));
            year.Open(Guid.NewGuid());
            return year;
        }

        private static Account CreateAccount(string code, string name, AccountBalance balance, bool isFinal = true, bool isLocked = false)
        {
            var account = new Account(code, name, AccountType.AssetCurrent, AccountGroup.LevelFour, balance,
                FinancialStatement.BalanceSheet, isFinal);

            if (isLocked)
                account.Lock();

            return account;
        }

        private sealed class CreateHarness
        {
            public Mock<IFinancialYearRepository> Years { get; } = new();
            public Mock<IAccountRepository> Accounts { get; } = new();
            public Mock<IDetailAccountRepository> DetailAccounts { get; } = new();
            public Mock<IDocumentRepository> Documents { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Document? Created { get; private set; }

            public CreateHarness(FinancialYear? year, IEnumerable<Account> accounts, AccountingPeriod? period = null)
            {
                Years.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(year);
                Years.Setup(r => r.GetPeriodByDateAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(period);

                var byId = accounts.ToDictionary(a => a.Id);
                Accounts.Setup(r => r.GetWithDetailGroupsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Guid id, CancellationToken _) => byId.GetValueOrDefault(id));

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("JournalEntry", paddingLength: 6));

                Documents.Setup(r => r.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                    .Callback<Document, CancellationToken>((document, _) => Created = document)
                    .Returns(Task.CompletedTask);
            }

            public CreateJournalEntryCommandHandler Build() =>
                new(Years.Object, Accounts.Object, DetailAccounts.Object, Documents.Object, Series.Object);
        }

        private static CreateJournalEntryCommand CreateCommand(Account debit, Account credit, decimal amount = 1_000m) => new()
        {
            Title = "Cash sale",
            Description = "Test entry",
            Type = DocumentType.Receive,
            FinancialEventDateTime = new DateTimeOffset(new DateTime(2025, 4, 1), TimeSpan.Zero),
            Lines =
            [
                new JournalEntryLineDto { AccountId = debit.Id, Debtor = amount },
                new JournalEntryLineDto { AccountId = credit.Id, Creditor = amount }
            ]
        };

        /// <summary>
        /// An entry that could be written and made final in one step leaves no moment in which
        /// anybody looks at it, and a posted entry can only be reversed afterwards.
        /// </summary>
        [Fact]
        public async Task A_new_entry_is_a_draft()
        {
            var debit = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var credit = CreateAccount("4001", "Revenue", AccountBalance.Creditor);
            var harness = new CreateHarness(CreateOpenYear(), [debit, credit]);

            var result = await harness.Build().Handle(CreateCommand(debit, credit), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(DocumentStatus.Draft, result.Value.Status);
            Assert.True(result.Value.IsBalanced);
            Assert.Equal(2, result.Value.Lines.Count);
        }

        [Fact]
        public async Task An_entry_with_no_financial_year_is_refused()
        {
            var debit = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var credit = CreateAccount("4001", "Revenue", AccountBalance.Creditor);
            var harness = new CreateHarness(null, [debit, credit]);

            var result = await harness.Build().Handle(CreateCommand(debit, credit), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoFinancialYearForDate", result.Error.Code);
        }

        /// <summary>
        /// Checked on the draft, not left until posting: by then somebody has done the work of
        /// entering it.
        /// </summary>
        [Fact]
        public async Task An_entry_into_a_closed_period_is_refused_while_still_a_draft()
        {
            var year = CreateOpenYear();
            var period = year.Periods[0];
            period.FinancialYear.CloseAccountingPeriod(period.Id, "closer");

            var debit = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var credit = CreateAccount("4001", "Revenue", AccountBalance.Creditor);
            var harness = new CreateHarness(year, [debit, credit], period);

            var result = await harness.Build().Handle(CreateCommand(debit, credit), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("AccountingPeriodIsClosed", result.Error.Code);
        }

        /// <summary>
        /// A summary account only totals its children, and a locked one has been deliberately
        /// closed to new postings. Neither may carry a journal line.
        /// </summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public async Task An_account_that_cannot_take_postings_is_refused(bool isFinal, bool isLocked)
        {
            var debit = CreateAccount("1001", "Cash", AccountBalance.Debtor, isFinal, isLocked);
            var credit = CreateAccount("4001", "Revenue", AccountBalance.Creditor);
            var harness = new CreateHarness(CreateOpenYear(), [debit, credit]);

            var result = await harness.Build().Handle(CreateCommand(debit, credit), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("AccountDoesNotAcceptPostings", result.Error.Code);
        }

        private static Document CreateEntry(FinancialYear year, Account debit, Account credit, decimal amount = 1_000m)
        {
            var entry = new Document(year, "Cash sale", new DateTimeOffset(new DateTime(2025, 4, 1), TimeSpan.Zero),
                "Test entry", DocumentType.Receive);

            entry.AddItem(new DocumentItem(entry, debit, 1, amount, 0m));
            entry.AddItem(new DocumentItem(entry, credit, 2, 0m, amount));
            return entry;
        }

        private static Mock<IDocumentRepository> DocumentsReturning(Document document)
        {
            var documents = new Mock<IDocumentRepository>();
            documents.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(document);
            return documents;
        }

        [Fact]
        public async Task Posting_records_who_asked_for_it()
        {
            var year = CreateOpenYear();
            var entry = CreateEntry(year, CreateAccount("1001", "Cash", AccountBalance.Debtor), CreateAccount("4001", "Revenue", AccountBalance.Creditor));
            var documents = DocumentsReturning(entry);

            var result = await new PostJournalEntryCommandHandler(documents.Object, Actor)
                .Handle(new PostJournalEntryCommand { DocumentId = entry.Id }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(DocumentStatus.Definite, result.Value.Status);
            Assert.Equal("kaveh.ahangar", result.Value.PostedBy);
        }

        [Fact]
        public async Task An_unbalanced_entry_cannot_be_posted()
        {
            var year = CreateOpenYear();
            var entry = new Document(year, "Cash sale", new DateTimeOffset(new DateTime(2025, 4, 1), TimeSpan.Zero),
                "Test entry", DocumentType.Receive);
            entry.AddItem(new DocumentItem(entry, CreateAccount("1001", "Cash", AccountBalance.Debtor), 1, 1_000m, 0m));
            entry.AddItem(new DocumentItem(entry, CreateAccount("4001", "Revenue", AccountBalance.Creditor), 2, 0m, 900m));

            var documents = DocumentsReturning(entry);

            var result = await new PostJournalEntryCommandHandler(documents.Object, Actor)
                .Handle(new PostJournalEntryCommand { DocumentId = entry.Id }, Token);

            Assert.True(result.IsFailure);
            Assert.Equal("EntryIsOutOfBalance", result.Error.Code);
        }

        [Fact]
        public async Task An_entry_already_posted_cannot_be_posted_again()
        {
            var year = CreateOpenYear();
            var entry = CreateEntry(year, CreateAccount("1001", "Cash", AccountBalance.Debtor), CreateAccount("4001", "Revenue", AccountBalance.Creditor));
            entry.Post("someone");

            var documents = DocumentsReturning(entry);

            var result = await new PostJournalEntryCommandHandler(documents.Object, Actor)
                .Handle(new PostJournalEntryCommand { DocumentId = entry.Id }, Token);

            Assert.True(result.IsFailure);
            Assert.Equal("EntryIsNotADraft", result.Error.Code);
        }

        private sealed class ReverseHarness
        {
            public Mock<IDocumentRepository> Documents { get; } = new();
            public Mock<IFinancialYearRepository> Years { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Document? Created { get; private set; }

            public ReverseHarness(Document original, FinancialYear year)
            {
                Documents.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(original);

                Years.Setup(r => r.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(year);
                Years.Setup(r => r.GetPeriodByDateAsync(It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AccountingPeriod?)null);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("JournalEntry", paddingLength: 6));

                Documents.Setup(r => r.CreateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
                    .Callback<Document, CancellationToken>((document, _) => Created = document)
                    .Returns(Task.CompletedTask);
            }

            public ReverseJournalEntryCommandHandler Build() =>
                new(Documents.Object, Years.Object, Series.Object, Actor);
        }

        /// <summary>
        /// A half-undone correction in somebody's queue is the worst of both states: the original
        /// is still in the ledger and the books say something nobody believes.
        /// </summary>
        [Fact]
        public async Task A_reversal_is_posted_immediately_and_mirrors_the_original()
        {
            var year = CreateOpenYear();
            var debit = CreateAccount("1001", "Cash", AccountBalance.Debtor);
            var credit = CreateAccount("4001", "Revenue", AccountBalance.Creditor);
            var original = CreateEntry(year, debit, credit);
            original.Post("someone");

            var harness = new ReverseHarness(original, year);

            var result = await harness.Build().Handle(
                new ReverseJournalEntryCommand { DocumentId = original.Id, Reason = "Posted to the wrong account." },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(DocumentStatus.Definite, result.Value.Status);

            // Debits became credits, line for line.
            Assert.Equal(original.TotalDebtor, result.Value.TotalCreditor);
            Assert.Equal(original.TotalCreditor, result.Value.TotalDebtor);
            Assert.Equal(result.Value.Id, original.ReversedByDocumentId);
        }

        [Fact]
        public async Task A_draft_cannot_be_reversed()
        {
            var year = CreateOpenYear();
            var original = CreateEntry(year, CreateAccount("1001", "Cash", AccountBalance.Debtor), CreateAccount("4001", "Revenue", AccountBalance.Creditor));
            var harness = new ReverseHarness(original, year);

            var result = await harness.Build().Handle(
                new ReverseJournalEntryCommand { DocumentId = original.Id, Reason = "Posted to the wrong account." },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OnlyAPostedEntryCanBeReversed", result.Error.Code);
        }

        [Fact]
        public async Task An_entry_cannot_be_reversed_twice()
        {
            var year = CreateOpenYear();
            var original = CreateEntry(year, CreateAccount("1001", "Cash", AccountBalance.Debtor), CreateAccount("4001", "Revenue", AccountBalance.Creditor));
            original.Post("someone");
            original.MarkAsReversed(Guid.NewGuid());

            var harness = new ReverseHarness(original, year);

            var result = await harness.Build().Handle(
                new ReverseJournalEntryCommand { DocumentId = original.Id, Reason = "Again." },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("EntryIsAlreadyReversed", result.Error.Code);
        }

        private static Mock<IChequeRepository> ChequesReturning(Cheque? cheque, Cheque? bySayad = null)
        {
            var cheques = new Mock<IChequeRepository>();
            cheques.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(cheque);
            cheques.Setup(r => r.GetBySayadNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(bySayad);
            return cheques;
        }

        /// <summary>
        /// The Sayad number is unique nationwide, so a duplicate means the cheque is already in
        /// the books - probably entered by somebody else.
        /// </summary>
        [Fact]
        public async Task A_cheque_already_registered_by_its_sayad_number_is_refused()
        {
            var existing = new Cheque(ChequeType.Received, "123456", Money.Create(5_000_000m, "IRR"),
                new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1));

            var cheques = ChequesReturning(null, bySayad: existing);

            var result = await new RegisterChequeCommandHandler(cheques.Object).Handle(
                new RegisterChequeCommand
                {
                    Type = ChequeType.Received,
                    Number = "123456",
                    SayadNumber = "1234567890123456",
                    Amount = 5_000_000m,
                    CurrencyCode = "IRR",
                    IssueDate = new DateOnly(2026, 1, 1),
                    DueDate = new DateOnly(2026, 4, 1),
                    DrawerName = "Sepehr Trading"
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ChequeNumberIsUsed", result.Error.Code);
        }

        /// <summary>
        /// Which moves are legal from which state is the cheque's rule, and a cheque that has
        /// bounced cannot then clear.
        /// </summary>
        [Fact]
        public async Task A_bounced_cheque_cannot_then_clear()
        {
            var cheque = new Cheque(ChequeType.Received, "123456", Money.Create(5_000_000m, "IRR"),
                new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1));
            cheque.Deposit(Guid.NewGuid(), new DateOnly(2026, 4, 1));
            cheque.Bounce(new DateOnly(2026, 4, 2), "Insufficient funds.");

            var cheques = ChequesReturning(cheque);

            var result = await new RecordChequeOperationCommandHandler(cheques.Object).Handle(
                new RecordChequeOperationCommand
                {
                    ChequeId = cheque.Id,
                    Operation = ChequeOperationKind.Clear,
                    OccurredOn = new DateOnly(2026, 4, 3)
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ChequeOperationIsNotAllowed", result.Error.Code);
        }

        [Fact]
        public async Task A_deposited_cheque_can_clear()
        {
            var cheque = new Cheque(ChequeType.Received, "123456", Money.Create(5_000_000m, "IRR"),
                new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1));
            cheque.Deposit(Guid.NewGuid(), new DateOnly(2026, 4, 1));

            var cheques = ChequesReturning(cheque);

            var result = await new RecordChequeOperationCommandHandler(cheques.Object).Handle(
                new RecordChequeOperationCommand
                {
                    ChequeId = cheque.Id,
                    Operation = ChequeOperationKind.Clear,
                    OccurredOn = new DateOnly(2026, 4, 2)
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(ChequeStatus.Cleared, result.Value.Status);
        }
    }
}
