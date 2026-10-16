namespace ERP.Application.UseCases.Modules.FinanceManagement.DTOs
{
    /// <summary>
    /// One account's movements over a window. The opening balance is carried separately from the
    /// rows, because without it the running balance means nothing.
    /// </summary>
    public sealed record AccountLedgerDto(
        Guid AccountId,
        string AccountCode,
        string AccountName,
        DateOnly From,
        DateOnly To,
        decimal OpeningBalance,
        decimal TotalDebtor,
        decimal TotalCreditor,
        decimal ClosingBalance,
        IReadOnlyList<AccountLedgerRowDto> Rows);

    public sealed record AccountLedgerRowDto(
        Guid DocumentId,
        string? DocumentNumber,
        DateTimeOffset FinancialEventDateTime,
        string Title,
        string? Description,
        decimal Debtor,
        decimal Creditor,
        decimal RunningBalance);

    /// <summary>
    /// The trial balance: every account that moved, with what it carried in, what moved and what
    /// it carries out. If the debit and credit totals differ, the books do not balance.
    /// </summary>
    public sealed record TrialBalanceDto(
        DateOnly From,
        DateOnly To,
        decimal TotalDebtor,
        decimal TotalCreditor,
        bool IsBalanced,
        IReadOnlyList<TrialBalanceRowDto> Rows);

    public sealed record TrialBalanceRowDto(
        Guid AccountId,
        string AccountCode,
        string AccountName,
        decimal OpeningBalance,
        decimal Debtor,
        decimal Creditor,
        decimal ClosingBalance);
}
