namespace ERP.Domain.Entities.Modules.PricingManagement;

/// <summary>
/// What a price list is for - selling, buying, a promotion, or one contract. Decides which
/// side of a transaction it may price.
/// </summary>
public enum PriceListType
{
    /// <summary>Prices charged to customers.</summary>
    Sales = 0,

    /// <summary>Prices paid to suppliers.</summary>
    Purchase = 1,

    /// <summary>Prices for a particular promotion.</summary>
    Promotional = 2,

    /// <summary>Prices agreed under one contract.</summary>
    Contract = 3
}
