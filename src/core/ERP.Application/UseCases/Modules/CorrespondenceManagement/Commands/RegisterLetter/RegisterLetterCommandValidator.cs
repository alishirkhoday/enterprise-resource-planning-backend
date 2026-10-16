using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.RegisterLetter
{
    public class RegisterLetterCommandValidator : AbstractValidator<RegisterLetterCommand>
    {
        public RegisterLetterCommandValidator()
        {
            RuleFor(l => l.Direction).IsInEnum();
            RuleFor(l => l.Confidentiality).IsInEnum();
            RuleFor(l => l.Urgency).IsInEnum();

            RuleFor(l => l.Subject).NotEmpty().WithMessage("A letter needs a subject.");
            RuleFor(l => l.CorrespondentName).NotEmpty().WithMessage("Who is it from, or to?");
            RuleFor(l => l.LetterDate).NotEqual(default(DateOnly)).WithMessage("What date is on the letter?");

            // An outgoing letter has to be addressed to somebody. An incoming one has already
            // arrived, so its addressee is this organization and needs no restating.
            RuleFor(l => l.Recipients)
                .NotEmpty()
                .WithMessage("An outgoing letter needs at least one addressee.")
                .When(l => l.Direction == LetterDirection.Outbound);

            RuleFor(l => l.Recipients)
                .Must(r => r.Count(x => x.Kind == LetterRecipientKind.Primary) <= 1)
                .WithMessage("A letter has one primary addressee; the rest are copies.")
                .When(l => l.Recipients.Count > 0);

            RuleForEach(l => l.Recipients).SetValidator(new LetterRecipientValidator());

            RuleFor(l => l.CorrespondentReferenceDate)
                .NotNull()
                .WithMessage("A quoted reference needs the date it was issued.")
                .When(l => !string.IsNullOrWhiteSpace(l.CorrespondentReference));

            RuleFor(l => l.DueDate)
                .GreaterThanOrEqualTo(l => l.LetterDate)
                .WithMessage("A reply cannot be due before the letter was written.")
                .When(l => l.DueDate is not null);
        }
    }

    public class LetterRecipientValidator : AbstractValidator<LetterRecipientDto>
    {
        public LetterRecipientValidator()
        {
            RuleFor(r => r.Kind).IsInEnum();
            RuleFor(r => r.Name).NotEmpty().WithMessage("Every addressee needs a name.");

            RuleFor(r => r.Email)
                .EmailAddress()
                .WithMessage("That is not an email address.")
                .When(r => !string.IsNullOrWhiteSpace(r.Email));
        }
    }
}
