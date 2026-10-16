using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetTrialBalance
{
    /// <summary>
    /// Every account that moved in the window, with what it carried in and out. The check anybody
    /// runs before they believe a set of books.
    /// </summary>
    public sealed record GetTrialBalanceQuery : IQuery<Result<TrialBalanceDto, Error>>
    {
        public DateOnly From { get; init; }

        public DateOnly To { get; init; }

        public Guid? BranchId { get; init; }

        /// <summary>
        /// Leave false and accounts that did not move are left out, which is what makes a trial
        /// balance readable. Set it and every account with a balance appears, moved or not.
        /// </summary>
        public bool IncludeAccountsWithNoMovement { get; init; }
    }
}
