using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.DefineReportTemplate
{
    public class DefineReportTemplateCommandValidator : AbstractValidator<DefineReportTemplateCommand>
    {
        private static readonly string[] OutputFormats = ["Table", "Chart", "Pivot", "Export"];

        public DefineReportTemplateCommandValidator()
        {
            RuleFor(r => r.Name)
                .NotEmpty().WithMessage("A report needs a name.")
                .MaximumLength(200);

            RuleFor(r => r.Description)
                .NotEmpty().WithMessage("What does the report answer?")
                .MaximumLength(1000);

            RuleFor(r => r.Category).IsInEnum();

            RuleFor(r => r.QueryDefinition)
                .NotEmpty().WithMessage("A report with no definition returns nothing.")
                .MaximumLength(8000);

            // A free text format would reach the renderer and be silently ignored, leaving a
            // report that looks defined and draws nothing.
            RuleFor(r => r.OutputFormat)
                .Must(format => OutputFormats.Contains(format))
                .WithMessage($"The output format is one of: {string.Join(", ", OutputFormats)}.");

            RuleFor(r => r.Filters)
                .Must(filters => filters.Select(f => f.FieldName).Distinct(StringComparer.OrdinalIgnoreCase).Count() == filters.Count)
                .WithMessage("The same field is filtered twice.");

            RuleForEach(r => r.Filters).SetValidator(new ReportFilterValidator());
        }
    }

    public class ReportFilterValidator : AbstractValidator<ReportFilterDto>
    {
        public ReportFilterValidator()
        {
            RuleFor(f => f.FieldName)
                .NotEmpty().WithMessage("A filter needs a field.")
                .MaximumLength(100);

            RuleFor(f => f.Type).IsInEnum();
            RuleFor(f => f.DefaultValue).MaximumLength(200);
        }
    }
}
