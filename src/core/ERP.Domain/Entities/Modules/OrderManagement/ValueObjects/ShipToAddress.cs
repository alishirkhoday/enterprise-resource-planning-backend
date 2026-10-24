namespace ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;

/// <summary>
/// The delivery address copied onto an order at the moment it is placed, together with who
/// receives it. It is a snapshot: later edits to the customer address must not rewrite history.
/// </summary>
public sealed class ShipToAddress : ValueObject
{
    public Address Address { get; private set; } = default!;
    public string ReceiverFullName { get; private set; } = default!;
    public string ReceiverMobileNumber { get; private set; } = default!;
    public string? ReceiverNationalId { get; private set; }
    public string? DeliveryInstruction { get; private set; }

    private ShipToAddress() { }

    public ShipToAddress(Address address, string receiverFullName, string receiverMobileNumber,
        string? receiverNationalId = null, string? deliveryInstruction = null)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverFullName, nameof(receiverFullName));
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverMobileNumber, nameof(receiverMobileNumber));
        Address = address.Copy();
        ReceiverFullName = receiverFullName.Trim();
        ReceiverMobileNumber = receiverMobileNumber.Trim();
        ReceiverNationalId = string.IsNullOrWhiteSpace(receiverNationalId) ? null : receiverNationalId.Trim();
        DeliveryInstruction = string.IsNullOrWhiteSpace(deliveryInstruction) ? null : deliveryInstruction.Trim();
    }

    internal ShipToAddress Copy() =>
        new(Address, ReceiverFullName, ReceiverMobileNumber, ReceiverNationalId, DeliveryInstruction);

    public ShipToAddress WithReceiverNationalId(string? nationalId) =>
        new(Address, ReceiverFullName, ReceiverMobileNumber, nationalId, DeliveryInstruction);

    public ShipToAddress WithDeliveryInstruction(string? instruction) =>
        new(Address, ReceiverFullName, ReceiverMobileNumber, ReceiverNationalId, instruction);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Address;
        yield return ReceiverFullName;
        yield return ReceiverMobileNumber;
        yield return ReceiverNationalId;
        yield return DeliveryInstruction;
    }
}
