namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The periodic filing that nets input tax against output tax and states what is owed.
/// </summary>
public sealed class TaxReturn : AggregateRoot, IBranchScoped
{
    public string? Number { get; private set; }
    public TaxType Type { get; private set; }
    public DateRange Period { get; private set; } = default!;
    public DateOnly DueDate { get; private set; }

    public Money OutputTax { get; private set; } = default!;
    public Money InputTax { get; private set; } = default!;

    /// <summary>Positive when tax is owed, negative when it is reclaimable.</summary>
    public Money NetPayable => OutputTax.Subtract(InputTax);

    public TaxReturnStatus Status { get; private set; }
    public DateOnly? SubmittedOn { get; private set; }
    public DateOnly? PaidOn { get; private set; }

    /// <summary>The receipt identifier the tax authority returns on submission.</summary>
    public string? SubmissionReference { get; private set; }

    public Guid? BranchId { get; private set; }
    public string? Notes { get; private set; }

    private TaxReturn() { }

    public TaxReturn(TaxType type, DateRange period, DateOnly dueDate, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        Type = type;
        Period = period;
        DueDate = dueDate;
        OutputTax = Money.Zero(currencyCode);
        InputTax = Money.Zero(currencyCode);
        Status = TaxReturnStatus.Draft;
    }

    public void SetNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        Number = number.Trim();
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AccumulateOutput(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        EnsureDraft();
        OutputTax = OutputTax.Add(amount);
    }

    public void AccumulateInput(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        EnsureDraft();
        InputTax = InputTax.Add(amount);
    }

    public void Reset()
    {
        EnsureDraft();
        OutputTax = Money.Zero(OutputTax.CurrencyCode);
        InputTax = Money.Zero(InputTax.CurrencyCode);
    }

    public void Submit(DateOnly submittedOn, string? submissionReference = null)
    {
        EnsureDraft();
        SubmittedOn = submittedOn;
        SubmissionReference = string.IsNullOrWhiteSpace(submissionReference) ? null : submissionReference.Trim();
        Status = TaxReturnStatus.Submitted;
    }

    public void MarkAsPaid(DateOnly paidOn)
    {
        if (Status != TaxReturnStatus.Submitted)
            throw new InvalidOperationException("A return must be submitted before it can be paid.");
        PaidOn = paidOn;
        Status = TaxReturnStatus.Paid;
    }

    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    public bool IsOverdueOn(DateOnly date) => Status == TaxReturnStatus.Draft && date > DueDate;

    private void EnsureDraft()
    {
        if (Status != TaxReturnStatus.Draft)
            throw new InvalidOperationException($"The return is {Status} and can no longer be edited.");
    }
}
