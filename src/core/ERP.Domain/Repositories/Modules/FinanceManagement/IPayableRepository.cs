using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// What the business owes - حساب‌های پرداختنی. Open items, not the invoices that created them.
/// </summary>
public interface IPayableRepository : IRepository<Payable>
{
    Task<IReadOnlyList<Payable>> GetOpenByPartyAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default);

    Task<Payable?> GetBySourceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payable>> GetOverdueAsync(DateOnly asOf, Guid? partyDetailAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// What is due for payment in a window, excluding anything on hold. This is the list a payment
    /// run is built from.
    /// </summary>
    Task<IReadOnlyList<Payable>> GetDueForPaymentAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<decimal> GetOutstandingTotalAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default);
}
