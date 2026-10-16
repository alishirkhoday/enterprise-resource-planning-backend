using ERP.Application.Common.Interfaces.Authentication;
using ERP.Application.Common.Interfaces.DbContext;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Users;

namespace ERP.Application.UseCases.UsersManagement.Authentication.SignIn
{
    public sealed class SignInCommandHandler(IMainDbContext context, IUserRepository userRepository, IUserSessionRepository userSessionRepository, IUserTokenRepository userTokenRepository, ITokenService tokenService) : IRequestHandler<SignInCommand, Result<string, Error>>
    {
        private readonly IMainDbContext _context = context;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;
        private readonly IUserTokenRepository _userTokenRepository = userTokenRepository;
        private readonly ITokenService _tokenService = tokenService;

        public async Task<Result<string, Error>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (user is null)
            {
                return Result.Failure<string, Error>(Errors.User.UserNotFound());
            }
            //if (user.Status == UserStatus.Inactive)
            //{
            //    return Result.Failure<string, Error>(Errors.User.UserIsNotActive());
            //}
            if (user.Status == UserStatus.Ban)
            {
                return Result.Failure<string, Error>(Errors.User.UserIsBan());
            }
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                return Result.Failure<string, Error>(Errors.User.UserAccountIsLocked(DateTimeOffset.UtcNow.ToHourMinuteDifference(user.LockoutEnd.Value)));
            }
            var isVerifyPassword = PasswordHashService.VerifyPassword(user.PasswordHash.Value, request.Password);
            if (!isVerifyPassword)
            {
                // Saved here rather than left to a unit of work, because this path returns a
                // failure and the failed attempt has to survive it. A lockout counter that is
                // rolled back with the refusal never counts to three.
                user.HandleAccessFailed();
                await _context.SaveChanges(cancellationToken);
                return Result.Failure<string, Error>(Errors.User.PasswordIsWrong());
            }
            var tokenExpireDate = DateTimeOffset.UtcNow.AddHours(6);
            var token = _tokenService.GenerateJsonWebToken(user, tokenExpireDate);
            if (token is null)
            {
                return Result.Failure<string, Error>(Errors.General.AnErrorHasOccurred());
            }
            var newSession = new UserSession(user, request.InternetProtocolAddress, request.UserAgent, request.DeviceName, request.OperatingSystem);
            await _userSessionRepository.CreateAsync(newSession, cancellationToken);

            var newToken = new UserToken(user, token.HashSha256());
            await _userTokenRepository.CreateAsync(newToken, cancellationToken);

            user.RecordSignIn(request.InternetProtocolAddress, request.DeviceName);
            await _context.SaveChanges(cancellationToken);
            return Result.Success<string, Error>(token);
        }
    }
}
