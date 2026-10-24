namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// A receipt into, an issue out of, or a move between storage locations. Confirming it is what
/// writes the inventory ledger; a draft moves nothing.
/// </summary>
public sealed partial class WarehouseTransaction : AggregateRoot, IBranchScoped
{
    public string? Number { get; private set; }

    public WarehouseStorageLocation SourceLocation { get; private set; } = default!;
    public Guid SourceLocationId { get; private set; }

    public WarehouseStorageLocation DestinationLocation { get; private set; } = default!;
    public Guid DestinationLocationId { get; private set; }

    public WarehouseTransactionType Type { get; private set; }
    public WarehouseTransactionStatus Status { get; private set; }
    public DateTimeOffset TransactionEventDateTime { get; private set; }
    public string Description { get; private set; } = default!;
    public bool IsAutomatic { get; private set; }

    public string CurrencyCode { get; private set; } = default!;

    /// <summary>What caused the movement, for example GoodsReceipt together with its identifier.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    public Guid? DocumentId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? ConfirmedByHumanId { get; private set; }
    public DateTimeOffset? ConfirmedOn { get; private set; }

    private readonly List<WarehouseTransactionItem> _items = [];
    public IReadOnlyList<WarehouseTransactionItem> Items => _items.AsReadOnly();

    private WarehouseTransaction() { }

    public WarehouseTransaction(
        WarehouseStorageLocation sourceLocation,
        WarehouseStorageLocation destinationLocation,
        WarehouseTransactionType type,
        DateTimeOffset transactionEventDateTime,
        string description,
        string currencyCode,
        bool isAutomatic = false)
    {
        ArgumentNullException.ThrowIfNull(sourceLocation, nameof(sourceLocation));
        ArgumentNullException.ThrowIfNull(destinationLocation, nameof(destinationLocation));
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (sourceLocation.Id == destinationLocation.Id)
            throw new ArgumentException("The source and destination locations must differ.", nameof(destinationLocation));

        SourceLocation = sourceLocation;
        SourceLocationId = sourceLocation.Id;
        DestinationLocation = destinationLocation;
        DestinationLocationId = destinationLocation.Id;
        Type = type;
        TransactionEventDateTime = transactionEventDateTime;
        Description = description.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        IsAutomatic = isAutomatic;
        Status = WarehouseTransactionStatus.Draft;
    }

    public void SetNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        Number = number.Trim();
    }

    public void SetReference(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    public void AddItem(WarehouseTransactionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.TransactionId, _items);

        EnsureDraft();
        if (item.UnitCost.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Transaction lines must be valued in {CurrencyCode}.");
        _items.Add(item);
    }

    public void RemoveItem(WarehouseTransactionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.TransactionId != Id || !_items.Contains(item))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    public Money TotalValue() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.TotalCost).Aggregate((accumulated, next) => accumulated.Add(next));

    /// <summary>
    /// Makes the movement final. The caller is expected to write the matching stock ledger
    /// entries in the same transaction.
    /// </summary>
    public void Confirm(Guid? confirmedByHumanId = null)
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A warehouse transaction cannot be confirmed without lines.");

        var untracked = _items.FirstOrDefault(i => !i.IsTrackingComplete);
        if (untracked is not null)
            throw new InvalidOperationException($"Item {untracked.Item.Name} needs its batch or serial identified before confirmation.");

        Status = WarehouseTransactionStatus.Definite;
        ConfirmedByHumanId = confirmedByHumanId == Guid.Empty ? null : confirmedByHumanId;
        ConfirmedOn = DateTimeOffset.UtcNow;
    }

    public void SetDescription(string description)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(description, nameof(description));
        Description = description.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != WarehouseTransactionStatus.Draft)
            throw new InvalidOperationException("A confirmed warehouse transaction cannot be modified; reverse it instead.");
    }
}
