namespace ERP.Application.UseCases.UsersManagement.Authentication.SignIn
{
    public record SignInDto
    {
        public string Username { get; init; } = default!;
        public string Password { get; init; } = default!;
    }
}
