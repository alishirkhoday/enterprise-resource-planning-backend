using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CloseTicket
{
    /// <summary>
    /// Closes a resolved ticket, and records the rating of the handling if one was given.
    /// </summary>
    public class CloseTicketCommandHandler(ITicketRepository tickets)
        : IRequestHandler<CloseTicketCommand, Result<TicketDto, Error>>
    {
        public async Task<Result<TicketDto, Error>> Handle(CloseTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetWithMessagesAsync(request.TicketId, cancellationToken);
            if (ticket is null)
                return Result.Failure<TicketDto, Error>(Errors.General.NotFound(request.TicketId.ToString(), nameof(Ticket)));

            try
            {
                ticket.Close();

                // Recorded after the close, because the domain only accepts a rating once the
                // ticket is closed - which is the only point at which there is anything to rate.
                if (request.SatisfactionRating is not null)
                    ticket.RecordSatisfaction(request.SatisfactionRating.Value);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<TicketDto, Error>(Errors.Support.TicketCannotBeChanged(exception.Message));
            }

            await tickets.UpdateAsync(ticket, cancellationToken);

            return Result.Success<TicketDto, Error>(ticket.ToDto());
        }
    }
}
