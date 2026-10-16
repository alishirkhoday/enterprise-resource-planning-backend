using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RaiseServiceOrder;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.AfterSalesServiceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/ServiceOrder/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AfterSalesSwaggerGroup)]
    public class RaiseServiceOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ServiceOrderDto>(mediator, logger)
    {
        /// <summary>
        /// Raise a service visit
        /// </summary>
        /// <remarks>
        /// Who pays is worked out here, not sent in: warranty first, then a service contract, then
        /// the customer. A caller who could choose would always choose free.
        /// </remarks>
        [HttpPost(Name = "RaiseServiceOrder")]
        public async Task<IActionResult> Index([FromBody] RaiseServiceOrderDto dto)
        {
            var command = new RaiseServiceOrderCommand
            {
                CustomerId = dto.CustomerId,
                ReportedFault = dto.ReportedFault,
                CurrencyCode = dto.CurrencyCode,
                ItemId = dto.ItemId,
                SerialNumberId = dto.SerialNumberId,
                TicketId = dto.TicketId,
                Priority = dto.Priority,
                ScheduledOn = dto.ScheduledOn,
                AssignedToHumanId = dto.AssignedToHumanId,
                WarehouseId = dto.WarehouseId,
                BranchId = dto.BranchId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
