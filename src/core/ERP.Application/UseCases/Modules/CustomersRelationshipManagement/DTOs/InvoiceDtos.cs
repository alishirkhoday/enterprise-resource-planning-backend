using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs
{
    public record IssueInvoiceDto
    {
        public Guid CustomerId { get; init; }

        public DateOnly IssueDate { get; init; }

        public DateOnly DueDate { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public IReadOnlyList<InvoiceLineDto> Lines { get; init; } = [];

        public Guid? SaleOrderId { get; init; }

        public Guid? PaymentTermId { get; init; }

        public Guid? SalesRepHumanId { get; init; }

        public Guid? BranchId { get; init; }

        public decimal AdditionalCharges { get; init; }

        /// <summary>A discount on the whole invoice, applied after the line discounts.</summary>
        public decimal HeaderDiscountPercentage { get; init; }

        public string? Description { get; init; }
    }

    public record InvoiceLineDto
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; }

        public Guid UnitOfMeasureId { get; init; }

        /// <summary>
        /// Optional. Left empty the price comes from the price list in effect for the customer,
        /// which is the only way a salesperson cannot quietly undercut the agreed one.
        /// </summary>
        public decimal? UnitPrice { get; init; }

        public decimal? DiscountPercentage { get; init; }

        public Guid? TaxId { get; init; }

        public decimal? TaxPercentage { get; init; }

        public Guid? ItemBatchId { get; init; }

        public string? Description { get; init; }
    }

    /// <summary>
    /// A credit note - برگشت از فروش. It is an invoice of its own type rather than a change to the
    /// original, because the original was already filed with the tax authority and reported.
    /// </summary>
    public record CreditSalesReturnDto
    {
        public Guid OriginalInvoiceId { get; init; }

        public DateOnly IssueDate { get; init; }

        public string Reason { get; init; } = default!;

        /// <summary>
        /// Which lines come back and how much of each. An empty list credits the whole invoice,
        /// which is the common case and the one worth making easy.
        /// </summary>
        public IReadOnlyList<SalesReturnLineDto> Lines { get; init; } = [];

        /// <summary>Where the goods go back to, when they are physically returned.</summary>
        public Guid? WarehouseId { get; init; }
    }

    public record SalesReturnLineDto
    {
        public Guid InvoiceItemId { get; init; }

        public decimal Quantity { get; init; }
    }

    public sealed record InvoiceDto(
        Guid Id,
        string Number,
        InvoiceType Type,
        InvoiceStatus Status,
        Guid CustomerId,
        string CustomerName,
        DateOnly IssueDate,
        DateOnly DueDate,
        string CurrencyCode,
        decimal SubTotal,
        decimal TotalDiscount,
        decimal TotalTax,
        decimal AdditionalCharges,
        decimal AmountPayable,
        Guid? OriginalInvoiceId,
        IReadOnlyList<InvoiceLineViewDto> Lines);

    public sealed record InvoiceLineViewDto(
        Guid Id,
        short LineNumber,
        Guid ItemId,
        string? ItemName,
        decimal Quantity,
        decimal UnitPrice,
        decimal Discount,
        decimal TaxAmount,
        decimal AmountPayable);

    /// <summary>
    /// A customer statement - صورت‌حساب: what was invoiced, what was paid, and what is still owed,
    /// aged so the conversation can start with the oldest item.
    /// </summary>
    public sealed record CustomerStatementDto(
        Guid CustomerId,
        string CustomerName,
        DateOnly From,
        DateOnly To,
        string CurrencyCode,
        decimal TotalInvoiced,
        decimal TotalOutstanding,
        decimal? CreditLimit,
        decimal? CreditAvailable,
        bool IsOverCreditLimit,
        IReadOnlyList<CustomerStatementLineDto> Lines,
        IReadOnlyList<CustomerAgeingBucketDto> Ageing);

    public sealed record CustomerStatementLineDto(
        Guid InvoiceId,
        string Number,
        InvoiceType Type,
        InvoiceStatus Status,
        DateOnly IssueDate,
        DateOnly DueDate,
        decimal AmountPayable,
        int DaysOverdue);

    public sealed record CustomerAgeingBucketDto(string Bucket, decimal Amount);
}
