using ERP.Application.Common.Interfaces.DbContext;
using ERP.Application.Common.Security;
using ERP.Application.UseCases.UsersManagement.Authentication.SendVerificationCode;
using ERP.Domain.Entities.Users;
using ERP.Domain.Primitives;
using ERP.Domain.Repositories.Users;

namespace ERP.Application.UseCases.UsersManagement.Authentication.SignUp
{
    /// <summary>
    /// Registers a user and sends them a code to prove they own the address they gave.
    /// <para>
    /// The account is committed before the code goes out, and that order is the whole point. A
    /// message cannot be recalled: sending first and committing second means a failed commit leaves
    /// somebody holding a verification code for an account that does not exist, and nothing can put
    /// that right. The other way round, a failed send leaves an unverified account, which they fix
    /// by asking for another code.
    /// </para>
    /// </summary>
    public sealed class SignUpCommandHandler(
        IMainDbContext context,
        IUserRepository userRepository,
        IMediator mediator) : IRequestHandler<SignUpCommand, Result<SignUpResultDto, Error>>
    {
        private readonly IMainDbContext _context = context;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMediator _mediator = mediator;

        public async Task<Result<SignUpResultDto, Error>> Handle(SignUpCommand request, CancellationToken cancellationToken)
        {
            var usesMobilePhoneNumber =
                !string.IsNullOrEmpty(request.MobilePhoneNumberRegionCode) && !string.IsNullOrEmpty(request.MobilePhoneNumber);
            var usesEmail = !string.IsNullOrEmpty(request.Email);

            if (!usesMobilePhoneNumber && !usesEmail)
                return Result.Failure<SignUpResultDto, Error>(Errors.User.YouMustChooseOneOfTheRegistrationMethods());

            if (await _userRepository.CheckUserExistsByUsernameAsync(request.Username, cancellationToken))
                return Result.Failure<SignUpResultDto, Error>(Errors.User.UsernameIsUsed(request.Username));

            var newUser = new User(request.Username, request.Password.HashPassword());

            if (usesMobilePhoneNumber)
            {
                var normalized = Normalization.NormalizeMobilePhoneNumber(request.MobilePhoneNumberRegionCode!, request.MobilePhoneNumber!);
                if (await _userRepository.CheckUserExistsByMobilePhoneNumberAsync(normalized, cancellationToken))
                {
                    return Result.Failure<SignUpResultDto, Error>(
                        Errors.User.MobilePhoneNumberIsUsed(request.MobilePhoneNumberRegionCode!, request.MobilePhoneNumber!));
                }

                newUser.SetMobilePhoneNumber(request.MobilePhoneNumberRegionCode!, request.MobilePhoneNumber!);
            }
            else
            {
                var normalized = Normalization.NormalizeEmail(request.Email!);
                if (await _userRepository.CheckUserExistsByEmailAsync(normalized, cancellationToken))
                    return Result.Failure<SignUpResultDto, Error>(Errors.User.EmailIsUsed(request.Email!));

                newUser.SetEmail(request.Email!);
            }

            await _userRepository.CreateAsync(newUser, cancellationToken);
            await _context.SaveChanges(cancellationToken);

            // Only now, with the account safely stored. A send that fails from here leaves an
            // account waiting to be verified, which the user can resolve by asking again.
            var sendVerificationCode = new SendVerificationCodeCommand
            {
                NormalizedMobilePhoneNumber = usesMobilePhoneNumber ? newUser.NormalizedMobilePhoneNumber : null,
                NormalizedEmail = usesMobilePhoneNumber ? null : newUser.NormalizedEmail
            };

            var sendResult = await _mediator.Send(sendVerificationCode, cancellationToken);
            if (sendResult.IsFailure)
                return Result.Failure<SignUpResultDto, Error>(Errors.User.ErrorForSendVerificationCode());

            return Result.Success<SignUpResultDto, Error>(
                new SignUpResultDto(newUser.NormalizedUsername, newUser.NormalizedMobilePhoneNumber, newUser.NormalizedEmail));
        }
    }
}
