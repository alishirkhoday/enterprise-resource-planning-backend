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

    public ShipToAddress(Address address, string receiverFullName, string receiverMobileNumber)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverFullName, nameof(receiverFullName));
        ArgumentException.ThrowIfNullOrWhiteSpace(receiverMobileNumber, nameof(receiverMobileNumber));
        Address = address;
        ReceiverFullName = receiverFullName.Trim();
        ReceiverMobileNumber = receiverMobileNumber.Trim();
    }

    public void SetReceiverNationalId(string? nationalId) =>
        ReceiverNationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId.Trim();

    public void SetDeliveryInstruction(string? instruction) =>
        DeliveryInstruction = string.IsNullOrWhiteSpace(instruction) ? null : instruction.Trim();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Address;
        yield return ReceiverFullName;
        yield return ReceiverMobileNumber;
        yield return ReceiverNationalId;
    }
}
