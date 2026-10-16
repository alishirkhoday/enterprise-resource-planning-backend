using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetExpiringContracts
{
    /// <summary>
    /// Contracts lapsing soon - سررسید قرارداد. The window defaults to the next month, which is
    /// about the notice a renewal needs.
    /// </summary>
    public sealed record GetExpiringContractsQuery : IQuery<Result<IReadOnlyList<ExpiringContractDto>, Error>>
    {
        public int WithinDays { get; init; } = 30;

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }
    }
}
