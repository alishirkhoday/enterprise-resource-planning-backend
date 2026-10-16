using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ConvertLead
{
    /// <summary>
    /// Turns a qualified lead into a customer - تبدیل سرنخ به مشتری.
    /// <para>
    /// This is the seam between marketing and sales, and it is where most systems lose the thread:
    /// a customer gets created by hand, the lead is left open, and nobody can say afterwards which
    /// campaign paid for which customer. Doing both here is what keeps the chain intact.
    /// </para>
    /// </summary>
    public class ConvertLeadCommandHandler(
        ILeadRepository leads,
        ICustomerRepository customers,
        IOpportunityRepository opportunities,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<ConvertLeadCommand, Result<ConvertLeadResultDto, Error>>
    {
        private const string CustomerNumberingKey = "Customer";

        public async Task<Result<ConvertLeadResultDto, Error>> Handle(ConvertLeadCommand request, CancellationToken cancellationToken)
        {
            var lead = await leads.GetWithHistoryAsync(request.LeadId, cancellationToken);
            if (lead is null)
                return Result.Failure<ConvertLeadResultDto, Error>(Errors.General.NotFound(request.LeadId.ToString(), nameof(Lead)));

            // Checked here as well as in the domain so the caller gets a sentence naming the state
            // rather than an exception, and so nothing is created before the refusal.
            if (lead.Status != LeadStatus.Qualified)
                return Result.Failure<ConvertLeadResultDto, Error>(Errors.Marketing.LeadIsNotQualified(lead.Status.ToString()));

            var resolved = await ResolveCustomerAsync(lead, request, cancellationToken);
            if (resolved.IsFailure)
                return Result.Failure<ConvertLeadResultDto, Error>(resolved.Error);

            var customer = resolved.Value;

            Opportunity? opportunity = null;

            if (request.Opportunity is not null)
            {
                var opened = OpenOpportunity(lead, customer, request.Opportunity);
                if (opened.IsFailure)
                    return Result.Failure<ConvertLeadResultDto, Error>(opened.Error);

                opportunity = opened.Value;
                await opportunities.CreateAsync(opportunity, cancellationToken);
            }

            try
            {
                // Last, because it raises LeadConverted and closes the lead. Raising the fact
                // before the customer exists would publish a conversion that might not happen.
                lead.Convert(customer.Id, request.ConvertedOn);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<ConvertLeadResultDto, Error>(Errors.Marketing.LeadCannotBeChanged(exception.Message));
            }

            await leads.UpdateAsync(lead, cancellationToken);

            return Result.Success<ConvertLeadResultDto, Error>(new ConvertLeadResultDto(
                lead.Id,
                customer.Id,
                customer.DisplayName,
                opportunity?.Id));
        }

        /// <summary>
        /// An existing customer when one was named - the lead turned out to be somebody already on
        /// the books - otherwise a new one built from what the lead told us.
        /// </summary>
        private async Task<Result<Customer, Error>> ResolveCustomerAsync(Lead lead, ConvertLeadCommand request, CancellationToken cancellationToken)
        {
            if (request.ExistingCustomerId is not null)
            {
                var existing = await customers.GetByIdAsync(request.ExistingCustomerId.Value, cancellationToken);

                return existing is null
                    ? Result.Failure<Customer, Error>(Errors.General.NotFound(request.ExistingCustomerId.Value.ToString(), nameof(Customer)))
                    : Result.Success<Customer, Error>(existing);
            }

            var series = await numberingSeries.GetForUpdateAsync(CustomerNumberingKey, lead.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<Customer, Error>(Errors.Order.NumberingSeriesMissing(CustomerNumberingKey));

            var type = request.CustomerType == CustomerTypeOption.Individual
                ? CustomerType.Individual
                : CustomerType.Organization;

            // An organisation is filed under its company name; a person under their own. Using the
            // wrong one is how a customer list ends up sorted by first names.
            var name = type == CustomerType.Organization && !string.IsNullOrWhiteSpace(lead.CompanyName)
                ? lead.CompanyName!
                : lead.FullName;

            var customer = new Customer(series.Next(request.ConvertedOn), name, type);

            customer.SetContactInformation(lead.MobilePhoneNumberWithRegionCode, lead.Email, null, null, null);

            if (lead.BranchId is not null)
                customer.AssignToBranch(lead.BranchId.Value);

            if (lead.AssignedToHumanId is not null)
                customer.AssignSalesRep(lead.AssignedToHumanId.Value);

            await customers.CreateAsync(customer, cancellationToken);

            return Result.Success<Customer, Error>(customer);
        }

        private static Result<Opportunity, Error> OpenOpportunity(Lead lead, Customer customer, OpenOpportunityDto dto)
        {
            try
            {
                var opportunity = new Opportunity(
                    dto.Title,
                    Money.Create(dto.EstimatedValue, dto.CurrencyCode.Trim().ToUpperInvariant()),
                    dto.ExpectedCloseDate);

                opportunity.AttachToCustomer(customer);
                opportunity.AttachToLead(lead);

                // The campaign follows the lead onto the deal, which is what lets marketing spend
                // be measured against revenue rather than against lead counts.
                if (lead.CampaignId is not null)
                    opportunity.AttributeToCampaign(lead.CampaignId.Value);

                if (lead.AssignedToHumanId is not null)
                    opportunity.AssignTo(lead.AssignedToHumanId.Value);

                if (lead.BranchId is not null)
                    opportunity.AssignToBranch(lead.BranchId.Value);

                return Result.Success<Opportunity, Error>(opportunity);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<Opportunity, Error>(Errors.Marketing.OpportunityCannotBeChanged(exception.Message));
            }
        }
    }
}
