using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RegisterLead
{
    public record RegisterLeadCommand : RegisterLeadDto, ICommand<Result<LeadDto, Error>>;
}
