using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RecordAttendance;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    public class RecordAttendanceController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<AttendanceRecordDto>(mediator, logger)
    {
        /// <summary>
        /// Clock somebody in or out
        /// </summary>
        /// <remarks>
        /// One day is one record. A second call for the same day fills in the half that was
        /// missing rather than starting a second working day.
        /// </remarks>
        [HttpPost(Name = "RecordAttendance")]
        public async Task<IActionResult> Index([FromBody] RecordAttendanceDto dto)
        {
            var command = new RecordAttendanceCommand
            {
                HumanId = dto.HumanId,
                Date = dto.Date,
                EntryTime = dto.EntryTime,
                ExitTime = dto.ExitTime
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
