namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.ReverseJournalEntry
{
    public class ReverseJournalEntryCommandValidator : AbstractValidator<ReverseJournalEntryCommand>
    {
        public ReverseJournalEntryCommandValidator()
        {
            RuleFor(e => e.DocumentId).NotEmpty().WithMessage("Which entry?");

            // An unexplained reversal is the one thing an auditor will always ask about, so the
            // reason is required rather than optional.
            RuleFor(e => e.Reason)
                .NotEmpty().WithMessage("A reversal needs a reason.")
                .MinimumLength(5).WithMessage("The reason has to say something.");
        }
    }
}
