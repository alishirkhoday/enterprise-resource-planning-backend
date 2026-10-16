using ERP.Application.UseCases.Modules.PricingManagement.Commands.SetPriceListItems;
using ERP.Application.UseCases.Modules.PricingManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.PricingManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/PriceList/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.PriceListSwaggerGroup)]
    public class SetPriceListItemsController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<PriceListDto>(mediator, logger)
    {
        /// <summary>
        /// Put prices on a list
        /// </summary>
        /// <remarks>
        /// Merges by default: a line for an item at an existing quantity break is restated and
        /// anything unmentioned is left alone, which is what a supplier price change looks like.
        /// ReplaceAll is for a complete price file, where a dropped item should stop being priced.
        /// </remarks>
        [HttpPost(Name = "SetPriceListItems")]
        public async Task<IActionResult> Index([FromBody] SetPriceListItemsDto dto)
        {
            var command = new SetPriceListItemsCommand
            {
                PriceListId = dto.PriceListId,
                Lines = dto.Lines,
                ReplaceAll = dto.ReplaceAll
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
