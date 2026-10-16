using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Mapping
{
    public static class ChequeMapper
    {
        public static ChequeDto ToDto(this Cheque cheque)
        {
            ArgumentNullException.ThrowIfNull(cheque, nameof(cheque));

            return new ChequeDto(
                cheque.Id,
                cheque.Type,
                cheque.Number,
                cheque.SayadNumber,
                cheque.Amount.Amount,
                cheque.Amount.CurrencyCode,
                cheque.IssueDate,
                cheque.DueDate,
                cheque.Status,
                cheque.DrawerName,
                cheque.PayeeName,
                cheque.DetailAccountId);
        }
    }
}
