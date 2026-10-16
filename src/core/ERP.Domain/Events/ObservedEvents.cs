namespace ERP.Domain.Events;

/// <summary>
/// Facts that no entity can announce about itself, because nothing happens to the entity when they
/// become true. A cheque does not change when its due date draws near, and a receivable does not
/// change on the morning it falls overdue - the calendar moved, not the record.
/// <para>
/// They are therefore raised by scheduled scans in the application layer rather than from inside an
/// aggregate. The repositories those scans need already exist: <c>GetDueSoonAsync</c>,
/// <c>GetOverdueAsync</c>, <c>GetExpiringAsync</c>, <c>GetOverCreditLimitAsync</c> and
/// <c>GetDueForMaintenanceAsync</c>.
/// </para>
/// <para>
/// A scan is free to run twice, so whatever handles these has to tolerate seeing the same fact
/// again; that is the price of the producer living outside the aggregate.
/// </para>
/// </summary>
public interface IObservedEvent : IDomainEvent;

/// <summary>
/// An invoice passed its due date without being paid - مطالبه معوق. Nothing happened to the
/// receivable; the calendar moved past it.
/// </summary>
/// <param name="ReceivableId">The open item.</param>
/// <param name="PartyDetailAccountId">Whose debt it is.</param>
/// <param name="DaysOverdue">How late it is as at the scan, so a listener can rank by it.</param>
public sealed record ReceivableOverdue(Guid ReceivableId, Guid PartyDetailAccountId, int DaysOverdue) : DomainEvent, IObservedEvent;

/// <summary>
/// A cheque is close to its due date - چک در شرف سررسید. What matters about it is having enough
/// in the account on the day, which is a decision somebody has to make in advance.
/// </summary>
/// <param name="ChequeId">The cheque.</param>
/// <param name="Number">Its number - شماره صیاد.</param>
/// <param name="DueDate">The day it will be presented.</param>
public sealed record ChequeDueSoon(Guid ChequeId, string Number, DateOnly DueDate) : DomainEvent, IObservedEvent;

/// <summary>
/// Spending on a budget line passed what was planned for it. Raised by a scan because the
/// posting that tipped it over belongs to another module and knows nothing about budgets.
/// </summary>
/// <param name="BudgetId">The budget.</param>
/// <param name="BudgetLineId">The line that went over.</param>
/// <param name="AccountId">The account it controls.</param>
/// <param name="OverspentBy">How far past the plan it is.</param>
public sealed record BudgetLineOverspent(Guid BudgetId, Guid BudgetLineId, Guid AccountId, decimal OverspentBy) : DomainEvent, IObservedEvent;

/// <summary>
/// A customer owes more than their limit allows. Distinct from the refusal at invoicing time:
/// this catches the case where the limit was lowered, or where other debts aged into place,
/// after the sales were already made.
/// </summary>
/// <param name="CustomerId">The customer.</param>
/// <param name="CurrentBalance">What they owe now.</param>
/// <param name="CreditLimit">The limit they are over.</param>
public sealed record CustomerCreditLimitExceeded(Guid CustomerId, decimal CurrentBalance, decimal CreditLimit) : DomainEvent, IObservedEvent;

/// <summary>
/// A batch is near its expiry with stock still on the shelf - نزدیک انقضا. The remaining
/// quantity is part of the fact because a batch with nothing left needs no action.
/// </summary>
/// <param name="ItemBatchId">The batch.</param>
/// <param name="ItemId">What it is a batch of.</param>
/// <param name="ExpirationDate">When it expires.</param>
/// <param name="RemainingQuantity">How much is still on hand.</param>
public sealed record ItemBatchExpiringSoon(Guid ItemBatchId, Guid ItemId, DateOnly ExpirationDate, decimal RemainingQuantity) : DomainEvent, IObservedEvent;

/// <summary>
/// A machine has run long enough to be serviced. Measured in hours since the last service rather
/// than by date, because a machine that sat idle for a month does not need servicing for it.
/// </summary>
/// <param name="MachineId">The machine.</param>
/// <param name="Code">Its code.</param>
/// <param name="RunningHoursSinceService">Hours run since it was last serviced.</param>
public sealed record MachineDueForMaintenance(Guid MachineId, string Code, decimal RunningHoursSinceService) : DomainEvent, IObservedEvent;
