namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Goods sent back to a supplier, and the credit expected in exchange.
/// </summary>
public sealed partial class PurchaseReturn : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public Guid? PurchaseOrderId { get; private set; }
    public Guid? GoodsReceiptId { get; private set; }
    public Guid? PurchaseInvoiceId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public DateOnly ReturnDate { get; private set; }
    public PurchaseReturnReason Reason { get; private set; }
    public PurchaseReturnStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The credit note number the supplier issued against the return.</summary>
    public string? CreditNoteNumber { get; private set; }

    public Guid? DocumentId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? Description { get; private set; }

    private readonly List<PurchaseReturnItem> _items = [];
    public IReadOnlyList<PurchaseReturnItem> Items => _items.AsReadOnly();

    private PurchaseReturn() { }

    public PurchaseReturn(string number, Supplier supplier, Guid warehouseId, DateOnly returnDate, PurchaseReturnReason reason, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is empty.", nameof(warehouseId));

        Number = number.Trim();
        Supplier = supplier;
        SupplierId = supplier.Id;
        WarehouseId = warehouseId;
        ReturnDate = returnDate;
        Reason = reason;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Status = PurchaseReturnStatus.Draft;
    }

    public void LinkToSource(Guid? purchaseOrderId, Guid? goodsReceiptId, Guid? purchaseInvoiceId)
    {
        PurchaseOrderId = purchaseOrderId == Guid.Empty ? null : purchaseOrderId;
        GoodsReceiptId = goodsReceiptId == Guid.Empty ? null : goodsReceiptId;
        PurchaseInvoiceId = purchaseInvoiceId == Guid.Empty ? null : purchaseInvoiceId;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(PurchaseReturnItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.ReturnId, _items);

        EnsureDraft();
        if (item.UnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Return lines must be priced in {CurrencyCode}.");
        _items.Add(item);
    }

    public Money TotalValue() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.LineTotal).Aggregate((accumulated, next) => accumulated.Add(next));

    /// <summary>
    /// Confirms the return. The caller is expected to write the matching stock ledger entries in
    /// the same transaction.
    /// </summary>
    public void Confirm()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A return cannot be confirmed without lines.");
        Status = PurchaseReturnStatus.Confirmed;
    }

    public void RecordCreditNote(string creditNoteNumber, Guid? documentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(creditNoteNumber, nameof(creditNoteNumber));
        if (Status != PurchaseReturnStatus.Confirmed)
            throw new InvalidOperationException("A credit note can only be recorded against a confirmed return.");
        CreditNoteNumber = creditNoteNumber.Trim();
        DocumentId = documentId == Guid.Empty ? null : documentId;
        Status = PurchaseReturnStatus.Credited;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == PurchaseReturnStatus.Credited)
            throw new InvalidOperationException("A credited return cannot be cancelled.");
        Status = PurchaseReturnStatus.Cancelled;
        Description = reason.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseReturnStatus.Draft)
            throw new InvalidOperationException($"The return is {Status} and can no longer be edited.");
    }
}
