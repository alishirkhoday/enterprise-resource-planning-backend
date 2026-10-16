using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.DraftBudget
{
    public class DraftBudgetCommandValidator : AbstractValidator<DraftBudgetCommand>
    {
        public DraftBudgetCommandValidator()
        {
            RuleFor(r => r.FinancialYearId).NotEmpty().WithMessage("Which financial year?");

            RuleFor(r => r.Title)
                .NotEmpty().WithMessage("A budget needs a title.")
                .MaximumLength(200);

            RuleFor(r => r.Type).IsInEnum();

            RuleFor(r => r.CurrencyCode)
                .NotEmpty().WithMessage("Which currency?")
                .Length(3).WithMessage("A currency code is three letters.");

            RuleFor(r => r.Lines).NotEmpty().WithMessage("A budget with no lines controls nothing.");

            // Two lines for the same account and period would each be checked separately, so a
            // spend could fit one while blowing the other.
            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => (l.AccountId, l.PeriodNumber, l.CostCenterId)).Distinct().Count() == lines.Count)
                .WithMessage("The same account is budgeted twice for one period.");

            RuleFor(r => r.Description).MaximumLength(2000);

            RuleForEach(r => r.Lines).SetValidator(new BudgetLineValidator());
        }
    }

    public class BudgetLineValidator : AbstractValidator<BudgetLineDto>
    {
        public BudgetLineValidator()
        {
            RuleFor(l => l.AccountId).NotEmpty().WithMessage("Every line names an account.");

            RuleFor(l => l.PeriodNumber)
                .InclusiveBetween((short)1, (short)12)
                .WithMessage("A period runs from one to twelve.");

            // Zero is meaningful: budgeting nothing for an account is how a line is explicitly
            // closed off rather than left unbudgeted and therefore unchecked.
            RuleFor(l => l.PlannedAmount).GreaterThanOrEqualTo(0m).WithMessage("A budget cannot be negative.");

            RuleFor(l => l.Description).MaximumLength(500);
        }
    }
}
