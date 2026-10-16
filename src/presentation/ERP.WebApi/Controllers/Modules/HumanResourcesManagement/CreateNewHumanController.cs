using ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.CreateNewHuman;
using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.HumanResourcesManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Human/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.HumanSwaggerGroup)]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "CreateNewHuman")]
    public class CreateNewHumanController : BaseController<string>
    {
        public CreateNewHumanController(IMediator mediator, Serilog.ILogger logger) : base(mediator, logger)
        {
        }

        /// <summary>
        /// Add new Human
        /// </summary>
        /// <param name="dto"></param>
        /// <remarks>
        /// Sample request:
        ///     body:
        ///     {
        ///         "username" : "string",
        ///         "password" : "string",
        ///         "name" : "string",
        ///         "family" : "string",
        ///         "gender" : "enum",
        ///         "birthDate" : "date",
        ///         "maritalStatus" : "bool",
        ///         "nationalId" : "string",
        ///         "passportId" : "string"
        ///     }
        ///     Sample response:
        ///     Full name
        /// </remarks>
        /// <returns></returns>
        [HttpPost(Name = "CreateNewHuman")]
        public async Task<IActionResult> Index([FromBody] AddEditHumanDto dto)
        {
            var command = new CreateNewHumanCommand
            {
                Username = dto.Username,
                Password = dto.Password,
                Name = dto.Name,
                Family = dto.Family,
                Gender = dto.Gender,
                BirthDate = dto.BirthDate,
                MaritalStatus = dto.MaritalStatus,
                NationalId = dto.NationalId,
                PassportId = dto.PassportId
            };
            return FromResult(await _mediator.Send(command));
        }
    }
}
