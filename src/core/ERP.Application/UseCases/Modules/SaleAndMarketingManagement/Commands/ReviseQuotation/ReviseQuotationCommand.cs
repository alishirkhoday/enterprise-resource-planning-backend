using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ReviseQuotation
{
    public record ReviseQuotationCommand : ReviseQuotationDto, ICommand<Result<QuotationDto, Error>>;
}
