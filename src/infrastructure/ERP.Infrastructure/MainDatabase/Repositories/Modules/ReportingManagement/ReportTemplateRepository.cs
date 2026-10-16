using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ReportingManagement;
using ERP.Domain.Repositories.Modules.ReportingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ReportingManagement;

public class ReportTemplateRepository(IMainDbContext context) : Repository<ReportTemplate>(context), IReportTemplateRepository
{
    public async Task<ReportTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<ReportTemplate?> GetWithFiltersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(t => t.Filters)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ReportTemplate>> GetByCategoryAsync(ReportCategory category, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(t => t.Category == category)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistenceByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(t => t.Name == name && (excludeId == null || t.Id != excludeId), cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledReport>> GetActiveSchedulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .Include(s => s.Template)
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledReport>> GetSchedulesForTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .Where(s => s.Template.Id == templateId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A schedule hangs off a template and has no life without one, so it is written through the
    /// template's repository rather than being given a repository of its own.
    /// </summary>
    public async Task AddScheduleAsync(ScheduledReport schedule, CancellationToken cancellationToken = default)
    {
        await _context.ScheduledReports.AddAsync(schedule, cancellationToken);
    }
}
