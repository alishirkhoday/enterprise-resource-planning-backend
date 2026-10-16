using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs
{
    public record OpenTicketDto
    {
        public Guid CustomerId { get; init; }

        public Guid SubjectId { get; init; }

        public string Title { get; init; } = default!;

        /// <summary>What the customer actually wrote. It becomes the ticket's first message.</summary>
        public string Message { get; init; } = default!;

        public TicketChannel Channel { get; init; } = TicketChannel.Portal;

        /// <summary>
        /// Optional. Left empty the subject's default priority applies, which is the agreed one
        /// rather than the one the person opening the ticket feels it deserves.
        /// </summary>
        public TicketPriority? Priority { get; init; }

        public Guid? AssignedToHumanId { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>What the ticket is about, such as an order or an invoice.</summary>
        public string? ReferenceType { get; init; }

        public string? ReferenceId { get; init; }
    }

    public record ReplyToTicketDto
    {
        public Guid TicketId { get; init; }

        public string Message { get; init; } = default!;

        /// <summary>
        /// Which side of the conversation this is. The author's identity is taken from the caller
        /// rather than from here, so a message can never be attributed to somebody else.
        /// </summary>
        public TicketMessageCreatorType CreatorType { get; init; } = TicketMessageCreatorType.Human;

        public string? ReplayMessageId { get; init; }

        /// <summary>
        /// Puts the ticket into awaiting-customer, for a reply that asks a question rather than
        /// answering one. A customer's own message never does this.
        /// </summary>
        public bool AwaitCustomer { get; init; }
    }

    public record ResolveTicketDto
    {
        public Guid TicketId { get; init; }

        public string Resolution { get; init; } = default!;

        /// <summary>
        /// Closes it in the same step. Support desks that do not wait for the customer to confirm
        /// would otherwise leave every ticket resolved and never closed.
        /// </summary>
        public bool CloseImmediately { get; init; }
    }

    public record CloseTicketDto
    {
        public Guid TicketId { get; init; }

        /// <summary>How the customer rated the handling, out of five. Optional.</summary>
        public int? SatisfactionRating { get; init; }
    }

    public sealed record TicketDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        string CustomerName,
        Guid SubjectId,
        string SubjectTitle,
        string Title,
        TicketStatus Status,
        TicketPriority Priority,
        TicketChannel Channel,
        Guid? AssignedToHumanId,
        DateTimeOffset? ResponseDueAt,
        DateTimeOffset? ResolutionDueAt,
        DateTimeOffset? FirstRespondedAt,
        DateTimeOffset? ResolvedAt,
        DateTimeOffset? ClosedAt,
        bool HasBreachedResponseTarget,
        bool HasBreachedResolutionTarget,
        int? SatisfactionRating,
        string? Resolution,
        IReadOnlyList<TicketMessageDto> Messages);

    public sealed record TicketMessageDto(
        Guid Id,
        TicketMessageCreatorType CreatorType,
        string CreatorId,
        string Message,
        DateTimeOffset WrittenAt,
        string? ReplayMessageId);
}
