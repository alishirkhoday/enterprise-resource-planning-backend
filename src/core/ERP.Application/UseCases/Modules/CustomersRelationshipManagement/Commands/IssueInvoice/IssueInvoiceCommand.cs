using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.IssueInvoice
{
    public record IssueInvoiceCommand : IssueInvoiceDto, ICommand<Result<InvoiceDto, Error>>;
}
