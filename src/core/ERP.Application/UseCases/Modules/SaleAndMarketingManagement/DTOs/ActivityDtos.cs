using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs
{
    /// <summary>A call, meeting or follow up to be made - فعالیت.</summary>
    public record ScheduleActivityDto
    {
        public string Subject { get; init; } = default!;

        public ActivityType Type { get; init; }

        /// <summary>What it is about: a Lead, an Opportunity, a Customer.</summary>
        public string ReferenceType { get; init; } = default!;

        public string ReferenceId { get; init; } = default!;

        public DateTimeOffset DueAt { get; init; }

        public Guid AssignedToHumanId { get; init; }

        public ActivityPriority Priority { get; init; } = ActivityPriority.Normal;

        /// <summary>When to nudge whoever owns it. Must not be after it is due.</summary>
        public DateTimeOffset? RemindAt { get; init; }

        public Guid? BranchId { get; init; }

        public string? Description { get; init; }
    }

    /// <summary>What came of it - ثبت نتیجه فعالیت.</summary>
    public record CompleteActivityDto
    {
        public Guid ActivityId { get; init; }

        /// <summary>
        /// What actually happened. A completed activity with no outcome tells the next person
        /// nothing, which is the same as it never having been done.
        /// </summary>
        public string Outcome { get; init; } = default!;

        public int? DurationMinutes { get; init; }

        /// <summary>Books the next step straight away, which is how a follow up stops slipping.</summary>
        public ScheduleActivityDto? FollowUp { get; init; }
    }

    public sealed record ActivityDto(
        Guid Id,
        string Subject,
        ActivityType Type,
        ActivityStatus Status,
        ActivityPriority Priority,
        string ReferenceType,
        string ReferenceId,
        Guid? AssignedToHumanId,
        DateTimeOffset? DueAt,
        DateTimeOffset? RemindAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        int? DurationMinutes,
        bool IsOverdue,
        string? Outcome,
        string? Description);

    /// <summary>The result of completing an activity, with the follow up if one was booked.</summary>
    public sealed record CompleteActivityResultDto(ActivityDto Activity, ActivityDto? FollowUp);
}
