using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetSalesPerformance
{
    /// <summary>
    /// How a salesperson is doing - کارنامه فروش. The window defaults to the current year, which
    /// is the period most quotas are set over.
    /// </summary>
    public sealed record GetSalesPerformanceQuery : IQuery<Result<SalesPerformanceDto, Error>>
    {
        public Guid SalesRepHumanId { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }
    }
}
