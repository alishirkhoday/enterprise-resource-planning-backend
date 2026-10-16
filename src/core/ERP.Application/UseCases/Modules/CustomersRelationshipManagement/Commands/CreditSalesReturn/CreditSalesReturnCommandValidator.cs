using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CreditSalesReturn
{
    public class CreditSalesReturnCommandValidator : AbstractValidator<CreditSalesReturnCommand>
    {
        public CreditSalesReturnCommandValidator()
        {
            RuleFor(r => r.OriginalInvoiceId).NotEmpty().WithMessage("Which invoice is being credited?");
            RuleFor(r => r.IssueDate).NotEqual(default(DateOnly)).WithMessage("A credit note needs a date.");

            // A credit note is a refund in accounting terms. Somebody will be asked why, possibly
            // by an auditor, and the answer belongs on the document rather than in a memory.
            RuleFor(r => r.Reason)
                .NotEmpty().WithMessage("A credit note needs a reason.")
                .MaximumLength(500);

            // An empty list is meaningful - it credits the whole invoice - so only the lines that
            // were named are checked.
            RuleForEach(r => r.Lines).SetValidator(new SalesReturnLineValidator());

            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => l.InvoiceItemId).Distinct().Count() == lines.Count)
                .WithMessage("The same line is credited twice.");
        }
    }

    public class SalesReturnLineValidator : AbstractValidator<SalesReturnLineDto>
    {
        public SalesReturnLineValidator()
        {
            RuleFor(l => l.InvoiceItemId).NotEmpty().WithMessage("Every line names an invoice line.");
            RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("Crediting nothing is not a credit.");
        }
    }
}
