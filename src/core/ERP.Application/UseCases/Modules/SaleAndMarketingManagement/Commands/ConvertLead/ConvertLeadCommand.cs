using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ConvertLead
{
    public record ConvertLeadCommand : ConvertLeadDto, ICommand<Result<ConvertLeadResultDto, Error>>;
}
