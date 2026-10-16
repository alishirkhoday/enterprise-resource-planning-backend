using ERP.Application.UseCases.UsersManagement.Authentication.SignUp;

namespace ERP.WebApi.Controllers.UsersManagement.Authentication
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Auth/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AuthenticationSwaggerGroup)]
    public class SignUpController : BaseController<SignUpResultDto>
    {
        public SignUpController(IMediator mediator, Serilog.ILogger logger) : base(mediator, logger)
        {
        }

        /// <summary>
        /// Sign up user
        /// </summary>
        /// <param name="dto"></param>
        /// <remarks>
        /// Sample request:
        ///     body:
        ///     {
        ///         "username" : "string",
        ///         "password" : "string",
        ///         "mobilePhoneNumberRegionCode" : "string",
        ///         "mobilePhoneNumber" : "string",
        ///         "email" : "string"
        ///     }
        /// Sample response:
        ///     {
        ///         Username,
        ///         MobilePhoneNumberWithRegionCode,
        ///         Email
        ///     }
        /// </remarks>
        /// <returns></returns>
        [HttpPost(Name = "SignUp")]
        public async Task<IActionResult> Index([FromBody] SignUpDto dto)
        {
            var command = new SignUpCommand()
            {
                Username = dto.Username,
                Password = dto.Password,
                MobilePhoneNumberRegionCode = dto.MobilePhoneNumberRegionCode,
                MobilePhoneNumber = dto.MobilePhoneNumber,
                Email = dto.Email
            };
            return FromResult(await _mediator.Send(command));
        }
    }
}
