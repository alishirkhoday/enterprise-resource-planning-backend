using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping
{
    public static class ActivityMapper
    {
        public static ActivityDto ToDto(this Activity activity)
        {
            ArgumentNullException.ThrowIfNull(activity, nameof(activity));

            return new ActivityDto(
                activity.Id,
                activity.Subject,
                activity.Type,
                activity.Status,
                activity.Priority,
                activity.ReferenceType,
                activity.ReferenceId,
                activity.AssignedToHumanId,
                activity.DueAt,
                activity.RemindAt,
                activity.StartedAt,
                activity.CompletedAt,
                activity.DurationMinutes,
                activity.IsOverdue,
                activity.Outcome,
                activity.Description);
        }

        /// <summary>
        /// Builds an activity from a request. Shared by scheduling one outright and booking a
        /// follow up off the back of another, which are the same act at different moments.
        /// </summary>
        public static Activity ToEntity(this ScheduleActivityDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto, nameof(dto));

            var activity = new Activity(dto.Subject, dto.Type, dto.ReferenceType, dto.ReferenceId);

            activity.Schedule(dto.DueAt, dto.RemindAt);
            activity.SetPriority(dto.Priority);
            activity.AssignTo(dto.AssignedToHumanId);
            activity.SetDescription(dto.Description);

            if (dto.BranchId is not null)
                activity.AssignToBranch(dto.BranchId.Value);

            return activity;
        }
    }
}
