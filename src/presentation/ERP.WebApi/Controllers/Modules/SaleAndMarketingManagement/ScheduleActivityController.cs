using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ScheduleActivity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.SaleAndMarketingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/SalesActivity/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.SalesActivitySwaggerGroup)]
    public class ScheduleActivityController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ActivityDto>(mediator, logger)
    {
        /// <summary>
        /// Book a call, meeting or follow up
        /// </summary>
        /// <remarks>
        /// Every activity points at something - a lead, a deal, a customer - because one that
        /// belongs to nothing drops out of every list that matters and never gets done.
        /// </remarks>
        [HttpPost(Name = "ScheduleActivity")]
        public async Task<IActionResult> Index([FromBody] ScheduleActivityDto dto)
        {
            var command = new ScheduleActivityCommand
            {
                Subject = dto.Subject,
                Type = dto.Type,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                DueAt = dto.DueAt,
                AssignedToHumanId = dto.AssignedToHumanId,
                Priority = dto.Priority,
                RemindAt = dto.RemindAt,
                BranchId = dto.BranchId,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
