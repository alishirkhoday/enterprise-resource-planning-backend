using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.IssueEmploymentContract
{
    public record IssueEmploymentContractCommand : IssueEmploymentContractDto, ICommand<Result<EmploymentContractDto, Error>>;
}
