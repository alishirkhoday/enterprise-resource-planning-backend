using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateJournalEntry
{
    public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
    {
        public CreateJournalEntryCommandValidator()
        {
            RuleFor(e => e.Title).NotEmpty().WithMessage("An entry needs a title.");
            RuleFor(e => e.Description).NotEmpty().WithMessage("An entry needs a description.");
            RuleFor(e => e.Type).IsInEnum();

            RuleFor(e => e.FinancialEventDateTime)
                .NotEqual(default(DateTimeOffset))
                .WithMessage("An entry needs the moment the event happened.");

            // Double entry needs two sides. One line can only ever be unbalanced, so the shortest
            // real entry has two, and saying so here spares the caller a trip to the handler.
            RuleFor(e => e.Lines)
                .Must(lines => lines.Count >= 2)
                .WithMessage("A journal entry needs at least two lines.");

            RuleForEach(e => e.Lines).SetValidator(new JournalEntryLineValidator());

            RuleFor(e => e.Lines)
                .Must(lines => lines.Sum(l => l.Debtor) == lines.Sum(l => l.Creditor))
                .WithMessage("The entry is out of balance; debits and credits have to match.")
                .When(e => e.Lines.Count >= 2);

            RuleFor(e => e.Lines)
                .Must(lines => lines.Sum(l => l.Debtor) > 0m)
                .WithMessage("An entry that moves nothing is not an entry.")
                .When(e => e.Lines.Count >= 2);
        }
    }

    public class JournalEntryLineValidator : AbstractValidator<JournalEntryLineDto>
    {
        public JournalEntryLineValidator()
        {
            RuleFor(l => l.AccountId).NotEmpty().WithMessage("Every line needs an account.");
            RuleFor(l => l.Debtor).GreaterThanOrEqualTo(0m).WithMessage("A debit cannot be negative.");
            RuleFor(l => l.Creditor).GreaterThanOrEqualTo(0m).WithMessage("A credit cannot be negative.");

            // A line is one side or the other. Both at once is really two lines written as one,
            // and neither is a line that says nothing.
            RuleFor(l => l)
                .Must(l => (l.Debtor > 0m) ^ (l.Creditor > 0m))
                .WithMessage("Every line is either a debit or a credit, and has to be one of them.");
        }
    }
}
