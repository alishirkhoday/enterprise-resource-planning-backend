using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ReceiveGoods
{
    public record ReceiveGoodsCommand : ReceiveGoodsDto, ICommand<Result<GoodsReceiptDto, Error>>;
}
