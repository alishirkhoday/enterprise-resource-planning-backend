using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.IssueMaterial
{
    public record IssueMaterialCommand : IssueMaterialDto, ICommand<Result<MaterialIssueDto, Error>>;
}
