namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.OpenTicket
{
    public class OpenTicketCommandValidator : AbstractValidator<OpenTicketCommand>
    {
        public OpenTicketCommandValidator()
        {
            RuleFor(r => r.CustomerId).NotEmpty().WithMessage("Who is reporting this?");
            RuleFor(r => r.SubjectId).NotEmpty().WithMessage("What is it about?");

            RuleFor(r => r.Title)
                .NotEmpty().WithMessage("A ticket needs a title.")
                .MaximumLength(200);

            RuleFor(r => r.Message)
                .NotEmpty().WithMessage("A ticket with no message says nothing.")
                .MaximumLength(4000);

            RuleFor(r => r.ReferenceId)
                .NotEmpty()
                .WithMessage("A reference needs both a type and an identifier.")
                .When(r => !string.IsNullOrWhiteSpace(r.ReferenceType));

            RuleFor(r => r.ReferenceType)
                .NotEmpty()
                .WithMessage("A reference needs both a type and an identifier.")
                .When(r => !string.IsNullOrWhiteSpace(r.ReferenceId));
        }
    }
}
