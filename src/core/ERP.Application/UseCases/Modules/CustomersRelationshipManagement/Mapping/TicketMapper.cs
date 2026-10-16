using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Mapping
{
    public static class TicketMapper
    {
        public static TicketDto ToDto(this Ticket ticket)
        {
            ArgumentNullException.ThrowIfNull(ticket, nameof(ticket));

            return new TicketDto(
                ticket.Id,
                ticket.Number,
                ticket.CustomerId,
                ticket.Customer.DisplayName,
                ticket.SubjectId,
                ticket.Subject.Title,
                ticket.Title,
                ticket.Status,
                ticket.Priority,
                ticket.Channel,
                ticket.AssignedToHumanId,
                ticket.ResponseDueAt,
                ticket.ResolutionDueAt,
                ticket.FirstRespondedAt,
                ticket.ResolvedAt,
                ticket.ClosedAt,
                ticket.HasBreachedResponseTarget,
                ticket.HasBreachedResolutionTarget,
                ticket.SatisfactionRating,
                ticket.Resolution,
                [.. ticket.Messages
                    .OrderBy(m => m.CreationDateTime)
                    .Select(m => new TicketMessageDto(
                        m.Id,
                        m.CreatorType,
                        m.CreatorId,
                        m.Message,
                        m.CreationDateTime,
                        m.ReplayMessageId))]);
        }
    }
}
