using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Queries.GetExpiringContracts
{
    /// <summary>
    /// Contracts about to lapse - سررسید قرارداد.
    /// <para>
    /// Somebody working with no agreement behind them is the thing nobody spots until payroll
    /// fails, and by then the gap is weeks old and has to be papered over retrospectively.
    /// </para>
    /// </summary>
    public class GetExpiringContractsQueryHandler(
        IEmploymentContractRepository contracts,
        TimeProvider timeProvider)
        : IRequestHandler<GetExpiringContractsQuery, Result<IReadOnlyList<ExpiringContractDto>, Error>>
    {
        public async Task<Result<IReadOnlyList<ExpiringContractDto>, Error>> Handle(GetExpiringContractsQuery request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var from = request.From ?? today;
            var to = request.To ?? today.AddDays(request.WithinDays);

            var expiring = await contracts.GetExpiringBetweenAsync(from, to, cancellationToken);

            return Result.Success<IReadOnlyList<ExpiringContractDto>, Error>(
                [.. expiring.Select(c => new ExpiringContractDto(
                    c.Id,
                    c.HumanId,
                    c.Human.FullName,
                    c.Human.EmployeeNumber,
                    c.Position.Title,
                    c.EmploymentType,
                    c.StartDate,
                    c.EndDate!.Value,
                    // Measured from today rather than from the window's start, so a list asked
                    // for next month still says how long is actually left.
                    c.EndDate.Value.DayNumber - today.DayNumber))]);
        }
    }
}
