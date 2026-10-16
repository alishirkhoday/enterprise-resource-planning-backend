using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Cheques in both directions - چک دریافتی و پرداختی. What they are worth depends entirely on
/// where they are in their lifecycle, so status is part of nearly every query.
/// </summary>
public interface IChequeRepository : IRepository<Cheque>
{
    Task<Cheque?> GetBySayadNumberAsync(string sayadNumber, CancellationToken cancellationToken = default);

    Task<Cheque?> GetWithOperationsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cheque>> GetByStatusAsync(ChequeType type, ChequeStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cheques falling due within the given window. Treasury runs on this: an issued cheque needs
    /// funds in the account before its date, and a received one needs depositing.
    /// </summary>
    Task<IReadOnlyList<Cheque>> GetDueBetweenAsync(ChequeType type, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Cheques past their date that have neither cleared nor bounced.</summary>
    Task<IReadOnlyList<Cheque>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>Everything still outstanding for one party, whichever direction it runs.</summary>
    Task<IReadOnlyList<Cheque>> GetOpenByPartyAsync(Guid detailAccountId, CancellationToken cancellationToken = default);
}
