using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;

public class OpportunityRepository(IMainDbContext context) : Repository<Opportunity>(context), IOpportunityRepository
{
    private static readonly OpportunityStage[] ClosedStages =
    [
        OpportunityStage.ClosedWon,
        OpportunityStage.ClosedLost
    ];

    public async Task<IReadOnlyList<Opportunity>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Where(o => !ClosedStages.Contains(o.Stage)
                        && (assignedToHumanId == null || o.AssignedToHumanId == assignedToHumanId))
            .OrderBy(o => o.ExpectedCloseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Opportunity>> GetByStageAsync(OpportunityStage stage, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(o => o.Stage == stage)
            .OrderBy(o => o.ExpectedCloseDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Value multiplied by probability, summed in the database. A forecast over a full pipeline
    /// should not have to load every deal to add it up.
    /// </summary>
    public async Task<decimal> GetWeightedPipelineValueAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(o => !ClosedStages.Contains(o.Stage)
                        && (assignedToHumanId == null || o.AssignedToHumanId == assignedToHumanId))
            .SumAsync(o => o.EstimatedValue.Amount * o.Probability.Value / 100m, cancellationToken);
    }

    public async Task<IReadOnlyList<Opportunity>> GetStaleAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(o => o.Customer)
            .Where(o => !ClosedStages.Contains(o.Stage) && o.ExpectedCloseDate < asOf)
            .OrderBy(o => o.ExpectedCloseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Opportunity>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreationDateTime)
            .ToListAsync(cancellationToken);
    }
}
