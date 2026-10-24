namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One item at an agreed price under a <see cref="PurchaseContract"/>, with the quantity committed
/// and the quantity actually taken.
/// </summary>
public sealed class PurchaseContractItem : BaseEntity
{
    public PurchaseContract Contract { get; private set; } = default!;
    public Guid ContractId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Money AgreedUnitPrice { get; private set; } = default!;
    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>The quantity promised over the term, when the agreement is a volume one.</summary>
    public decimal? CommittedQuantity { get; private set; }

    public decimal ConsumedQuantity { get; private set; }

    /// <summary>The smallest order the supplier will accept at this price.</summary>
    public decimal MinimumOrderQuantity { get; private set; }

    /// <summary>Days from order to delivery, which planning turns into a release date.</summary>
    public int? LeadTimeDays { get; private set; }

    public string? Description { get; private set; }

    private PurchaseContractItem() { }

    public PurchaseContractItem(PurchaseContract contract, Item item, Money agreedUnitPrice, Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(contract, nameof(contract));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(agreedUnitPrice, nameof(agreedUnitPrice));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        if (agreedUnitPrice.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(agreedUnitPrice), "An agreed price cannot be negative.");
        if (!item.IsPurchasable)
            throw new InvalidOperationException($"Item {item.Name} is not marked as purchasable.");

        Contract = contract;
        ContractId = contract.Id;
        Item = item;
        ItemId = item.Id;
        AgreedUnitPrice = agreedUnitPrice;
        UnitOfMeasureId = unitOfMeasureId;
    }

    internal void SetCommittedQuantity(decimal? committedQuantity)
    {
        if (committedQuantity.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(committedQuantity.Value, nameof(committedQuantity));
        CommittedQuantity = committedQuantity;
    }

    internal void SetOrderingRules(decimal minimumOrderQuantity, int? leadTimeDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumOrderQuantity, nameof(minimumOrderQuantity));
        if (leadTimeDays.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(leadTimeDays.Value, nameof(leadTimeDays));
        MinimumOrderQuantity = minimumOrderQuantity;
        LeadTimeDays = leadTimeDays;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void RecordConsumption(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        ConsumedQuantity += quantity;
    }

    /// <summary>Called when the contract is renewed and the volume promise starts again.</summary>
    internal void ResetConsumption() => ConsumedQuantity = 0m;

    public decimal? RemainingQuantity => CommittedQuantity - ConsumedQuantity;

    public bool IsCommitmentMet => CommittedQuantity is null || ConsumedQuantity >= CommittedQuantity.Value;
}
