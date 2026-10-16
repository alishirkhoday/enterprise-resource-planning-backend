using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.CreateProductionOrder
{
    public record CreateProductionOrderCommand : CreateProductionOrderDto, ICommand<Result<ProductionOrderDto, Error>>;
}
