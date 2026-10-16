using ERP.Application.Common.Interfaces.Authentication;
using ERP.Domain.Primitives;
using ERP.Domain.Repositories.Users;

namespace ERP.Application.UseCases.UsersManagement.Authentication.Verify
{
    public sealed class VerifyCommandHandler(IUserRepository userRepository, IOneTimePasswordService otpCodeService) : IRequestHandler<VerifyCommand, Result<string, Error>>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IOneTimePasswordService _otpCodeService = otpCodeService;

        public async Task<Result<string, Error>> Handle(VerifyCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (user is null)
            {
                return Result.Failure<string, Error>(Errors.User.UserNotFound());
            }
            bool isVerifyOtp;
            if (!string.IsNullOrEmpty(request.MobilePhoneNumberRegionCode) && !string.IsNullOrEmpty(request.MobilePhoneNumber))
            {
                if (user.NormalizedMobilePhoneNumber != Normalization.NormalizeMobilePhoneNumber(request.MobilePhoneNumberRegionCode, request.MobilePhoneNumber))
                {
                    return Result.Failure<string, Error>(Errors.User.ThisMobilePhoneNumberDoesNotMatch());
                }
                isVerifyOtp = await _otpCodeService.VerifyAsync(Normalization.NormalizeMobilePhoneNumber(request.MobilePhoneNumberRegionCode, request.MobilePhoneNumber), request.Code, cancellationToken);
                if (!isVerifyOtp)
                {
                    return Result.Failure<string, Error>(Errors.User.VerificationCodeIsNotValid());
                }
                user.ConfirmMobilePhoneNumber();
            }
            else if (!string.IsNullOrEmpty(request.Email))
            {
                if (user.NormalizedEmail != Normalization.NormalizeEmail(request.Email))
                {
                    return Result.Failure<string, Error>(Errors.User.ThisEmailDoesNotMatch());
                }
                isVerifyOtp = await _otpCodeService.VerifyAsync(Normalization.NormalizeEmail(request.Email), request.Code, cancellationToken);
                if (!isVerifyOtp)
                {
                    return Result.Failure<string, Error>(Errors.User.VerificationCodeIsNotValid());
                }
                user.ConfirmEmail();
            }
            else
            {
                return Result.Failure<string, Error>(Errors.User.YouMustChooseOneOfTheMethods());
            }
            user.Active();
            return Result.Success<string, Error>(user.NormalizedUsername);
        }
    }
}
