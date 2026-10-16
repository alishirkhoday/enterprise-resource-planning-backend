namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RegisterLead
{
    public class RegisterLeadCommandValidator : AbstractValidator<RegisterLeadCommand>
    {
        public RegisterLeadCommandValidator()
        {
            RuleFor(r => r.FullName)
                .NotEmpty().WithMessage("A lead needs a name.")
                .MaximumLength(200);

            // The number is what a salesperson actually uses, and what catches the duplicate.
            // A lead with no way to reach them is a row, not a lead.
            RuleFor(r => r.MobilePhoneNumberWithRegionCode)
                .NotEmpty().WithMessage("How do we reach them?")
                .MaximumLength(20);

            RuleFor(r => r.Source).IsInEnum();

            RuleFor(r => r.Email)
                .EmailAddress().WithMessage("That is not an email address.")
                .When(r => !string.IsNullOrWhiteSpace(r.Email));

            RuleFor(r => r.CompanyName).MaximumLength(200);
            RuleFor(r => r.JobTitle).MaximumLength(150);
            RuleFor(r => r.Note).MaximumLength(2000);
        }
    }
}
