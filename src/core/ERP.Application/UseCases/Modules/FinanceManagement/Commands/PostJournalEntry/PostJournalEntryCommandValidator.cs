namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.PostJournalEntry
{
    public class PostJournalEntryCommandValidator : AbstractValidator<PostJournalEntryCommand>
    {
        public PostJournalEntryCommandValidator()
        {
            RuleFor(e => e.DocumentId).NotEmpty().WithMessage("Which entry?");
        }
    }
}
