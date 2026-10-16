using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.PostJournalEntry
{
    /// <summary>Makes a draft entry final. After this it can only be reversed, never edited.</summary>
    public sealed record PostJournalEntryCommand : ICommand<Result<JournalEntryDto, Error>>
    {
        public Guid DocumentId { get; init; }
    }
}
