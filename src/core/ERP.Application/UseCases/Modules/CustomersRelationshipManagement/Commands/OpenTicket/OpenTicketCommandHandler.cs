using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.OpenTicket
{
    /// <summary>
    /// Opens a support ticket and starts the clock on it.
    /// <para>
    /// The clock is the point. A ticket without a promised response time is a note; one with a
    /// promised time is something the desk can be measured against, and the promise comes from the
    /// subject rather than from whoever opened it.
    /// </para>
    /// </summary>
    public class OpenTicketCommandHandler(
        ICustomerRepository customers,
        ITicketSubjectRepository subjects,
        ITicketRepository tickets,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<OpenTicketCommand, Result<TicketDto, Error>>
    {
        private const string TicketNumberingKey = "Ticket";

        public async Task<Result<TicketDto, Error>> Handle(OpenTicketCommand request, CancellationToken cancellationToken)
        {
            var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return Result.Failure<TicketDto, Error>(Errors.General.NotFound(request.CustomerId.ToString(), nameof(Customer)));

            var subject = await subjects.GetWithAncestorsAsync(request.SubjectId, cancellationToken);
            if (subject is null)
                return Result.Failure<TicketDto, Error>(Errors.General.NotFound(request.SubjectId.ToString(), nameof(TicketSubject)));

            // A retired subject stays on the old tickets filed under it; what it stops is new ones
            // arriving somewhere nobody is watching any more.
            if (!subject.IsActive)
                return Result.Failure<TicketDto, Error>(Errors.Support.TicketSubjectIsInactive(subject.Title));

            var series = await numberingSeries.GetForUpdateAsync(TicketNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<TicketDto, Error>(Errors.Order.NumberingSeriesMissing(TicketNumberingKey));

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var ticket = new Ticket(series.Next(today), customer, subject, request.Title, request.Channel);

            ticket.SetPriority(request.Priority ?? subject.DefaultPriority);

            if (request.BranchId is not null)
                ticket.AssignToBranch(request.BranchId.Value);

            if (request.ReferenceType is not null && request.ReferenceId is not null)
                ticket.SetReference(request.ReferenceType, request.ReferenceId);

            // The subject's service level, or its nearest ancestor's. A subject nobody gave one to
            // leaves the ticket without a due time rather than inventing a promise.
            var serviceLevel = subject.EffectiveServiceLevel();
            if (serviceLevel is not null)
                ticket.ApplyServiceLevel(serviceLevel.Value.Response, serviceLevel.Value.Resolution);

            // The first message is the customer's, so it must not count as a response - assigning
            // it afterwards is what keeps the response clock running.
            ticket.AddMessage(new TicketMessage(
                ticket,
                TicketMessageCreatorType.Customer,
                customer.Id.ToString(),
                request.Message));

            var assignee = request.AssignedToHumanId ?? subject.DefaultAssigneeHumanId;
            if (assignee is not null)
                ticket.AssignTo(assignee.Value);

            await tickets.CreateAsync(ticket, cancellationToken);

            return Result.Success<TicketDto, Error>(ticket.ToDto());
        }
    }
}
