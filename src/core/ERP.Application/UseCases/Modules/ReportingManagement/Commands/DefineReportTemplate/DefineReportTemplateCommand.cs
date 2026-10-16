using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.DefineReportTemplate
{
    public record DefineReportTemplateCommand : DefineReportTemplateDto, ICommand<Result<ReportTemplateDto, Error>>;
}
