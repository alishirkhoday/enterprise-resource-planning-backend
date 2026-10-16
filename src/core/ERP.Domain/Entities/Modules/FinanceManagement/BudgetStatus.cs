namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Where a budget has got to. Only an approved budget controls anything: until then there is
/// nothing for a spend to be checked against.
/// </summary>
public enum BudgetStatus
{
    Draft = 0,
    AwaitingApproval = 1,
    Approved = 2,
    Closed = 3
}
