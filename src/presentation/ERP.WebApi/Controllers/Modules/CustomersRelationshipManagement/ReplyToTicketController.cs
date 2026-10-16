using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ReplyToTicket;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CustomersRelationshipManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Ticket/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.TicketSwaggerGroup)]
    public class ReplyToTicketController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<TicketDto>(mediator, logger)
    {
        /// <summary>
        /// Add a message to a ticket
        /// </summary>
        /// <remarks>
        /// The first message from the desk stops the response clock. The author is taken from the
        /// caller rather than from the request, so a message cannot be attributed to somebody else.
        /// </remarks>
        [HttpPost(Name = "ReplyToTicket")]
        public async Task<IActionResult> Index([FromBody] ReplyToTicketDto dto)
        {
            var command = new ReplyToTicketCommand
            {
                TicketId = dto.TicketId,
                Message = dto.Message,
                CreatorType = dto.CreatorType,
                ReplayMessageId = dto.ReplayMessageId,
                AwaitCustomer = dto.AwaitCustomer
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
