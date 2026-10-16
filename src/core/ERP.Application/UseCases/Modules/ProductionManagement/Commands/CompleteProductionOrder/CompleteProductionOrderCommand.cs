using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.CompleteProductionOrder
{
    public record CompleteProductionOrderCommand : CompleteProductionOrderDto, ICommand<Result<ProductionOrderDto, Error>>;
}
