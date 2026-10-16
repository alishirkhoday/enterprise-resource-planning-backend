using CSharpFunctionalExtensions;
using ERP.Application.Common;

namespace ERP.WebApi.Controllers
{
    public class BaseController<T>(IMediator mediator, Serilog.ILogger logger) : ControllerBase where T : class
    {
        protected readonly IMediator _mediator = mediator;
        protected readonly Serilog.ILogger _logger = logger;

        /// <summary>
        /// Error codes that mean the thing the caller asked for is not there, as opposed to the
        /// request being wrong. Matched on the code: the messages carry identifiers and dates, so
        /// no two instances of a not-found error are ever the same object.
        /// </summary>
        private static readonly HashSet<string> NotFoundCodes = new(StringComparer.Ordinal)
        {
            "RecordNotFound",
            "UserNotFound",
            "AccountNotFound",
            "ParentAccountNotFound"
        };

        /// <summary>Error codes that mean the caller is not who they need to be.</summary>
        private static readonly HashSet<string> ForbiddenCodes = new(StringComparer.Ordinal)
        {
            "UserIsBan",
            "UserIsNotActive",
            "UserAccountIsLocked"
        };

        /// <summary>Error codes that mean somebody else already has it.</summary>
        private static readonly HashSet<string> ConflictCodes = new(StringComparer.Ordinal)
        {
            "RecordAlreadyExistence",
            "UsernameIsUsed",
            "NameIsUsed",
            "NationalIdIsUsed",
            "PassportIdIsUsed",
            "UserEmailIsAlreadyRegistered",
            "UserMobilePhoneNumberIsAlreadyRegistered"
        };

        protected IActionResult Ok(T result)
        {
            return base.Ok(Envelope<T>.Ok(result));
        }

        /// <summary>
        /// Turns a handler's answer into a response. The mapping lives here so every endpoint
        /// answers the same way, and so a caller can tell "you asked for something that is not
        /// there" apart from "what you sent was wrong" without reading the message.
        /// </summary>
        protected IActionResult FromResult(IResult<T, Error> result)
        {
            if (result.IsSuccess)
                return Ok(result.Value);

            var error = result.Error;
            var envelope = Envelope<T>.Error(error);

            if (NotFoundCodes.Contains(error.Code))
                return NotFound(envelope);

            if (ForbiddenCodes.Contains(error.Code))
                return StatusCode(StatusCodes.Status403Forbidden, envelope);

            if (ConflictCodes.Contains(error.Code))
                return Conflict(envelope);

            return BadRequest(envelope);
        }
    }
}
