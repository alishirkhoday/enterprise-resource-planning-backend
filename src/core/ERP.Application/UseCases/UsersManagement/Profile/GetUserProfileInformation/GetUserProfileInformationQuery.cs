namespace ERP.Application.UseCases.UsersManagement.Profile.GetUserProfileInformation
{
    public record GetUserProfileInformationQuery : IQuery<Result<GetUserProfileInformationResponseDto, Error>>
    {
        public string UserId { get; init; } = default!;
    }
}
