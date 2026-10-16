namespace ERP.Application.UseCases.UsersManagement.Authentication.SignUp
{
    public record SignUpDto
    {
        public string Username { get; init; } = default!;
        public string Password { get; init; } = default!;
        public string? MobilePhoneNumberRegionCode { get; init; }
        public string? MobilePhoneNumber { get; init; }
        public string? Email { get; init; }
    }
}
