using ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateJournalEntry;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/JournalEntry/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.JournalEntrySwaggerGroup)]
    public class CreateJournalEntryController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<JournalEntryDto>(mediator, logger)
    {
        /// <summary>
        /// Open a journal entry as a draft
        /// </summary>
        /// <remarks>
        /// The entry is not posted here. It has to balance and every line has to name a final,
        /// unlocked account; posting is a separate call so there is a moment in which somebody
        /// can look at it, because a posted entry can only be reversed afterwards.
        /// </remarks>
        [HttpPost(Name = "CreateJournalEntry")]
        public async Task<IActionResult> Index([FromBody] CreateJournalEntryDto dto)
        {
            var command = new CreateJournalEntryCommand
            {
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                FinancialEventDateTime = dto.FinancialEventDateTime,
                Lines = dto.Lines,
                BranchId = dto.BranchId,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
