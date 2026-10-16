using ERP.Application.Common.Interfaces.Events;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Events;
using ERP.Domain.Primitives;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Application.Services.Events;

/// <summary>
/// One pass over the books, looking for the six things that become true without anything happening.
/// <para>
/// Every scan reads through a repository rather than a query written here, so the definition of
/// "overdue" or "due soon" stays in one place and cannot drift between this service and whatever
/// screen shows the same list.
/// </para>
/// </summary>
public sealed class ObservedEventScanner(
    IChequeRepository cheques,
    IReceivableRepository receivables,
    IItemBatchRepository batches,
    ICustomerRepository customers,
    IMachineRepository machines,
    IBudgetRepository budgets,
    IDomainEventDispatcher dispatcher) : IObservedEventScanner
{
    /// <summary>
    /// How far ahead counts as "soon". A cheque needs enough notice to move money before it is
    /// presented, and an expiring batch needs enough to be sold or written down.
    /// </summary>
    private const int ChequeNoticeDays = 7;
    private const int BatchExpiryNoticeDays = 30;

    public async Task<ObservedEventScanResult> ScanAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var chequesDueSoon = await ScanChequesAsync(asOf, cancellationToken);
        var receivablesOverdue = await ScanReceivablesAsync(asOf, cancellationToken);
        var batchesExpiring = await ScanBatchesAsync(asOf, cancellationToken);
        var overCreditLimit = await ScanCreditLimitsAsync(cancellationToken);
        var machinesDue = await ScanMachinesAsync(cancellationToken);
        var linesOverspent = await ScanBudgetsAsync(cancellationToken);

        return new ObservedEventScanResult(
            chequesDueSoon,
            receivablesOverdue,
            batchesExpiring,
            overCreditLimit,
            machinesDue,
            linesOverspent);
    }

    /// <summary>
    /// Cheques this organization has to honour in the next few days. Only issued cheques are
    /// scanned: a cheque received from a customer falling due is their problem to fund, not ours.
    /// </summary>
    private async Task<int> ScanChequesAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var dueSoon = await cheques.GetDueBetweenAsync(
            ChequeType.Issued,
            asOf,
            asOf.AddDays(ChequeNoticeDays),
            cancellationToken);

        return await PublishAsync(
            dueSoon.Select(cheque => new ChequeDueSoon(cheque.Id, cheque.Number, cheque.DueDate)),
            cancellationToken);
    }

    private async Task<int> ScanReceivablesAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var overdue = await receivables.GetOverdueAsync(asOf, cancellationToken: cancellationToken);

        return await PublishAsync(
            overdue.Select(receivable => new ReceivableOverdue(
                receivable.Id,
                receivable.PartyDetailAccountId,
                asOf.DayNumber - receivable.DueDate.DayNumber)),
            cancellationToken);
    }

    private async Task<int> ScanBatchesAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var expiring = await batches.GetExpiringBeforeAsync(asOf.AddDays(BatchExpiryNoticeDays), cancellationToken);

        return await PublishAsync(
            expiring
                .Where(batch => batch.ExpirationDate.HasValue)
                .Select(batch => new ItemBatchExpiringSoon(
                    batch.Id,
                    batch.ItemId,
                    batch.ExpirationDate!.Value,
                    batch.RemainingQuantity)),
            cancellationToken);
    }

    private async Task<int> ScanCreditLimitsAsync(CancellationToken cancellationToken)
    {
        var exceeded = await customers.GetOverCreditLimitAsync(cancellationToken);
        var published = 0;

        foreach (var customer in exceeded)
        {
            if (customer.CreditLimit is null || customer.DetailAccountId is null)
                continue;

            var outstanding = await receivables.GetOutstandingTotalAsync(customer.DetailAccountId.Value, cancellationToken);

            await dispatcher.DispatchAsync(
                new CustomerCreditLimitExceeded(customer.Id, outstanding, customer.CreditLimit.Amount),
                cancellationToken);

            published++;
        }

        return published;
    }

    private async Task<int> ScanMachinesAsync(CancellationToken cancellationToken)
    {
        var due = await machines.GetDueForMaintenanceAsync(cancellationToken);

        return await PublishAsync(
            due.Select(machine => new MachineDueForMaintenance(
                machine.Id,
                machine.Code,
                machine.RunningHours - machine.RunningHoursAtLastService)),
            cancellationToken);
    }

    private async Task<int> ScanBudgetsAsync(CancellationToken cancellationToken)
    {
        var overspent = await budgets.GetOverspentLinesAsync(cancellationToken: cancellationToken);

        return await PublishAsync(
            overspent.Select(line => new BudgetLineOverspent(
                line.BudgetId,
                line.Id,
                line.AccountId,
                line.ActualAmount.Amount - line.PlannedAmount.Amount)),
            cancellationToken);
    }

    private async Task<int> PublishAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        var materialized = events.ToList();
        await dispatcher.DispatchAsync(materialized, cancellationToken);
        return materialized.Count;
    }
}
