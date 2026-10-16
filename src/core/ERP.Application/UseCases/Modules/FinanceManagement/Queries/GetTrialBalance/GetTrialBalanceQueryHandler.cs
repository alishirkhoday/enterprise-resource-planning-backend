using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetTrialBalance
{
    /// <summary>
    /// Builds the trial balance from two grouped queries: what every account carried in, and what
    /// it moved. Two round trips for the whole chart rather than two per account.
    /// </summary>
    public class GetTrialBalanceQueryHandler(IAccountRepository accounts, IDocumentRepository documents)
        : IRequestHandler<GetTrialBalanceQuery, Result<TrialBalanceDto, Error>>
    {
        public async Task<Result<TrialBalanceDto, Error>> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
        {
            if (request.To < request.From)
                return Result.Failure<TrialBalanceDto, Error>(Errors.General.AnErrorHasOccurred());

            var opening = await documents.GetTurnoverByAccountAsync(null, request.From.AddDays(-1), request.BranchId, cancellationToken);
            var movement = await documents.GetTurnoverByAccountAsync(request.From, request.To, request.BranchId, cancellationToken);

            var openingByAccount = opening.ToDictionary(o => o.AccountId);
            var movementByAccount = movement.ToDictionary(m => m.AccountId);

            // An account that only carried a balance forward still belongs on the report, so the
            // two sets are unioned rather than the movements alone being walked.
            var accountIds = request.IncludeAccountsWithNoMovement
                ? openingByAccount.Keys.Union(movementByAccount.Keys).ToHashSet()
                : [.. movementByAccount.Keys];

            var postable = await accounts.GetPostableAccountsAsync(cancellationToken);
            var byId = postable.ToDictionary(a => a.Id);

            var rows = new List<TrialBalanceRowDto>(accountIds.Count);

            foreach (var accountId in accountIds)
            {
                if (!byId.TryGetValue(accountId, out var account))
                    continue;

                var carried = openingByAccount.GetValueOrDefault(accountId);
                var moved = movementByAccount.GetValueOrDefault(accountId);

                var openingBalance = account.SignedAmount(carried?.Debtor ?? 0m, carried?.Creditor ?? 0m);
                var debtor = moved?.Debtor ?? 0m;
                var creditor = moved?.Creditor ?? 0m;

                rows.Add(new TrialBalanceRowDto(
                    account.Id,
                    account.Code,
                    account.Name,
                    openingBalance,
                    debtor,
                    creditor,
                    openingBalance + account.SignedAmount(debtor, creditor)));
            }

            var orderedRows = rows.OrderBy(r => r.AccountCode, StringComparer.Ordinal).ToArray();
            var totalDebtor = orderedRows.Sum(r => r.Debtor);
            var totalCreditor = orderedRows.Sum(r => r.Creditor);

            return Result.Success<TrialBalanceDto, Error>(new TrialBalanceDto(
                request.From,
                request.To,
                totalDebtor,
                totalCreditor,
                totalDebtor == totalCreditor,
                orderedRows));
        }
    }
}
