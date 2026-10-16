using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;
using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Mapping
{
    public static class ProductionMapper
    {
        public static ProductionOrderDto ToDto(this ProductionOrder order)
        {
            ArgumentNullException.ThrowIfNull(order, nameof(order));

            return new ProductionOrderDto(
                order.Id,
                order.Code,
                order.ItemId,
                order.Item.Name,
                order.Quantity,
                order.ProducedQuantity,
                order.ScrappedQuantity,
                order.RemainingQuantity,
                order.YieldPercentage,
                order.Status,
                order.Priority,
                order.PlannedStartDate,
                order.DueDate,
                order.StartedDate,
                order.CompletedDate,
                order.BillOfMaterialsId,
                order.WarehouseId,
                order.CurrencyCode,
                order.MaterialCost.Amount,
                order.LabourCost.Amount,
                order.OverheadCost.Amount,
                order.TotalCost.Amount,
                order.UnitCost.Amount);
        }

        public static MaterialIssueDto ToDto(this MaterialIssue issue)
        {
            ArgumentNullException.ThrowIfNull(issue, nameof(issue));

            return new MaterialIssueDto(
                issue.Id,
                issue.Number,
                issue.ProductionOrderId,
                issue.ProductionOrder.Code,
                issue.WarehouseId,
                issue.IssueDate,
                issue.Status,
                issue.CurrencyCode,
                issue.TotalCost().Amount,
                [.. issue.Items
                    .OrderBy(i => i.LineNumber)
                    .Select(i => new MaterialIssueLineViewDto(
                        i.Id,
                        i.LineNumber,
                        i.ComponentId,
                        i.Component.Name,
                        i.PlannedQuantity,
                        i.IssuedQuantity,
                        i.Variance,
                        i.UnitCost.Amount,
                        i.TotalCost.Amount))]);
        }

        public static QualityCheckDto ToDto(this QualityControlCheck check)
        {
            ArgumentNullException.ThrowIfNull(check, nameof(check));

            return new QualityCheckDto(
                check.Id,
                check.InspectorId,
                check.Inspector.FullName,
                check.CheckedAt,
                check.ProductionOrderId,
                check.TaskId,
                check.ReferenceType,
                check.ReferenceId,
                check.InspectedQuantity,
                check.PassedQuantity,
                check.FailedQuantity,
                check.PassRate,
                check.Passed,
                check.Notes,
                [.. check.Measurements.Select(m => new QualityMeasurementViewDto(
                    m.Id,
                    m.ParameterId,
                    m.Parameter.Name,
                    m.Parameter.IsCritical,
                    m.NumericValue,
                    m.TextValue,
                    m.Parameter.TargetValue,
                    m.Parameter.MinimumValue,
                    m.Parameter.MaximumValue,
                    m.IsWithinTolerance,
                    m.Notes))]);
        }
    }
}
