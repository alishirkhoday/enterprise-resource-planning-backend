namespace ERP.Application.Common.Interfaces.Events;

/// <summary>
/// Finds the facts that nothing announces about itself and publishes them.
/// <para>
/// A cheque does not change when its due date draws near and a receivable does not change on the
/// morning it falls overdue - the calendar moved, not the record. There is no state transition for
/// an aggregate to raise an event from, so these are found by looking.
/// </para>
/// <para>
/// A scan can be run twice over the same day, so whatever handles these events must tolerate
/// seeing the same fact again. That is the price of the producer living outside the aggregate, and
/// it is why these events do not go through the outbox: re-running the scan regenerates them, so
/// the exactly-once guarantee an outbox row buys would be paying for nothing.
/// </para>
/// </summary>
public interface IObservedEventScanner
{
    /// <summary>Runs every scan and returns how many facts were published.</summary>
    Task<ObservedEventScanResult> ScanAsync(DateOnly asOf, CancellationToken cancellationToken = default);
}

/// <summary>What one pass over the books turned up.</summary>
public sealed record ObservedEventScanResult(
    int ChequesDueSoon,
    int ReceivablesOverdue,
    int BatchesExpiringSoon,
    int CustomersOverCreditLimit,
    int MachinesDueForMaintenance,
    int BudgetLinesOverspent)
{
    public int Total =>
        ChequesDueSoon
        + ReceivablesOverdue
        + BatchesExpiringSoon
        + CustomersOverCreditLimit
        + MachinesDueForMaintenance
        + BudgetLinesOverspent;

    public static ObservedEventScanResult Empty => new(0, 0, 0, 0, 0, 0);
}
