namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Something spent on a mission and claimed back - هزینه مأموریت.
/// </summary>
public sealed class MissionExpense : BaseEntity
{
    public Mission Mission { get; private set; } = default!;
    public Guid MissionId { get; private set; }

    public ExpenseCategory Category { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateOnly SpentOn { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Identifier of the receipt image in the attachment store.</summary>
    public Guid? AttachmentId { get; private set; }

    public bool IsReimbursed { get; private set; }
    public DateOnly? ReimbursedOn { get; private set; }

    private MissionExpense() { }

    public MissionExpense(Mission mission, ExpenseCategory category, Money amount, DateOnly spentOn)
    {
        ArgumentNullException.ThrowIfNull(mission, nameof(mission));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "An expense must be a positive amount.");

        Mission = mission;
        MissionId = mission.Id;
        Category = category;
        Amount = amount;
        SpentOn = spentOn;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    internal void AttachReceipt(Guid attachmentId) => AttachmentId = attachmentId == Guid.Empty ? null : attachmentId;

    internal void MarkAsReimbursed(DateOnly reimbursedOn)
    {
        IsReimbursed = true;
        ReimbursedOn = reimbursedOn;
    }
}
