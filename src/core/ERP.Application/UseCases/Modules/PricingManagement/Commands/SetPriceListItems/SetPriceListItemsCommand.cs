using ERP.Application.UseCases.Modules.PricingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.PricingManagement.Commands.SetPriceListItems
{
    public record SetPriceListItemsCommand : SetPriceListItemsDto, ICommand<Result<PriceListDto, Error>>;
}
