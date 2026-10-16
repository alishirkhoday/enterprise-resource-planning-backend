using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs
{
    /// <summary>Commission earned on a document - پورسانت فروش.</summary>
    public record EarnCommissionDto
    {
        public Guid SalesRepHumanId { get; init; }

        /// <summary>What it was earned on: an Invoice, an Order, a SalesContract.</summary>
        public string ReferenceType { get; init; } = default!;

        public string ReferenceId { get; init; } = default!;

        public DateOnly EarnedOn { get; init; }

        /// <summary>The figure the rate applies to, normally the net value of the sale.</summary>
        public decimal BaseAmount { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public decimal Rate { get; init; }

        public string? Description { get; init; }
    }

    /// <summary>
    /// Approving commission for payment - تأیید پورسانت. Separate from earning it because the two
    /// are decided by different people, usually once the customer has actually paid.
    /// </summary>
    public record ApproveCommissionDto
    {
        public Guid CommissionId { get; init; }

        /// <summary>Rejects it instead, which reverses the accrual with a reason.</summary>
        public bool IsApproved { get; init; } = true;

        public string? ReversalReason { get; init; }
    }

    public sealed record CommissionDto(
        Guid Id,
        Guid SalesRepHumanId,
        string ReferenceType,
        string ReferenceId,
        DateOnly EarnedOn,
        string CurrencyCode,
        decimal BaseAmount,
        decimal Rate,
        decimal Amount,
        CommissionStatus Status,
        DateOnly? PaidOn,
        Guid? PayrollId,
        string? Description);
}
