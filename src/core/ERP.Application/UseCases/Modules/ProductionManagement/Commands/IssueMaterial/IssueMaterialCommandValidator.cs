using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.IssueMaterial
{
    public class IssueMaterialCommandValidator : AbstractValidator<IssueMaterialCommand>
    {
        public IssueMaterialCommandValidator()
        {
            RuleFor(r => r.ProductionOrderId).NotEmpty().WithMessage("Which order is the material for?");
            RuleFor(r => r.WarehouseId).NotEmpty().WithMessage("Which warehouse is it coming out of?");
            RuleFor(r => r.IssueDate).NotEqual(default(DateOnly)).WithMessage("When was it taken?");

            RuleFor(r => r.OperationSequenceNumber)
                .GreaterThan(0)
                .WithMessage("An operation is numbered from one.")
                .When(r => r.OperationSequenceNumber is not null);

            // An empty list is meaningful: it issues what the bill still calls for.
            RuleForEach(r => r.Lines).SetValidator(new MaterialIssueLineValidator());

            RuleFor(r => r.Lines)
                .Must(lines => lines.Select(l => l.ComponentId).Distinct().Count() == lines.Count)
                .WithMessage("The same component is issued twice in one request; add the quantities together.");
        }
    }

    public class MaterialIssueLineValidator : AbstractValidator<MaterialIssueLineDto>
    {
        public MaterialIssueLineValidator()
        {
            RuleFor(l => l.ComponentId).NotEmpty().WithMessage("Every line names a component.");
            RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("Issuing nothing is not an issue.");
        }
    }
}
