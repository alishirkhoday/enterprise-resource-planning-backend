using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.ComposeDashboard
{
    public record ComposeDashboardCommand : ComposeDashboardDto, ICommand<Result<DashboardDto, Error>>;
}
