using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Journal entries - اسناد حسابداری. The ledger itself: everything that moved a balance.
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetByNumberAsync(string number, Guid financialYearId, CancellationToken cancellationToken = default);

    /// <summary>
    /// A journal entry with its lines. An entry is only meaningful whole, so the aggregate is
    /// loaded whole rather than leaving the caller to fetch lines separately.
    /// </summary>
    Task<Document?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The entry that a business document produced, if it has been posted yet.</summary>
    Task<Document?> GetByReferenceAsync(string referenceType, string referenceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetByPeriodAsync(Guid accountingPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entries still in draft. A financial year cannot close while any of these remain, so the
    /// closing routine asks for them by name.
    /// </summary>
    Task<IReadOnlyList<Document>> GetDraftsAsync(Guid financialYearId, CancellationToken cancellationToken = default);

    /// <summary>The highest number issued in a year, used to continue the sequence.</summary>
    Task<string?> GetLastNumberAsync(Guid financialYearId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The debit and credit totals posted to an account in a period, which is what a trial balance
    /// is built from. Summed in the database rather than by loading every line.
    /// </summary>
    Task<(decimal Debtor, decimal Creditor)> GetAccountTurnoverAsync(
        Guid accountId,
        Guid financialYearId,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The posted lines on one account over a window, oldest first, which is the order a ledger is
    /// read in and the order a running balance has to be accumulated in.
    /// </summary>
    Task<IReadOnlyList<LedgerLineRow>> GetLedgerLinesAsync(
        Guid accountId,
        DateOnly from,
        DateOnly to,
        Guid? detailAccountId = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// What each account carried before a date, for the opening column of a ledger or a trial
    /// balance. Everything posted up to the day before, netted.
    /// </summary>
    Task<IReadOnlyList<AccountTurnoverRow>> GetTurnoverByAccountAsync(
        DateOnly? from,
        DateOnly to,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>One posted line, flattened for a ledger.</summary>
public sealed record LedgerLineRow(
    Guid DocumentId,
    string? DocumentNumber,
    DateTimeOffset FinancialEventDateTime,
    string Title,
    string? Description,
    decimal Debtor,
    decimal Creditor);

/// <summary>What one account moved over a window.</summary>
public sealed record AccountTurnoverRow(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debtor,
    decimal Creditor);
