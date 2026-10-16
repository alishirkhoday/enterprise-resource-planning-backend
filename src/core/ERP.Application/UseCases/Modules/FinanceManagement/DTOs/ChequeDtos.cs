using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.DTOs
{
    public record RegisterChequeDto
    {
        public ChequeType Type { get; init; }

        public string Number { get; init; } = default!;

        /// <summary>The sixteen digit identifier the central bank's cheque register issues.</summary>
        public string? SayadNumber { get; init; }

        public decimal Amount { get; init; }

        public string CurrencyCode { get; init; } = default!;

        public DateOnly IssueDate { get; init; }

        public DateOnly DueDate { get; init; }

        /// <summary>Who wrote it, for a cheque received from somebody else.</summary>
        public string? BankName { get; init; }

        public string? DrawerName { get; init; }

        public string? AccountNumber { get; init; }

        /// <summary>Who it is payable to, for a cheque this organization writes.</summary>
        public string? PayeeName { get; init; }

        /// <summary>The party account the cheque settles against.</summary>
        public Guid? DetailAccountId { get; init; }

        public Guid? BranchId { get; init; }

        public string? Description { get; init; }
    }

    public sealed record ChequeDto(
        Guid Id,
        ChequeType Type,
        string Number,
        string? SayadNumber,
        decimal Amount,
        string CurrencyCode,
        DateOnly IssueDate,
        DateOnly DueDate,
        ChequeStatus Status,
        string? DrawerName,
        string? PayeeName,
        Guid? DetailAccountId);
}
