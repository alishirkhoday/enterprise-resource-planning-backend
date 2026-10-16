using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetAccountLedger
{
    /// <summary>
    /// Every posted movement on one account over a window, with the balance carried down each row.
    /// </summary>
    public sealed record GetAccountLedgerQuery : IQuery<Result<AccountLedgerDto, Error>>
    {
        public Guid AccountId { get; init; }

        public DateOnly From { get; init; }

        public DateOnly To { get; init; }

        public Guid? DetailAccountId { get; init; }

        public Guid? BranchId { get; init; }
    }
}
