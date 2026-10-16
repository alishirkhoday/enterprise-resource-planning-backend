using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.ReverseJournalEntry
{
    /// <summary>
    /// Undoes a posted entry by writing its mirror image, because a posted entry is never edited
    /// or removed - that is what makes the ledger worth trusting.
    /// </summary>
    public sealed record ReverseJournalEntryCommand : ICommand<Result<JournalEntryDto, Error>>
    {
        public Guid DocumentId { get; init; }

        public string Reason { get; init; } = default!;

        /// <summary>
        /// When the reversal is dated. Left empty it falls on the original's date, which keeps the
        /// pair inside one period; given a date it can be booked into the period that is still
        /// open, which is what a month-end correction needs.
        /// </summary>
        public DateTimeOffset? ReverseOn { get; init; }
    }
}
