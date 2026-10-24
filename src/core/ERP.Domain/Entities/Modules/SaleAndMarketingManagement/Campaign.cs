namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// A marketing push with a budget, whose worth is judged by the leads and revenue attributed to it.
/// </summary>
public sealed class Campaign : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Channel { get; private set; } = default!;
    public CampaignStatus Status { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public DateOnly StartDate => Period.StartDate;
    public DateOnly? EndDate => Period.EndDate;

    public Money Budget { get; private set; } = default!;
    public Money ActualCost { get; private set; } = default!;

    /// <summary>Revenue attributed to the campaign, used to judge its return.</summary>
    public Money AttributedRevenue { get; private set; } = default!;

    public int? TargetLeadCount { get; private set; }
    public Guid? OwnerHumanId { get; private set; }
    public Guid? CustomerGroupId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? Description { get; private set; }

    private readonly List<Lead> _leads = [];
    public IReadOnlyList<Lead> Leads => _leads.AsReadOnly();

    private Campaign() { }

    public Campaign(string code, string name, string channel, DateRange period, Money budget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(channel, nameof(channel));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentNullException.ThrowIfNull(budget, nameof(budget));
        if (budget.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(budget), "A budget cannot be negative.");

        Code = code.Trim();
        Name = name.Trim();
        Channel = channel.Trim();
        Period = period;
        Budget = budget;
        ActualCost = Money.Zero(budget.CurrencyCode);
        AttributedRevenue = Money.Zero(budget.CurrencyCode);
        Status = CampaignStatus.Planned;
    }

    public void Update(string name, string channel, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(channel, nameof(channel));
        Name = name.Trim();
        Channel = channel.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetTargets(int? targetLeadCount, Guid? customerGroupId)
    {
        if (targetLeadCount.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetLeadCount.Value, nameof(targetLeadCount));
        TargetLeadCount = targetLeadCount;
        CustomerGroupId = customerGroupId == Guid.Empty ? null : customerGroupId;
    }

    public void AssignOwner(Guid humanId) => OwnerHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void Launch()
    {
        if (Status != CampaignStatus.Planned)
            throw new InvalidOperationException($"Only a planned campaign can be launched; this one is {Status}.");
        Status = CampaignStatus.Running;
    }

    public void Pause()
    {
        if (Status != CampaignStatus.Running)
            throw new InvalidOperationException("Only a running campaign can be paused.");
        Status = CampaignStatus.Paused;
    }

    public void Resume()
    {
        if (Status != CampaignStatus.Paused)
            throw new InvalidOperationException("Only a paused campaign can be resumed.");
        Status = CampaignStatus.Running;
    }

    public void Complete() => Status = CampaignStatus.Completed;

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == CampaignStatus.Completed)
            throw new InvalidOperationException("A completed campaign cannot be cancelled.");
        Status = CampaignStatus.Cancelled;
        Description = reason.Trim();
    }

    public void RecordCost(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ActualCost = ActualCost.Add(amount);
    }

    public void RecordRevenue(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        AttributedRevenue = AttributedRevenue.Add(amount);
    }

    public void AttributeLead(Lead lead)
    {
        ArgumentNullException.ThrowIfNull(lead, nameof(lead));
        if (_leads.Any(l => l.Id == lead.Id))
            return;
        lead.AttributeToCampaign(Id);
        _leads.Add(lead);
    }

    public Money Profit => AttributedRevenue.Subtract(ActualCost);

    /// <summary>Return on investment as a percentage of what was spent.</summary>
    public decimal? ReturnOnInvestment =>
        ActualCost.IsZero ? null : Profit.Amount / ActualCost.Amount * 100m;

    public Money CostPerLead => _leads.Count == 0 ? Money.Zero(ActualCost.CurrencyCode) : ActualCost.Divide(_leads.Count);

    public int ConvertedLeadCount => _leads.Count(l => l.Status == LeadStatus.Converted);

    public decimal? ConversionRate => _leads.Count == 0 ? null : (decimal)ConvertedLeadCount / _leads.Count * 100m;

    public bool IsOverBudget => ActualCost.Amount > Budget.Amount;
}
