using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.IssueInvoice
{
    /// <summary>
    /// Issues a sales invoice.
    /// <para>
    /// Issuing is the moment the customer owes the money, so the credit check happens here and not
    /// when the order was taken: an order can sit for weeks while the customer's position moves.
    /// </para>
    /// </summary>
    public class IssueInvoiceCommandHandler(
        ICustomerRepository customers,
        IItemRepository items,
        IPriceListRepository priceLists,
        IReceivableRepository receivables,
        IInvoiceRepository invoices,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<IssueInvoiceCommand, Result<InvoiceDto, Error>>
    {
        private const string InvoiceNumberingKey = "SalesInvoice";

        public async Task<Result<InvoiceDto, Error>> Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
        {
            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();

            var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return Result.Failure<InvoiceDto, Error>(Errors.General.NotFound(request.CustomerId.ToString(), nameof(Customer)));

            if (!customer.CanBeSoldTo)
                return Result.Failure<InvoiceDto, Error>(Errors.Order.CustomerCannotBeSoldTo(customer.DisplayName));

            var priceList = await priceLists.GetEffectiveForSaleAsync(
                customer.PriceListId,
                customer.CustomerGroupId,
                currencyCode,
                request.IssueDate,
                cancellationToken);

            var series = await numberingSeries.GetForUpdateAsync(InvoiceNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<InvoiceDto, Error>(Errors.Order.NumberingSeriesMissing(InvoiceNumberingKey));

            var invoice = new Invoice(
                customer,
                series.Next(request.IssueDate),
                InvoiceType.Sales,
                request.IssueDate,
                request.DueDate,
                currencyCode);

            var lines = await BuildLinesAsync(invoice, request, priceList, cancellationToken);
            if (lines.IsFailure)
                return Result.Failure<InvoiceDto, Error>(lines.Error);

            if (request.AdditionalCharges > 0m)
                invoice.SetAdditionalCharges(Money.Create(request.AdditionalCharges, currencyCode));

            if (request.HeaderDiscountPercentage > 0m)
                invoice.SetHeaderDiscount(Percentage.Create(request.HeaderDiscountPercentage));

            if (request.SaleOrderId is not null)
                invoice.LinkToSaleOrder(request.SaleOrderId.Value);

            if (request.PaymentTermId is not null)
                invoice.SetPaymentTerm(request.PaymentTermId.Value, request.DueDate);

            if (request.SalesRepHumanId is not null)
                invoice.AssignSalesRep(request.SalesRepHumanId.Value);

            if (request.BranchId is not null)
                invoice.AssignToBranch(request.BranchId.Value);

            invoice.SetDescription(request.Description);

            // Checked once the total is known, because a limit is about the total and not about
            // any one line. Refused rather than warned: an invoice that breaches a limit is a
            // decision somebody has to make deliberately.
            var breach = await CheckCreditLimitAsync(customer, invoice.AmountPayable, cancellationToken);
            if (breach.IsFailure)
                return Result.Failure<InvoiceDto, Error>(breach.Error);

            try
            {
                invoice.Issue();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<InvoiceDto, Error>(Errors.Sales.InvoiceCannotBeIssued(exception.Message));
            }

            await invoices.CreateAsync(invoice, cancellationToken);

            return Result.Success<InvoiceDto, Error>(invoice.ToDto());
        }

        private async Task<Result<List<InvoiceItem>, Error>> BuildLinesAsync(
            Invoice invoice,
            IssueInvoiceCommand request,
            PriceList? priceList,
            CancellationToken cancellationToken)
        {
            var built = new List<InvoiceItem>(request.Lines.Count);
            var found = await items.GetByIdsAsync([.. request.Lines.Select(l => l.ItemId)], cancellationToken);
            var byId = found.ToDictionary(i => i.Id);

            short lineNumber = 1;

            foreach (var line in request.Lines)
            {
                if (!byId.TryGetValue(line.ItemId, out var item))
                    return Result.Failure<List<InvoiceItem>, Error>(Errors.General.NotFound(line.ItemId.ToString(), nameof(Item)));

                if (!item.IsSellable)
                    return Result.Failure<List<InvoiceItem>, Error>(Errors.Order.ItemIsNotSellable(item.Name ?? line.ItemId.ToString()));

                var price = ResolvePrice(line, item, priceList, invoice.CurrencyCode);
                if (price.IsFailure)
                    return Result.Failure<List<InvoiceItem>, Error>(price.Error);

                var invoiceItem = new InvoiceItem(invoice, item, lineNumber++, price.Value, line.Quantity, line.UnitOfMeasureId);
                invoice.AddItem(invoiceItem);
                built.Add(invoiceItem);

                if (line.DiscountPercentage is not null)
                    invoice.SetInvoiceItemDiscountPercentage(invoiceItem.Id, Percentage.Create(line.DiscountPercentage.Value));

                if (line.TaxId is not null && line.TaxPercentage is not null)
                    invoice.SetInvoiceItemTax(invoiceItem.Id, line.TaxId.Value, Percentage.Create(line.TaxPercentage.Value));

                if (line.ItemBatchId is not null)
                    invoice.SetInvoiceItemBatch(invoiceItem.Id, line.ItemBatchId.Value);

                invoice.SetInvoiceItemDescription(invoiceItem.Id, line.Description);
            }

            return Result.Success<List<InvoiceItem>, Error>(built);
        }

        private static Result<Money, Error> ResolvePrice(InvoiceLineDto line, Item item, PriceList? priceList, string currencyCode)
        {
            if (line.UnitPrice is not null)
                return Result.Success<Money, Error>(Money.Create(line.UnitPrice.Value, currencyCode));

            var priced = priceList?.PriceFor(item.Id, line.Quantity);

            return priced is null
                ? Result.Failure<Money, Error>(Errors.Order.ItemHasNoPrice(item.Name ?? item.Id.ToString(), priceList?.Code ?? "none"))
                : Result.Success<Money, Error>(priced.NetPrice);
        }

        /// <summary>
        /// What is already outstanding plus what this invoice adds, against the limit. A customer
        /// with no limit set is unlimited, which is a deliberate choice somebody made.
        /// </summary>
        private async Task<Result<bool, Error>> CheckCreditLimitAsync(Customer customer, Money amount, CancellationToken cancellationToken)
        {
            if (customer.CreditLimit is null || customer.DetailAccountId is null)
                return Result.Success<bool, Error>(true);

            var outstanding = await receivables.GetOutstandingTotalAsync(customer.DetailAccountId.Value, cancellationToken);
            var wouldBe = outstanding + amount.Amount;

            return wouldBe > customer.CreditLimit.Amount
                ? Result.Failure<bool, Error>(
                    Errors.Sales.CreditLimitWouldBeExceeded(customer.DisplayName, wouldBe, customer.CreditLimit.Amount))
                : Result.Success<bool, Error>(true);
        }
    }
}
