namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.DecideLeaveRequest
{
    public class DecideLeaveRequestCommandValidator : AbstractValidator<DecideLeaveRequestCommand>
    {
        public DecideLeaveRequestCommandValidator()
        {
            RuleFor(r => r.RequestId).NotEmpty().WithMessage("Which request?");

            RuleFor(r => r.Reason)
                .NotEmpty()
                .WithMessage("A refused request needs a reason.")
                .When(r => !r.IsApproved);
        }
    }
}
