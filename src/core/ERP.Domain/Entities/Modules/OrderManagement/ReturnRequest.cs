namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// A customer asking to send goods back. Approving it authorises the return; receiving it is
/// what puts the stock back and releases the refund.
/// </summary>
public sealed partial class ReturnRequest : AggregateRoot
{
    public string Number { get; private set; } = default!;

    public Order Order { get; private set; } = default!;
    public Guid OrderId { get; private set; }

    public DateOnly RequestDate { get; private set; }
    public ReturnRequestStatus Status { get; private set; }
    public ReturnReason Reason { get; private set; }

    /// <summary>Whether the customer wants money back or a replacement.</summary>
    public ReturnResolution Resolution { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Guid? WarehouseId { get; private set; }
    public Guid? RefundId { get; private set; }
    public Guid? CreditNoteInvoiceId { get; private set; }
    public Guid? ReplacementOrderId { get; private set; }

    public DateOnly? ReceivedOn { get; private set; }
    public DateOnly? CompletedOn { get; private set; }
    public string? CustomerComment { get; private set; }
    public string? InspectionNotes { get; private set; }
    public string? RejectionReason { get; private set; }

    private readonly List<ReturnRequestItem> _items = [];
    public IReadOnlyList<ReturnRequestItem> Items => _items.AsReadOnly();

    private ReturnRequest() { }

    public ReturnRequest(string number, Order order, DateOnly requestDate, ReturnReason reason)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Completed))
            throw new InvalidOperationException($"An order in state {order.Status} cannot be returned.");

        Number = number.Trim();
        Order = order;
        OrderId = order.Id;
        RequestDate = requestDate;
        Reason = reason;
        CurrencyCode = order.CurrencyCode;
        Resolution = ReturnResolution.Refund;
        Status = ReturnRequestStatus.Requested;
    }

    public void SetResolution(ReturnResolution resolution)
    {
        EnsureOpen();
        Resolution = resolution;
    }

    public void SetCustomerComment(string? comment) =>
        CustomerComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    public void SetReceivingWarehouse(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void AddItem(ReturnRequestItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.RequestId, _items);

        if (Status != ReturnRequestStatus.Requested)
            throw new InvalidOperationException($"Lines cannot be added while the request is {Status}.");
        _items.Add(item);
    }

    public Money TotalValue() =>
        _items.Count == 0
            ? Money.Zero(CurrencyCode)
            : _items.Select(i => i.LineTotal).Aggregate((accumulated, next) => accumulated.Add(next));

    public void Approve()
    {
        if (Status != ReturnRequestStatus.Requested)
            throw new InvalidOperationException($"Only a requested return can be approved; this one is {Status}.");
        if (_items.Count == 0)
            throw new InvalidOperationException("A return cannot be approved without lines.");
        Status = ReturnRequestStatus.Approved;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != ReturnRequestStatus.Requested)
            throw new InvalidOperationException($"Only a requested return can be rejected; this one is {Status}.");
        Status = ReturnRequestStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    /// <summary>
    /// Records the goods arriving back. The caller is expected to write the matching stock ledger
    /// entries for whatever was accepted.
    /// </summary>
    public void Receive(DateOnly receivedOn, string? inspectionNotes = null)
    {
        if (Status != ReturnRequestStatus.Approved)
            throw new InvalidOperationException($"Only an approved return can be received; this one is {Status}.");
        ReceivedOn = receivedOn;
        InspectionNotes = string.IsNullOrWhiteSpace(inspectionNotes) ? null : inspectionNotes.Trim();
        Status = ReturnRequestStatus.Received;
    }

    public void Complete(DateOnly completedOn, Guid? refundId = null, Guid? creditNoteInvoiceId = null, Guid? replacementOrderId = null)
    {
        if (Status != ReturnRequestStatus.Received)
            throw new InvalidOperationException($"Only a received return can be completed; this one is {Status}.");
        RefundId = refundId == Guid.Empty ? null : refundId;
        CreditNoteInvoiceId = creditNoteInvoiceId == Guid.Empty ? null : creditNoteInvoiceId;
        ReplacementOrderId = replacementOrderId == Guid.Empty ? null : replacementOrderId;
        CompletedOn = completedOn;
        Status = ReturnRequestStatus.Completed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is ReturnRequestStatus.Completed or ReturnRequestStatus.Received)
            throw new InvalidOperationException($"A return in state {Status} cannot be cancelled.");
        Status = ReturnRequestStatus.Cancelled;
        RejectionReason = reason.Trim();
    }

    private void EnsureOpen()
    {
        if (Status is ReturnRequestStatus.Completed or ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled)
            throw new InvalidOperationException($"The return is {Status} and is no longer open.");
    }
}
