using ERP.Application.UseCases.Modules.PricingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.PricingManagement.Queries.GetEffectivePrice
{
    /// <summary>
    /// What an item costs a given customer - استعلام قیمت. Resolved exactly as an invoice will,
    /// so a quote and the bill that follows it cannot disagree.
    /// </summary>
    public sealed record GetEffectivePriceQuery : IQuery<Result<EffectivePriceDto, Error>>
    {
        public Guid ItemId { get; init; }

        public decimal Quantity { get; init; } = 1m;

        /// <summary>Left empty the default list applies, which is the walk-in price.</summary>
        public Guid? CustomerId { get; init; }

        /// <summary>Forces a particular list, for checking what a promotion would give.</summary>
        public Guid? PriceListId { get; init; }

        public string? CurrencyCode { get; init; }

        /// <summary>Defaults to today. Set it to reproduce a price quoted last month.</summary>
        public DateOnly? OnDate { get; init; }
    }
}
