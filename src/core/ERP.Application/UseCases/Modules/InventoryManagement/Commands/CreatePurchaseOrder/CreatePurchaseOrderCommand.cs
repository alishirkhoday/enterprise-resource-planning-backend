using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreatePurchaseOrder
{
    public record CreatePurchaseOrderCommand : CreatePurchaseOrderDto, ICommand<Result<PurchaseOrderDto, Error>>;
}
