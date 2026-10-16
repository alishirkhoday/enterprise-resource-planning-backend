using ERP.Application.UseCases.UsersManagement.Authentication.SignIn;

namespace ERP.WebApi.Controllers.UsersManagement.Authentication
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Auth/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AuthenticationSwaggerGroup)]
    public class SignInController : BaseController<string>
    {
        public SignInController(IMediator mediator, Serilog.ILogger logger) : base(mediator, logger)
        {
        }

        /// <summary>
        /// Sign in user
        /// </summary>
        /// <param name="dto"></param>
        /// <remarks>
        /// Sample request:
        ///     body:
        ///     {
        ///         "username" : "string",
        ///         "password" : "string"
        ///     }
        ///     Sample response:
        ///     Token
        /// </remarks>
        /// <returns></returns>
        [HttpPost(Name = "SignIn")]
        public async Task<IActionResult> Index([FromBody] SignInDto dto)
        {
            var command = new SignInCommand()
            {
                Username = dto.Username,
                Password = dto.Password,
                InternetProtocolAddress = Request.Headers.Host.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                DeviceName = Request.Headers["sec-ch-ua"].ToString(),
                OperatingSystem = Request.Headers["sec-ch-ua-platform"].ToString()
            };
            return FromResult(await _mediator.Send(command));
        }
    }
}
