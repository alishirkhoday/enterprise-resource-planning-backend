using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ProductionManagement.Commands.RecordQualityCheck
{
    public class RecordQualityCheckCommandValidator : AbstractValidator<RecordQualityCheckCommand>
    {
        public RecordQualityCheckCommandValidator()
        {
            // An inspection with no inspector is a number nobody stands behind.
            RuleFor(r => r.InspectorHumanId).NotEmpty().WithMessage("Who carried out the inspection?");

            RuleFor(r => r.InspectedQuantity).GreaterThan(0m).WithMessage("An inspection of nothing is not an inspection.");
            RuleFor(r => r.PassedQuantity).GreaterThanOrEqualTo(0m).WithMessage("A quantity cannot be negative.");

            RuleFor(r => r.PassedQuantity)
                .LessThanOrEqualTo(r => r.InspectedQuantity)
                .WithMessage("More units cannot pass than were inspected.");

            RuleFor(r => r.ReferenceId)
                .NotEmpty()
                .WithMessage("A reference needs both a type and an identifier.")
                .When(r => !string.IsNullOrWhiteSpace(r.ReferenceType));

            RuleFor(r => r.ReferenceType)
                .NotEmpty()
                .WithMessage("A reference needs both a type and an identifier.")
                .When(r => !string.IsNullOrWhiteSpace(r.ReferenceId));

            RuleForEach(r => r.Measurements).SetValidator(new QualityMeasurementValidator());
        }
    }

    public class QualityMeasurementValidator : AbstractValidator<QualityMeasurementDto>
    {
        public QualityMeasurementValidator()
        {
            RuleFor(m => m.ParameterId).NotEmpty().WithMessage("Every reading names a parameter.");

            // One or the other, never neither and never both: a parameter is either measured or
            // judged, and a reading that claims to be both cannot be checked against a tolerance.
            RuleFor(m => m)
                .Must(m => m.NumericValue is not null ^ !string.IsNullOrWhiteSpace(m.TextValue))
                .WithMessage("A reading is either a number or an observation, not both and not neither.");
        }
    }
}
