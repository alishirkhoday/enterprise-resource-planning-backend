using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RunPayroll;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.Payroll
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Payroll/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PayrollSwaggerGroup)]
    public class RunPayrollController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<PayrollDto>(mediator, logger)
    {
        /// <summary>
        /// Build a payroll for one person over one period
        /// </summary>
        /// <remarks>
        /// The salary is read from the contract in effect, not from the request. Running the same
        /// period twice is refused rather than merged.
        /// </remarks>
        [HttpPost(Name = "RunPayroll")]
        public async Task<IActionResult> Index([FromBody] RunPayrollDto dto)
        {
            var command = new RunPayrollCommand
            {
                HumanId = dto.HumanId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                WorkedDays = dto.WorkedDays,
                AbsentDays = dto.AbsentDays,
                PaidLeaveDays = dto.PaidLeaveDays,
                UnpaidLeaveDays = dto.UnpaidLeaveDays,
                OvertimeHours = dto.OvertimeHours,
                AdditionalLines = dto.AdditionalLines,
                SalaryStructureId = dto.SalaryStructureId,
                CostCenterId = dto.CostCenterId,
                BranchId = dto.BranchId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
