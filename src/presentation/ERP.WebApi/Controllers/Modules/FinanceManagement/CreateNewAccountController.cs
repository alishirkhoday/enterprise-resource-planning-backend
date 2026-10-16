using ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateNewAccount;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.WebApi.Controllers.Modules.FinanceManagement
{
    [ApiController]
    [Route("api/v{version:apiVersion}/Account/[controller]")]
    [ApiVersion(Helpers.ApiVersion)]
    [ApiExplorerSettings(GroupName = Helpers.AccountSwaggerGroup)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "CreateNewAccount")]
    public class CreateNewAccountController : BaseController<string>
    {
        public CreateNewAccountController(IMediator mediator, Serilog.ILogger logger) : base(mediator, logger)
        {
        }

        /// <summary>
        /// Add new Account
        /// </summary>
        /// <param name="dto"></param>
        /// <remarks>
        /// Sample request:
        ///     body:
        ///     {
        ///         "name" : "string",
        ///         "type" : "enum",
        ///         "group" : "enum",
        ///         "balance" : "enum",
        ///         "financialStatement" : "enum",
        ///         "isFinal" : "bool",
        ///         "parentAccountId" : "string",
        ///     }
        ///     Sample response:
        ///     Account Code
        /// </remarks>
        /// <returns></returns>
        [HttpPost(Name = "CreateNewAccount")]
        public async Task<IActionResult> Index([FromBody] AddEditAccountDto dto)
        {
            var command = new CreateNewAccountCommand
            {
                Name = dto.Name,
                Type = dto.Type,
                Group = dto.Group,
                Balance = dto.Balance,
                FinancialStatement = dto.FinancialStatement,
                IsFinal = dto.IsFinal,
                ParentAccountId = dto.ParentAccountId
            };
            return FromResult(await _mediator.Send(command));
        }
    }
}
