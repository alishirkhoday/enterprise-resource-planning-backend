namespace ERP.Application.UseCases.UsersManagement.Authentication.SignIn
{
    public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
    {
        public SignInCommandValidator()
        {
            RuleFor(u => u.Username).NotEmpty().WithMessage(Helpers.ValidatorsMessages.UsernameIsRequired);
            RuleFor(u => u.Password).NotEmpty().WithMessage(Helpers.ValidatorsMessages.PasswordIsRequired);
        }
    }
}
