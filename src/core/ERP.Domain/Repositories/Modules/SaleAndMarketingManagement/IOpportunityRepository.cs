using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Deals in the pipeline - فرصت فروش. The weighted value is summed in the database because a
/// forecast should not mean loading every open deal.
/// </summary>
public interface IOpportunityRepository : IRepository<Opportunity>
{
    /// <summary>Deals still being worked, for one salesperson or for everyone.</summary>
    Task<IReadOnlyList<Opportunity>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Opportunity>> GetByStageAsync(OpportunityStage stage, CancellationToken cancellationToken = default);

    /// <summary>
    /// The weighted value of the open pipeline, which is estimated value multiplied by probability.
    /// Summed in the database so a forecast does not load every deal.
    /// </summary>
    Task<decimal> GetWeightedPipelineValueAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    /// <summary>Deals whose expected close date has passed while they are still open.</summary>
    Task<IReadOnlyList<Opportunity>> GetStaleAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Opportunity>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
