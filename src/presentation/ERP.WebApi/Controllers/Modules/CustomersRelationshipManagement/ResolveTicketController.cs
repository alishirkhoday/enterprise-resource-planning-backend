using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ResolveTicket;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CustomersRelationshipManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Ticket/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.TicketSwaggerGroup)]
    public class ResolveTicketController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<TicketDto>(mediator, logger)
    {
        /// <summary>
        /// Record what was done about a ticket
        /// </summary>
        /// <remarks>
        /// Resolved and closed answer different questions: resolved is the desk saying it is done,
        /// closed is the customer no longer disputing it. A desk that does not wait for the
        /// customer sets CloseImmediately.
        /// </remarks>
        [HttpPost(Name = "ResolveTicket")]
        public async Task<IActionResult> Index([FromBody] ResolveTicketDto dto)
        {
            var command = new ResolveTicketCommand
            {
                TicketId = dto.TicketId,
                Resolution = dto.Resolution,
                CloseImmediately = dto.CloseImmediately
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
