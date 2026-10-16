using ERP.Application.UseCases.Modules.FinanceManagement.Commands.RegisterCheque;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Cheque/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ChequeSwaggerGroup)]
    public class RegisterChequeController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ChequeDto>(mediator, logger)
    {
        /// <summary>
        /// Record a cheque, received or issued
        /// </summary>
        /// <remarks>
        /// A received cheque needs the name of whoever wrote it; an issued one needs a payee.
        /// Moving the cheque afterwards is a separate call.
        /// </remarks>
        [HttpPost(Name = "RegisterCheque")]
        public async Task<IActionResult> Index([FromBody] RegisterChequeDto dto)
        {
            var command = new RegisterChequeCommand
            {
                Type = dto.Type,
                Number = dto.Number,
                SayadNumber = dto.SayadNumber,
                Amount = dto.Amount,
                CurrencyCode = dto.CurrencyCode,
                IssueDate = dto.IssueDate,
                DueDate = dto.DueDate,
                BankName = dto.BankName,
                DrawerName = dto.DrawerName,
                AccountNumber = dto.AccountNumber,
                PayeeName = dto.PayeeName,
                DetailAccountId = dto.DetailAccountId,
                BranchId = dto.BranchId,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
