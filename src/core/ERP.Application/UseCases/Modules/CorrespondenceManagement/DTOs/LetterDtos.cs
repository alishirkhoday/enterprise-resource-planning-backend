using ERP.Domain.Entities.Modules.CorrespondenceManagement;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs
{
    /// <summary>Enters a letter in the register and gives it its number.</summary>
    public record RegisterLetterDto
    {
        public LetterDirection Direction { get; init; }

        public string Subject { get; init; } = default!;

        public string CorrespondentName { get; init; } = default!;

        public DateOnly LetterDate { get; init; }

        public string? Body { get; init; }

        public LetterConfidentiality Confidentiality { get; init; } = LetterConfidentiality.Normal;

        public LetterUrgency Urgency { get; init; } = LetterUrgency.Normal;

        /// <summary>The correspondent's own reference, quoted when replying.</summary>
        public string? CorrespondentReference { get; init; }

        public DateOnly? CorrespondentReferenceDate { get; init; }

        public IReadOnlyList<LetterRecipientDto> Recipients { get; init; } = [];

        public Guid? CustomerId { get; init; }

        public Guid? SupplierId { get; init; }

        public Guid? HumanId { get; init; }

        /// <summary>The letter this one answers.</summary>
        public Guid? InReplyToLetterId { get; init; }

        /// <summary>An earlier letter in the same thread that this one continues.</summary>
        public Guid? FollowsLetterId { get; init; }

        public DateOnly? DueDate { get; init; }

        public Guid? OwnerUserId { get; init; }

        public Guid? BranchId { get; init; }

        public string? Description { get; init; }
    }

    public record LetterRecipientDto
    {
        public LetterRecipientKind Kind { get; init; }

        public string Name { get; init; } = default!;

        public string? Organization { get; init; }

        public string? PostalAddress { get; init; }

        public string? Email { get; init; }

        public Guid? UserId { get; init; }

        public Guid? HumanId { get; init; }
    }

    public sealed record LetterDto(
        Guid Id,
        string Number,
        LetterDirection Direction,
        LetterStatus Status,
        string Subject,
        string CorrespondentName,
        DateOnly LetterDate,
        DateTimeOffset RegisteredOn,
        LetterConfidentiality Confidentiality,
        LetterUrgency Urgency,
        DateOnly? DueDate,
        int OutstandingReferralCount,
        IReadOnlyList<LetterReferralDto> Referrals);

    public sealed record LetterReferralDto(
        Guid Id,
        Guid FromUserId,
        Guid ToUserId,
        string? Instruction,
        LetterReferralStatus Status,
        DateTimeOffset ReferredOn,
        DateOnly? DueDate,
        DateTimeOffset? SeenOn,
        DateTimeOffset? RespondedOn,
        string? Response);

    public record ReferLetterDto
    {
        public Guid LetterId { get; init; }

        public Guid ToUserId { get; init; }

        public Guid FromUserId { get; init; }

        /// <summary>The note written across the top of the letter.</summary>
        public string? Instruction { get; init; }

        public DateOnly? DueDate { get; init; }
    }

    public record AnswerReferralDto
    {
        public Guid ReferralId { get; init; }

        public string Response { get; init; } = default!;

        /// <summary>Set when taking the letter back rather than answering it.</summary>
        public bool IsWithdrawal { get; init; }
    }
}
