using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.OpenTicket;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CustomersRelationshipManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Ticket/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.TicketSwaggerGroup)]
    public class OpenTicketController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<TicketDto>(mediator, logger)
    {
        /// <summary>
        /// Open a support ticket
        /// </summary>
        /// <remarks>
        /// The promised response and resolution times come from the subject, or from the nearest
        /// ancestor of it that defines them, so only the root of a branch has to. Priority and
        /// assignee fall back to the same place when the request leaves them out.
        /// </remarks>
        [HttpPost(Name = "OpenTicket")]
        public async Task<IActionResult> Index([FromBody] OpenTicketDto dto)
        {
            var command = new OpenTicketCommand
            {
                CustomerId = dto.CustomerId,
                SubjectId = dto.SubjectId,
                Title = dto.Title,
                Message = dto.Message,
                Channel = dto.Channel,
                Priority = dto.Priority,
                AssignedToHumanId = dto.AssignedToHumanId,
                BranchId = dto.BranchId,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
