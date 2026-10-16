using ERP.Application.UseCases.UsersManagement.Profile.GetUserProfileInformation;
using ERP.WebApi.Services;

namespace ERP.WebApi.Controllers.UsersManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/User/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.UserSwaggerGroup)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GetUserProfileInformationController : BaseController<GetUserProfileInformationResponseDto>
    {
        public GetUserProfileInformationController(IMediator mediator, Serilog.ILogger logger) : base(mediator, logger)
        {
        }

        [HttpGet(Name = "GetUserProfileInformation")]
        public async Task<IActionResult> Index()
        {
            var query = new GetUserProfileInformationQuery
            {
                UserId = User.GetUserID()
            };
            return FromResult(await _mediator.Send(query));
        }
    }
}
