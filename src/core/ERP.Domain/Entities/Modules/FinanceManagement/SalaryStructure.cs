namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The set of earnings and deductions that make up pay for a class of employees, and the rules
/// that compute each one. Payroll runs read a structure instead of hard coding the arithmetic.
/// </summary>
public sealed partial class SalaryStructure : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public DateRange EffectivePeriod { get; private set; } = default!;
    public string CurrencyCode { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<SalaryStructureItem> _items = [];
    public IReadOnlyList<SalaryStructureItem> Items => _items.AsReadOnly();

    private SalaryStructure() { }

    public SalaryStructure(string code, string name, DateRange effectivePeriod, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        ArgumentNullException.ThrowIfNull(effectivePeriod, nameof(effectivePeriod));

        Code = code.Trim();
        Name = name.Trim();
        EffectivePeriod = effectivePeriod;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void AddItem(SalaryStructureItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.StructureId, _items);

        if (_items.Any(i => i.Code == item.Code))
            throw new InvalidOperationException($"An item with code {item.Code} already exists in this structure.");
        _items.Add(item);
    }

    public void RemoveItem(string code)
    {
        var item = _items.FirstOrDefault(i => i.Code == code);
        if (item is not null)
            _items.Remove(item);
    }

    public IEnumerable<SalaryStructureItem> Earnings =>
        _items.Where(i => i.Nature == SalaryItemNature.Earning).OrderBy(i => i.CalculationOrder);

    public IEnumerable<SalaryStructureItem> Deductions =>
        _items.Where(i => i.Nature == SalaryItemNature.Deduction).OrderBy(i => i.CalculationOrder);

    public bool IsEffectiveOn(DateOnly date) => IsActive && EffectivePeriod.Contains(date);

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
