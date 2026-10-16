using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.DTOs
{
    /// <summary>What a caller sends to open a journal entry.</summary>
    public record CreateJournalEntryDto
    {
        public string Title { get; init; } = default!;

        public string Description { get; init; } = default!;

        public DocumentType Type { get; init; }

        public DateTimeOffset FinancialEventDateTime { get; init; }

        public IReadOnlyList<JournalEntryLineDto> Lines { get; init; } = [];

        public Guid? BranchId { get; init; }

        /// <summary>What the entry is about, when it was raised by another document.</summary>
        public string? ReferenceType { get; init; }

        public string? ReferenceId { get; init; }
    }

    /// <summary>
    /// One side of one line. Debit and credit are separate fields rather than a signed amount,
    /// because that is how a ledger is read and how an accountant checks it.
    /// </summary>
    public record JournalEntryLineDto
    {
        public Guid AccountId { get; init; }

        public decimal Debtor { get; init; }

        public decimal Creditor { get; init; }

        public string? Description { get; init; }

        public Guid? DetailAccount1Id { get; init; }

        public Guid? DetailAccount2Id { get; init; }

        public Guid? DetailAccount3Id { get; init; }

        public Guid? CostCenterId { get; init; }

        public Guid? ProjectId { get; init; }
    }

    /// <summary>A journal entry as it is read back.</summary>
    public sealed record JournalEntryDto(
        Guid Id,
        string? Number,
        string Title,
        DocumentType Type,
        DocumentStatus Status,
        DateTimeOffset FinancialEventDateTime,
        string CurrencyCode,
        decimal TotalDebtor,
        decimal TotalCreditor,
        bool IsBalanced,
        DateTimeOffset? PostedOn,
        string? PostedBy,
        Guid? ReversedByDocumentId,
        IReadOnlyList<JournalEntryLineViewDto> Lines);

    public sealed record JournalEntryLineViewDto(
        Guid Id,
        short Priority,
        Guid AccountId,
        string AccountCode,
        string AccountName,
        decimal Debtor,
        decimal Creditor,
        string? Description);
}
