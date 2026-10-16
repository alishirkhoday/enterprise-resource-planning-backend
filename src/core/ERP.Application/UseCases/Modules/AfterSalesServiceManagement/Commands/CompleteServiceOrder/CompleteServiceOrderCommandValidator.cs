using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.CompleteServiceOrder
{
    public class CompleteServiceOrderCommandValidator : AbstractValidator<CompleteServiceOrderCommand>
    {
        public CompleteServiceOrderCommandValidator()
        {
            RuleFor(s => s.ServiceOrderId).NotEmpty().WithMessage("Which visit?");
            RuleFor(s => s.CompletedOn).NotEqual(default(DateOnly)).WithMessage("When was it finished?");

            // The one field that still matters six months later, when somebody asks what was done.
            RuleFor(s => s.WorkPerformed)
                .NotEmpty().WithMessage("A completed visit has to say what was done.")
                .MinimumLength(5).WithMessage("The work performed has to say something.");

            RuleFor(s => s.LabourHours).GreaterThanOrEqualTo(0m);
            RuleFor(s => s.LabourCharge).GreaterThanOrEqualTo(0m);
            RuleFor(s => s.TravelCharge).GreaterThanOrEqualTo(0m);

            RuleForEach(s => s.Parts).SetValidator(new ServiceOrderPartValidator());
        }
    }

    public class ServiceOrderPartValidator : AbstractValidator<ServiceOrderPartDto>
    {
        public ServiceOrderPartValidator()
        {
            RuleFor(p => p.ItemId).NotEmpty().WithMessage("Every part names an item.");
            RuleFor(p => p.UnitOfMeasureId).NotEmpty().WithMessage("Every part needs a unit of measure.");
            RuleFor(p => p.Quantity).GreaterThan(0m).WithMessage("A part of no quantity is not a part.");
            RuleFor(p => p.UnitPrice).GreaterThanOrEqualTo(0m).WithMessage("A price cannot be negative.");
        }
    }
}
