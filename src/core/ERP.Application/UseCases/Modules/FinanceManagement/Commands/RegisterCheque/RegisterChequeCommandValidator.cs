using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RegisterCheque
{
    public class RegisterChequeCommandValidator : AbstractValidator<RegisterChequeCommand>
    {
        public RegisterChequeCommandValidator()
        {
            RuleFor(c => c.Type).IsInEnum();
            RuleFor(c => c.Number).NotEmpty().WithMessage("A cheque needs its number.");
            RuleFor(c => c.Amount).GreaterThan(0m).WithMessage("A cheque for nothing is not a cheque.");

            RuleFor(c => c.CurrencyCode)
                .NotEmpty().WithMessage("A currency is required.")
                .Matches("^[A-Za-z]{3}$").WithMessage("A currency code is three letters.");

            RuleFor(c => c.DueDate)
                .GreaterThanOrEqualTo(c => c.IssueDate)
                .WithMessage("A cheque cannot fall due before it was written.");

            // Sixteen digits, and only digits. A malformed one is worth catching here rather than
            // after it has been filed with the bank.
            RuleFor(c => c.SayadNumber)
                .Matches("^[0-9]{16}$")
                .WithMessage("A Sayad number is sixteen digits.")
                .When(c => !string.IsNullOrWhiteSpace(c.SayadNumber));

            // Which side the cheque is on decides which party has to be named. A received cheque
            // without a drawer cannot be chased if it bounces.
            RuleFor(c => c.DrawerName)
                .NotEmpty()
                .WithMessage("A received cheque needs the name of whoever wrote it.")
                .When(c => c.Type == ChequeType.Received);

            RuleFor(c => c.PayeeName)
                .NotEmpty()
                .WithMessage("An issued cheque needs a payee.")
                .When(c => c.Type == ChequeType.Issued);
        }
    }
}
