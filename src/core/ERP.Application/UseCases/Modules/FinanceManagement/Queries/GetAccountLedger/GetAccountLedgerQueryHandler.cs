using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetAccountLedger
{
    /// <summary>
    /// One account's movements, with the balance carried down.
    /// <para>
    /// The running balance is accumulated here rather than in the database. It depends on the row
    /// before it, which is the one thing a set-based query cannot express without a window
    /// function, and the rows are already in memory by then anyway.
    /// </para>
    /// </summary>
    public class GetAccountLedgerQueryHandler(IAccountRepository accounts, IDocumentRepository documents)
        : IRequestHandler<GetAccountLedgerQuery, Result<AccountLedgerDto, Error>>
    {
        public async Task<Result<AccountLedgerDto, Error>> Handle(GetAccountLedgerQuery request, CancellationToken cancellationToken)
        {
            var account = await accounts.GetByIdAsync(request.AccountId, cancellationToken);
            if (account is null)
                return Result.Failure<AccountLedgerDto, Error>(Errors.Account.AccountNotFound());

            // Everything posted before the window, netted, is what the account carried into it.
            var priorTurnover = await documents.GetTurnoverByAccountAsync(
                from: null,
                to: request.From.AddDays(-1),
                request.BranchId,
                cancellationToken);

            var prior = priorTurnover.FirstOrDefault(t => t.AccountId == account.Id);
            var opening = account.SignedAmount(prior?.Debtor ?? 0m, prior?.Creditor ?? 0m);

            var lines = await documents.GetLedgerLinesAsync(
                request.AccountId,
                request.From,
                request.To,
                request.DetailAccountId,
                request.BranchId,
                cancellationToken);

            var rows = new List<AccountLedgerRowDto>(lines.Count);
            var running = opening;

            foreach (var line in lines)
            {
                // Signed by the account's own normal balance, so a debit on a liability reduces it
                // rather than adding to it.
                running += account.SignedAmount(line.Debtor, line.Creditor);

                rows.Add(new AccountLedgerRowDto(
                    line.DocumentId,
                    line.DocumentNumber,
                    line.FinancialEventDateTime,
                    line.Title,
                    line.Description,
                    line.Debtor,
                    line.Creditor,
                    running));
            }

            return Result.Success<AccountLedgerDto, Error>(new AccountLedgerDto(
                account.Id,
                account.Code,
                account.Name,
                request.From,
                request.To,
                opening,
                lines.Sum(l => l.Debtor),
                lines.Sum(l => l.Creditor),
                running,
                rows));
        }
    }
}
