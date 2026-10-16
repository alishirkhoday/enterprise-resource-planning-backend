namespace ERP.Application.UseCases.UsersManagement.Authentication.SignIn
{
    /// <summary>
    /// Deliberately not an <see cref="ICommand{TResponse}"/>. A rejected sign-in still has to
    /// persist the failed attempt, and a unit of work that discards the work of a refused command -
    /// which is what makes it safe everywhere else - would throw the lockout counter away with it.
    /// </summary>
    public sealed record SignInCommand : SignInDto, IRequest<Result<string, Error>>
    {
        public string InternetProtocolAddress { get; init; } = default!;
        public string? UserAgent { get; init; }
        public string? DeviceName { get; init; }
        public string? OperatingSystem { get; init; }
    }
}
