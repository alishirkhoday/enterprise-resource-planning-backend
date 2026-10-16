using ERP.Application.UseCases.Modules.ProjectManagement.DTOs;
using ERP.Application.UseCases.Modules.ProjectManagement.Mapping;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;

namespace ERP.Application.UseCases.Modules.ProjectManagement.Queries.GetProjectStatement
{
    /// <summary>
    /// What a project has cost, earned and still has to invoice.
    /// <para>
    /// Only approved hours are counted. A draft timesheet is somebody's private note about their
    /// week, and letting it into a project's cost reports work nobody has signed off - which then
    /// gets invoiced, and then argued about.
    /// </para>
    /// </summary>
    public class GetProjectStatementQueryHandler(IProjectRepository projects, ITimesheetRepository timesheets)
        : IRequestHandler<GetProjectStatementQuery, Result<ProjectStatementDto, Error>>
    {
        public async Task<Result<ProjectStatementDto, Error>> Handle(GetProjectStatementQuery request, CancellationToken cancellationToken)
        {
            var project = await projects.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project is null)
                return Result.Failure<ProjectStatementDto, Error>(Errors.General.NotFound(request.ProjectId.ToString(), nameof(Project)));

            var from = request.From ?? project.PlannedPeriod.StartDate;
            var to = request.To ?? project.PlannedPeriod.EndDate ?? DateOnly.MaxValue;

            var approvedHours = await timesheets.GetApprovedHoursForProjectAsync(project.Id, from, to, cancellationToken);
            var billableHours = await timesheets.GetApprovedBillableHoursForProjectAsync(project.Id, from, to, cancellationToken);

            var billable = await projects.GetBillableMilestonesAsync(cancellationToken);
            var forThisProject = billable.Where(m => m.ProjectId == project.Id).ToArray();

            return Result.Success<ProjectStatementDto, Error>(new ProjectStatementDto(
                project.Id,
                project.Code,
                project.Name,
                project.Status,
                from,
                to,
                project.CurrencyCode,
                project.BudgetedCost.Amount,
                project.ActualCost.Amount,
                project.InvoicedAmount.Amount,
                approvedHours,
                billableHours,
                forThisProject.Sum(m => m.BillableAmount?.Amount ?? 0m),
                [.. forThisProject.Select(m => m.ToDto())]));
        }
    }
}
