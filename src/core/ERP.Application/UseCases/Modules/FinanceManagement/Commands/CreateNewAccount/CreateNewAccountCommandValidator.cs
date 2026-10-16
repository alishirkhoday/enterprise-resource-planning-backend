namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateNewAccount
{
    public class CreateNewAccountCommandValidator : AbstractValidator<CreateNewAccountCommand>
    {
        public CreateNewAccountCommandValidator()
        {
            RuleFor(a => a.Name).NotEmpty().WithMessage("");
            RuleFor(a => a.Type).IsInEnum();
            RuleFor(a => a.Group).IsInEnum();
            RuleFor(a => a.Balance).IsInEnum();
            RuleFor(a => a.FinancialStatement).IsInEnum();
        }
    }
}
