using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RecordChequeOperation
{
    /// <summary>What happened to a cheque.</summary>
    public enum ChequeOperationKind
    {
        /// <summary>Handed to the bank for collection.</summary>
        Deposit = 0,

        /// <summary>The money arrived.</summary>
        Clear = 1,

        /// <summary>It did not.</summary>
        Bounce = 2,

        /// <summary>Signed over to somebody else in settlement.</summary>
        Endorse = 3,

        /// <summary>Handed over, for a cheque this organization wrote.</summary>
        Issue = 4,

        /// <summary>Given back to whoever wrote it.</summary>
        Return = 5,

        Cancel = 6
    }

    /// <summary>
    /// One endpoint for every move a cheque can make, rather than seven that differ only in which
    /// method they call. Which moves are legal from which state is the cheque's own business, so
    /// it is not repeated here.
    /// </summary>
    public sealed record RecordChequeOperationCommand : ICommand<Result<ChequeDto, Error>>
    {
        public Guid ChequeId { get; init; }

        public ChequeOperationKind Operation { get; init; }

        public DateOnly OccurredOn { get; init; }

        /// <summary>Required when bouncing: what the bank said.</summary>
        public string? Reason { get; init; }

        /// <summary>Required when depositing: which of our accounts it went into.</summary>
        public Guid? BankAccountId { get; init; }

        /// <summary>Required when endorsing: who it was signed over to.</summary>
        public string? EndorsedTo { get; init; }
    }
}
