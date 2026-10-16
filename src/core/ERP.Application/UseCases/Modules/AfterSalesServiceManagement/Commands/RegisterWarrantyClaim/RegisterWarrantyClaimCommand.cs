using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RegisterWarrantyClaim
{
    public record RegisterWarrantyClaimCommand : RegisterWarrantyClaimDto, ICommand<Result<WarrantyClaimDto, Error>>;
}
