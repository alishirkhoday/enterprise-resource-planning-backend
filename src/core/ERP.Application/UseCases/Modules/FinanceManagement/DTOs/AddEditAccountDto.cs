using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.DTOs
{
    public record AddEditAccountDto
    {
        public string Name { get; init; } = default!;
        public AccountType Type { get; init; }
        public AccountGroup Group { get; init; }
        public AccountBalance Balance { get; init; }
        public FinancialStatement FinancialStatement { get; init; }
        public bool IsFinal { get; init; }
        public string? ParentAccountId { get; init; }
    }
}
