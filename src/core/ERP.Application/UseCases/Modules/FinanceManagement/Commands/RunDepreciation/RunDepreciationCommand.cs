using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RunDepreciation
{
    public record RunDepreciationCommand : RunDepreciationDto, ICommand<Result<DepreciationRunDto, Error>>;
}
