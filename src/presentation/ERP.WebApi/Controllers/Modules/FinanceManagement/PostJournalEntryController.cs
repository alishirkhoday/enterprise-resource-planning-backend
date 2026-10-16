using ERP.Application.UseCases.Modules.FinanceManagement.Commands.PostJournalEntry;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/JournalEntry/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.JournalEntrySwaggerGroup)]
    public class PostJournalEntryController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<JournalEntryDto>(mediator, logger)
    {
        /// <summary>
        /// Make a draft entry final
        /// </summary>
        /// <remarks>After this the entry can only be reversed, never edited.</remarks>
        [HttpPost("{documentId:guid}", Name = "PostJournalEntry")]
        public async Task<IActionResult> Index(Guid documentId)
        {
            return FromResult(await _mediator.Send(new PostJournalEntryCommand { DocumentId = documentId }));
        }
    }
}
