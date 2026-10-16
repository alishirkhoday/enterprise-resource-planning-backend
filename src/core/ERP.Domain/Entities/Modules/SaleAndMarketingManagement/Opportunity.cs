using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// A deal being worked on, with a value and a probability that together give the weighted
/// pipeline. It belongs either to an existing customer or to the lead it came from.
/// </summary>
public sealed class Opportunity : BaseEntity, IBranchScoped
{
    public string Title { get; private set; } = default!;
    public OpportunityStage Stage { get; private set; }

    public Customer? Customer { get; private set; }
    public Guid? CustomerId { get; private set; }

    public Lead? SourceLead { get; private set; }
    public Guid? SourceLeadId { get; private set; }

    public Money EstimatedValue { get; private set; } = default!;

    /// <summary>The chance of winning, which the stage normally sets.</summary>
    public Percentage Probability { get; private set; } = default!;

    /// <summary>Estimated value weighted by probability, which is what a pipeline forecast sums.</summary>
    public Money WeightedValue => EstimatedValue.Percent(Probability);

    public DateOnly ExpectedCloseDate { get; private set; }
    public DateOnly? ActualCloseDate { get; private set; }

    public Guid? CampaignId { get; private set; }
    public Guid? AssignedToHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The quotation sent for this deal, when one has been.</summary>
    public Guid? QuotationId { get; private set; }

    /// <summary>The order the deal turned into, once it was won.</summary>
    public Guid? SaleOrderId { get; private set; }

    public string? LostReason { get; private set; }
    public string? CompetitorName { get; private set; }
    public string? Description { get; private set; }

    private Opportunity() { }

    public Opportunity(string title, Money estimatedValue, DateOnly expectedCloseDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(estimatedValue, nameof(estimatedValue));
        if (estimatedValue.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(estimatedValue), "An opportunity cannot have a negative value.");

        Title = title.Trim();
        EstimatedValue = estimatedValue;
        ExpectedCloseDate = expectedCloseDate;
        Stage = OpportunityStage.Prospecting;
        Probability = ProbabilityFor(OpportunityStage.Prospecting);
    }

    public void AttachToCustomer(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        Customer = customer;
        CustomerId = customer.Id;
    }

    public void AttachToLead(Lead lead)
    {
        ArgumentNullException.ThrowIfNull(lead, nameof(lead));
        SourceLead = lead;
        SourceLeadId = lead.Id;
    }

    public void Update(string title, Money estimatedValue, DateOnly expectedCloseDate)
    {
        EnsureOpen();
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(estimatedValue, nameof(estimatedValue));
        Title = title.Trim();
        EstimatedValue = estimatedValue;
        ExpectedCloseDate = expectedCloseDate;
    }

    /// <summary>Moves the deal along, which resets the probability unless one has been set by hand.</summary>
    public void MoveToStage(OpportunityStage stage)
    {
        EnsureOpen();
        if (stage is OpportunityStage.ClosedWon or OpportunityStage.ClosedLost)
            throw new InvalidOperationException("Use Win or Lose to close an opportunity.");
        Stage = stage;
        Probability = ProbabilityFor(stage);
    }

    public void SetProbability(Percentage probability)
    {
        EnsureOpen();
        ArgumentNullException.ThrowIfNull(probability, nameof(probability));
        Probability = probability;
    }

    public void AttributeToCampaign(Guid campaignId) => CampaignId = campaignId == Guid.Empty ? null : campaignId;

    public void AssignTo(Guid humanId) => AssignedToHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void LinkToQuotation(Guid quotationId) => QuotationId = quotationId == Guid.Empty ? null : quotationId;

    public void SetCompetitor(string? competitorName) =>
        CompetitorName = string.IsNullOrWhiteSpace(competitorName) ? null : competitorName.Trim();

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Win(DateOnly closedOn, Guid? saleOrderId = null)
    {
        EnsureOpen();
        Stage = OpportunityStage.ClosedWon;
        Probability = Percentage.Create(100m);
        ActualCloseDate = closedOn;
        SaleOrderId = saleOrderId == Guid.Empty ? null : saleOrderId;
        Raise(new OpportunityWon(Id, CustomerId, EstimatedValue.Amount, EstimatedValue.CurrencyCode));
    }

    public void Lose(DateOnly closedOn, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        EnsureOpen();
        Stage = OpportunityStage.ClosedLost;
        Probability = Percentage.Zero();
        ActualCloseDate = closedOn;
        LostReason = reason.Trim();
    }

    public bool IsOpen => Stage is not (OpportunityStage.ClosedWon or OpportunityStage.ClosedLost);

    public bool IsOverdueOn(DateOnly date) => IsOpen && date > ExpectedCloseDate;

    private static Percentage ProbabilityFor(OpportunityStage stage) => stage switch
    {
        OpportunityStage.Prospecting => Percentage.Create(10m),
        OpportunityStage.Qualification => Percentage.Create(25m),
        OpportunityStage.NeedsAnalysis => Percentage.Create(40m),
        OpportunityStage.Proposal => Percentage.Create(60m),
        OpportunityStage.Negotiation => Percentage.Create(80m),
        OpportunityStage.ClosedWon => Percentage.Create(100m),
        _ => Percentage.Zero()
    };

    private void EnsureOpen()
    {
        if (!IsOpen)
            throw new InvalidOperationException($"The opportunity is {Stage} and can no longer change.");
    }
}
