using ERP.Domain.Repositories.Users;

namespace ERP.Application.UseCases.UsersManagement.Profile.GetUserProfileInformation
{
    public class GetUserProfileInformationQueryHandler(IUserRepository userRepository, IUserProfileRepository userProfileImageRepository) : IRequestHandler<GetUserProfileInformationQuery, Result<GetUserProfileInformationResponseDto, Error>>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IUserProfileRepository _userProfileImageRepository = userProfileImageRepository;

        public async Task<Result<GetUserProfileInformationResponseDto, Error>> Handle(GetUserProfileInformationQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<GetUserProfileInformationResponseDto, Error>(Errors.User.UserNotFound());
            }
            var result = new GetUserProfileInformationResponseDto
            {
                Username = user.Username.Value,
                ImageUrlOrPath = await _userProfileImageRepository.GetImageByUserIdAsync(user.Id.ToString(), cancellationToken)
            };
            return Result.Success<GetUserProfileInformationResponseDto, Error>(result);
        }
    }
}
