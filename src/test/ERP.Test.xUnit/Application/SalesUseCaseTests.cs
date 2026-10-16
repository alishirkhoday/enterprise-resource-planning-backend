using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CreditSalesReturn;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.IssueInvoice;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Queries.GetCustomerStatement;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Invoicing, crediting and the statement that reports both.
    /// <para>
    /// The three cases worth having are the ones where money can quietly go missing: a price the
    /// salesperson chose over the agreed one, a credit for more than went out, and a credit note
    /// counted as a debt rather than against one.
    /// </para>
    /// </summary>
    public sealed class SalesUseCaseTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly Guid DetailAccount = Guid.NewGuid();
        private static readonly DateOnly Today = new(2026, 5, 10);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static Customer CreateCustomer(decimal? creditLimit = null, bool suspended = false)
        {
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            customer.LinkToDetailAccount(DetailAccount);

            if (creditLimit is not null)
                customer.SetCreditLimit(Money.Create(creditLimit.Value, "IRR"));

            if (suspended)
                customer.Suspend("Unpaid invoices.");

            return customer;
        }

        private static Item CreateItem(string name = "Bolt")
        {
            var item = new Item(new ItemGroup("G-1", "Fasteners"), ItemType.Part);
            item.Update("SKU-1", name);
            item.SetUsage(isPurchasable: true, isSellable: true, isManufactured: false);
            return item;
        }

        private static PriceList CreatePriceList(Item item, decimal price)
        {
            var priceList = new PriceList("PL-1", "Retail", PriceListType.Sales, "IRR",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

            priceList.AddItem(new PriceListItem(priceList, item, Unit, Money.Create(price, "IRR")));

            return priceList;
        }

        private sealed class IssueHarness
        {
            public Mock<ICustomerRepository> Customers { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IPriceListRepository> PriceLists { get; } = new();
            public Mock<IReceivableRepository> Receivables { get; } = new();
            public Mock<IInvoiceRepository> Invoices { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Invoice? Created { get; private set; }

            public IssueHarness(Customer? customer, Item item, PriceList? priceList, decimal outstanding = 0m)
            {
                Customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);

                PriceLists.Setup(r => r.GetEffectiveForSaleAsync(
                        It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(priceList);

                Receivables.Setup(r => r.GetOutstandingTotalAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(outstanding);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("SalesInvoice", paddingLength: 5));

                Invoices.Setup(r => r.CreateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
                    .Callback<Invoice, CancellationToken>((invoice, _) => Created = invoice)
                    .Returns(Task.CompletedTask);
            }

            public IssueInvoiceCommandHandler Build() => new(
                Customers.Object, Items.Object, PriceLists.Object, Receivables.Object, Invoices.Object, Series.Object);
        }

        private static IssueInvoiceCommand IssueCommand(Guid customerId, Guid itemId, decimal? unitPrice = null, decimal quantity = 10m) => new()
        {
            CustomerId = customerId,
            IssueDate = Today,
            DueDate = Today.AddDays(30),
            CurrencyCode = "IRR",
            Lines = [new InvoiceLineDto { ItemId = itemId, Quantity = quantity, UnitOfMeasureId = Unit, UnitPrice = unitPrice }]
        };

        [Fact]
        public async Task A_line_without_a_price_is_priced_from_the_list()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var harness = new IssueHarness(customer, item, CreatePriceList(item, 500m));

            var result = await harness.Build().Handle(IssueCommand(customer.Id, item.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(5_000m, result.Value.AmountPayable);
            Assert.Equal(InvoiceStatus.Issued, result.Value.Status);
        }

        [Fact]
        public async Task An_item_the_price_list_does_not_carry_is_refused_rather_than_priced_at_nothing()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var empty = new PriceList("PL-2", "Empty", PriceListType.Sales, "IRR",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

            var harness = new IssueHarness(customer, item, empty);

            var result = await harness.Build().Handle(IssueCommand(customer.Id, item.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ItemHasNoPrice", result.Error.Code);
        }

        [Fact]
        public async Task The_credit_limit_is_checked_against_what_is_already_owed_plus_this_invoice()
        {
            var customer = CreateCustomer(creditLimit: 6_000m);
            var item = CreateItem();

            // 2,000 already owed plus 5,000 on this invoice is 7,000 against a limit of 6,000.
            // Either number alone is under it, which is the case a per invoice check would miss.
            var harness = new IssueHarness(customer, item, CreatePriceList(item, 500m), outstanding: 2_000m);

            var result = await harness.Build().Handle(IssueCommand(customer.Id, item.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("CreditLimitWouldBeExceeded", result.Error.Code);
            Assert.Null(harness.Created);
        }

        [Fact]
        public async Task A_customer_with_no_limit_is_not_stopped_by_what_they_owe()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var harness = new IssueHarness(customer, item, CreatePriceList(item, 500m), outstanding: 900_000m);

            var result = await harness.Build().Handle(IssueCommand(customer.Id, item.Id), Token);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task A_suspended_customer_cannot_be_invoiced()
        {
            var customer = CreateCustomer(suspended: true);
            var item = CreateItem();
            var harness = new IssueHarness(customer, item, CreatePriceList(item, 500m));

            var result = await harness.Build().Handle(IssueCommand(customer.Id, item.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("CustomerCannotBeSoldTo", result.Error.Code);
        }

        private static Invoice CreateIssuedInvoice(Customer customer, Item item, decimal unitPrice = 500m, decimal quantity = 10m)
        {
            var invoice = new Invoice(customer, "INV-1", InvoiceType.Sales, Today, Today.AddDays(30), "IRR");
            invoice.AddItem(new InvoiceItem(invoice, item, 1, Money.Create(unitPrice, "IRR"), quantity, Unit));
            invoice.Issue();
            return invoice;
        }

        private sealed class CreditHarness
        {
            public Mock<IInvoiceRepository> Invoices { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Invoice? Created { get; private set; }

            public CreditHarness(Invoice? original)
            {
                Invoices.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(original);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("CreditNote", paddingLength: 5));

                Invoices.Setup(r => r.CreateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
                    .Callback<Invoice, CancellationToken>((invoice, _) => Created = invoice)
                    .Returns(Task.CompletedTask);
            }

            public CreditSalesReturnCommandHandler Build() => new(Invoices.Object, Series.Object);
        }

        [Fact]
        public async Task Naming_no_lines_credits_the_whole_invoice()
        {
            var invoice = CreateIssuedInvoice(CreateCustomer(), CreateItem());
            var harness = new CreditHarness(invoice);

            var result = await harness.Build().Handle(
                new CreditSalesReturnCommand { OriginalInvoiceId = invoice.Id, IssueDate = Today, Reason = "Wrong size." },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(InvoiceType.CreditNote, result.Value.Type);
            Assert.Equal(invoice.Id, result.Value.OriginalInvoiceId);
            Assert.Equal(invoice.AmountPayable.Amount, result.Value.AmountPayable);
        }

        [Fact]
        public async Task A_credit_note_is_priced_at_what_was_sold_not_at_todays_price()
        {
            var item = CreateItem();
            var invoice = CreateIssuedInvoice(CreateCustomer(), item, unitPrice: 400m);
            var harness = new CreditHarness(invoice);

            var result = await harness.Build().Handle(
                new CreditSalesReturnCommand
                {
                    OriginalInvoiceId = invoice.Id,
                    IssueDate = Today.AddDays(40),
                    Reason = "Returned.",
                    Lines = [new SalesReturnLineDto { InvoiceItemId = invoice.Items[0].Id, Quantity = 4m }]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1_600m, result.Value.AmountPayable);
            Assert.Equal(400m, result.Value.Lines[0].UnitPrice);
        }

        [Fact]
        public async Task More_cannot_be_credited_than_was_invoiced()
        {
            var invoice = CreateIssuedInvoice(CreateCustomer(), CreateItem(), quantity: 10m);
            var harness = new CreditHarness(invoice);

            var result = await harness.Build().Handle(
                new CreditSalesReturnCommand
                {
                    OriginalInvoiceId = invoice.Id,
                    IssueDate = Today,
                    Reason = "Returned.",
                    Lines = [new SalesReturnLineDto { InvoiceItemId = invoice.Items[0].Id, Quantity = 11m }]
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("MoreReturnedThanSold", result.Error.Code);
        }

        [Fact]
        public async Task A_credit_note_cannot_be_dated_before_the_invoice_it_credits()
        {
            var invoice = CreateIssuedInvoice(CreateCustomer(), CreateItem());
            var harness = new CreditHarness(invoice);

            var result = await harness.Build().Handle(
                new CreditSalesReturnCommand { OriginalInvoiceId = invoice.Id, IssueDate = Today.AddDays(-1), Reason = "Returned." },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("CreditPredatesTheInvoice", result.Error.Code);
        }

        [Fact]
        public async Task A_blocked_customer_can_still_be_credited()
        {
            // The block is often exactly why the goods came back. Refusing here would leave a
            // customer owing for stock that is sitting in the warehouse.
            var customer = CreateCustomer();
            var invoice = CreateIssuedInvoice(customer, CreateItem());
            customer.Suspend("Unpaid invoices.");

            var result = await new CreditHarness(invoice).Build().Handle(
                new CreditSalesReturnCommand { OriginalInvoiceId = invoice.Id, IssueDate = Today, Reason = "Returned." },
                Token);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task A_draft_invoice_is_corrected_by_editing_it_not_by_a_credit_note()
        {
            var customer = CreateCustomer();
            var draft = new Invoice(customer, "INV-2", InvoiceType.Sales, Today, Today.AddDays(30), "IRR");
            draft.AddItem(new InvoiceItem(draft, CreateItem(), 1, Money.Create(500m, "IRR"), 10m, Unit));

            var result = await new CreditHarness(draft).Build().Handle(
                new CreditSalesReturnCommand { OriginalInvoiceId = draft.Id, IssueDate = Today, Reason = "Mistake." },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("InvoiceCannotBeCredited", result.Error.Code);
        }

        private sealed class StatementHarness
        {
            public Mock<ICustomerRepository> Customers { get; } = new();
            public Mock<IInvoiceRepository> Invoices { get; } = new();
            public Mock<IReceivableRepository> Receivables { get; } = new();

            public StatementHarness(Customer customer, IReadOnlyList<Invoice> invoices, decimal outstanding, IReadOnlyList<ReceivableAgeingRow>? ageing = null)
            {
                Customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);

                Invoices.Setup(r => r.GetByCustomerAsync(
                        It.IsAny<Guid>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(invoices);

                Receivables.Setup(r => r.GetOutstandingTotalAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(outstanding);

                Receivables.Setup(r => r.GetAgeingRowsAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ageing ?? []);
            }

            public GetCustomerStatementQueryHandler Build() => new(Customers.Object, Invoices.Object, Receivables.Object);
        }

        [Fact]
        public async Task A_credit_note_reduces_the_total_invoiced_rather_than_adding_to_it()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var invoice = CreateIssuedInvoice(customer, item, unitPrice: 500m, quantity: 10m);

            var creditNote = new Invoice(customer, "CN-1", InvoiceType.CreditNote, Today, Today, "IRR");
            creditNote.AddItem(new InvoiceItem(creditNote, item, 1, Money.Create(500m, "IRR"), 4m, Unit));
            creditNote.Issue();

            var harness = new StatementHarness(customer, [invoice, creditNote], outstanding: 3_000m);

            var result = await harness.Build().Handle(new GetCustomerStatementQuery { CustomerId = customer.Id, AsOf = Today }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(3_000m, result.Value.TotalInvoiced);
        }

        [Fact]
        public async Task Drafts_and_cancelled_invoices_are_left_off_the_statement()
        {
            var customer = CreateCustomer();
            var item = CreateItem();

            var draft = new Invoice(customer, "INV-3", InvoiceType.Sales, Today, Today.AddDays(30), "IRR");
            draft.AddItem(new InvoiceItem(draft, item, 1, Money.Create(999m, "IRR"), 1m, Unit));

            var cancelled = CreateIssuedInvoice(customer, item);
            cancelled.Cancel("Raised in error.");

            var harness = new StatementHarness(customer, [draft, cancelled], outstanding: 0m);

            var result = await harness.Build().Handle(new GetCustomerStatementQuery { CustomerId = customer.Id, AsOf = Today }, Token);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Lines);
            Assert.Equal(0m, result.Value.TotalInvoiced);
        }

        [Theory]
        [InlineData(0, "Not yet due")]
        [InlineData(20, "1-30 days")]
        [InlineData(45, "31-60 days")]
        [InlineData(75, "61-90 days")]
        [InlineData(200, "Over 90 days")]
        public async Task Each_open_item_lands_in_the_bucket_its_age_puts_it_in(int daysOverdue, string expectedBucket)
        {
            var customer = CreateCustomer();

            var row = new ReceivableAgeingRow(
                Guid.NewGuid(),
                DetailAccount,
                Today.AddDays(-daysOverdue),
                1_000m,
                "IRR");

            var harness = new StatementHarness(customer, [], outstanding: 1_000m, ageing: [row]);

            var result = await harness.Build().Handle(new GetCustomerStatementQuery { CustomerId = customer.Id, AsOf = Today }, Token);

            Assert.True(result.IsSuccess);

            var filled = result.Value.Ageing.Single(b => b.Amount != 0m);
            Assert.Equal(expectedBucket, filled.Bucket);

            // Every bucket comes back whether or not it holds anything, so a report reads the
            // same shape each time and a zero means nothing here rather than not asked.
            Assert.Equal(5, result.Value.Ageing.Count);
        }

        [Fact]
        public async Task Being_over_the_limit_is_reported_rather_than_left_to_be_worked_out()
        {
            var customer = CreateCustomer(creditLimit: 1_000m);
            var harness = new StatementHarness(customer, [], outstanding: 2_500m);

            var result = await harness.Build().Handle(new GetCustomerStatementQuery { CustomerId = customer.Id, AsOf = Today }, Token);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsOverCreditLimit);
            Assert.Equal(-1_500m, result.Value.CreditAvailable);
        }
    }
}
