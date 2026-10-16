using ERP.Application.UseCases.Modules.OrderManagement.DTOs;

namespace ERP.Application.UseCases.Modules.OrderManagement.Commands.PlaceOrder
{
    public record PlaceOrderCommand : PlaceOrderDto, ICommand<Result<PlaceOrderResultDto, Error>>;
}
