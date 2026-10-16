using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.QualifyLead
{
    public record QualifyLeadCommand : QualifyLeadDto, ICommand<Result<LeadDto, Error>>;
}
