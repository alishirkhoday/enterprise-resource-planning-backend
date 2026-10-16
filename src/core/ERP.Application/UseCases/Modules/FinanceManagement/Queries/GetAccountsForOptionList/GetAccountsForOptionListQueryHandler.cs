using ERP.Application.Common.ResponseDTOs;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetAccountsForOptionList
{
    public class GetAccountsForOptionListQueryHandler(IAccountRepository accountRepository) : IRequestHandler<GetAccountsForOptionListQuery, Result<IReadOnlyList<GetResponseForOptionListDto>, Error>>
    {
        private readonly IAccountRepository _accountRepository = accountRepository;

        public async Task<Result<IReadOnlyList<GetResponseForOptionListDto>, Error>> Handle(GetAccountsForOptionListQuery request, CancellationToken cancellationToken)
        {
            var accounts = await _accountRepository.GetForOptionListAsync(
                a => new GetResponseForOptionListDto(a.Id.ToString(), a.Name),
                a => a.IsFinal == true && a.Name.Contains(request.Search ?? ""),
                a => a.Name,
                cancellationToken: cancellationToken);
            return Result.Success<IReadOnlyList<GetResponseForOptionListDto>, Error>(accounts);
        }
    }
}
