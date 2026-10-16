namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.CreateNewHuman
{
    public class CreateNewHumanCommandValidator : AbstractValidator<CreateNewHumanCommand>
    {
        public CreateNewHumanCommandValidator()
        {
            RuleFor(u => u.Username)
                .NotEmpty().WithMessage(Helpers.ValidatorsMessages.UsernameIsRequired)
                .Must(u => u.IsValidateUsername()).WithMessage(Helpers.ValidatorsMessages.UsernameIsNotValid);

            RuleFor(u => u.Password)
                .NotEmpty().WithMessage(Helpers.ValidatorsMessages.PasswordIsRequired)
                .Must(u => u.IsValidatePassword()).WithMessage(Helpers.ValidatorsMessages.PasswordIsNotValid);

            RuleFor(h => h.Name)
                .NotEmpty();

            RuleFor(h => h.Family)
                .NotEmpty();

            RuleFor(h => h.Gender)
                .IsInEnum();

            RuleFor(u => u.NationalId)
                .NotEmpty()
                .Must(u => u.IsValidateIranianNationalId());
        }
    }
}
