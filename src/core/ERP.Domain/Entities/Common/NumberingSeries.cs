namespace ERP.Domain.Entities.Common;

/// <summary>
/// Generates the human facing numbers of business documents, so every document type has one
/// place that owns its format instead of each handler inventing its own.
/// </summary>
public sealed class NumberingSeries : AggregateRoot, IBranchScoped
{
    /// <summary>Identifies what is numbered, for example SalesInvoice or PurchaseOrder.</summary>
    public string Key { get; private set; } = default!;

    public string? Prefix { get; private set; }
    public string? Suffix { get; private set; }

    /// <summary>Zero padding width of the numeric part.</summary>
    public int PaddingLength { get; private set; }

    public long CurrentValue { get; private set; }
    public NumberingResetPolicy ResetPolicy { get; private set; }

    /// <summary>When set, the series is private to one branch.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>When set, the series is private to one financial year.</summary>
    public Guid? FinancialYearId { get; private set; }

    public DateOnly? LastResetDate { get; private set; }
    public bool IsActive { get; private set; }

    private NumberingSeries() { }

    public NumberingSeries(string key, string? prefix = null, int paddingLength = 6, NumberingResetPolicy resetPolicy = NumberingResetPolicy.Never, long startValue = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentOutOfRangeException.ThrowIfNegative(paddingLength, nameof(paddingLength));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(paddingLength, 18, nameof(paddingLength));
        ArgumentOutOfRangeException.ThrowIfNegative(startValue, nameof(startValue));
        Key = key.Trim();
        Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim();
        PaddingLength = paddingLength;
        ResetPolicy = resetPolicy;
        CurrentValue = startValue;
        IsActive = true;
    }

    public void SetSuffix(string? suffix) => Suffix = string.IsNullOrWhiteSpace(suffix) ? null : suffix.Trim();

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void ScopeToFinancialYear(Guid financialYearId) => FinancialYearId = financialYearId == Guid.Empty ? null : financialYearId;

    /// <summary>
    /// Consumes the next number in the series and returns its formatted form.
    /// Concurrency is the caller's responsibility; the row must be locked or the stamp checked.
    /// </summary>
    public string Next(DateOnly? onDate = null)
    {
        if (!IsActive)
            throw new InvalidOperationException($"Numbering series {Key} is not active.");

        var date = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (ShouldReset(date))
        {
            CurrentValue = 0;
            LastResetDate = date;
        }

        CurrentValue++;
        return Format(CurrentValue);
    }

    /// <summary>Formats a value without consuming it, for previews.</summary>
    public string Peek() => Format(CurrentValue + 1);

    private string Format(long value)
    {
        var numericPart = PaddingLength > 0 ? value.ToString().PadLeft(PaddingLength, '0') : value.ToString();
        return $"{Prefix}{numericPart}{Suffix}";
    }

    private bool ShouldReset(DateOnly date)
    {
        if (ResetPolicy == NumberingResetPolicy.Never || LastResetDate is null)
            return false;

        var last = LastResetDate.Value;
        return ResetPolicy switch
        {
            NumberingResetPolicy.Yearly => date.Year != last.Year,
            NumberingResetPolicy.Monthly => date.Year != last.Year || date.Month != last.Month,
            NumberingResetPolicy.Daily => date != last,
            _ => false
        };
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
