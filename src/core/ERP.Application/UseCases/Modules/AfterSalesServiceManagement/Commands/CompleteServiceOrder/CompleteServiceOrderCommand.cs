using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.CompleteServiceOrder
{
    public record CompleteServiceOrderCommand : CompleteServiceOrderDto, ICommand<Result<ServiceOrderDto, Error>>;
}
