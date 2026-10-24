namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A book of blank cheque leaves issued by the bank against one account. Tracking the range
/// is what lets the organization notice a missing leaf.
/// </summary>
public sealed class ChequeBook : AggregateRoot
{
    public BankBranchAccount BankBranchAccount { get; private set; } = default!;
    public Guid BankBranchAccountId { get; private set; }

    public string SerialNumber { get; private set; } = default!;
    public long FirstLeafNumber { get; private set; }
    public long LastLeafNumber { get; private set; }
    public long NextLeafNumber { get; private set; }

    public DateOnly IssuedOn { get; private set; }
    public bool IsActive { get; private set; }

    private ChequeBook() { }

    public ChequeBook(BankBranchAccount bankBranchAccount, string serialNumber, long firstLeafNumber, long lastLeafNumber, DateOnly issuedOn)
    {
        ArgumentNullException.ThrowIfNull(bankBranchAccount, nameof(bankBranchAccount));
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber, nameof(serialNumber));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(firstLeafNumber, nameof(firstLeafNumber));
        if (lastLeafNumber < firstLeafNumber)
            throw new ArgumentOutOfRangeException(nameof(lastLeafNumber), "The last leaf cannot precede the first leaf.");

        BankBranchAccount = bankBranchAccount;
        BankBranchAccountId = bankBranchAccount.Id;
        SerialNumber = serialNumber.Trim();
        FirstLeafNumber = firstLeafNumber;
        LastLeafNumber = lastLeafNumber;
        NextLeafNumber = firstLeafNumber;
        IssuedOn = issuedOn;
        IsActive = true;
    }

    public int TotalLeaves => (int)(LastLeafNumber - FirstLeafNumber + 1);
    public int RemainingLeaves => (int)(LastLeafNumber - NextLeafNumber + 1);
    public bool IsExhausted => NextLeafNumber > LastLeafNumber;

    /// <summary>Consumes the next leaf and returns its number.</summary>
    public long TakeNextLeaf()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Cheque book {SerialNumber} is not active.");
        if (IsExhausted)
            throw new InvalidOperationException($"Cheque book {SerialNumber} has no leaves left.");
        return NextLeafNumber++;
    }

    public void Close() => IsActive = false;
}
