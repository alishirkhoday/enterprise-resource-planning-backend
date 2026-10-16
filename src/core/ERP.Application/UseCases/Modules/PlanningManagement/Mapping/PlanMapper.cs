using ERP.Application.UseCases.Modules.PlanningManagement.DTOs;
using ERP.Domain.Entities.Modules.PlanningManagement;

namespace ERP.Application.UseCases.Modules.PlanningManagement.Mapping
{
    public static class PlanMapper
    {
        public static MaterialRequirementPlanDto ToDto(this MaterialRequirementPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan, nameof(plan));

            var shortages = plan.ShortageLines;

            return new MaterialRequirementPlanDto(
                plan.Id,
                plan.Code,
                plan.Horizon.StartDate,
                plan.Horizon.EndDate ?? plan.Horizon.StartDate,
                plan.Status,
                plan.StartedOn,
                plan.CompletedOn,
                plan.Lines.Count,
                shortages.Count,
                [.. shortages.Select(l => l.ToDto())]);
        }

        public static MaterialRequirementPlanLineDto ToDto(this MaterialRequirementPlanLine line)
        {
            ArgumentNullException.ThrowIfNull(line, nameof(line));

            return new MaterialRequirementPlanLineDto(
                line.Id,
                line.ItemId,
                line.Item.Name,
                line.RequiredOn,
                line.GrossRequirement,
                line.ScheduledReceipts,
                line.ProjectedOnHand,
                line.NetRequirement,
                line.PlannedOrderQuantity,
                line.SuggestedAction,
                line.SuggestedReleaseDate,
                line.IsReleased);
        }
    }
}
