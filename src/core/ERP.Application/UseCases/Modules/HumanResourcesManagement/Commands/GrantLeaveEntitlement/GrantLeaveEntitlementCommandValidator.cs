namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.GrantLeaveEntitlement
{
    public class GrantLeaveEntitlementCommandValidator : AbstractValidator<GrantLeaveEntitlementCommand>
    {
        public GrantLeaveEntitlementCommandValidator()
        {
            RuleFor(r => r.HumanId).NotEmpty().WithMessage("Whose balance is it?");
            RuleFor(r => r.LeaveType).IsInEnum();

            RuleFor(r => r.Year)
                .InclusiveBetween(1300, 2200)
                .WithMessage("That is not a year anybody is granting leave for.");

            // Zero is meaningful: a leave type somebody is explicitly entitled to none of reads
            // differently from one they simply have no row for.
            RuleFor(r => r.EntitledDays)
                .GreaterThanOrEqualTo(0m).WithMessage("An entitlement cannot be negative.")
                .LessThanOrEqualTo(366m).WithMessage("An entitlement cannot exceed a year.");

            RuleFor(r => r.CarriedOverDays)
                .GreaterThanOrEqualTo(0m).WithMessage("A carry over cannot be negative.")
                .LessThanOrEqualTo(366m).WithMessage("A carry over cannot exceed a year.");
        }
    }
}
