using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RegisterLead
{
    /// <summary>
    /// Records a new lead - سرنخ.
    /// <para>
    /// The duplicate check is the point. The same person filling in a web form twice, or being
    /// entered by two salespeople from the same trade show, is the commonest way a sales database
    /// rots: two rows, two people calling, and neither knowing the other did.
    /// </para>
    /// </summary>
    public class RegisterLeadCommandHandler(
        ILeadRepository leads,
        ICampaignRepository campaigns)
        : IRequestHandler<RegisterLeadCommand, Result<LeadDto, Error>>
    {
        public async Task<Result<LeadDto, Error>> Handle(RegisterLeadCommand request, CancellationToken cancellationToken)
        {
            var mobileNumber = request.MobilePhoneNumberWithRegionCode.Trim();

            var existing = await leads.GetByMobileNumberAsync(mobileNumber, cancellationToken);

            // A lead that was disqualified or converted is history, not a duplicate: the same
            // person coming back a year later is a new opportunity, not a second entry of an
            // open one.
            if (existing is not null && existing.Status is LeadStatus.New or LeadStatus.Contacted or LeadStatus.Qualified)
                return Result.Failure<LeadDto, Error>(Errors.Marketing.LeadAlreadyExists(mobileNumber, existing.FullName));

            var attributed = await ResolveCampaignAsync(request, cancellationToken);
            if (attributed.IsFailure)
                return Result.Failure<LeadDto, Error>(attributed.Error);

            var lead = new Lead(request.FullName, mobileNumber, request.Source);

            lead.Update(request.FullName, request.CompanyName, request.JobTitle);
            lead.SetEmail(request.Email);

            if (attributed.Value is not null)
                lead.AttributeToCampaign(attributed.Value.Id);

            if (request.AssignedToHumanId is not null)
                lead.AssignTo(request.AssignedToHumanId.Value);

            if (request.BranchId is not null)
                lead.AssignToBranch(request.BranchId.Value);

            if (!string.IsNullOrWhiteSpace(request.Note))
                lead.AddNote(new LeadNote(lead, request.Note));

            await leads.CreateAsync(lead, cancellationToken);

            return Result.Success<LeadDto, Error>(lead.ToDto());
        }

        /// <summary>
        /// A campaign that is paused or finished stops taking attribution. Letting a lead in
        /// afterwards would quietly flatter a campaign that had already stopped spending.
        /// </summary>
        private async Task<Result<Campaign?, Error>> ResolveCampaignAsync(RegisterLeadCommand request, CancellationToken cancellationToken)
        {
            if (request.CampaignId is null)
                return Result.Success<Campaign?, Error>(null);

            var campaign = await campaigns.GetByIdAsync(request.CampaignId.Value, cancellationToken);
            if (campaign is null)
                return Result.Failure<Campaign?, Error>(Errors.General.NotFound(request.CampaignId.Value.ToString(), nameof(Campaign)));

            if (campaign.Status != CampaignStatus.Running)
                return Result.Failure<Campaign?, Error>(
                    Errors.Marketing.CampaignIsNotRunning(campaign.Code, campaign.Status.ToString()));

            return Result.Success<Campaign?, Error>(campaign);
        }
    }
}
