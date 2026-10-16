using ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.RegisterLetter;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.CorrespondenceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Letter/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.CorrespondenceSwaggerGroup)]
    public class RegisterLetterController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<LetterDto>(mediator, logger)
    {
        /// <summary>
        /// Enter a letter in the register
        /// </summary>
        /// <remarks>
        /// The registration number is issued here and never reissued. Inbound and outbound run in
        /// separate sequences, so a number always says which way the letter went.
        /// </remarks>
        [HttpPost(Name = "RegisterLetter")]
        public async Task<IActionResult> Index([FromBody] RegisterLetterDto dto)
        {
            var command = new RegisterLetterCommand
            {
                Direction = dto.Direction,
                Subject = dto.Subject,
                CorrespondentName = dto.CorrespondentName,
                LetterDate = dto.LetterDate,
                Body = dto.Body,
                Confidentiality = dto.Confidentiality,
                Urgency = dto.Urgency,
                CorrespondentReference = dto.CorrespondentReference,
                CorrespondentReferenceDate = dto.CorrespondentReferenceDate,
                Recipients = dto.Recipients,
                CustomerId = dto.CustomerId,
                SupplierId = dto.SupplierId,
                HumanId = dto.HumanId,
                InReplyToLetterId = dto.InReplyToLetterId,
                FollowsLetterId = dto.FollowsLetterId,
                DueDate = dto.DueDate,
                OwnerUserId = dto.OwnerUserId,
                BranchId = dto.BranchId,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
