namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.AnswerReferral
{
    public class AnswerReferralCommandValidator : AbstractValidator<AnswerReferralCommand>
    {
        public AnswerReferralCommandValidator()
        {
            RuleFor(a => a.ReferralId).NotEmpty().WithMessage("Which referral?");

            // Both an answer and a withdrawal close an open item, and both have to say why - a
            // referral that was taken back without explanation looks identical to one that was
            // never acted on.
            RuleFor(a => a.Response)
                .NotEmpty().WithMessage("A referral needs an answer, or a reason for taking it back.");
        }
    }
}
