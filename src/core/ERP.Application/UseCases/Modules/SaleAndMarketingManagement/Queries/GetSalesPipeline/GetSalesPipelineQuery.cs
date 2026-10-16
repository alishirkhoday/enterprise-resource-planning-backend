using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetSalesPipeline
{
    /// <summary>
    /// The pipeline - قیف فروش. Narrowed to one salesperson when asked, and to one currency,
    /// because deals in different currencies cannot be added up without a rate.
    /// </summary>
    public sealed record GetSalesPipelineQuery : IQuery<Result<SalesPipelineDto, Error>>
    {
        public Guid? AssignedToHumanId { get; init; }

        public string? CurrencyCode { get; init; }

        /// <summary>The date overdue deals are measured against. Defaults to today.</summary>
        public DateOnly? AsOf { get; init; }
    }
}
