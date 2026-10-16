using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.DecideQuotation
{
    /// <summary>
    /// Records the customer's answer to a quotation - پذیرش یا رد پیش‌فاکتور.
    /// <para>
    /// An expired quotation is refused rather than quietly accepted. The expiry is a promise about
    /// how long the price holds; honouring an acceptance past it means the expiry never meant
    /// anything, and the next argument about a stale price has no answer.
    /// </para>
    /// </summary>
    public class DecideQuotationCommandHandler(
        IQuotationRepository quotations,
        IOpportunityRepository opportunities,
        TimeProvider timeProvider)
        : IRequestHandler<DecideQuotationCommand, Result<QuotationDto, Error>>
    {
        public async Task<Result<QuotationDto, Error>> Handle(DecideQuotationCommand request, CancellationToken cancellationToken)
        {
            var quotation = await quotations.GetWithItemsAsync(request.QuotationId, cancellationToken);
            if (quotation is null)
                return Result.Failure<QuotationDto, Error>(Errors.General.NotFound(request.QuotationId.ToString(), nameof(Quotation)));

            var now = timeProvider.GetUtcNow();

            // Only an acceptance is blocked by expiry. A customer saying no to a lapsed quotation
            // is still useful information, and refusing to record it loses the reason.
            if (request.IsAccepted && quotation.ExpiryDate is not null && quotation.ExpiryDate < now)
                return Result.Failure<QuotationDto, Error>(Errors.Marketing.QuotationHasExpired(quotation.ExpiryDate.Value));

            try
            {
                if (request.IsAccepted)
                    quotation.Accept(request.SaleOrderId);
                else
                    quotation.Reject(request.RejectionReason!);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<QuotationDto, Error>(Errors.Marketing.QuotationCannotBeChanged(exception.Message));
            }

            await quotations.UpdateAsync(quotation, cancellationToken);
            await SettleOpportunityAsync(quotation, request, now, cancellationToken);

            return Result.Success<QuotationDto, Error>(quotation.ToDto());
        }

        /// <summary>
        /// An accepted quotation wins the deal; a rejected one only moves it back to negotiation.
        /// A rejection is rarely the end of a deal - it is usually the start of a second version -
        /// so losing the opportunity here would close something the salesperson is still working.
        /// </summary>
        private async Task SettleOpportunityAsync(
            Quotation quotation,
            DecideQuotationCommand request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (quotation.OpportunityId is null || !request.CloseOpportunity)
                return;

            var opportunity = await opportunities.GetByIdAsync(quotation.OpportunityId.Value, cancellationToken);
            if (opportunity is null || !opportunity.IsOpen)
                return;

            if (request.IsAccepted)
                opportunity.Win(DateOnly.FromDateTime(now.UtcDateTime), request.SaleOrderId);
            else
                opportunity.MoveToStage(OpportunityStage.Negotiation);

            await opportunities.UpdateAsync(opportunity, cancellationToken);
        }
    }
}
