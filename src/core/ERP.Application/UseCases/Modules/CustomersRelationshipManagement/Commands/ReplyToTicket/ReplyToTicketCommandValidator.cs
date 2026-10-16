namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ReplyToTicket
{
    public class ReplyToTicketCommandValidator : AbstractValidator<ReplyToTicketCommand>
    {
        public ReplyToTicketCommandValidator()
        {
            RuleFor(r => r.TicketId).NotEmpty().WithMessage("Which ticket?");

            RuleFor(r => r.Message)
                .NotEmpty().WithMessage("An empty reply is not a reply.")
                .MaximumLength(4000);

            RuleFor(r => r.CreatorType).IsInEnum();
        }
    }
}
