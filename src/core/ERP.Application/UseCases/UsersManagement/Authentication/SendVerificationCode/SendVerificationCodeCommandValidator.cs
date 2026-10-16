namespace ERP.Application.UseCases.UsersManagement.Authentication.SendVerificationCode
{
    public sealed class SendVerificationCodeCommandValidator : AbstractValidator<SendVerificationCodeCommand>
    {
        public SendVerificationCodeCommandValidator()
        {
            RuleFor(u => u.NormalizedMobilePhoneNumber)
                .MaximumLength(20).WithMessage(Helpers.ValidatorsMessages.MobilePhoneNumberWithRegionCodeLength)
                .When(u => u.NormalizedMobilePhoneNumber != null && u.NormalizedMobilePhoneNumber.Length > 0);

            RuleFor(u => u.NormalizedEmail)
                .MaximumLength(256).WithMessage(Helpers.ValidatorsMessages.EmailLength)
                .When(u => u.NormalizedEmail != null && u.NormalizedEmail.Length > 0);
        }
    }
}
