using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.ComposeDashboard
{
    public class ComposeDashboardCommandValidator : AbstractValidator<ComposeDashboardCommand>
    {
        public ComposeDashboardCommandValidator()
        {
            RuleFor(r => r.Title)
                .NotEmpty().WithMessage("A dashboard needs a title.")
                .MaximumLength(200);

            RuleFor(r => r.Description)
                .NotEmpty().WithMessage("What is the dashboard for?")
                .MaximumLength(1000);

            RuleFor(r => r.Widgets).NotEmpty().WithMessage("A dashboard with no widgets shows nothing.");

            // Two widgets at the same position render in whatever order the database returned,
            // which means the screen looks different on refresh.
            RuleFor(r => r.Widgets)
                .Must(widgets => widgets.Select(w => w.DisplayOrder).Distinct().Count() == widgets.Count)
                .WithMessage("Two widgets share a display order.");

            RuleForEach(r => r.Widgets).SetValidator(new DashboardWidgetValidator());
        }
    }

    public class DashboardWidgetValidator : AbstractValidator<DashboardWidgetDto>
    {
        private static readonly string[] Visualizations = ["Table", "Chart", "KPI"];

        public DashboardWidgetValidator()
        {
            RuleFor(w => w.ReportTemplateId).NotEmpty().WithMessage("Every widget names a report.");
            RuleFor(w => w.DisplayOrder).GreaterThanOrEqualTo(0);

            RuleFor(w => w.VisualizationType)
                .Must(type => Visualizations.Contains(type))
                .WithMessage($"A widget is drawn as one of: {string.Join(", ", Visualizations)}.");
        }
    }
}
