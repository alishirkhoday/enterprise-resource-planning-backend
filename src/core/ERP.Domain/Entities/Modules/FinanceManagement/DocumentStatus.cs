namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Whether a journal entry is still a draft or has reached the ledger - موقت یا قطعی. A
/// draft moves no balance and can still be edited.
/// </summary>
public enum DocumentStatus
{
    Draft = 0,
    Definite = 1
}
