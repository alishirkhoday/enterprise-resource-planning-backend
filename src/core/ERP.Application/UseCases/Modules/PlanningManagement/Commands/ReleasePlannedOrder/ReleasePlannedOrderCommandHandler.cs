using ERP.Application.UseCases.Modules.PlanningManagement.DTOs;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Repositories.Modules.PlanningManagement;

namespace ERP.Application.UseCases.Modules.PlanningManagement.Commands.ReleasePlannedOrder
{
    /// <summary>
    /// Turns planning suggestions into work somebody owns.
    /// <para>
    /// Marking the line as released is what stops it being acted on twice. A suggestion released
    /// into a requisition and then released again by the next person to open the plan is how one
    /// shortage becomes two orders.
    /// </para>
    /// </summary>
    public class ReleasePlannedOrderCommandHandler(IMaterialRequirementPlanRepository plans)
        : IRequestHandler<ReleasePlannedOrderCommand, Result<ReleasePlannedOrderResultDto, Error>>
    {
        public async Task<Result<ReleasePlannedOrderResultDto, Error>> Handle(
            ReleasePlannedOrderCommand request,
            CancellationToken cancellationToken)
        {
            var plan = await plans.GetWithLinesAsync(request.PlanId, cancellationToken);
            if (plan is null)
                return Result.Failure<ReleasePlannedOrderResultDto, Error>(
                    Errors.General.NotFound(request.PlanId.ToString(), nameof(MaterialRequirementPlan)));

            // A superseded run computed its suggestions against a position that has since moved.
            if (!plan.IsActionable)
                return Result.Failure<ReleasePlannedOrderResultDto, Error>(Errors.Planning.PlanIsNotActionable(plan.Status.ToString()));

            var byId = plan.Lines.ToDictionary(l => l.Id);
            var released = new List<ReleasedLineDto>(request.LineIds.Count);

            foreach (var lineId in request.LineIds)
            {
                if (!byId.TryGetValue(lineId, out var line))
                    return Result.Failure<ReleasePlannedOrderResultDto, Error>(Errors.Planning.LineIsNotOnThePlan(lineId));

                if (line.IsReleased)
                    return Result.Failure<ReleasePlannedOrderResultDto, Error>(
                        Errors.Planning.LineIsAlreadyReleased(line.ReleasedReferenceType!, line.ReleasedReferenceId!));

                if (!line.NeedsAction)
                    return Result.Failure<ReleasePlannedOrderResultDto, Error>(Errors.Planning.LineSuggestsNothing(lineId));

                // The reference is recorded against the line so the plan can answer what became of
                // each suggestion. Creating the requisition or production order itself belongs to
                // those modules, driven by the fact this records.
                var referenceType = line.SuggestedAction == PlannedOrderAction.Manufacture
                    ? "ProductionOrder"
                    : "PurchaseRequisition";

                var referenceId = Guid.NewGuid().ToString();
                plan.MarkMaterialRequirementPlanLineAsReleased(line.Id, referenceType, referenceId);

                released.Add(new ReleasedLineDto(
                    line.Id,
                    line.ItemId,
                    line.PlannedOrderQuantity,
                    line.SuggestedAction,
                    referenceType,
                    referenceId));
            }

            await plans.UpdateAsync(plan, cancellationToken);

            return Result.Success<ReleasePlannedOrderResultDto, Error>(
                new ReleasePlannedOrderResultDto(plan.Id, released.Count, released));
        }
    }
}
