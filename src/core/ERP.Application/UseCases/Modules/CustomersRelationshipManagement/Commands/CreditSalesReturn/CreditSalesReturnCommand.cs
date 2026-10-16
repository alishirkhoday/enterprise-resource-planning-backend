using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CreditSalesReturn
{
    public record CreditSalesReturnCommand : CreditSalesReturnDto, ICommand<Result<InvoiceDto, Error>>;
}
