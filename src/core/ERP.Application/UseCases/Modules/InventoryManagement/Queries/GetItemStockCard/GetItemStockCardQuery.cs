using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Queries.GetItemStockCard
{
    /// <summary>
    /// The stock card - کاردکس کالا. The window defaults to the last month, which is what somebody
    /// asking what has been happening to an item usually means.
    /// </summary>
    public sealed record GetItemStockCardQuery : IQuery<Result<ItemStockCardDto, Error>>
    {
        public Guid ItemId { get; init; }

        public Guid WarehouseId { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }
    }
}
