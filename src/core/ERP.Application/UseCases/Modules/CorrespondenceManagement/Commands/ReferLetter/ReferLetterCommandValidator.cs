namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.ReferLetter
{
    public class ReferLetterCommandValidator : AbstractValidator<ReferLetterCommand>
    {
        public ReferLetterCommandValidator()
        {
            RuleFor(r => r.LetterId).NotEmpty().WithMessage("Which letter?");
            RuleFor(r => r.ToUserId).NotEmpty().WithMessage("Refer it to whom?");
            RuleFor(r => r.FromUserId).NotEmpty().WithMessage("Referred by whom?");

            RuleFor(r => r.ToUserId)
                .NotEqual(r => r.FromUserId)
                .WithMessage("A letter cannot be referred to the person referring it.");
        }
    }
}
