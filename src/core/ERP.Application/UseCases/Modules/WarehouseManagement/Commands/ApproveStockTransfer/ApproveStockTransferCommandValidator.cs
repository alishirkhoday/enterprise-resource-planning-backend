namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ApproveStockTransfer
{
    public class ApproveStockTransferCommandValidator : AbstractValidator<ApproveStockTransferCommand>
    {
        public ApproveStockTransferCommandValidator()
        {
            RuleFor(r => r.TransferId).NotEmpty().WithMessage("Which transfer?");
        }
    }
}
