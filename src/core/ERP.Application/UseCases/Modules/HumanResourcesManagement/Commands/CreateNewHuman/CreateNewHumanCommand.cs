using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.CreateNewHuman
{
    public record CreateNewHumanCommand : AddEditHumanDto, ICommand<Result<string, Error>>
    {
    }
}
