using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Mapping
{
    public static class PayrollMapper
    {
        public static PayrollDto ToDto(this Payroll payroll)
        {
            ArgumentNullException.ThrowIfNull(payroll, nameof(payroll));

            return new PayrollDto(
                payroll.Id,
                payroll.HumanId,
                payroll.Human.FullName,
                payroll.StartDate,
                payroll.EndDate,
                payroll.Status,
                payroll.CurrencyCode,
                payroll.BaseSalary.Amount,
                payroll.GrossPay.Amount,
                payroll.TotalDeductions.Amount,
                payroll.NetPayable.Amount,
                [.. payroll.Items.Select(i => new PayrollLineViewDto(i.Code, i.Title, i.Nature, i.Amount.Amount))]);
        }
    }
}
