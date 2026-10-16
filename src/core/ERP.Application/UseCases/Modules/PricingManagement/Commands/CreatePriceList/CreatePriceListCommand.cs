using ERP.Application.UseCases.Modules.PricingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.PricingManagement.Commands.CreatePriceList
{
    public record CreatePriceListCommand : CreatePriceListDto, ICommand<Result<PriceListDto, Error>>;
}
