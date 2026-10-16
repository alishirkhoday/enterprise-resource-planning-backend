namespace ERP.Application.UseCases.UsersManagement.Profile.GetUserProfileInformation
{
    public record GetUserProfileInformationResponseDto
    {
        public string Username { get; init; } = default!;
        public string? ImageUrlOrPath { get; init; }
    }
}
