using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs
{
    /// <summary>
    /// A quotation - پیش‌فاکتور. Issued and sent in one step: a quotation that is written and
    /// never sent is a draft, and drafts do not need an endpoint of their own.
    /// </summary>
    public record IssueQuotationDto
    {
        public Guid CustomerId { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public IReadOnlyList<QuotationLineDto> Lines { get; init; } = [];

        /// <summary>
        /// How long the prices hold. Without it a quotation never expires, and a customer can
        /// hold you to last year's price.
        /// </summary>
        public int ValidForDays { get; init; } = 30;

        public Guid? OpportunityId { get; init; }

        public Guid? PaymentTermId { get; init; }

        public Guid? PriceListId { get; init; }

        public Guid? SalesRepHumanId { get; init; }

        public Guid? BranchId { get; init; }

        public decimal AdditionalCharges { get; init; }

        public decimal HeaderDiscountPercentage { get; init; }

        public string? Terms { get; init; }

        public string? Description { get; init; }
    }

    public record QuotationLineDto
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; }

        public Guid UnitOfMeasureId { get; init; }

        /// <summary>
        /// Optional. Left empty the price comes from the list in effect for the customer, which
        /// is the only way an agreed price cannot be quietly undercut.
        /// </summary>
        public decimal? UnitPrice { get; init; }

        public decimal? DiscountPercentage { get; init; }

        public Guid? TaxId { get; init; }

        public decimal? TaxPercentage { get; init; }

        public int? DeliveryLeadTimeDays { get; init; }

        public string? Description { get; init; }
    }

    /// <summary>The customer's answer - پذیرش یا رد پیش‌فاکتور.</summary>
    public record DecideQuotationDto
    {
        public Guid QuotationId { get; init; }

        public bool IsAccepted { get; init; }

        /// <summary>Required on a rejection.</summary>
        public string? RejectionReason { get; init; }

        /// <summary>The order the acceptance turned into, when there is one already.</summary>
        public Guid? SaleOrderId { get; init; }

        /// <summary>
        /// Closes the linked deal as won at the same time. An accepted quotation that leaves its
        /// opportunity open is how a pipeline keeps forecasting money already banked.
        /// </summary>
        public bool CloseOpportunity { get; init; } = true;
    }

    /// <summary>
    /// A new version of a quotation - بازنگری پیش‌فاکتور.
    /// <para>
    /// The version counter moves on the same document rather than a second one being raised, so
    /// the number a customer is holding keeps referring to the live offer. What the earlier
    /// version said is not kept: if that history matters, the superseded lines have to be
    /// archived before this is called.
    /// </para>
    /// </summary>
    public record ReviseQuotationDto
    {
        public Guid QuotationId { get; init; }

        /// <summary>An empty list carries the previous version's lines across unchanged.</summary>
        public IReadOnlyList<QuotationLineDto> Lines { get; init; } = [];

        public int? ValidForDays { get; init; }

        public decimal? HeaderDiscountPercentage { get; init; }

        public string? Reason { get; init; }
    }

    public sealed record QuotationDto(
        Guid Id,
        string Number,
        int Version,
        QuotationStatus Status,
        Guid CustomerId,
        string CustomerName,
        DateTimeOffset IssueDate,
        DateTimeOffset? ExpiryDate,
        string CurrencyCode,
        decimal SubTotal,
        decimal HeaderDiscount,
        decimal TotalTax,
        decimal AdditionalCharges,
        decimal GrandTotal,
        Guid? OpportunityId,
        Guid? SaleOrderId,
        string? Terms,
        string? RejectionReason,
        IReadOnlyList<QuotationLineViewDto> Lines);

    public sealed record QuotationLineViewDto(
        Guid Id,
        short LineNumber,
        Guid ItemId,
        string? ItemName,
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal LineTotalWithTax,
        int? DeliveryLeadTimeDays);
}
