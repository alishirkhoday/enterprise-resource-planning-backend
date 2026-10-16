using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.AdvanceOpportunity
{
    public record AdvanceOpportunityCommand : AdvanceOpportunityDto, ICommand<Result<OpportunityDto, Error>>;
}
