using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CloseTicket;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CustomersRelationshipManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Ticket/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.TicketSwaggerGroup)]
    public class CloseTicketController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<TicketDto>(mediator, logger)
    {
        /// <summary>
        /// Close a resolved ticket
        /// </summary>
        /// <remarks>
        /// A rating is only accepted here, on the close, because that is the first point at which
        /// there is anything to rate.
        /// </remarks>
        [HttpPost(Name = "CloseTicket")]
        public async Task<IActionResult> Index([FromBody] CloseTicketDto dto)
        {
            var command = new CloseTicketCommand
            {
                TicketId = dto.TicketId,
                SatisfactionRating = dto.SatisfactionRating
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
