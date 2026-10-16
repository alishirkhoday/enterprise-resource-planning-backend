using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>One item's expected demand in one bucket of the horizon.</summary>
public sealed class DemandForecastLine : BaseEntity
{
    public DemandForecast Forecast { get; private set; } = default!;
    public Guid ForecastId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public DateOnly BucketStartDate { get; private set; }
    public DateOnly BucketEndDate { get; private set; }

    public decimal Quantity { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>How much the planner trusts this figure, which downstream safety stock can widen.</summary>
    public Percentage? Confidence { get; private set; }

    /// <summary>What actually sold in this bucket, filled in afterwards to judge the method.</summary>
    public decimal? ActualQuantity { get; private set; }

    public string? Description { get; private set; }

    private DemandForecastLine() { }

    public DemandForecastLine(DemandForecast forecast, Item item, DateOnly bucketStartDate, DateOnly bucketEndDate, decimal quantity, Guid unitOfMeasureId)
    {
        ArgumentNullException.ThrowIfNull(forecast, nameof(forecast));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentOutOfRangeException.ThrowIfNegative(quantity, nameof(quantity));
        if (bucketEndDate < bucketStartDate)
            throw new ArgumentException("A bucket cannot end before it starts.", nameof(bucketEndDate));
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));

        Forecast = forecast;
        ForecastId = forecast.Id;
        Item = item;
        ItemId = item.Id;
        BucketStartDate = bucketStartDate;
        BucketEndDate = bucketEndDate;
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
    }

    public void SetConfidence(Percentage confidence)
    {
        ArgumentNullException.ThrowIfNull(confidence, nameof(confidence));
        Confidence = confidence;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Records what really happened, so the next forecast can be better than this one.</summary>
    public void RecordActual(decimal actualQuantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(actualQuantity, nameof(actualQuantity));
        ActualQuantity = actualQuantity;
    }

    public bool Covers(DateOnly date) => date >= BucketStartDate && date <= BucketEndDate;

    /// <summary>Signed error: positive when demand was underestimated.</summary>
    public decimal? Variance => ActualQuantity - Quantity;
}
