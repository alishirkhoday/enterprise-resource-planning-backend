using ERP.Application.UseCases.Modules.ReportingManagement.DTOs;
using ERP.Domain.Entities.Modules.ReportingManagement;
using ERP.Domain.Repositories.Modules.ReportingManagement;

namespace ERP.Application.UseCases.Modules.ReportingManagement.Commands.DefineReportTemplate
{
    /// <summary>
    /// Registers a report definition - تعریف گزارش.
    /// <para>
    /// Deliberately there is no command that runs one. The template holds a query string, and an
    /// endpoint that executed a caller-supplied query would be remote code execution against the
    /// database dressed up as a feature. Running reports belongs to a reporting service with its
    /// own, read-only credentials; this only records what it is allowed to run.
    /// </para>
    /// </summary>
    public class DefineReportTemplateCommandHandler(IReportTemplateRepository templates)
        : IRequestHandler<DefineReportTemplateCommand, Result<ReportTemplateDto, Error>>
    {
        public async Task<Result<ReportTemplateDto, Error>> Handle(DefineReportTemplateCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();

            if (await templates.IsExistenceByNameAsync(name, cancellationToken: cancellationToken))
                return Result.Failure<ReportTemplateDto, Error>(Errors.Reporting.ReportNameIsUsed(name));

            try
            {
                var template = new ReportTemplate(
                    name,
                    request.Description,
                    request.Category,
                    request.QueryDefinition,
                    request.OutputFormat);

                foreach (var filter in request.Filters)
                {
                    template.AddFilter(new ReportFilterDefinition(template, filter.FieldName, filter.Type, filter.DefaultValue));
                }

                await templates.CreateAsync(template, cancellationToken);

                return Result.Success<ReportTemplateDto, Error>(ToDto(template));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return Result.Failure<ReportTemplateDto, Error>(Errors.Reporting.ReportCannotBeChanged(exception.Message));
            }
        }

        internal static ReportTemplateDto ToDto(ReportTemplate template) => new(
            template.Id,
            template.Name,
            template.Description,
            template.Category,
            template.OutputFormat,
            [.. template.Filters.Select(f => new ReportFilterViewDto(f.Id, f.FieldName, f.Type, f.DefaultValue))]);
    }
}
