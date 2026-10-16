using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.CompleteServiceOrder;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.AfterSalesServiceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/ServiceOrder/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AfterSalesSwaggerGroup)]
    public class CompleteServiceOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ServiceOrderDto>(mediator, logger)
    {
        /// <summary>
        /// Close a visit
        /// </summary>
        /// <remarks>
        /// Parts leave stock whoever is paying. A part fitted under warranty is still gone from
        /// the shelf; it simply never reaches the bill.
        /// </remarks>
        [HttpPost(Name = "CompleteServiceOrder")]
        public async Task<IActionResult> Index([FromBody] CompleteServiceOrderDto dto)
        {
            var command = new CompleteServiceOrderCommand
            {
                ServiceOrderId = dto.ServiceOrderId,
                CompletedOn = dto.CompletedOn,
                WorkPerformed = dto.WorkPerformed,
                LabourHours = dto.LabourHours,
                LabourCharge = dto.LabourCharge,
                TravelCharge = dto.TravelCharge,
                Parts = dto.Parts
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
