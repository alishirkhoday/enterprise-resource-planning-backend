using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.IssueQuotation
{
    public record IssueQuotationCommand : IssueQuotationDto, ICommand<Result<QuotationDto, Error>>;
}
