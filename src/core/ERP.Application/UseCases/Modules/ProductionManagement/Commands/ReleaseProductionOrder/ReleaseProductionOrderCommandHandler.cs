using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;
using ERP.Application.UseCases.Modules.ProductionManagement.Mapping;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.ReleaseProductionOrder
{
    /// <summary>
    /// Hands a planned order to the shop floor.
    /// <para>
    /// Releasing is what makes material issuable against the order, so it is the moment the rest
    /// of the business starts paying for it. That is why it is a step of its own and not a side
    /// effect of planning.
    /// </para>
    /// </summary>
    public class ReleaseProductionOrderCommandHandler(IProductionOrderRepository productionOrders)
        : IRequestHandler<ReleaseProductionOrderCommand, Result<ProductionOrderDto, Error>>
    {
        public async Task<Result<ProductionOrderDto, Error>> Handle(ReleaseProductionOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await productionOrders.GetWithTasksAsync(request.ProductionOrderId, cancellationToken);
            if (order is null)
                return Result.Failure<ProductionOrderDto, Error>(Errors.General.NotFound(request.ProductionOrderId.ToString(), nameof(ProductionOrder)));

            try
            {
                order.Release();

                if (request.StartImmediately)
                    order.Start();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<ProductionOrderDto, Error>(Errors.Production.OrderCannotBeChanged(exception.Message));
            }

            await productionOrders.UpdateAsync(order, cancellationToken);

            return Result.Success<ProductionOrderDto, Error>(order.ToDto());
        }
    }
}
