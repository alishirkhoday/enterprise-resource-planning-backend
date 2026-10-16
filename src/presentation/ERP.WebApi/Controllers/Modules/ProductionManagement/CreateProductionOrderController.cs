using ERP.Application.UseCases.Modules.ProductionManagement.Commands.CreateProductionOrder;
using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProductionManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Production/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProductionSwaggerGroup)]
    public class CreateProductionOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<ProductionOrderDto>(mediator, logger)
    {
        /// <summary>
        /// Raise a works order and plan it
        /// </summary>
        /// <remarks>
        /// The recipe is pinned to the order as it is created, using the version approved and in
        /// effect on the planned start date. Bills of materials get revised, and an order that
        /// looked the recipe up later would be costed against a formula it was never built to.
        /// </remarks>
        [HttpPost(Name = "CreateProductionOrder")]
        public async Task<IActionResult> Index([FromBody] CreateProductionOrderDto dto)
        {
            var command = new CreateProductionOrderCommand
            {
                ItemId = dto.ItemId,
                Quantity = dto.Quantity,
                CurrencyCode = dto.CurrencyCode,
                PlannedStartDate = dto.PlannedStartDate,
                DueDate = dto.DueDate,
                Priority = dto.Priority,
                BillOfMaterialsId = dto.BillOfMaterialsId,
                RoutingId = dto.RoutingId,
                WarehouseId = dto.WarehouseId,
                CostCenterId = dto.CostCenterId,
                UnitOfMeasureId = dto.UnitOfMeasureId,
                BranchId = dto.BranchId,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
