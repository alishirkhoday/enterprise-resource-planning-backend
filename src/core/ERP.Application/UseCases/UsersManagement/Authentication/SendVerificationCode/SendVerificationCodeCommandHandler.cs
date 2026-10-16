using ERP.Application.Common.Interfaces.Authentication;
using ERP.Domain.Entities.Users;

namespace ERP.Application.UseCases.UsersManagement.Authentication.SendVerificationCode
{
    public sealed class SendVerificationCodeCommandHandler(IOneTimePasswordService otpService) : IRequestHandler<SendVerificationCodeCommand, Result<string, Error>>
    {
        private readonly IOneTimePasswordService _otpService = otpService;

        public async Task<Result<string, Error>> Handle(SendVerificationCodeCommand request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.NormalizedMobilePhoneNumber))
            {
                var otpCode = await _otpService.GenerateAsync(request.NormalizedMobilePhoneNumber, OneTimePasswordChannel.SMS, cancellationToken);
                if (!otpCode.Item2)
                {
                    return Result.Failure<string, Error>(Errors.User.TheNumberOfRequestsForVerificationCodeHasBeenReached());
                }
                //to do : send verification code by mobile phone number
            }
            else if (!string.IsNullOrEmpty(request.NormalizedEmail))
            {
                var otpCode = await _otpService.GenerateAsync(request.NormalizedEmail, OneTimePasswordChannel.Email, cancellationToken);
                if (!otpCode.Item2)
                {
                    return Result.Failure<string, Error>(Errors.User.TheNumberOfRequestsForVerificationCodeHasBeenReached());
                }
                //to do : send verification code by email
            }
            else
            {
                return Result.Failure<string, Error>(Errors.User.YouMustChooseOneOfTheMethods());
            }
            return Result.Success<string, Error>("Verification Code Sent");
        }
    }
}
