using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs
{
    /// <summary>A new lead - سرنخ. The top of the funnel, before anybody is a customer.</summary>
    public record RegisterLeadDto
    {
        public string FullName { get; init; } = default!;

        public string MobilePhoneNumberWithRegionCode { get; init; } = default!;

        public LeadSource Source { get; init; }

        public string? CompanyName { get; init; }

        public string? JobTitle { get; init; }

        public string? Email { get; init; }

        /// <summary>
        /// Which campaign brought them in. It is what makes marketing spend answerable later,
        /// so it is worth capturing at the moment the lead arrives rather than reconstructing.
        /// </summary>
        public Guid? CampaignId { get; init; }

        public Guid? AssignedToHumanId { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>A first note, such as what they asked for.</summary>
        public string? Note { get; init; }
    }

    /// <summary>
    /// The decision about whether a lead is worth pursuing. Both answers close the lead for
    /// scoring purposes; only one of them opens the door to a sale.
    /// </summary>
    public record QualifyLeadDto
    {
        public Guid LeadId { get; init; }

        public bool IsQualified { get; init; }

        /// <summary>Required when disqualifying. It is what stops the same lead being bought again.</summary>
        public string? Reason { get; init; }

        /// <summary>Optional score out of a hundred, for ordering a worklist.</summary>
        public int? Score { get; init; }
    }

    /// <summary>
    /// Turns a qualified lead into a customer - تبدیل سرنخ به مشتری - and optionally opens the
    /// first deal against them in the same step.
    /// </summary>
    public record ConvertLeadDto
    {
        public Guid LeadId { get; init; }

        public DateOnly ConvertedOn { get; init; }

        /// <summary>
        /// An existing customer to attach the lead to. Left empty a new one is created from the
        /// lead's own details, which is the usual case.
        /// </summary>
        public Guid? ExistingCustomerId { get; init; }

        public CustomerTypeOption CustomerType { get; init; } = CustomerTypeOption.Organization;

        /// <summary>Opens a deal against the new customer as part of the conversion.</summary>
        public OpenOpportunityDto? Opportunity { get; init; }
    }

    /// <summary>
    /// Mirrors the domain's customer type without the application layer having to name the
    /// finance module's enum in a request body.
    /// </summary>
    public enum CustomerTypeOption
    {
        Individual = 0,
        Organization = 1
    }

    /// <summary>A deal opened as part of converting a lead.</summary>
    public record OpenOpportunityDto
    {
        public string Title { get; init; } = default!;

        public decimal EstimatedValue { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public DateOnly ExpectedCloseDate { get; init; }
    }

    public sealed record LeadDto(
        Guid Id,
        string FullName,
        string? CompanyName,
        string? JobTitle,
        string MobilePhoneNumberWithRegionCode,
        string? Email,
        LeadSource Source,
        LeadStatus Status,
        int? Score,
        Guid? CampaignId,
        Guid? AssignedToHumanId,
        Guid? ConvertedCustomerId,
        DateOnly? ConvertedOn,
        string? DisqualificationReason,
        IReadOnlyList<LeadNoteDto> Notes,
        IReadOnlyList<LeadInteractionDto> Interactions);

    public sealed record LeadNoteDto(Guid Id, string Note, DateTimeOffset WrittenAt);

    public sealed record LeadInteractionDto(
        Guid Id,
        string Subject,
        LeadInteractionType Type,
        string Note,
        DateTimeOffset HappenedAt);

    /// <summary>What a conversion produced: the customer, and the deal if one was opened.</summary>
    public sealed record ConvertLeadResultDto(
        Guid LeadId,
        Guid CustomerId,
        string CustomerName,
        Guid? OpportunityId);
}
