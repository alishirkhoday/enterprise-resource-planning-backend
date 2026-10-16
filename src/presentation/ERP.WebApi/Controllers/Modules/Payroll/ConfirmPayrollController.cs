using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.ConfirmPayroll;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.Payroll
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Payroll/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PayrollSwaggerGroup)]
    public class ConfirmPayrollController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<PayrollDto>(mediator, logger)
    {
        /// <summary>
        /// Make a payroll final
        /// </summary>
        /// <remarks>
        /// Refused if the net pay comes out negative, which is a data problem rather than a
        /// payslip - confirming it would send somebody a demand for money.
        /// </remarks>
        [HttpPost("{payrollId:guid}", Name = "ConfirmPayroll")]
        public async Task<IActionResult> Index(Guid payrollId)
        {
            return FromResult(await _mediator.Send(new ConfirmPayrollCommand { PayrollId = payrollId }));
        }
    }
}
