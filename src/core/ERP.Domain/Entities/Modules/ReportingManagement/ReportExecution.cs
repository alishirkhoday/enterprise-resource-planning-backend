namespace ERP.Domain.Entities.Modules.ReportingManagement;

/// <summary>
/// A record that a report was run, with a snapshot of what it returned. Kept so a figure
/// somebody acted on can be produced again even after the underlying data has moved.
/// </summary>
public sealed class ReportExecution : AggregateRoot
{
    public ReportTemplate Template { get; private set; } = default!;
    public DateTimeOffset ExecutionDateTime { get; private set; } = DateTimeOffset.UtcNow;
    public string ExecutedBy { get; private set; } = default!; // کاربری که گزارش را اجرا کرده
    public string ResultSnapshot { get; private set; } = default!; // خروجی گزارش (به صورت JSON ذخیره می‌شود)

    // Required by Entity Framework to materialise the entity.
    private ReportExecution() { }

    public ReportExecution(ReportTemplate template, string executedBy, string resultSnapshot)
    {
        Template = template;
        ExecutedBy = executedBy;
        ResultSnapshot = resultSnapshot;
    }
}
