namespace ERP.Application.UseCases.UsersManagement.Authentication.Verify
{
    public sealed class VerifyCommandValidator : AbstractValidator<VerifyCommand>
    {
        public VerifyCommandValidator()
        {
            RuleFor(u => u.Username)
                .NotEmpty().WithMessage(Helpers.ValidatorsMessages.UsernameIsRequired)
                .Must(u => u.IsValidateUsername()).WithMessage(Helpers.ValidatorsMessages.UsernameIsNotValid);

            RuleFor(u => u.MobilePhoneNumberRegionCode)
                .MaximumLength(10).WithMessage(Helpers.ValidatorsMessages.MobilePhoneNumberRegionCodeLength)
                .When(u => u.MobilePhoneNumberRegionCode != null && u.MobilePhoneNumberRegionCode.Length > 0);

            RuleFor(u => u.MobilePhoneNumber)
                .MaximumLength(15).WithMessage(Helpers.ValidatorsMessages.MobilePhoneNumberLength)
                .When(u => u.MobilePhoneNumber != null && u.MobilePhoneNumber.Length > 0);

            RuleFor(u => u.Email)
                .MaximumLength(256).WithMessage(Helpers.ValidatorsMessages.EmailLength)
                .When(u => u.Email != null && u.Email.Length > 0);

            RuleFor(u => u.Code)
                .NotEmpty().WithMessage(Helpers.ValidatorsMessages.VerificationCodeIsRequired);
        }
    }
}
