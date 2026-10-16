namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ResolveTicket
{
    public class ResolveTicketCommandValidator : AbstractValidator<ResolveTicketCommand>
    {
        public ResolveTicketCommandValidator()
        {
            RuleFor(r => r.TicketId).NotEmpty().WithMessage("Which ticket?");

            // The resolution is what the next person reads when the same problem comes back.
            RuleFor(r => r.Resolution)
                .NotEmpty().WithMessage("What was done about it?")
                .MaximumLength(2000);
        }
    }
}
