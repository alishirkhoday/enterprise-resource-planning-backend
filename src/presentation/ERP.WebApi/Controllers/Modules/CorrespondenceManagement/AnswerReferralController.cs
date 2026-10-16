using ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.AnswerReferral;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CorrespondenceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Letter/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CorrespondenceSwaggerGroup)]
    public class AnswerReferralController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LetterDto>(mediator, logger)
    {
        /// <summary>
        /// Answer a referral, or take it back
        /// </summary>
        /// <remarks>
        /// Both close the same open item. Once nobody is holding the letter any more it closes
        /// itself, rather than waiting for somebody to remember to file it.
        /// </remarks>
        [HttpPost(Name = "AnswerReferral")]
        public async Task<IActionResult> Index([FromBody] AnswerReferralDto dto)
        {
            var command = new AnswerReferralCommand
            {
                ReferralId = dto.ReferralId,
                Response = dto.Response,
                IsWithdrawal = dto.IsWithdrawal
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
