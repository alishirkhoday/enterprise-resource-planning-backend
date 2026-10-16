using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.ReleaseProductionOrder
{
    public record ReleaseProductionOrderCommand : ReleaseProductionOrderDto, ICommand<Result<ProductionOrderDto, Error>>;
}
