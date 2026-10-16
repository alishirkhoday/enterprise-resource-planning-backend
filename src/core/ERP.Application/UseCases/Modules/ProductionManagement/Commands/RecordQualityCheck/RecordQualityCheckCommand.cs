using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.RecordQualityCheck
{
    public record RecordQualityCheckCommand : RecordQualityCheckDto, ICommand<Result<QualityCheckDto, Error>>;
}
