using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// One item at one moment in the horizon, with the arithmetic that produced the suggestion spelled
/// out rather than hidden in the planner: what is needed, what is already coming, what that leaves
/// on hand, and therefore what is short.
/// <para>
/// The intermediate figures are stored, not recomputed. A buyer who is about to commit money asks
/// why, and a line that can only answer "the planner said so" does not survive that question.
/// </para>
/// </summary>
public sealed class MaterialRequirementPlanLine : BaseEntity
{
    public MaterialRequirementPlan Plan { get; private set; } = default!;
    public Guid PlanId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Guid? WarehouseId { get; private set; }
    public DateOnly RequiredOn { get; private set; }

    /// <summary>Everything that wants this item on that date: orders, forecast and parent demand.</summary>
    public decimal GrossRequirement { get; private set; }

    /// <summary>Supply already committed to arrive by then, from purchase orders and production.</summary>
    public decimal ScheduledReceipts { get; private set; }

    /// <summary>What is on hand before this date's movements.</summary>
    public decimal OpeningQuantity { get; private set; }

    /// <summary>Stock held back rather than planned away.</summary>
    public decimal SafetyQuantity { get; private set; }

    public decimal ProjectedOnHand => OpeningQuantity + ScheduledReceipts - GrossRequirement;

    /// <summary>How far below safety stock the projection falls; zero when there is enough.</summary>
    public decimal NetRequirement => Math.Max(0m, SafetyQuantity - ProjectedOnHand);

    /// <summary>What to order, which is the shortfall rounded up to how the item is actually bought.</summary>
    public decimal PlannedOrderQuantity { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }
    public PlannedOrderAction SuggestedAction { get; private set; }

    /// <summary>
    /// When the order has to be placed for the goods to exist on <see cref="RequiredOn"/>: the
    /// required date less the lead time. A plan that only says what is needed and not when to act
    /// is already late.
    /// </summary>
    public DateOnly SuggestedReleaseDate { get; private set; }

    public int LeadTimeDays { get; private set; }
    public Guid? SupplierId { get; private set; }

    /// <summary>
    /// The demand this line exists to serve, for a component exploded out of a parent. It is what
    /// lets a shortage be traced up to the sales order that caused it.
    /// </summary>
    public Guid? ParentLineId { get; private set; }
    public short BillOfMaterialsLevel { get; private set; }

    /// <summary>What the suggestion became once somebody acted on it.</summary>
    public string? ReleasedReferenceType { get; private set; }
    public string? ReleasedReferenceId { get; private set; }
    public DateTimeOffset? ReleasedOn { get; private set; }

    public string? Description { get; private set; }

    private MaterialRequirementPlanLine() { }

    public MaterialRequirementPlanLine(
        MaterialRequirementPlan plan,
        Item item,
        DateOnly requiredOn,
        decimal grossRequirement,
        decimal scheduledReceipts,
        decimal openingQuantity,
        Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(plan, nameof(plan));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentOutOfRangeException.ThrowIfNegative(grossRequirement, nameof(grossRequirement));
        ArgumentOutOfRangeException.ThrowIfNegative(scheduledReceipts, nameof(scheduledReceipts));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Plan = plan;
        PlanId = plan.Id;
        Item = item;
        ItemId = item.Id;
        WarehouseId = plan.WarehouseId;
        RequiredOn = requiredOn;
        GrossRequirement = grossRequirement;
        ScheduledReceipts = scheduledReceipts;
        OpeningQuantity = openingQuantity;
        UnitOfMeasureId = unitOfMeasureId;
        SuggestedReleaseDate = requiredOn;
        SuggestedAction = PlannedOrderAction.None;
    }

    internal void SetSafetyQuantity(decimal safetyQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(safetyQuantity, nameof(safetyQuantity));
        SafetyQuantity = safetyQuantity;
    }

    internal void TraceToParent(Guid parentLineId, short billOfMaterialsLevel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(billOfMaterialsLevel, nameof(billOfMaterialsLevel));
        ParentLineId = parentLineId == Guid.Empty ? null : parentLineId;
        BillOfMaterialsLevel = billOfMaterialsLevel;
    }

    /// <summary>
    /// Records the proposal. The release date is derived here rather than supplied, so it can never
    /// disagree with the lead time that was used.
    /// </summary>
    internal void Suggest(PlannedOrderAction action, decimal plannedOrderQuantity, int leadTimeDays, Guid? supplierId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(plannedOrderQuantity, nameof(plannedOrderQuantity));
        ArgumentOutOfRangeException.ThrowIfNegative(leadTimeDays, nameof(leadTimeDays));
        if (action != PlannedOrderAction.None && plannedOrderQuantity == 0m)
            throw new ArgumentException("An action that orders nothing is not an action.", nameof(plannedOrderQuantity));

        var releaseDate = RequiredOn.AddDays(-leadTimeDays);
        SuggestedAction = action;
        PlannedOrderQuantity = plannedOrderQuantity;
        LeadTimeDays = leadTimeDays;
        SupplierId = supplierId == Guid.Empty ? null : supplierId;
        SuggestedReleaseDate = releaseDate;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Records what the suggestion turned into, so it is not acted on twice.</summary>
    internal void MarkAsReleased(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        if (SuggestedAction == PlannedOrderAction.None)
            throw new InvalidOperationException("This line suggests nothing, so there is nothing to release.");
        if (IsReleased)
            throw new InvalidOperationException($"This line was already released as {ReleasedReferenceType} {ReleasedReferenceId}.");

        ReleasedReferenceType = referenceType.Trim();
        ReleasedReferenceId = referenceId.Trim();
        ReleasedOn = DateTimeOffset.UtcNow;
    }

    public bool IsReleased => ReleasedOn.HasValue;

    public bool NeedsAction => SuggestedAction != PlannedOrderAction.None;

    /// <summary>
    /// True when the order would have had to be placed before today to arrive on time. The
    /// shortfall is already unavoidable and the date has to be renegotiated, not planned around.
    /// </summary>
    public bool IsPastDue(DateOnly asOf) => NeedsAction && !IsReleased && SuggestedReleaseDate < asOf;
}
