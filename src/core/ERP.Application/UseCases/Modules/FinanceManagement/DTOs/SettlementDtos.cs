using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.DTOs
{
    /// <summary>
    /// Money in or out, applied against what it pays off - تسویه حساب.
    /// <para>
    /// Receiving money and deciding which invoices it settles are one act, not two. Split apart,
    /// cash sits unapplied and a customer who has paid still shows as overdue.
    /// </para>
    /// </summary>
    public record SettleAccountDto
    {
        public SettlementDirection Direction { get; init; }

        public SettlementMethod Method { get; init; }

        /// <summary>Whose account it moves: the customer's or supplier's detail account.</summary>
        public Guid PartyDetailAccountId { get; init; }

        public decimal Amount { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public DateOnly SettlementDate { get; init; }

        /// <summary>
        /// What the money pays off. Leaving it empty is only allowed when the method is
        /// <see cref="SettlementMethod.Advance"/>: money taken with nothing to apply it to has to
        /// say so, or it sits on the account unexplained and somebody writes it off later.
        /// </summary>
        public IReadOnlyList<SettlementAllocationDto> Allocations { get; init; } = [];

        public Guid? CashBoxId { get; init; }

        public Guid? BankBranchAccountId { get; init; }

        public Guid? ChequeId { get; init; }

        public Guid? DocumentId { get; init; }

        public Guid? BranchId { get; init; }

        public string? Number { get; init; }

        public string? TrackingCode { get; init; }

        public string? Description { get; init; }
    }

    public record SettlementAllocationDto
    {
        /// <summary>One of these two, never both: money goes against a debt in one direction.</summary>
        public Guid? ReceivableId { get; init; }

        public Guid? PayableId { get; init; }

        public decimal Amount { get; init; }

        /// <summary>An early settlement discount taken on this item.</summary>
        public decimal? DiscountTaken { get; init; }

        public string? Description { get; init; }
    }

    public sealed record SettlementDto(
        Guid Id,
        string? Number,
        SettlementDirection Direction,
        SettlementMethod Method,
        SettlementStatus Status,
        Guid PartyDetailAccountId,
        DateOnly SettlementDate,
        string CurrencyCode,
        decimal Amount,
        decimal AllocatedAmount,
        decimal UnallocatedAmount,
        IReadOnlyList<SettlementAllocationViewDto> Allocations);

    public sealed record SettlementAllocationViewDto(
        Guid Id,
        Guid? ReceivableId,
        Guid? PayableId,
        decimal Amount,
        decimal? DiscountTaken);
}
