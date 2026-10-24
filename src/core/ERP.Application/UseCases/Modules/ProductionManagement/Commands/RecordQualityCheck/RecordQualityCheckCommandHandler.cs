using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;
using ERP.Application.UseCases.Modules.ProductionManagement.Mapping;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.RecordQualityCheck
{
    /// <summary>
    /// Records an inspection - کنترل کیفیت.
    /// <para>
    /// The readings are kept, not only the verdict. A verdict says this batch was wrong; a run of
    /// readings creeping toward a tolerance says the process is drifting and will be wrong next
    /// week, and that is the only one of the two anybody can act on in time.
    /// </para>
    /// <para>
    /// A numeric reading is judged against its own parameter's tolerance and cannot be talked into
    /// passing: the request says what was measured, the parameter says whether that is acceptable.
    /// </para>
    /// </summary>
    public class RecordQualityCheckCommandHandler(
        IHumanRepository humans,
        IProductionOrderRepository productionOrders,
        IQualityControlCheckRepository qualityChecks)
        : IRequestHandler<RecordQualityCheckCommand, Result<QualityCheckDto, Error>>
    {
        public async Task<Result<QualityCheckDto, Error>> Handle(RecordQualityCheckCommand request, CancellationToken cancellationToken)
        {
            var inspector = await humans.GetByIdAsync(request.InspectorHumanId, cancellationToken);
            if (inspector is null)
                return Result.Failure<QualityCheckDto, Error>(Errors.General.NotFound(request.InspectorHumanId.ToString(), nameof(Human)));

            if (request.PassedQuantity > request.InspectedQuantity)
                return Result.Failure<QualityCheckDto, Error>(
                    Errors.Production.QualityCheckCannotBeRecorded("More units cannot pass than were inspected."));

            var check = new QualityControlCheck(inspector, request.InspectedQuantity);

            if (request.ProductionTaskId is not null)
            {
                var task = await productionOrders.GetTaskAsync(request.ProductionTaskId.Value, cancellationToken);
                if (task is null)
                    return Result.Failure<QualityCheckDto, Error>(
                        Errors.General.NotFound(request.ProductionTaskId.Value.ToString(), nameof(ProductionTask)));

                check.LinkToTask(task);
            }

            if (request.InspectionPlanId is not null)
                check.LinkToInspectionPlan(request.InspectionPlanId.Value);

            if (request.ReferenceType is not null && request.ReferenceId is not null)
                check.SetReference(request.ReferenceType, request.ReferenceId);

            var measured = await AddMeasurementsAsync(check, request, cancellationToken);
            if (measured.IsFailure)
                return Result.Failure<QualityCheckDto, Error>(measured.Error);

            // Last, because the verdict is derived from the readings: a pass cannot be recorded
            // over a measurement that was out of tolerance.
            check.RecordResult(request.PassedQuantity, request.Notes);

            await qualityChecks.CreateAsync(check, cancellationToken);

            return Result.Success<QualityCheckDto, Error>(check.ToDto());
        }

        private async Task<Result<bool, Error>> AddMeasurementsAsync(
            QualityControlCheck check,
            RecordQualityCheckCommand request,
            CancellationToken cancellationToken)
        {
            if (request.Measurements.Count == 0)
                return Result.Success<bool, Error>(true);

            var parameters = await qualityChecks.GetParametersAsync(
                [.. request.Measurements.Select(m => m.ParameterId)],
                cancellationToken);

            var byId = parameters.ToDictionary(p => p.Id);

            foreach (var measurement in request.Measurements)
            {
                if (!byId.TryGetValue(measurement.ParameterId, out var parameter))
                    return Result.Failure<bool, Error>(
                        Errors.General.NotFound(measurement.ParameterId.ToString(), nameof(QualityParameter)));

                // A retired parameter has a tolerance nobody stands behind any more, so a reading
                // against it would pass or fail on a rule the business has withdrawn.
                if (!parameter.IsActive)
                    return Result.Failure<bool, Error>(Errors.Production.QualityCheckCannotBeRecorded(
                        $"Parameter {parameter.Name} is no longer in use."));

                var reading = measurement.NumericValue is not null
                    ? new QualityMeasurement(check, parameter, measurement.NumericValue.Value)
                    : new QualityMeasurement(check, parameter, measurement.TextValue!, measurement.IsAcceptable);

                check.AddMeasurement(reading);
                check.SetQualityMeasurementNotes(reading.Id, measurement.Notes);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
