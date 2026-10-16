using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.DecideQuotation
{
    public record DecideQuotationCommand : DecideQuotationDto, ICommand<Result<QuotationDto, Error>>;
}
