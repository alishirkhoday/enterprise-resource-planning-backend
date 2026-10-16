using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateJournalEntry
{
    public record CreateJournalEntryCommand : CreateJournalEntryDto, ICommand<Result<JournalEntryDto, Error>>;
}
