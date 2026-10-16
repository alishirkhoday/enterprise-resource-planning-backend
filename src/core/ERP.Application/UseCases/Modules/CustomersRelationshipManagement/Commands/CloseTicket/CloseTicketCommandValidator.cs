namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CloseTicket
{
    public class CloseTicketCommandValidator : AbstractValidator<CloseTicketCommand>
    {
        public CloseTicketCommandValidator()
        {
            RuleFor(r => r.TicketId).NotEmpty().WithMessage("Which ticket?");

            RuleFor(r => r.SatisfactionRating)
                .InclusiveBetween(1, 5).WithMessage("A rating runs from one to five.")
                .When(r => r.SatisfactionRating is not null);
        }
    }
}
