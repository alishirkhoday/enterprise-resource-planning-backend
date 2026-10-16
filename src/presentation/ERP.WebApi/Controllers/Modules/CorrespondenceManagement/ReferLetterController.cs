using ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.ReferLetter;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CorrespondenceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Letter/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CorrespondenceSwaggerGroup)]
    public class ReferLetterController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LetterDto>(mediator, logger)
    {
        /// <summary>
        /// Refer a letter to somebody
        /// </summary>
        /// <remarks>
        /// The act the register exists to make traceable. Each hand-off survives the next one, so
        /// a letter that went nowhere can be answered for.
        /// </remarks>
        [HttpPost(Name = "ReferLetter")]
        public async Task<IActionResult> Index([FromBody] ReferLetterDto dto)
        {
            var command = new ReferLetterCommand
            {
                LetterId = dto.LetterId,
                ToUserId = dto.ToUserId,
                FromUserId = dto.FromUserId,
                Instruction = dto.Instruction,
                DueDate = dto.DueDate
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
