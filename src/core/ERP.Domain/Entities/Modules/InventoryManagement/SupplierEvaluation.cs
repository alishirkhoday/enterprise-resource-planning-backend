namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A periodic scoring of a supplier across the dimensions purchasing cares about. Scores drive
/// which supplier is preferred next time.
/// </summary>
public sealed class SupplierEvaluation : BaseEntity
{
    public Supplier Supplier { get; private set; } = default!;
    public Guid SupplierId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public DateOnly EvaluatedOn { get; private set; }
    public Guid? EvaluatedByHumanId { get; private set; }

    /// <summary>How well the goods met specification, out of one hundred.</summary>
    public int QualityScore { get; private set; }

    /// <summary>How reliably delivery dates were met, out of one hundred.</summary>
    public int DeliveryScore { get; private set; }

    /// <summary>How competitive the pricing was, out of one hundred.</summary>
    public int PriceScore { get; private set; }

    /// <summary>How responsive the supplier was, out of one hundred.</summary>
    public int ServiceScore { get; private set; }

    public decimal TotalScore => (QualityScore + DeliveryScore + PriceScore + ServiceScore) / 4m;

    public SupplierGrade Grade => TotalScore switch
    {
        >= 90m => SupplierGrade.A,
        >= 75m => SupplierGrade.B,
        >= 60m => SupplierGrade.C,
        _ => SupplierGrade.D
    };

    public string? Comment { get; private set; }

    private SupplierEvaluation() { }

    public SupplierEvaluation(Supplier supplier, DateRange period, DateOnly evaluatedOn, int qualityScore, int deliveryScore, int priceScore, int serviceScore)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        EnsureInRange(qualityScore, nameof(qualityScore));
        EnsureInRange(deliveryScore, nameof(deliveryScore));
        EnsureInRange(priceScore, nameof(priceScore));
        EnsureInRange(serviceScore, nameof(serviceScore));

        Supplier = supplier;
        SupplierId = supplier.Id;
        Period = period;
        EvaluatedOn = evaluatedOn;
        QualityScore = qualityScore;
        DeliveryScore = deliveryScore;
        PriceScore = priceScore;
        ServiceScore = serviceScore;
    }

    internal void SetEvaluator(Guid humanId) => EvaluatedByHumanId = humanId == Guid.Empty ? null : humanId;

    internal void SetComment(string? comment) => Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    private static void EnsureInRange(int score, string parameterName)
    {
        if (score < 0 || score > 100)
            throw new ArgumentOutOfRangeException(parameterName, "A score must be between 0 and 100.");
    }
}
