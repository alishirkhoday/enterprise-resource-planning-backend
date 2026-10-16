using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RaiseServiceOrder
{
    public record RaiseServiceOrderCommand : RaiseServiceOrderDto, ICommand<Result<ServiceOrderDto, Error>>;
}
