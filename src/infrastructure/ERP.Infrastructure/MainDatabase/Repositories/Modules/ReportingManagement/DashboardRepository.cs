using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.ReportingManagement;
using ERP.Domain.Repositories.Modules.ReportingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.ReportingManagement;

public class DashboardRepository(IMainDbContext context) : Repository<Dashboard>(context), IDashboardRepository
{
    public async Task<Dashboard?> GetWithWidgetsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(d => d.Widgets).ThenInclude(w => w.ReportTemplate)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Dashboard?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(d => d.Title == title, cancellationToken);
    }
}
