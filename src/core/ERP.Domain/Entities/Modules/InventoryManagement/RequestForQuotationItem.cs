namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One line suppliers are being asked to price.
/// </summary>
public sealed class RequestForQuotationItem : BaseEntity
{
    public RequestForQuotation Request { get; private set; } = default!;
    public Guid RequestId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public short LineNumber { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public DateOnly? RequiredByDate { get; private set; }
    public string? Specification { get; private set; }

    private RequestForQuotationItem() { }

    public RequestForQuotationItem(RequestForQuotation request, Item item, short lineNumber, decimal quantity, Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Request = request;
        RequestId = request.Id;
        Item = item;
        ItemId = item.Id;
        LineNumber = lineNumber;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
    }

    internal void SetRequiredByDate(DateOnly requiredByDate) => RequiredByDate = requiredByDate;

    internal void SetSpecification(string? specification) =>
        Specification = string.IsNullOrWhiteSpace(specification) ? null : specification.Trim();
}
