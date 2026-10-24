namespace ERP.Domain.Entities.Modules.PricingManagement;

/// <summary>
/// A set of prices that applies to a currency, a period and normally a customer group. Orders
/// and quotations read the price that was in force on their own date.
/// </summary>
public sealed partial class PriceList : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public PriceListType Type { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public DateRange ValidityPeriod { get; private set; } = default!;

    /// <summary>The effective date the list starts from, kept for the older accessor.</summary>
    public DateOnly EffectiveDate => ValidityPeriod.StartDate;

    public Guid? CustomerGroupId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>Whether the prices already contain tax.</summary>
    public bool IsTaxInclusive { get; private set; }

    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public string? Description { get; private set; }

    private readonly List<PriceListItem> _items = [];
    public IReadOnlyList<PriceListItem> Items => _items.AsReadOnly();

    private PriceList() { }

    public PriceList(string code, string name, PriceListType type, string currencyCode, DateRange validityPeriod)
    {
        ArgumentNullException.ThrowIfNull(validityPeriod, nameof(validityPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ValidityPeriod = validityPeriod;
        IsActive = true;
    }

    public void Update(string name, DateRange validityPeriod)
    {
        ArgumentNullException.ThrowIfNull(validityPeriod, nameof(validityPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        ValidityPeriod = validityPeriod;
    }

    public void RestrictToCustomerGroup(Guid customerGroupId) =>
        CustomerGroupId = customerGroupId == Guid.Empty ? null : customerGroupId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void MarkAsTaxInclusive() => IsTaxInclusive = true;

    public void MarkAsDefault() => IsDefault = true;
    public void UnmarkAsDefault() => IsDefault = false;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(PriceListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.PriceListId, _items);

        if (item.BasePrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Price list lines must be expressed in {CurrencyCode}.");
        if (_items.Any(i => i.ItemId == item.ItemId && i.MinimumQuantity == item.MinimumQuantity))
            throw new InvalidOperationException("That item already has a price at this quantity break.");
        _items.Add(item);
    }

    /// <summary>Validates a complete replacement or merge before changing any existing prices.</summary>
    public void SetItems(IEnumerable<PriceListItem> items, bool replaceAll = false)
    {
        ArgumentNullException.ThrowIfNull(items);
        var incoming = items.ToArray();
        if (incoming.Any(item => item is null))
            throw new ArgumentException("Price list lines cannot be null.", nameof(items));
        var replacement = replaceAll ? new List<PriceListItem>() : _items
            .Where(existing => !incoming.Any(item => item.ItemId == existing.ItemId && item.MinimumQuantity == existing.MinimumQuantity))
            .ToList();
        foreach (var item in incoming)
        {
            EnsureCanAddChild(item, item.PriceListId, replacement);
            if (item.BasePrice.CurrencyCode != CurrencyCode)
                throw new InvalidOperationException($"Price list lines must be expressed in {CurrencyCode}.");
            if (replacement.Any(existing => existing.ItemId == item.ItemId && existing.MinimumQuantity == item.MinimumQuantity))
                throw new InvalidOperationException("That item already has a price at this quantity break.");
            replacement.Add(item);
        }
        _items.Clear();
        _items.AddRange(replacement);
    }

    public void RemoveItem(PriceListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.PriceListId != Id || !_items.Contains(item))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(item, nameof(item));
        _items.Remove(item);
    }

    /// <summary>
    /// The price for an item at a given quantity, honouring volume breaks by taking the highest
    /// break the quantity qualifies for.
    /// </summary>
    public PriceListItem? PriceFor(Guid itemId, decimal quantity = 1m)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        return _items
            .Where(i => i.ItemId == itemId && i.MinimumQuantity <= quantity && i.IsActive)
            .OrderByDescending(i => i.MinimumQuantity)
            .FirstOrDefault();
    }

    public bool IsEffectiveOn(DateOnly date) => IsActive && ValidityPeriod.Contains(date);

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
