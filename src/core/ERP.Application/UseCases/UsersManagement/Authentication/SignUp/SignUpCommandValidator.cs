namespace ERP.Application.UseCases.UsersManagement.Authentication.SignUp
{
    public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
    {
        public SignUpCommandValidator()
        {
            RuleFor(u => u.Username)
                .NotEmpty().WithMessage(Helpers.ValidatorsMessages.UsernameIsRequired)
                .Must(u => u.IsValidateUsername()).WithMessage(Helpers.ValidatorsMessages.UsernameIsNotValid);

            RuleFor(u => u.Password)
                .NotEmpty().WithMessage(Helpers.ValidatorsMessages.PasswordIsRequired)
                .Must(u => u.IsValidatePassword()).WithMessage(Helpers.ValidatorsMessages.PasswordIsNotValid);

            RuleFor(u => u.MobilePhoneNumberRegionCode)
                .MaximumLength(10).WithMessage(Helpers.ValidatorsMessages.MobilePhoneNumberRegionCodeLength)
                .Must(u => u.IsValidateMobilePhoneNumberRegionCode()).WithMessage(Helpers.ValidatorsMessages.MobilePhoneNumberRegionCodeIsNotValid)
                .When(u => u.MobilePhoneNumberRegionCode != null && u.MobilePhoneNumberRegionCode.Length > 0);

            RuleFor(u => u.MobilePhoneNumber)
                .MaximumLength(15).WithMessage(Helpers.ValidatorsMessages.MobilePhoneNumberLength)
                .Must(u => u.IsValidateMobilePhoneNumber()).WithMessage(Helpers.ValidatorsMessages.MobilePhoneNumberIsNotValid)
                .When(u => u.MobilePhoneNumber != null && u.MobilePhoneNumber.Length > 0);

            RuleFor(u => u.Email)
                .MaximumLength(256).WithMessage(Helpers.ValidatorsMessages.EmailLength)
                .Must(u => u.IsValidateEmail()).WithMessage(Helpers.ValidatorsMessages.EmailIsNotValid)
                .When(u => u.Email != null && u.Email.Length > 0);
        }
    }
}
