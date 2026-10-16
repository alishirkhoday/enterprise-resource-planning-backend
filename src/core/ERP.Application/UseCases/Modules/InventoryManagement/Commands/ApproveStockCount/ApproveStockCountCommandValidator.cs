namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApproveStockCount
{
    public class ApproveStockCountCommandValidator : AbstractValidator<ApproveStockCountCommand>
    {
        public ApproveStockCountCommandValidator()
        {
            RuleFor(r => r.StockCountId).NotEmpty().WithMessage("Which count sheet?");

            // Adjustments change stock without anything having moved, so the approval carries a
            // name. An unattributed one is the kind nobody can answer for afterwards.
            RuleFor(r => r.ApprovedByHumanId).NotEmpty().WithMessage("Who is approving these adjustments?");
        }
    }
}
