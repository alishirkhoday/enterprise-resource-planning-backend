using ERP.Application.UseCases.Modules.OrderManagement.Commands.PlaceOrder;
using ERP.Application.UseCases.Modules.OrderManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.OrderManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Order/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.OrderSwaggerGroup)]
    public class PlaceOrderController(IMediator mediator, Serilog.ILogger logger)
        : BaseController<PlaceOrderResultDto>(mediator, logger)
    {
        /// <summary>
        /// Place an order
        /// </summary>
        /// <param name="dto"></param>
        /// <remarks>
        /// Prices are taken from the price list in effect for the customer, not from the request.
        /// The response carries what is owed and the moment the order stops waiting for it.
        ///
        /// Sample request:
        ///     body:
        ///     {
        ///         "customerId" : "guid",
        ///         "currencyCode" : "IRR",
        ///         "items" : [ { "itemId" : "guid", "quantity" : 2 } ],
        ///         "shipTo" : {
        ///             "receiverFullName" : "string",
        ///             "receiverMobileNumber" : "string",
        ///             "country" : "string",
        ///             "province" : "string",
        ///             "city" : "string",
        ///             "region" : "string",
        ///             "street" : "string",
        ///             "plaque" : "string",
        ///             "postalCode" : "string"
        ///         },
        ///         "paymentWindowMinutes" : 30
        ///     }
        /// </remarks>
        /// <returns></returns>
        [HttpPost(Name = "PlaceOrder")]
        public async Task<IActionResult> Index([FromBody] PlaceOrderDto dto)
        {
            var command = new PlaceOrderCommand
            {
                CustomerId = dto.CustomerId,
                CurrencyCode = dto.CurrencyCode,
                Items = dto.Items,
                ShipTo = dto.ShipTo,
                PaymentWindowMinutes = dto.PaymentWindowMinutes,
                WarehouseId = dto.WarehouseId
            };

            return FromResult(await _mediator.Send(command));
        }
    }
}
