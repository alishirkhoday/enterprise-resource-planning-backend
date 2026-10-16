using ERP.Application.UseCases.Modules.FinanceManagement.Commands.ReverseJournalEntry;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/JournalEntry/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.JournalEntrySwaggerGroup)]
    public class ReverseJournalEntryController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<JournalEntryDto>(mediator, logger)
    {
        /// <summary>
        /// Reverse a posted entry
        /// </summary>
        /// <remarks>
        /// Writes the mirror image and posts it immediately, linking the two. The reason is
        /// required: an unexplained reversal is the first thing an auditor asks about.
        /// </remarks>
        [HttpPost("{documentId:guid}", Name = "ReverseJournalEntry")]
        public async Task<IActionResult> Index(Guid documentId, [FromBody] ReverseJournalEntryRequest body)
        {
            var command = new ReverseJournalEntryCommand
            {
                DocumentId = documentId,
                Reason = body.Reason,
                ReverseOn = body.ReverseOn
            };

            return FromResult(await _mediator.Send(command));
        }

        public sealed record ReverseJournalEntryRequest(string Reason, DateTimeOffset? ReverseOn);
    }
}
