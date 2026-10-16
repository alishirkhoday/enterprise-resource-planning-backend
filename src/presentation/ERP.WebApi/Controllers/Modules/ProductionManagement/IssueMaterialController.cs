using ERP.Application.UseCases.Modules.ProductionManagement.Commands.IssueMaterial;
using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.ProductionManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Production/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.ProductionSwaggerGroup)]
    public class IssueMaterialController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<MaterialIssueDto>(mediator, logger)
    {
        /// <summary>
        /// Take material out of the warehouse and charge it to a works order
        /// </summary>
        /// <remarks>
        /// The stock comes off the shelf, the ledger records why, and the order's material cost
        /// goes up by what it took, all together. Naming no lines issues what the bill of
        /// materials still calls for, less whatever has already gone out; issuing more than the
        /// run needs, or a component the product is not made of, is refused.
        /// </remarks>
        [HttpPost(Name = "IssueMaterial")]
        public async Task<IActionResult> Index([FromBody] IssueMaterialDto dto)
        {
            var command = new IssueMaterialCommand
            {
                ProductionOrderId = dto.ProductionOrderId,
                WarehouseId = dto.WarehouseId,
                IssueDate = dto.IssueDate,
                Lines = dto.Lines,
                OperationSequenceNumber = dto.OperationSequenceNumber,
                IssuedByHumanId = dto.IssuedByHumanId,
                StorageLocationId = dto.StorageLocationId,
                Description = dto.Description
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
