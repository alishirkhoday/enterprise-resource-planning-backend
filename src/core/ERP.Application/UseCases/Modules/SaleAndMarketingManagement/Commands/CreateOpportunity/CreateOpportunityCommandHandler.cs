using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CreateOpportunity
{
    /// <summary>
    /// Opens a deal - فرصت فروش.
    /// <para>
    /// A deal must belong to a customer or a lead. One that belongs to neither cannot be chased,
    /// cannot be attributed, and still counts toward a forecast - which is how a pipeline ends up
    /// predicting revenue from nobody.
    /// </para>
    /// </summary>
    public class CreateOpportunityCommandHandler(
        IOpportunityRepository opportunities,
        ICustomerRepository customers,
        ILeadRepository leads)
        : IRequestHandler<CreateOpportunityCommand, Result<OpportunityDto, Error>>
    {
        public async Task<Result<OpportunityDto, Error>> Handle(CreateOpportunityCommand request, CancellationToken cancellationToken)
        {
            if (request.CustomerId is null && request.SourceLeadId is null)
                return Result.Failure<OpportunityDto, Error>(Errors.Marketing.OpportunityNeedsACounterparty());

            Customer? customer = null;
            Lead? lead = null;

            if (request.CustomerId is not null)
            {
                customer = await customers.GetByIdAsync(request.CustomerId.Value, cancellationToken);
                if (customer is null)
                    return Result.Failure<OpportunityDto, Error>(Errors.General.NotFound(request.CustomerId.Value.ToString(), nameof(Customer)));
            }

            if (request.SourceLeadId is not null)
            {
                lead = await leads.GetByIdAsync(request.SourceLeadId.Value, cancellationToken);
                if (lead is null)
                    return Result.Failure<OpportunityDto, Error>(Errors.General.NotFound(request.SourceLeadId.Value.ToString(), nameof(Lead)));
            }

            try
            {
                var opportunity = new Opportunity(
                    request.Title,
                    Money.Create(request.EstimatedValue, request.CurrencyCode.Trim().ToUpperInvariant()),
                    request.ExpectedCloseDate);

                if (customer is not null)
                    opportunity.AttachToCustomer(customer);

                if (lead is not null)
                    opportunity.AttachToLead(lead);

                // The campaign falls back to the lead's, so a deal opened off a lead stays
                // attributable to whatever brought that lead in.
                var campaignId = request.CampaignId ?? lead?.CampaignId;
                if (campaignId is not null)
                    opportunity.AttributeToCampaign(campaignId.Value);

                if (request.AssignedToHumanId is not null)
                    opportunity.AssignTo(request.AssignedToHumanId.Value);

                if (request.BranchId is not null)
                    opportunity.AssignToBranch(request.BranchId.Value);

                opportunity.SetCompetitor(request.CompetitorName);
                opportunity.SetDescription(request.Description);

                await opportunities.CreateAsync(opportunity, cancellationToken);

                return Result.Success<OpportunityDto, Error>(opportunity.ToDto());
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<OpportunityDto, Error>(Errors.Marketing.OpportunityCannotBeChanged(exception.Message));
            }
        }
    }
}
