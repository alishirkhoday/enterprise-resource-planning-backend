namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.DecideBudget
{
    public class DecideBudgetCommandValidator : AbstractValidator<DecideBudgetCommand>
    {
        public DecideBudgetCommandValidator()
        {
            RuleFor(r => r.BudgetId).NotEmpty().WithMessage("Which budget?");

            // An approval nobody signed is one nobody can be asked about later.
            RuleFor(r => r.ApprovedBy)
                .NotEmpty().WithMessage("Who is approving it?")
                .MaximumLength(200)
                .When(r => r.IsApproved);
        }
    }
}
