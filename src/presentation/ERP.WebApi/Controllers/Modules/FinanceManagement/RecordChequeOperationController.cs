using ERP.Application.UseCases.Modules.FinanceManagement.Commands.RecordChequeOperation;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Cheque/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ChequeSwaggerGroup)]
    public class RecordChequeOperationController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ChequeDto>(mediator, logger)
    {
        /// <summary>
        /// Move a cheque to its next state
        /// </summary>
        /// <remarks>
        /// Deposit, clear, bounce, endorse, issue, return or cancel. Which moves are legal from
        /// which state is the cheque's own rule: a cheque that has bounced cannot then clear.
        /// </remarks>
        [HttpPost("{chequeId:guid}", Name = "RecordChequeOperation")]
        public async Task<IActionResult> Index(Guid chequeId, [FromBody] RecordChequeOperationRequest body)
        {
            var command = new RecordChequeOperationCommand
            {
                ChequeId = chequeId,
                Operation = body.Operation,
                OccurredOn = body.OccurredOn,
                Reason = body.Reason,
                BankAccountId = body.BankAccountId,
                EndorsedTo = body.EndorsedTo
            };

            return FromResult(await _mediator.Send(command));
        }

        public sealed record RecordChequeOperationRequest(
            ChequeOperationKind Operation,
            DateOnly OccurredOn,
            string? Reason,
            Guid? BankAccountId,
            string? EndorsedTo);
    }
}
