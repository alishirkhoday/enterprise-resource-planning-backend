using ERP.Application.Common.ResponseDTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.GetAccountsForOptionList
{
    public record GetAccountsForOptionListQuery : IQuery<Result<IReadOnlyList<GetResponseForOptionListDto>, Error>>
    {
        public string? Search { get; init; }
    }
}
