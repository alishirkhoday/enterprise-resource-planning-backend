using ERP.Domain.Entities.Modules.ReportingManagement;

namespace ERP.Application.UseCases.Modules.ReportingManagement.DTOs
{
    /// <summary>
    /// Registers a report definition - تعریف گزارش.
    /// <para>
    /// Defining a report is not the same as running one. The query text is stored as a definition
    /// for a reporting service to pick up under its own credentials; nothing in this application
    /// executes it, because an endpoint that runs caller-supplied SQL is remote code execution
    /// against the database whatever it is called.
    /// </para>
    /// </summary>
    public record DefineReportTemplateDto
    {
        public string Name { get; init; } = default!;

        public string Description { get; init; } = default!;

        public ReportCategory Category { get; init; }

        /// <summary>
        /// The query the reporting service will run. Stored, never executed here.
        /// </summary>
        public string QueryDefinition { get; init; } = default!;

        /// <summary>How the result is meant to be shown: Table, Chart, Pivot, Export.</summary>
        public string OutputFormat { get; init; } = "Table";

        public IReadOnlyList<ReportFilterDto> Filters { get; init; } = [];
    }

    public record ReportFilterDto
    {
        public string FieldName { get; init; } = default!;

        public FilterType Type { get; init; }

        public string? DefaultValue { get; init; }
    }

    /// <summary>A screen assembled out of report templates - داشبورد.</summary>
    public record ComposeDashboardDto
    {
        public string Title { get; init; } = default!;

        public string Description { get; init; } = default!;

        public IReadOnlyList<DashboardWidgetDto> Widgets { get; init; } = [];
    }

    public record DashboardWidgetDto
    {
        public Guid ReportTemplateId { get; init; }

        public int DisplayOrder { get; init; }

        /// <summary>How this widget draws its report: Table, Chart, KPI.</summary>
        public string VisualizationType { get; init; } = "Table";
    }

    /// <summary>Arranges for a report to be sent on a schedule - زمان‌بندی گزارش.</summary>
    public record ScheduleReportDto
    {
        public Guid ReportTemplateId { get; init; }

        /// <summary>Standard five field cron, as the dispatcher reads it.</summary>
        public string CronExpression { get; init; } = default!;

        /// <summary>Who receives it, as a comma separated list of addresses.</summary>
        public string Recipients { get; init; } = default!;

        public bool IsActive { get; init; } = true;
    }

    public sealed record ReportTemplateDto(
        Guid Id,
        string Name,
        string Description,
        ReportCategory Category,
        string OutputFormat,
        IReadOnlyList<ReportFilterViewDto> Filters);

    public sealed record ReportFilterViewDto(Guid Id, string FieldName, FilterType Type, string? DefaultValue);

    public sealed record DashboardDto(
        Guid Id,
        string Title,
        string Description,
        IReadOnlyList<DashboardWidgetViewDto> Widgets);

    public sealed record DashboardWidgetViewDto(
        Guid Id,
        Guid ReportTemplateId,
        string ReportTemplateName,
        int DisplayOrder,
        string VisualizationType);

    public sealed record ScheduledReportDto(
        Guid Id,
        Guid ReportTemplateId,
        string ReportTemplateName,
        string CronExpression,
        string Recipients,
        bool IsActive);
}
