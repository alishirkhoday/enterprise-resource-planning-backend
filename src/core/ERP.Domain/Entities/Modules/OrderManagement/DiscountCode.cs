namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// A code that reduces the price of an order, bounded by a validity window, a usage limit and
/// a minimum order value.
/// </summary>
public sealed class DiscountCode : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string? Title { get; private set; }

    public bool IsPercentage { get; private set; }

    /// <summary>The fixed reduction, for a code expressed as an amount.</summary>
    public Money? DiscountAmount { get; private set; }

    /// <summary>The rate, for a code expressed as a percentage.</summary>
    public Percentage? DiscountPercentage { get; private set; }

    /// <summary>The most a percentage code may take off, when a cap applies.</summary>
    public Money? MaximumDiscountAmount { get; private set; }

    /// <summary>The order value the code needs before it applies.</summary>
    public Money? MinimumOrderAmount { get; private set; }

    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset ExpireDate { get; private set; }

    /// <summary>How many times the code may be used in total. Zero means there is no limit.</summary>
    public int UsageLimit { get; private set; }
    public int UsageCount { get; private set; }

    /// <summary>How many times one customer may use it.</summary>
    public int? PerCustomerLimit { get; private set; }

    public Guid? CustomerGroupId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public bool IsActive { get; private set; }

    private DiscountCode() { }

    public DiscountCode(string code, Money discountAmount, DateTimeOffset startDate, DateTimeOffset expireDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentNullException.ThrowIfNull(discountAmount, nameof(discountAmount));
        EnsureValidWindow(startDate, expireDate);
        if (!discountAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "A discount must be positive.");

        Code = code.Trim().ToUpperInvariant();
        DiscountAmount = discountAmount;
        IsPercentage = false;
        StartDate = startDate;
        ExpireDate = expireDate;
        IsActive = true;
    }

    public DiscountCode(string code, Percentage discountPercentage, DateTimeOffset startDate, DateTimeOffset expireDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentNullException.ThrowIfNull(discountPercentage, nameof(discountPercentage));
        EnsureValidWindow(startDate, expireDate);

        Code = code.Trim().ToUpperInvariant();
        DiscountPercentage = discountPercentage;
        IsPercentage = true;
        StartDate = startDate;
        ExpireDate = expireDate;
        IsActive = true;
    }

    public void SetTitle(string? title) => Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

    public void SetBounds(Money? minimumOrderAmount, Money? maximumDiscountAmount)
    {
        MinimumOrderAmount = minimumOrderAmount;
        MaximumDiscountAmount = maximumDiscountAmount;
    }

    public void SetUsageLimits(int usageLimit, int? perCustomerLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(usageLimit, nameof(usageLimit));
        if (perCustomerLimit.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(perCustomerLimit.Value, nameof(perCustomerLimit));
        UsageLimit = usageLimit;
        PerCustomerLimit = perCustomerLimit;
    }

    public void RestrictToCustomerGroup(Guid customerGroupId) =>
        CustomerGroupId = customerGroupId == Guid.Empty ? null : customerGroupId;

    public void AttributeToCampaign(Guid campaignId) => CampaignId = campaignId == Guid.Empty ? null : campaignId;

    /// <summary>The reduction this code gives on a given order value, respecting any cap.</summary>
    public Money CalculateOn(Money orderAmount)
    {
        ArgumentNullException.ThrowIfNull(orderAmount, nameof(orderAmount));
        if (MinimumOrderAmount is not null && orderAmount.Amount < MinimumOrderAmount.Amount)
            throw new InvalidOperationException($"This code needs an order of at least {MinimumOrderAmount}.");

        var discount = IsPercentage && DiscountPercentage is not null
            ? orderAmount.Percent(DiscountPercentage)
            : DiscountAmount ?? Money.Zero(orderAmount.CurrencyCode);

        if (MaximumDiscountAmount is not null && discount.Amount > MaximumDiscountAmount.Amount)
            discount = MaximumDiscountAmount;

        return discount;
    }

    public void RecordUsage()
    {
        if (UsageLimit > 0 && UsageCount >= UsageLimit)
            throw new InvalidOperationException($"Discount code {Code} has reached its usage limit.");
        UsageCount++;
    }

    public void ReleaseUsage()
    {
        if (UsageCount > 0)
            UsageCount--;
    }

    public bool IsValidOn(DateTimeOffset moment) =>
        IsActive
        && !IsDeleted
        && moment >= StartDate
        && moment <= ExpireDate
        && (UsageLimit == 0 || UsageCount < UsageLimit);

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static void EnsureValidWindow(DateTimeOffset startDate, DateTimeOffset expireDate)
    {
        if (expireDate <= startDate)
            throw new ArgumentOutOfRangeException(nameof(expireDate), "The expiry must come after the start.");
    }
}
