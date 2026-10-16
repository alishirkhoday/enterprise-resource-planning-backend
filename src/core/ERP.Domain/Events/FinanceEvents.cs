namespace ERP.Domain.Events;

/// <summary>
/// A journal entry reached the ledger - سند حسابداری صادر شد. Until this happens the entry is a
/// draft and affects no balance.
/// </summary>
/// <param name="DocumentId">The document that was posted.</param>
/// <param name="FinancialYearId">The year it lands in, which decides which balances move.</param>
/// <param name="Number">The document number, assigned on posting rather than on drafting.</param>
/// <param name="TotalDebtor">
/// The total debit side. Carried rather than both sides because a posted entry balances by
/// definition, so one figure states the size of the entry without inviting a comparison.
/// </param>
public sealed record JournalEntryPosted(Guid DocumentId, Guid FinancialYearId, string? Number, decimal TotalDebtor) : DomainEvent;

/// <summary>
/// A posted entry was undone by an opposite one. Both identifiers travel together because
/// reversal never edits the original: the audit trail is the pair.
/// </summary>
/// <param name="OriginalDocumentId">The entry that was wrong.</param>
/// <param name="ReversingDocumentId">The entry that cancels it out.</param>
public sealed record JournalEntryReversed(Guid OriginalDocumentId, Guid ReversingDocumentId) : DomainEvent;

/// <summary>
/// A period was closed - بستن دوره مالی. Nothing can be posted into it afterwards, so anything
/// that was going to needs to hear about it.
/// </summary>
/// <param name="AccountingPeriodId">The period that closed.</param>
/// <param name="FinancialYearId">The year it belongs to.</param>
public sealed record AccountingPeriodClosed(Guid AccountingPeriodId, Guid FinancialYearId) : DomainEvent;

/// <summary>The whole year was closed off and the opening balances of the next one settled.</summary>
/// <param name="FinancialYearId">The year that closed.</param>
/// <param name="Title">Its title, as people refer to it.</param>
public sealed record FinancialYearClosed(Guid FinancialYearId, string Title) : DomainEvent;

/// <summary>
/// A cheque was returned unpaid - چک برگشتی. The money that was counted as arriving never did,
/// so whatever it settled has to be reopened.
/// </summary>
/// <param name="ChequeId">The cheque.</param>
/// <param name="Number">Its number, as the bank refers to it - شماره صیاد.</param>
/// <param name="Reason">What the bank gave as the reason.</param>
public sealed record ChequeBounced(Guid ChequeId, string Number, string Reason) : DomainEvent;

/// <summary>Money moved in or out against a party's account and was applied to what it pays off.</summary>
/// <param name="SettlementId">The settlement.</param>
/// <param name="PartyDetailAccountId">Whose account moved.</param>
/// <param name="Amount">How much moved, before allocation.</param>
/// <param name="CurrencyCode">The currency.</param>
public sealed record SettlementConfirmed(Guid SettlementId, Guid PartyDetailAccountId, decimal Amount, string CurrencyCode) : DomainEvent;

/// <summary>
/// A payslip was finalised - حکم حقوق قطعی شد. Carries the net figure only: the breakdown stays
/// on the payslip, where the person it belongs to can see it and nobody else has to.
/// </summary>
/// <param name="PayrollId">The payroll record.</param>
/// <param name="HumanId">Whose pay it is.</param>
/// <param name="NetPayable">What actually gets paid, after deductions.</param>
/// <param name="CurrencyCode">The currency.</param>
public sealed record PayrollFinalized(Guid PayrollId, Guid HumanId, decimal NetPayable, string CurrencyCode) : DomainEvent;
