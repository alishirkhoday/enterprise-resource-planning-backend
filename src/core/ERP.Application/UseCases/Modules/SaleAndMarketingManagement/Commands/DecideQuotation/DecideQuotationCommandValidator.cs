namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.DecideQuotation
{
    public class DecideQuotationCommandValidator : AbstractValidator<DecideQuotationCommand>
    {
        public DecideQuotationCommandValidator()
        {
            RuleFor(r => r.QuotationId).NotEmpty().WithMessage("Which quotation?");

            RuleFor(r => r.RejectionReason)
                .NotEmpty().WithMessage("Why was it rejected?")
                .MaximumLength(1000)
                .When(r => !r.IsAccepted);
        }
    }
}
