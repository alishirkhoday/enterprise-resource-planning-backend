namespace ERP.Domain.Events;

/// <summary>Somebody committed to buy. The payment clock starts here.</summary>
/// <param name="OrderId">The order that was placed.</param>
/// <param name="CustomerId">Who placed it.</param>
/// <param name="Code">The order number as the customer sees it.</param>
/// <param name="AmountPayable">What the order came to, after discounts and tax.</param>
/// <param name="CurrencyCode">The currency the amount is in.</param>
public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, string Code, decimal AmountPayable, string CurrencyCode) : DomainEvent;

/// <summary>The order was accepted and is now the warehouse's problem rather than sales'.</summary>
/// <param name="OrderId">The order that was confirmed.</param>
/// <param name="CustomerId">Who it belongs to.</param>
/// <param name="Code">The order number.</param>
public sealed record OrderConfirmed(Guid OrderId, Guid CustomerId, string Code) : DomainEvent;

/// <summary>
/// The order will not be fulfilled. Anything reserved against it has to be released, which is
/// why the reason travels with the fact.
/// </summary>
/// <param name="OrderId">The order that was cancelled.</param>
/// <param name="Code">The order number.</param>
/// <param name="Reason">Why it was cancelled.</param>
public sealed record OrderCancelled(Guid OrderId, string Code, string Reason) : DomainEvent;

/// <summary>Money arrived against an order.</summary>
/// <param name="PaymentId">The payment record.</param>
/// <param name="OrderId">What it paid for.</param>
/// <param name="AmountPaid">How much arrived, which may be less than the order total.</param>
/// <param name="CurrencyCode">The currency it arrived in.</param>
public sealed record PaymentSucceeded(Guid PaymentId, Guid OrderId, decimal AmountPaid, string CurrencyCode) : DomainEvent;

/// <summary>
/// A payment attempt did not go through. Recorded rather than discarded: a customer whose card
/// keeps failing is a different conversation from one who never tried.
/// </summary>
/// <param name="PaymentId">The attempt.</param>
/// <param name="OrderId">What it was for.</param>
/// <param name="Reason">What the gateway said.</param>
public sealed record PaymentFailed(Guid PaymentId, Guid OrderId, string Reason) : DomainEvent;

/// <summary>Money went back to the customer.</summary>
/// <param name="RefundId">The refund record.</param>
/// <param name="PaymentId">The payment it reverses.</param>
/// <param name="Amount">How much was returned, which may be part of the original.</param>
/// <param name="CurrencyCode">The currency.</param>
public sealed record RefundProcessed(Guid RefundId, Guid PaymentId, decimal Amount, string CurrencyCode) : DomainEvent;

/// <summary>
/// An invoice reached the ledger. This is the moment the customer owes the money and the output
/// tax becomes reportable, as against merely having been issued.
/// </summary>
/// <param name="InvoiceId">The invoice.</param>
/// <param name="CustomerId">Who owes it.</param>
/// <param name="Number">The invoice number, which the tax authority will refer to.</param>
/// <param name="AmountPayable">The total owed, including tax and charges.</param>
/// <param name="CurrencyCode">The currency.</param>
public sealed record InvoicePosted(Guid InvoiceId, Guid CustomerId, string Number, decimal AmountPayable, string CurrencyCode) : DomainEvent;

/// <summary>The customer said yes to a quotation, which normally turns into an order.</summary>
/// <param name="QuotationId">The quotation.</param>
/// <param name="CustomerId">Who accepted it.</param>
/// <param name="Number">The quotation number.</param>
public sealed record QuotationAccepted(Guid QuotationId, Guid CustomerId, string Number) : DomainEvent;

/// <summary>
/// A deal was won. Carries the estimated value because that is what a forecast was built on and
/// what a target gets credited with.
/// </summary>
/// <param name="OpportunityId">The deal.</param>
/// <param name="CustomerId">Who it was won with, when the deal reached a customer record.</param>
/// <param name="EstimatedValue">What the deal was valued at.</param>
/// <param name="CurrencyCode">The currency the value is in.</param>
public sealed record OpportunityWon(Guid OpportunityId, Guid? CustomerId, decimal EstimatedValue, string CurrencyCode) : DomainEvent;

/// <summary>
/// A lead became a customer. The link between the two is what lets marketing spend be answered
/// for later: without it nobody can say which campaign paid for which customer.
/// </summary>
/// <param name="LeadId">The lead that closed.</param>
/// <param name="CustomerId">The customer it became.</param>
public sealed record LeadConverted(Guid LeadId, Guid CustomerId) : DomainEvent;
