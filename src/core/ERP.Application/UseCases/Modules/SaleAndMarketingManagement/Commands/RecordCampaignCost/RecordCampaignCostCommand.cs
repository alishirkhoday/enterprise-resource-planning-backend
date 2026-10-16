using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RecordCampaignCost
{
    public record RecordCampaignCostCommand : RecordCampaignCostDto, ICommand<Result<CampaignDto, Error>>;
}
