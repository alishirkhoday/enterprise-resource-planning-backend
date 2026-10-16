using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ResolveTicket
{
    /// <summary>
    /// Records what was done about a ticket, and optionally closes it in the same step.
    /// <para>
    /// Resolved and closed are kept apart because they answer different questions: resolved is the
    /// desk saying it is done, closed is the customer no longer disputing it. Desks that do not
    /// wait for the customer say so with <c>CloseImmediately</c> rather than by conflating the two.
    /// </para>
    /// </summary>
    public class ResolveTicketCommandHandler(ITicketRepository tickets)
        : IRequestHandler<ResolveTicketCommand, Result<TicketDto, Error>>
    {
        public async Task<Result<TicketDto, Error>> Handle(ResolveTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetWithMessagesAsync(request.TicketId, cancellationToken);
            if (ticket is null)
                return Result.Failure<TicketDto, Error>(Errors.General.NotFound(request.TicketId.ToString(), nameof(Ticket)));

            try
            {
                ticket.Resolve(request.Resolution);

                if (request.CloseImmediately)
                    ticket.Close();
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
