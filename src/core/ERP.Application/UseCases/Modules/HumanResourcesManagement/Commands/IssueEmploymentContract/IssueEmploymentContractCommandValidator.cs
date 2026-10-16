namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.IssueEmploymentContract
{
    public class IssueEmploymentContractCommandValidator : AbstractValidator<IssueEmploymentContractCommand>
    {
        public IssueEmploymentContractCommandValidator()
        {
            RuleFor(c => c.HumanId).NotEmpty().WithMessage("Whose contract?");
            RuleFor(c => c.PositionId).NotEmpty().WithMessage("A contract needs a position.");
            RuleFor(c => c.EmploymentType).IsInEnum();
            RuleFor(c => c.StartDate).NotEqual(default(DateOnly)).WithMessage("A contract needs a start date.");
            RuleFor(c => c.BaseSalary).GreaterThan(0m).WithMessage("A contract needs a salary.");

            RuleFor(c => c.CurrencyCode)
                .NotEmpty().WithMessage("A currency is required.")
                .Matches("^[A-Za-z]{3}$").WithMessage("A currency code is three letters.");

            RuleFor(c => c.EndDate)
                .GreaterThan(c => c.StartDate)
                .WithMessage("A contract cannot end before it begins.")
                .When(c => c.EndDate is not null);

            // Probation runs from the start of the contract, so an end date before it describes a
            // probation that was over before anybody turned up.
            RuleFor(c => c.ProbationEndDate)
                .GreaterThan(c => c.StartDate)
                .WithMessage("Probation cannot end before the contract begins.")
                .When(c => c.ProbationEndDate is not null);

            RuleFor(c => c.WeeklyWorkingHours)
                .InclusiveBetween(1m, 168m)
                .WithMessage("Weekly hours have to fit inside a week.")
                .When(c => c.WeeklyWorkingHours is not null);
        }
    }
}
