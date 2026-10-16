using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CloseOpportunity
{
    public record CloseOpportunityCommand : CloseOpportunityDto, ICommand<Result<OpportunityDto, Error>>;
}
