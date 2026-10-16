namespace ERP.Application.UseCases.UsersManagement.Authentication.SendVerificationCode
{
    /// <summary>
    /// Touches no table of its own - the code lives in the cache - so there is nothing for a unit
    /// of work to commit.
    /// </summary>
    public sealed record SendVerificationCodeCommand : IRequest<Result<string, Error>>
    {
        public string? NormalizedMobilePhoneNumber { get; init; }
        public string? NormalizedEmail { get; init; }
    }
}
