using ERP.Application.UseCases.Modules.PlanningManagement.DTOs;

namespace ERP.Application.UseCases.Modules.PlanningManagement.Commands.RunMaterialRequirementPlan
{
    public record RunMaterialRequirementPlanCommand : RunMaterialRequirementPlanDto, ICommand<Result<MaterialRequirementPlanDto, Error>>;
}
