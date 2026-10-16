using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Mapping
{
    public static class ServiceMapper
    {
        public static ServiceOrderDto ToDto(this ServiceOrder order)
        {
            ArgumentNullException.ThrowIfNull(order, nameof(order));

            return new ServiceOrderDto(
                order.Id,
                order.Number,
                order.CustomerId,
                order.Type,
                order.Status,
                order.Priority,
                order.ReportedFault,
                order.ServiceContractId,
                order.RequestedAt,
                order.ResponseDueAt,
                order.ResolutionDueAt,
                order.ScheduledOn,
                order.CompletedOn,
                order.IsChargeable,
                order.GrandTotal().Amount,
                order.CurrencyCode);
        }

        public static WarrantyClaimDto ToDto(this WarrantyClaim claim)
        {
            ArgumentNullException.ThrowIfNull(claim, nameof(claim));

            return new WarrantyClaimDto(
                claim.Id,
                claim.Number,
                claim.CustomerId,
                claim.ItemId,
                claim.SerialNumberId,
                claim.FaultOccurredOn,
                claim.ReportedOn,
                claim.Status,
                claim.Resolution,
                claim.ServiceContractId,
                claim.RejectionReason);
        }
    }
}
