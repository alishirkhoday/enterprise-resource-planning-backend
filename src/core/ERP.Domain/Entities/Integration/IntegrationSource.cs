namespace ERP.Domain.Entities.Integration;

/// <summary>
/// Who sent the message. Message identifiers are only unique per sender, so this is half of the
/// key that makes a delivery recognisable as a repeat.
/// </summary>
public enum IntegrationSource
{
    Other = 0,

    /// <summary>The tax authority filing system.</summary>
    TaxAuthority = 1,

    PaymentGateway = 2,
    Bank = 3,
    ShippingCarrier = 4,
    Marketplace = 5,
    ShortMessageServiceProvider = 6,
    IdentityProvider = 7
}
