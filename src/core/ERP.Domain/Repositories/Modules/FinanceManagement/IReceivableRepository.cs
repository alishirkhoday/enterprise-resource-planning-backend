using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;

/// <summary>
/// One open item as an ageing report sees it. Only the columns the report needs are read, so a
/// year of invoices does not have to be loaded as entities.
/// </summary>
public sealed record ReceivableAgeingRow(
    Guid ReceivableId,
    Guid PartyDetailAccountId,
    DateOnly DueDate,
    decimal OutstandingAmount,
    string CurrencyCode)
{
    public int DaysOverdueOn(DateOnly asOf) => asOf > DueDate ? asOf.DayNumber - DueDate.DayNumber : 0;
}
/// <summary>
/// What customers owe - حساب‌های دریافتنی. Open items rather than invoices: an invoice is a
/// document, a receivable is the debt it left behind.
/// </summary>
public interface IReceivableRepository : IRepository<Receivable>
{
    /// <summary>Everything a party still owes, oldest first so payments can be applied in order.</summary>
    Task<IReadOnlyList<Receivable>> GetOpenByPartyAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default);

    Task<Receivable?> GetBySourceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Receivable>> GetOverdueAsync(DateOnly asOf, Guid? partyDetailAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// What a party owes in total right now, which is what a credit limit is checked against
    /// before a further sale is allowed. Summed in the database.
    /// </summary>
    Task<decimal> GetOutstandingTotalAsync(Guid partyDetailAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The open items an ageing report is built from, projected rather than loaded as entities.
    /// The caller decides the buckets, because how far overdue counts as far depends on the terms.
    /// </summary>
    Task<IReadOnlyList<ReceivableAgeingRow>> GetAgeingRowsAsync(Guid? partyDetailAccountId = null, CancellationToken cancellationToken = default);
}
