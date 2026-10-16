using ERP.Application.Common.Interfaces.Identity;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ReplyToTicket
{
    /// <summary>
    /// Adds a message to a ticket.
    /// <para>
    /// The first message from the desk is what stops the response clock, and the domain records
    /// that rather than this handler: the fact belongs to the ticket, not to the way it was added.
    /// </para>
    /// </summary>
    public class ReplyToTicketCommandHandler(
        ITicketRepository tickets,
        ICurrentUser currentUser)
        : IRequestHandler<ReplyToTicketCommand, Result<TicketDto, Error>>
    {
        public async Task<Result<TicketDto, Error>> Handle(ReplyToTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetWithMessagesAsync(request.TicketId, cancellationToken);
            if (ticket is null)
                return Result.Failure<TicketDto, Error>(Errors.General.NotFound(request.TicketId.ToString(), nameof(Ticket)));

            // Who is speaking comes from the request; who they are does not. Taking the identity
            // from the caller is what stops a message being attributed to somebody else.
            var creatorId = request.CreatorType == TicketMessageCreatorType.Customer
                ? ticket.CustomerId.ToString()
                : currentUser.UserId?.ToString() ?? currentUser.UserName ?? "system";

            var message = new TicketMessage(ticket, request.CreatorType, creatorId, request.Message, request.ReplayMessageId);

            try
            {
                ticket.AddMessage(message);

                // A question back to the customer stops the desk's clock honestly: the ticket is
                // not waiting on support any more. A customer's own message never does this.
                if (request.AwaitCustomer && request.CreatorType != TicketMessageCreatorType.Customer)
                    ticket.AwaitCustomer();
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
