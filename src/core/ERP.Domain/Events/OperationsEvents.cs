namespace ERP.Domain.Events;

/// <summary>
/// A works order was handed to the shop floor - صدور سفارش تولید. This is the point material can
/// be issued against it, so it is also the point the order starts costing money.
/// </summary>
/// <param name="ProductionOrderId">The order.</param>
/// <param name="Code">Its code.</param>
/// <param name="ItemId">What is being made.</param>
/// <param name="Quantity">How much was ordered, not how much has been made.</param>
public sealed record ProductionOrderReleased(Guid ProductionOrderId, string Code, Guid ItemId, decimal Quantity) : DomainEvent;

/// <summary>
/// A works order finished. Good output and scrap are carried separately because they mean
/// different things: scrap is costed but never becomes stock.
/// </summary>
/// <param name="ProductionOrderId">The order.</param>
/// <param name="Code">Its code.</param>
/// <param name="ProducedQuantity">What came off the line usable.</param>
/// <param name="ScrappedQuantity">What came off unusable.</param>
public sealed record ProductionOrderCompleted(Guid ProductionOrderId, string Code, decimal ProducedQuantity, decimal ScrappedQuantity) : DomainEvent;

/// <summary>
/// An inspection failed - کنترل کیفیت رد شد. Raised whether the failure was a quantity that did
/// not pass or a reading outside tolerance, since both mean the batch cannot go out as it is.
/// </summary>
/// <param name="CheckId">The inspection.</param>
/// <param name="ProductionOrderId">The works order, when the inspection was against one.</param>
/// <param name="FailedQuantity">How many units did not pass.</param>
public sealed record QualityCheckFailed(Guid CheckId, Guid? ProductionOrderId, decimal FailedQuantity) : DomainEvent;

/// <summary>
/// A machine stopped unexpectedly. Whatever was scheduled on it has to be moved, which is why
/// this is a fact the rest of the system hears rather than a field on the machine.
/// </summary>
/// <param name="MachineId">The machine.</param>
/// <param name="Code">Its code.</param>
/// <param name="Reason">What went wrong.</param>
public sealed record MachineBrokeDown(Guid MachineId, string Code, string Reason) : DomainEvent;

/// <summary>
/// Something is waiting on a person to approve it. The reference is a type and an identifier
/// rather than a foreign key because approval spans modules: a purchase order, a leave request
/// and a budget all queue through the same mechanism.
/// </summary>
/// <param name="ApprovalRequestId">The request.</param>
/// <param name="ReferenceType">What kind of thing needs approving.</param>
/// <param name="ReferenceId">Which one.</param>
/// <param name="StepOrder">Which step of the chain is waiting, counted from one.</param>
public sealed record ApprovalRequested(Guid ApprovalRequestId, string ReferenceType, string ReferenceId, int StepOrder) : DomainEvent;

/// <summary>The approval chain finished and the answer was yes.</summary>
/// <param name="ApprovalRequestId">The request.</param>
/// <param name="ReferenceType">What kind of thing was approved.</param>
/// <param name="ReferenceId">Which one.</param>
public sealed record ApprovalGranted(Guid ApprovalRequestId, string ReferenceType, string ReferenceId) : DomainEvent;

/// <summary>The answer was no. The reason travels with it, since that is what gets fed back.</summary>
/// <param name="ApprovalRequestId">The request.</param>
/// <param name="ReferenceType">What kind of thing was refused.</param>
/// <param name="ReferenceId">Which one.</param>
/// <param name="Reason">Why it was refused.</param>
public sealed record ApprovalRejected(Guid ApprovalRequestId, string ReferenceType, string ReferenceId, string Reason) : DomainEvent;

/// <summary>
/// Leave was granted - مرخصی تأیید شد. The dates travel with it because whoever schedules work
/// needs them, and reading the request back would mean a query per approval.
/// </summary>
/// <param name="RequestId">The request.</param>
/// <param name="HumanId">Whose leave it is.</param>
/// <param name="StartDate">First day off.</param>
/// <param name="EndDate">Last day off, inclusive.</param>
public sealed record LeaveRequestApproved(Guid RequestId, Guid HumanId, DateOnly StartDate, DateOnly EndDate) : DomainEvent;

/// <summary>
/// Somebody left the company. Almost every module holds something that has to be closed off when
/// this happens - assets in their custody, open approvals, system access.
/// </summary>
/// <param name="HumanId">Who left.</param>
/// <param name="TerminationDate">Their last day.</param>
public sealed record EmployeeTerminated(Guid HumanId, DateOnly TerminationDate) : DomainEvent;

/// <summary>
/// A project milestone was reached - رسیدن به مایلستون. Often the trigger for billing a stage of
/// the contract, which is why the date is part of the fact.
/// </summary>
/// <param name="MilestoneId">The milestone.</param>
/// <param name="ProjectId">The project it belongs to.</param>
/// <param name="AchievedOn">The day it was reached, which may be before it was recorded.</param>
public sealed record ProjectMilestoneAchieved(Guid MilestoneId, Guid ProjectId, DateOnly AchievedOn) : DomainEvent;

/// <summary>
/// A planning run finished. The shortage count travels with it so a subscriber can decide whether
/// anyone needs waking up without reloading the whole plan.
/// </summary>
/// <param name="PlanId">The run.</param>
/// <param name="Code">Its code.</param>
/// <param name="ShortageLineCount">How many lines came back short.</param>
public sealed record MaterialRequirementPlanCompleted(Guid PlanId, string Code, int ShortageLineCount) : DomainEvent;

/// <summary>
/// A service visit was finished - خدمات پس از فروش. The customer travels with it because what
/// usually follows is either an invoice or a warranty claim, and both are about them.
/// </summary>
/// <param name="ServiceOrderId">The service order.</param>
/// <param name="Number">Its number.</param>
/// <param name="CustomerId">Who it was for.</param>
/// <param name="CompletedOn">The day the work was finished.</param>
public sealed record ServiceOrderCompleted(Guid ServiceOrderId, string Number, Guid CustomerId, DateOnly CompletedOn) : DomainEvent;

/// <summary>A letter landed in somebody's queue and they have not been told yet.</summary>
/// <param name="LetterId">The letter - نامه.</param>
/// <param name="Number">Its registry number - شماره اندیکاتور.</param>
/// <param name="ToUserId">Whose queue it landed in.</param>
/// <param name="ReferralId">The referral itself, which is what gets answered.</param>
public sealed record LetterReferred(Guid LetterId, string Number, Guid ToUserId, Guid ReferralId) : DomainEvent;
