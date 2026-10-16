using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs
{
    /// <summary>
    /// Raises a visit. Who pays is deliberately not in the request: it is worked out from the
    /// warranty and contract position, because a caller who can choose would always choose free.
    /// </summary>
    public record RaiseServiceOrderDto
    {
        public Guid CustomerId { get; init; }

        public string ReportedFault { get; init; } = default!;

        public string CurrencyCode { get; init; } = default!;

        public Guid? ItemId { get; init; }

        public Guid? SerialNumberId { get; init; }

        public Guid? TicketId { get; init; }

        public ServiceOrderPriority Priority { get; init; } = ServiceOrderPriority.Normal;

        public DateOnly? ScheduledOn { get; init; }

        public Guid? AssignedToHumanId { get; init; }

        public Guid? WarehouseId { get; init; }

        public Guid? BranchId { get; init; }
    }

    public sealed record ServiceOrderDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        ServiceOrderType Type,
        ServiceOrderStatus Status,
        ServiceOrderPriority Priority,
        string ReportedFault,
        Guid? ServiceContractId,
        DateTimeOffset RequestedAt,
        DateTimeOffset? ResponseDueAt,
        DateTimeOffset? ResolutionDueAt,
        DateOnly? ScheduledOn,
        DateOnly? CompletedOn,
        bool IsChargeable,
        decimal GrandTotal,
        string CurrencyCode);

    public record CompleteServiceOrderDto
    {
        public Guid ServiceOrderId { get; init; }

        public DateOnly CompletedOn { get; init; }

        public string WorkPerformed { get; init; } = default!;

        public decimal LabourHours { get; init; }

        public decimal LabourCharge { get; init; }

        public decimal TravelCharge { get; init; }

        public IReadOnlyList<ServiceOrderPartDto> Parts { get; init; } = [];
    }

    public record ServiceOrderPartDto
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; }

        public decimal UnitPrice { get; init; }

        public Guid UnitOfMeasureId { get; init; }

        public Guid? ItemBatchId { get; init; }

        public Guid? SerialNumberId { get; init; }
    }

    public record RegisterWarrantyClaimDto
    {
        public Guid CustomerId { get; init; }

        public Guid ItemId { get; init; }

        public DateOnly FaultOccurredOn { get; init; }

        public DateOnly ReportedOn { get; init; }

        public string FaultDescription { get; init; } = default!;

        public Guid? SerialNumberId { get; init; }

        public Guid? ItemBatchId { get; init; }

        public Guid? InvoiceId { get; init; }

        public Guid? BranchId { get; init; }
    }

    public sealed record WarrantyClaimDto(
        Guid Id,
        string Number,
        Guid CustomerId,
        Guid ItemId,
        Guid? SerialNumberId,
        DateOnly FaultOccurredOn,
        DateOnly ReportedOn,
        WarrantyClaimStatus Status,
        WarrantyClaimResolution Resolution,
        Guid? ServiceContractId,
        string? RejectionReason);
}
