using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.IssueQuotation
{
    /// <summary>
    /// Issues a quotation and sends it - صدور پیش‌فاکتور.
    /// <para>
    /// Prices fall back to the list in effect for the customer, exactly as an invoice does. A
    /// quotation is the number the customer will hold you to, so it has to come from the same
    /// place the invoice eventually will, or the two disagree in front of the customer.
    /// </para>
    /// <para>
    /// The expiry is not optional in practice. Without one a quotation never lapses and a price
    /// quoted a year ago is still, on paper, a price you offered.
    /// </para>
    /// </summary>
    public class IssueQuotationCommandHandler(
        ICustomerRepository customers,
        IItemRepository items,
        IPriceListRepository priceLists,
        IQuotationRepository quotations,
        IOpportunityRepository opportunities,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<IssueQuotationCommand, Result<QuotationDto, Error>>
    {
        private const string QuotationNumberingKey = "Quotation";

        public async Task<Result<QuotationDto, Error>> Handle(IssueQuotationCommand request, CancellationToken cancellationToken)
        {
            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
            var now = timeProvider.GetUtcNow();
            var today = DateOnly.FromDateTime(now.UtcDateTime);

            var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return Result.Failure<QuotationDto, Error>(Errors.General.NotFound(request.CustomerId.ToString(), nameof(Customer)));

            if (!customer.CanBeSoldTo)
                return Result.Failure<QuotationDto, Error>(Errors.Order.CustomerCannotBeSoldTo(customer.DisplayName));

            var priceList = await priceLists.GetEffectiveForSaleAsync(
                request.PriceListId ?? customer.PriceListId,
                customer.CustomerGroupId,
                currencyCode,
                today,
                cancellationToken);

            var series = await numberingSeries.GetForUpdateAsync(QuotationNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<QuotationDto, Error>(Errors.Order.NumberingSeriesMissing(QuotationNumberingKey));

            var quotation = new Quotation(series.Next(today), customer, now, currencyCode);

            quotation.SetExpiryDate(now.AddDays(request.ValidForDays));
            quotation.SetCommercialTerms(request.PaymentTermId, priceList?.Id, request.Terms);
            quotation.SetDescription(request.Description);

            if (request.OpportunityId is not null)
                quotation.LinkToOpportunity(request.OpportunityId.Value);

            if (request.SalesRepHumanId is not null)
                quotation.AssignSalesRep(request.SalesRepHumanId.Value);

            if (request.BranchId is not null)
                quotation.AssignToBranch(request.BranchId.Value);

            var built = await BuildLinesAsync(quotation, request.Lines, priceList, cancellationToken);
            if (built.IsFailure)
                return Result.Failure<QuotationDto, Error>(built.Error);

            if (request.AdditionalCharges > 0m)
                quotation.SetAdditionalCharges(Money.Create(request.AdditionalCharges, currencyCode));

            if (request.HeaderDiscountPercentage > 0m)
                quotation.SetHeaderDiscount(Percentage.Create(request.HeaderDiscountPercentage));

            try
            {
                quotation.Send();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<QuotationDto, Error>(Errors.Marketing.QuotationCannotBeChanged(exception.Message));
            }

            await quotations.CreateAsync(quotation, cancellationToken);

            // The deal now has a proposal against it, which is both a link and a stage: a deal
            // with a quotation out is past needs analysis whatever anybody last clicked.
            await LinkOpportunityAsync(quotation, request.OpportunityId, cancellationToken);

            return Result.Success<QuotationDto, Error>(quotation.ToDto());
        }

        internal static async Task<Result<List<QuotationItem>, Error>> BuildLinesInternalAsync(
            IItemRepository items,
            Quotation quotation,
            IReadOnlyList<QuotationLineDto> lines,
            PriceList? priceList,
            CancellationToken cancellationToken)
        {
            var built = new List<QuotationItem>(lines.Count);
            var found = await items.GetByIdsAsync([.. lines.Select(l => l.ItemId)], cancellationToken);
            var byId = found.ToDictionary(i => i.Id);

            short lineNumber = 1;

            foreach (var line in lines)
            {
                if (!byId.TryGetValue(line.ItemId, out var item))
                    return Result.Failure<List<QuotationItem>, Error>(Errors.General.NotFound(line.ItemId.ToString(), nameof(Item)));

                if (!item.IsSellable)
                    return Result.Failure<List<QuotationItem>, Error>(Errors.Order.ItemIsNotSellable(item.Name ?? line.ItemId.ToString()));

                var price = ResolvePrice(line, item, priceList, quotation.CurrencyCode);
                if (price.IsFailure)
                    return Result.Failure<List<QuotationItem>, Error>(price.Error);

                var quotationItem = new QuotationItem(quotation, item, lineNumber++, line.Quantity, line.UnitOfMeasureId, price.Value);
                quotation.AddItem(quotationItem);
                built.Add(quotationItem);

                if (line.DiscountPercentage is not null)
                    quotation.SetQuotationItemDiscount(quotationItem.Id, Percentage.Create(line.DiscountPercentage.Value));

                if (line.TaxId is not null && line.TaxPercentage is not null)
                    quotation.SetQuotationItemTax(quotationItem.Id, line.TaxId.Value, Percentage.Create(line.TaxPercentage.Value));

                if (line.DeliveryLeadTimeDays is not null)
                    quotation.SetQuotationItemDeliveryLeadTime(quotationItem.Id, line.DeliveryLeadTimeDays.Value);

                quotation.SetQuotationItemDescription(quotationItem.Id, line.Description);
            }

            return Result.Success<List<QuotationItem>, Error>(built);
        }

        private Task<Result<List<QuotationItem>, Error>> BuildLinesAsync(
            Quotation quotation,
            IReadOnlyList<QuotationLineDto> lines,
            PriceList? priceList,
            CancellationToken cancellationToken) =>
            BuildLinesInternalAsync(items, quotation, lines, priceList, cancellationToken);

        private static Result<Money, Error> ResolvePrice(QuotationLineDto line, Item item, PriceList? priceList, string currencyCode)
        {
            if (line.UnitPrice is not null)
                return Result.Success<Money, Error>(Money.Create(line.UnitPrice.Value, currencyCode));

            var priced = priceList?.PriceFor(item.Id, line.Quantity);

            return priced is null
                ? Result.Failure<Money, Error>(Errors.Order.ItemHasNoPrice(item.Name ?? item.Id.ToString(), priceList?.Code ?? "none"))
                : Result.Success<Money, Error>(priced.NetPrice);
        }

        private async Task LinkOpportunityAsync(Quotation quotation, Guid? opportunityId, CancellationToken cancellationToken)
        {
            if (opportunityId is null)
                return;

            var opportunity = await opportunities.GetByIdAsync(opportunityId.Value, cancellationToken);
            if (opportunity is null || !opportunity.IsOpen)
                return;

            opportunity.LinkToQuotation(quotation.Id);

            if (opportunity.Stage < OpportunityStage.Proposal)
                opportunity.MoveToStage(OpportunityStage.Proposal);

            await opportunities.UpdateAsync(opportunity, cancellationToken);
        }
    }
}
