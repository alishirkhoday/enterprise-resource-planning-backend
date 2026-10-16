using ERP.Application.UseCases.Modules.PricingManagement.Commands.CreatePriceList;
using ERP.Application.UseCases.Modules.PricingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.PricingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/PriceList/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PriceListSwaggerGroup)]
    public class CreatePriceListController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<PriceListDto>(mediator, logger)
    {
        /// <summary>
        /// Set up a price list
        /// </summary>
        /// <remarks>
        /// Price lists are what let an invoice refuse a quietly undercut price. Marking one as
        /// default takes that position off whichever list holds it, so two defaults can never
        /// exist and the resolution never has to pick arbitrarily.
        /// </remarks>
        [HttpPost(Name = "CreatePriceList")]
        public async Task<IActionResult> Index([FromBody] CreatePriceListDto dto)
        {
            var command = new CreatePriceListCommand
            {
                Code = dto.Code,
                Name = dto.Name,
                Type = dto.Type,
                CurrencyCode = dto.CurrencyCode,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CustomerGroupId = dto.CustomerGroupId,
                BranchId = dto.BranchId,
                IsTaxInclusive = dto.IsTaxInclusive,
                IsDefault = dto.IsDefault,
                Description = dto.Description,
                Lines = dto.Lines
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
