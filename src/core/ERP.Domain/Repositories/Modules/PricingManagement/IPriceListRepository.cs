using ERP.Domain.Entities.Modules.PricingManagement;

namespace ERP.Domain.Repositories.Modules.PricingManagement;
/// <summary>
/// Price lists - لیست قیمت. The resolution order in here is what stops a negotiated price
/// being quietly replaced by a standard one.
/// </summary>
public interface IPriceListRepository : IRepository<PriceList>
{
    Task<PriceList?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PriceList?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The list that should price a sale to this customer on this date, with its lines loaded.
    /// <para>
    /// Specificity decides, in the order a salesperson would expect: a list named on the customer
    /// beats one for their group, which beats the company default. Without that order a customer
    /// with a negotiated list would silently be charged from whichever row came back first.
    /// </para>
    /// </summary>
    Task<PriceList?> GetEffectiveForSaleAsync(
        Guid? preferredPriceListId,
        Guid? customerGroupId,
        string currencyCode,
        DateOnly onDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceList>> GetActiveOnAsync(DateOnly onDate, CancellationToken cancellationToken = default);
}
