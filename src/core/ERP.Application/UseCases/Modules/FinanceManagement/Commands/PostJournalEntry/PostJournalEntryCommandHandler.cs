using ERP.Application.Common.Interfaces.Identity;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Mapping;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.PostJournalEntry
{
    /// <summary>
    /// Makes an entry final.
    /// <para>
    /// Whether it may be posted is the document's own judgement - balanced, two lines or more, in
    /// a year that still accepts postings - so this loads it with everything that judgement needs
    /// and lets it answer. What it adds is the name of whoever asked, which is the one fact the
    /// domain cannot know.
    /// </para>
    /// </summary>
    public class PostJournalEntryCommandHandler(IDocumentRepository documents, ICurrentUser currentUser)
        : IRequestHandler<PostJournalEntryCommand, Result<JournalEntryDto, Error>>
    {
        public async Task<Result<JournalEntryDto, Error>> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
        {
            var document = await documents.GetWithItemsAsync(request.DocumentId, cancellationToken);
            if (document is null)
                return Result.Failure<JournalEntryDto, Error>(Errors.General.NotFound(request.DocumentId.ToString(), nameof(Document)));

            if (document.Status != DocumentStatus.Draft)
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.EntryIsNotADraft(document.Status.ToString()));

            if (!document.IsBalanced())
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.EntryIsOutOfBalance(document.TotalDebtor, document.TotalCreditor));

            try
            {
                document.Post(currentUser.UserName);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<JournalEntryDto, Error>(Errors.Finance.EntryCannotBePosted(exception.Message));
            }

            await documents.UpdateAsync(document, cancellationToken);

            return Result.Success<JournalEntryDto, Error>(document.ToDto());
        }
    }
}
