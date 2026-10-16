using ERP.Domain.Entities.Modules.PricingManagement;

namespace ERP.Application.UseCases.Modules.PricingManagement.DTOs
{
    /// <summary>A price list - لیست قیمت.</summary>
    public record CreatePriceListDto
    {
        public string Code { get; init; } = default!;

        public string Name { get; init; } = default!;

        public PriceListType Type { get; init; } = PriceListType.Sales;

        public string CurrencyCode { get; init; } = default!;

        public DateOnly StartDate { get; init; }

        /// <summary>Left empty the list stays in force until something replaces it.</summary>
        public DateOnly? EndDate { get; init; }

        /// <summary>Restricts the list to one customer group.</summary>
        public Guid? CustomerGroupId { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>The prices already include tax, as retail prices usually do.</summary>
        public bool IsTaxInclusive { get; init; }

        /// <summary>
        /// Makes this the list used when a customer names none. Only one list can hold that
        /// position, so setting it here takes it off whichever list had it.
        /// </summary>
        public bool IsDefault { get; init; }

        public string? Description { get; init; }

        /// <summary>The prices to start it with. A list with none prices nothing.</summary>
        public IReadOnlyList<PriceListLineDto> Lines { get; init; } = [];
    }

    public record PriceListLineDto
    {
        public Guid ItemId { get; init; }

        public Guid UnitOfMeasureId { get; init; }

        public decimal BasePrice { get; init; }

        /// <summary>
        /// The quantity this price starts applying at, for a volume break. Lines for the same
        /// item at different minimums are how a tiered price is expressed.
        /// </summary>
        public decimal MinimumQuantity { get; init; } = 1m;

        public decimal? DiscountPercentage { get; init; }

        public Guid? TaxId { get; init; }

        public decimal? TaxPercentage { get; init; }

        /// <summary>The floor a salesperson cannot go under, whatever else they negotiate.</summary>
        public decimal? MinimumPrice { get; init; }
    }

    /// <summary>Adds or replaces prices on an existing list - به‌روزرسانی قیمت‌ها.</summary>
    public record SetPriceListItemsDto
    {
        public Guid PriceListId { get; init; }

        public IReadOnlyList<PriceListLineDto> Lines { get; init; } = [];

        /// <summary>
        /// Replaces the whole list rather than merging into it. Used for a fresh price file from
        /// a supplier, where an item that has dropped off should stop being priced.
        /// </summary>
        public bool ReplaceAll { get; init; }
    }

    public sealed record PriceListDto(
        Guid Id,
        string Code,
        string Name,
        PriceListType Type,
        string CurrencyCode,
        DateOnly StartDate,
        DateOnly? EndDate,
        bool IsActive,
        bool IsDefault,
        bool IsTaxInclusive,
        Guid? CustomerGroupId,
        int ItemCount,
        IReadOnlyList<PriceListItemDto> Items);

    public sealed record PriceListItemDto(
        Guid Id,
        Guid ItemId,
        string? ItemName,
        Guid UnitOfMeasureId,
        decimal BasePrice,
        decimal MinimumQuantity,
        decimal? DiscountPercentage,
        decimal NetPrice,
        decimal? TaxPercentage,
        decimal PriceWithTax,
        decimal? MinimumPrice,
        bool IsActive);

    /// <summary>
    /// What one item costs a given customer today - استعلام قیمت. The question a salesperson
    /// actually asks, answered through the same resolution an invoice will use.
    /// </summary>
    public sealed record EffectivePriceDto(
        Guid ItemId,
        string? ItemName,
        decimal Quantity,
        Guid PriceListId,
        string PriceListCode,
        string CurrencyCode,
        decimal BasePrice,
        decimal? DiscountPercentage,
        decimal NetPrice,
        decimal TaxAmount,
        decimal PriceWithTax,
        decimal LineTotal,
        decimal? MinimumPrice);
}
