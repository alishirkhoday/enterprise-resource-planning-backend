namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The part of a settlement that clears one open item.
/// </summary>
public sealed class SettlementAllocation : BaseEntity
{
    public Settlement Settlement { get; private set; } = default!;
    public Guid SettlementId { get; private set; }

    /// <summary>Set when the allocation clears a receivable.</summary>
    public Guid? ReceivableId { get; private set; }

    /// <summary>Set when the allocation clears a payable.</summary>
    public Guid? PayableId { get; private set; }

    public Money Amount { get; private set; } = default!;

    /// <summary>The early settlement discount taken as part of clearing this item.</summary>
    public Money? DiscountTaken { get; private set; }

    public string? Description { get; private set; }

    private SettlementAllocation() { }

    public SettlementAllocation(Settlement settlement, Money amount, Guid? receivableId = null, Guid? payableId = null)
    {
        ArgumentNullException.ThrowIfNull(settlement, nameof(settlement));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "An allocation must carry a positive amount.");
        if (receivableId is null && payableId is null)
            throw new ArgumentException("An allocation must point at either a receivable or a payable.", nameof(receivableId));
        if (receivableId is not null && payableId is not null)
            throw new ArgumentException("An allocation cannot point at both a receivable and a payable.", nameof(payableId));

        Settlement = settlement;
        SettlementId = settlement.Id;
        Amount = amount;
        ReceivableId = receivableId;
        PayableId = payableId;
    }

    internal void SetDiscountTaken(Money discount)
    {
        ArgumentNullException.ThrowIfNull(discount, nameof(discount));
        if (discount.CurrencyCode != Amount.CurrencyCode)
            throw new ArgumentException("The discount must share the allocation currency.", nameof(discount));
        DiscountTaken = discount;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
