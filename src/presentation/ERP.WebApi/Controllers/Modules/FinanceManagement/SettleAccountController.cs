using ERP.Application.UseCases.Modules.FinanceManagement.Commands.SettleAccount;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Settlement/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.SettlementSwaggerGroup)]
    public class SettleAccountController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<SettlementDto>(mediator, logger)
    {
        /// <summary>
        /// Record money in or out and apply it against what it pays off
        /// </summary>
        /// <remarks>
        /// Receiving the money and deciding which invoices it settles are one act: split apart,
        /// cash sits unapplied and a customer who has paid still shows as overdue. The open items
        /// are settled in the same transaction, so the two records cannot disagree. Naming no
        /// allocations leaves the money on account, which is right when a customer pays early.
        /// </remarks>
        [HttpPost(Name = "SettleAccount")]
        public async Task<IActionResult> Index([FromBody] SettleAccountDto dto)
        {
            var command = new SettleAccountCommand
            {
                Direction = dto.Direction,
                Method = dto.Method,
                PartyDetailAccountId = dto.PartyDetailAccountId,
                Amount = dto.Amount,
                CurrencyCode = dto.CurrencyCode,
                SettlementDate = dto.SettlementDate,
                Allocations = dto.Allocations,
                CashBoxId = dto.CashBoxId,
                BankBranchAccountId = dto.BankBranchAccountId,
                ChequeId = dto.ChequeId,
                DocumentId = dto.DocumentId,
                BranchId = dto.BranchId,
                Number = dto.Number,
                TrackingCode = dto.TrackingCode,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
