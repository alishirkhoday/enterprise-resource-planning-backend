namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RegisterWarrantyClaim
{
    public class RegisterWarrantyClaimCommandValidator : AbstractValidator<RegisterWarrantyClaimCommand>
    {
        public RegisterWarrantyClaimCommandValidator()
        {
            RuleFor(c => c.CustomerId).NotEmpty().WithMessage("Whose claim?");
            RuleFor(c => c.ItemId).NotEmpty().WithMessage("Which item failed?");
            RuleFor(c => c.FaultDescription).NotEmpty().WithMessage("What went wrong?");

            RuleFor(c => c.FaultOccurredOn)
                .NotEqual(default(DateOnly))
                .WithMessage("When did it fail?");

            // Cover is judged on the failure date, so a claim reported before the fault happened
            // would be assessed against a window that had not opened yet.
            RuleFor(c => c.ReportedOn)
                .GreaterThanOrEqualTo(c => c.FaultOccurredOn)
                .WithMessage("A fault cannot be reported before it happened.");
        }
    }
}
