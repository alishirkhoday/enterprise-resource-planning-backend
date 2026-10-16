namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RecordChequeOperation
{
    public class RecordChequeOperationCommandValidator : AbstractValidator<RecordChequeOperationCommand>
    {
        public RecordChequeOperationCommandValidator()
        {
            RuleFor(c => c.ChequeId).NotEmpty().WithMessage("Which cheque?");
            RuleFor(c => c.Operation).IsInEnum();

            RuleFor(c => c.OccurredOn)
                .NotEqual(default(DateOnly))
                .WithMessage("When did it happen?");

            // Each operation carries its own required field. Checking them here means the handler
            // never has to unpack an operation that was going to be rejected anyway.
            RuleFor(c => c.BankAccountId)
                .NotNull().NotEqual(Guid.Empty)
                .WithMessage("A deposit needs the account it went into.")
                .When(c => c.Operation == ChequeOperationKind.Deposit);

            RuleFor(c => c.Reason)
                .NotEmpty()
                .WithMessage("A bounced cheque needs the reason the bank gave.")
                .When(c => c.Operation == ChequeOperationKind.Bounce);

            RuleFor(c => c.EndorsedTo)
                .NotEmpty()
                .WithMessage("An endorsement needs the name it was signed over to.")
                .When(c => c.Operation == ChequeOperationKind.Endorse);
        }
    }
}
