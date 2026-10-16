using ERP.Application.UseCases.UsersManagement.Authentication.Verify;

namespace ERP.WebApi.Controllers.UsersManagement.Authentication
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Auth/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AuthenticationSwaggerGroup)]
    public class VerifyController : BaseController<string>
    {
        public VerifyController(IMediator mediator, Serilog.ILogger logger) : base(mediator, logger)
        {
        }

        /// <summary>
        /// Verify user
        /// </summary>
        /// <param name="dto"></param>
        /// <remarks>
        /// Sample request:
        ///     body:
        ///     {
        ///         "username" : "string",
        ///         "mobilePhoneNumberRegionCode" : "string",
        ///         "mobilePhoneNumber" : "string",
        ///         "email" : "string",
        ///         "code" : "string"
        ///     }
        ///     Sample response:
        ///     Username
        /// </remarks>
        /// <returns></returns>
        [HttpPost(Name = "Verify")]
        public async Task<IActionResult> Index([FromBody] VerifyDto dto)
        {
            var command = new VerifyCommand()
            {
                Username = dto.Username,
                MobilePhoneNumberRegionCode = dto.MobilePhoneNumberRegionCode,
                MobilePhoneNumber = dto.MobilePhoneNumber,
                Email = dto.Email,
                Code = dto.Code
            };
            return FromResult(await _mediator.Send(command));
        }
    }
}
