using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CreateOpportunity
{
    public record CreateOpportunityCommand : CreateOpportunityDto, ICommand<Result<OpportunityDto, Error>>;
}
