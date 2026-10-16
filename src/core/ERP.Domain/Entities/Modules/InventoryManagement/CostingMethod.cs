namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// How the cost of what leaves the shelf is worked out - روش قیمت‌گذاری. Changing it on an
/// item that already has stock changes what that stock is worth, which is why it is a
/// property of the item rather than a setting somewhere.
/// </summary>
public enum CostingMethod
{
    /// <summary>The cost of an issue is the running average of what is on hand.</summary>
    WeightedAverage = 0,

    /// <summary>The oldest cost on hand is released first.</summary>
    FirstInFirstOut = 1,

    /// <summary>The newest cost on hand is released first.</summary>
    LastInFirstOut = 2,

    /// <summary>Each unit carries the cost it was bought at, which needs serial or batch tracking.</summary>
    SpecificIdentification = 3,

    /// <summary>A fixed planned cost, with the difference posted as a variance.</summary>
    Standard = 4
}
