using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// What is on hand of one item in one warehouse, and what it is worth. Holding it per warehouse
/// is what makes it possible to answer where the stock actually is.
/// </summary>
public sealed class ItemInventory : BaseEntity
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public Warehouse Warehouse { get; private set; } = default!;
    public Guid WarehouseId { get; private set; }

    /// <summary>Narrows the balance to one bin inside the warehouse, when bins are used.</summary>
    public Guid? StorageLocationId { get; private set; }

    public decimal MinimumQuantity { get; private set; }
    public decimal? MaximumQuantity { get; private set; }

    /// <summary>The level at which replenishment is triggered.</summary>
    public decimal? ReorderPoint { get; private set; }

    /// <summary>How much to order once the reorder point is reached.</summary>
    public decimal? ReorderQuantity { get; private set; }

    public decimal OnHandQuantity { get; private set; }

    /// <summary>Promised to orders but not yet issued.</summary>
    public decimal ReservedQuantity { get; private set; }

    /// <summary>Ordered from suppliers but not yet received.</summary>
    public decimal OnOrderQuantity { get; private set; }

    /// <summary>Held back from use, for instance pending quality inspection.</summary>
    public decimal QuarantinedQuantity { get; private set; }

    public decimal AvailableQuantity => OnHandQuantity - ReservedQuantity - QuarantinedQuantity;

    /// <summary>The value of what is on hand, which the costing method keeps in step with quantity.</summary>
    public Money TotalValue { get; private set; } = default!;

    public Money AverageUnitCost => OnHandQuantity == 0m
        ? Money.Zero(TotalValue.CurrencyCode)
        : TotalValue.Divide(OnHandQuantity);

    public DateTimeOffset LastUpdatedOnHandQuantity { get; private set; }
    public DateTimeOffset LastUpdatedReservedQuantity { get; private set; }
    public DateOnly? LastCountedOn { get; private set; }

    private ItemInventory() { }

    public ItemInventory(Item item, Warehouse warehouse, string currencyCode, decimal minimumQuantity = 0m)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(warehouse, nameof(warehouse));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        ArgumentOutOfRangeException.ThrowIfNegative(minimumQuantity, nameof(minimumQuantity));

        Item = item;
        ItemId = item.Id;
        Warehouse = warehouse;
        WarehouseId = warehouse.Id;
        MinimumQuantity = minimumQuantity;
        TotalValue = Money.Zero(currencyCode);
    }

    public void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    public void SetStockPolicy(decimal minimumQuantity, decimal? maximumQuantity, decimal? reorderPoint, decimal? reorderQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumQuantity, nameof(minimumQuantity));
        if (maximumQuantity.HasValue && maximumQuantity.Value < minimumQuantity)
            throw new ArgumentOutOfRangeException(nameof(maximumQuantity), "The maximum cannot be below the minimum.");
        if (reorderPoint.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(reorderPoint.Value, nameof(reorderPoint));
        if (reorderQuantity.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reorderQuantity.Value, nameof(reorderQuantity));

        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
        ReorderPoint = reorderPoint;
        ReorderQuantity = reorderQuantity;
    }

    /// <summary>Receives stock and rolls the value forward with it.</summary>
    public void Increase(decimal quantity, Money cost)
    {
        ArgumentNullException.ThrowIfNull(cost, nameof(cost));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        EnsureSameCurrency(cost);

        OnHandQuantity += quantity;
        TotalValue = TotalValue.Add(cost);
        LastUpdatedOnHandQuantity = DateTimeOffset.UtcNow;
    }

    /// <summary>Issues stock and releases the matching value.</summary>
    public void Decrease(decimal quantity, Money cost)
    {
        ArgumentNullException.ThrowIfNull(cost, nameof(cost));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        EnsureSameCurrency(cost);
        if (quantity > OnHandQuantity)
            throw new InvalidOperationException($"Insufficient inventory: {OnHandQuantity} on hand, {quantity} requested.");

        OnHandQuantity -= quantity;
        TotalValue = OnHandQuantity == 0m ? Money.Zero(TotalValue.CurrencyCode) : TotalValue.Subtract(cost);
        LastUpdatedOnHandQuantity = DateTimeOffset.UtcNow;

        if (IsBelowMinimum)
            Raise(new StockFellBelowMinimum(ItemId, WarehouseId, OnHandQuantity, MinimumQuantity));
        if (NeedsReorder)
            Raise(new ReorderPointReached(ItemId, WarehouseId, ReorderQuantity ?? 0m));
    }

    /// <summary>The cost of issuing a quantity under the weighted average method.</summary>
    public Money CostOfIssuing(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > OnHandQuantity)
            throw new InvalidOperationException($"Insufficient inventory: {OnHandQuantity} on hand, {quantity} requested.");
        return OnHandQuantity == 0m ? Money.Zero(TotalValue.CurrencyCode) : AverageUnitCost.Multiply(quantity);
    }

    public void Reserve(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > AvailableQuantity)
            throw new InvalidOperationException($"Insufficient available inventory: {AvailableQuantity} free, {quantity} requested.");
        ReservedQuantity += quantity;
        LastUpdatedReservedQuantity = DateTimeOffset.UtcNow;
    }

    public void ReleaseReserved(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > ReservedQuantity)
            throw new InvalidOperationException("Release quantity exceeds reserved quantity.");
        ReservedQuantity -= quantity;
        LastUpdatedReservedQuantity = DateTimeOffset.UtcNow;
    }

    public void AddOnOrder(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        OnOrderQuantity += quantity;
    }

    public void ReleaseOnOrder(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > OnOrderQuantity)
            throw new InvalidOperationException("Release quantity exceeds the quantity on order.");
        OnOrderQuantity -= quantity;
    }

    public void Quarantine(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > AvailableQuantity)
            throw new InvalidOperationException("Cannot quarantine more than is available.");
        QuarantinedQuantity += quantity;
    }

    public void ReleaseQuarantine(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (quantity > QuarantinedQuantity)
            throw new InvalidOperationException("Release quantity exceeds the quarantined quantity.");
        QuarantinedQuantity -= quantity;
    }

    /// <summary>Corrects the book quantity to what was physically counted.</summary>
    public void AdjustToCountedQuantity(decimal countedQuantity, DateOnly countedOn)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(countedQuantity, nameof(countedQuantity));
        var unitCost = AverageUnitCost;
        OnHandQuantity = countedQuantity;
        TotalValue = unitCost.Multiply(countedQuantity);
        LastCountedOn = countedOn;
        LastUpdatedOnHandQuantity = DateTimeOffset.UtcNow;
    }

    /// <summary>Restates the value without touching the quantity, after a revaluation.</summary>
    public void Revalue(Money totalValue)
    {
        ArgumentNullException.ThrowIfNull(totalValue, nameof(totalValue));
        EnsureSameCurrency(totalValue);
        if (totalValue.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(totalValue), "Inventory value cannot be negative.");
        TotalValue = totalValue;
    }

    public bool IsBelowMinimum => OnHandQuantity < MinimumQuantity;

    public bool NeedsReorder => ReorderPoint.HasValue && OnHandQuantity + OnOrderQuantity <= ReorderPoint.Value;

    private void EnsureSameCurrency(Money amount)
    {
        if (amount.CurrencyCode != TotalValue.CurrencyCode)
            throw new ArgumentException($"The amount must be expressed in {TotalValue.CurrencyCode}.", nameof(amount));
    }
}
