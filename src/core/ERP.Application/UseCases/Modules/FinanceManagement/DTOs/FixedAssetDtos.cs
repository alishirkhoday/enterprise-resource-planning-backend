using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.DTOs
{
    /// <summary>
    /// A depreciation run - محاسبه استهلاک.
    /// <para>
    /// Run for a period rather than per asset, because depreciation is a period close activity:
    /// everything in service gets charged, or the month's figures are wrong for the whole company
    /// rather than for one machine.
    /// </para>
    /// </summary>
    public record RunDepreciationDto
    {
        public DateOnly PeriodStart { get; init; }

        public DateOnly PeriodEnd { get; init; }

        /// <summary>Narrows the run to one category, for a correction that missed one class.</summary>
        public Guid? CategoryId { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>The journal document the charges are posted through, if one exists yet.</summary>
        public Guid? DocumentId { get; init; }

        public Guid? AccountingPeriodId { get; init; }

        /// <summary>
        /// Units produced in the period, for assets depreciated that way. Anything not listed is
        /// treated as having produced nothing, which charges it nothing.
        /// </summary>
        public IReadOnlyList<AssetProductionDto> Production { get; init; } = [];

        /// <summary>
        /// Works out the charges and reports them without writing anything. What a controller
        /// does before committing to a month end.
        /// </summary>
        public bool PreviewOnly { get; init; }
    }

    public record AssetProductionDto
    {
        public Guid FixedAssetId { get; init; }

        public decimal UnitsProduced { get; init; }
    }

    public sealed record DepreciationRunDto(
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        bool WasPreview,
        string CurrencyCode,
        int AssetCount,
        decimal TotalCharge,
        int SkippedCount,
        IReadOnlyList<DepreciationChargeDto> Charges,
        IReadOnlyList<DepreciationSkipDto> Skipped);

    public sealed record DepreciationChargeDto(
        Guid FixedAssetId,
        string AssetCode,
        string AssetName,
        DepreciationMethod Method,
        decimal Charge,
        decimal AccumulatedAfter,
        decimal BookValueAfter,
        bool IsNowFullyDepreciated);

    /// <summary>
    /// An asset the run passed over, and why. Reported rather than silently omitted: an asset
    /// missing from a depreciation run is exactly the thing nobody notices for a year.
    /// </summary>
    public sealed record DepreciationSkipDto(Guid FixedAssetId, string AssetCode, string Reason);
}
