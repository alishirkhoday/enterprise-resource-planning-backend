using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.IssueQuotation;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ReviseQuotation
{
    /// <summary>
    /// Opens a new version of an offer after the customer pushed back - بازنگری پیش‌فاکتور.
    /// <para>
    /// The version number moves and the document goes back out. This is the normal path after a
    /// rejection: a customer saying no to a price is rarely saying no to the deal, and forcing a
    /// brand new quotation would lose the link to the opportunity and restart the numbering.
    /// </para>
    /// </summary>
    public class ReviseQuotationCommandHandler(
        IQuotationRepository quotations,
        ICustomerRepository customers,
        IItemRepository items,
        IPriceListRepository priceLists,
        TimeProvider timeProvider)
        : IRequestHandler<ReviseQuotationCommand, Result<QuotationDto, Error>>
    {
        public async Task<Result<QuotationDto, Error>> Handle(ReviseQuotationCommand request, CancellationToken cancellationToken)
        {
            var quotation = await quotations.GetWithItemsAsync(request.QuotationId, cancellationToken);
            if (quotation is null)
                return Result.Failure<QuotationDto, Error>(Errors.General.NotFound(request.QuotationId.ToString(), nameof(Quotation)));

            var now = timeProvider.GetUtcNow();

            try
            {
                // Back to draft, so the lines and terms below are editable again.
                quotation.Revise();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<QuotationDto, Error>(Errors.Marketing.QuotationCannotBeChanged(exception.Message));
            }

            if (request.Lines.Count > 0)
            {
                var replaced = await ReplaceLinesAsync(quotation, request, now, cancellationToken);
                if (replaced.IsFailure)
                    return Result.Failure<QuotationDto, Error>(replaced.Error);
            }

            // The clock restarts from today. A revision that inherited the original expiry would
            // sometimes go out already lapsed.
            quotation.SetExpiryDate(now.AddDays(request.ValidForDays ?? 30));

            if (request.HeaderDiscountPercentage is not null)
                quotation.SetHeaderDiscount(Percentage.Create(request.HeaderDiscountPercentage.Value));

            if (!string.IsNullOrWhiteSpace(request.Reason))
                quotation.SetDescription(request.Reason);

            try
            {
                quotation.Send();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<QuotationDto, Error>(Errors.Marketing.QuotationCannotBeChanged(exception.Message));
            }

            await quotations.UpdateAsync(quotation, cancellationToken);

            return Result.Success<QuotationDto, Error>(quotation.ToDto());
        }

        /// <summary>
        /// Every line is built before any of the old ones is dropped, so a revision that cannot be
        /// priced leaves the quotation as it was rather than empty.
        /// </summary>
        private async Task<Result<bool, Error>> ReplaceLinesAsync(
            Quotation quotation,
            ReviseQuotationCommand request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var customer = await customers.GetByIdAsync(quotation.CustomerId, cancellationToken);

            var priceList = await priceLists.GetEffectiveForSaleAsync(
                quotation.PriceListId ?? customer?.PriceListId,
                customer?.CustomerGroupId,
                quotation.CurrencyCode,
                DateOnly.FromDateTime(now.UtcDateTime),
                cancellationToken);

            var built = await IssueQuotationCommandHandler.BuildLinesInternalAsync(
                items, quotation, request.Lines, priceList, cancellationToken);

            if (built.IsFailure)
                return Result.Failure<bool, Error>(built.Error);

            foreach (var existing in quotation.Items.ToArray())
            {
                quotation.RemoveItem(existing);
            }

            foreach (var line in built.Value)
            {
                quotation.AddItem(line);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
