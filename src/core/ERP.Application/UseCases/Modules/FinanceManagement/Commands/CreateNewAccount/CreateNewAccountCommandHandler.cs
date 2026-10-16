using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateNewAccount
{
    /// <summary>
    /// Opens an account in the chart. What the code should be is the domain's business; this reads
    /// the request, counts what already exists so the domain can number it, and reports which of
    /// the rules it broke if it broke one.
    /// </summary>
    public class CreateNewAccountCommandHandler(IAccountRepository accountRepository)
        : IRequestHandler<CreateNewAccountCommand, Result<string, Error>>
    {
        private readonly IAccountRepository _accountRepository = accountRepository;

        public async Task<Result<string, Error>> Handle(CreateNewAccountCommand request, CancellationToken cancellationToken)
        {
            if (await _accountRepository.IsExistenceByNameAsync(request.Name, cancellationToken))
                return Result.Failure<string, Error>(Errors.Account.NameIsUsed(request.Name));

            return request.Group == AccountGroup.LevelOne
                ? await CreateTopLevelAsync(request, cancellationToken)
                : await CreateChildAsync(request, cancellationToken);
        }

        private async Task<Result<string, Error>> CreateTopLevelAsync(CreateNewAccountCommand request, CancellationToken cancellationToken)
        {
            var existingCount = await _accountRepository.GetCountAccountsByAccountTypeAndAccountGroupAsync(
                request.Type,
                AccountGroup.LevelOne,
                cancellationToken);

            Account account;
            try
            {
                account = Account.CreateTopLevel(
                    request.Name,
                    request.Type,
                    request.Balance,
                    request.FinancialStatement,
                    request.IsFinal,
                    existingCount);
            }
            catch (InvalidOperationException)
            {
                // The only way the domain refuses a top level account is a block that has run out
                // of numbers, which is a chart outgrowing its scheme rather than a bad request.
                return Result.Failure<string, Error>(Errors.Account.CodeBlockIsFull(request.Type));
            }

            await _accountRepository.CreateAsync(account, cancellationToken);
            return Result.Success<string, Error>(account.Code);
        }

        private async Task<Result<string, Error>> CreateChildAsync(CreateNewAccountCommand request, CancellationToken cancellationToken)
        {
            if (request.ParentAccountId is null)
                return Result.Failure<string, Error>(Errors.Account.ParentAccountIdIsEmpty());

            var parent = await _accountRepository.GetByIdAsync(request.ParentAccountId, cancellationToken);
            if (parent is null)
                return Result.Failure<string, Error>(Errors.Account.ParentAccountNotFound());

            // Checked here as well as in the domain, so the caller is told which rule they broke
            // instead of being handed an exception. The domain keeps its own guards for the callers
            // that are not this one.
            if (parent.IsFinal)
                return Result.Failure<string, Error>(Errors.Account.ParentAccountIsFinal());

            if (request.Group <= parent.Group)
                return Result.Failure<string, Error>(Errors.Account.ParentAccountGroupIsEqualToAccountGroup());

            if (request.Type != parent.Type)
                return Result.Failure<string, Error>(Errors.Account.AccountTypeDoesNotMatchParent(request.Type, parent.Type));

            var existingChildren = await _accountRepository.GetCountChildAccountsByAccountIdAsync(parent.Id, cancellationToken);

            var account = parent.CreateChild(
                request.Name,
                request.Group,
                request.Balance,
                request.FinancialStatement,
                request.IsFinal,
                existingChildren);

            await _accountRepository.CreateAsync(account, cancellationToken);
            return Result.Success<string, Error>(account.Code);
        }
    }
}
