namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What a journal entry is for. Receipts and payments are separated from everything else
/// because cash movements are the entries most often reported on their own.
/// </summary>
public enum DocumentType
{
    Receive = 0,
    Payment = 1,
    Payroll = 2,
    Other = 99
}
