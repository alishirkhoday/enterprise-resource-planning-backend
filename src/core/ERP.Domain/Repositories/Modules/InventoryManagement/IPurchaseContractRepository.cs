using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Repositories.Modules.InventoryManagement;
/// <summary>
/// Supply agreements - قرارداد خرید. The prices an order draws on instead of asking again.
/// </summary>
public interface IPurchaseContractRepository : IRepository<PurchaseContract>
{
    Task<PurchaseContract?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<PurchaseContract?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The active contract that covers an item on a date, so a purchase order can inherit the
    /// agreed price instead of whatever the buyer types.
    /// </summary>
    Task<PurchaseContract?> GetEffectiveForItemAsync(Guid supplierId, Guid itemId, DateOnly onDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseContract>> GetActiveForSupplierAsync(Guid supplierId, DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Contracts whose renewal notice window has opened. Missing it on an auto-renewing agreement
    /// commits another term nobody decided to commit to.
    /// </summary>
    Task<IReadOnlyList<PurchaseContract>> GetDueForRenewalAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active contracts whose committed value is still a long way from being taken up, with how
    /// much is left. A volume promise that is not met is usually discovered by the supplier first.
    /// </summary>
    Task<IReadOnlyList<PurchaseContractCommitmentRow>> GetUnderconsumedAsync(DateOnly asOf, decimal minimumRemainingRatio, CancellationToken cancellationToken = default);
}

/// <summary>A contract with how much of its commitment is still outstanding.</summary>
public sealed record PurchaseContractCommitmentRow(
    Guid ContractId,
    string Number,
    Guid SupplierId,
    string CurrencyCode,
    decimal CommittedAmount,
    decimal ConsumedAmount,
    DateOnly? EndDate)
{
    public decimal RemainingAmount => CommittedAmount - ConsumedAmount;

    public decimal RemainingRatio => CommittedAmount == 0m ? 0m : RemainingAmount / CommittedAmount;
}
