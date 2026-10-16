using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.LaunchCampaign
{
    public record LaunchCampaignCommand : LaunchCampaignDto, ICommand<Result<CampaignDto, Error>>;
}
