using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping
{
    public static class LeadMapper
    {
        public static LeadDto ToDto(this Lead lead)
        {
            ArgumentNullException.ThrowIfNull(lead, nameof(lead));

            return new LeadDto(
                lead.Id,
                lead.FullName,
                lead.CompanyName,
                lead.JobTitle,
                lead.MobilePhoneNumberWithRegionCode,
                lead.Email,
                lead.Source,
                lead.Status,
                lead.Score,
                lead.CampaignId,
                lead.AssignedToHumanId,
                lead.ConvertedCustomerId,
                lead.ConvertedOn,
                lead.DisqualificationReason,
                [.. lead.Notes
                    .OrderBy(n => n.CreationDateTime)
                    .Select(n => new LeadNoteDto(n.Id, n.Note, n.CreationDateTime))],
                [.. lead.Interactions
                    .OrderBy(i => i.CreationDateTime)
                    .Select(i => new LeadInteractionDto(i.Id, i.Subject, i.Type, i.Note, i.CreationDateTime))]);
        }
    }
}
