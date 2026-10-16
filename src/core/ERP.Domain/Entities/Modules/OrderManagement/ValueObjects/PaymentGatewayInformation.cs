namespace ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;

/// <summary>
/// What the payment gateway returned - the reference a dispute is traced by. Held as a value
/// object so it travels with the payment and cannot be half-assigned.
/// </summary>
public class PaymentGatewayInformation : ValueObject
{
    public string? Name { get; }
    public string? Domain { get; }
    public string? TrackingCode { get; }

    private PaymentGatewayInformation() { }

    public PaymentGatewayInformation(string name, string domain, string trackingCode)
    {
        Name = name;
        Domain = domain;
        TrackingCode = trackingCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Domain!;
        yield return Name!;
        yield return TrackingCode!;
    }
}
