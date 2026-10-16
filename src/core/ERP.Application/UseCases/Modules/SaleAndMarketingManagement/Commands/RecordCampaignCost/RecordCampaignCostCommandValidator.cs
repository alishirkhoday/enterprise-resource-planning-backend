namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RecordCampaignCost
{
    public class RecordCampaignCostCommandValidator : AbstractValidator<RecordCampaignCostCommand>
    {
        public RecordCampaignCostCommandValidator()
        {
            RuleFor(r => r.CampaignId).NotEmpty().WithMessage("Which campaign?");
            RuleFor(r => r.Amount).GreaterThan(0m).WithMessage("Recording nothing spent is not a cost.");
            RuleFor(r => r.Description).MaximumLength(500);
        }
    }
}
