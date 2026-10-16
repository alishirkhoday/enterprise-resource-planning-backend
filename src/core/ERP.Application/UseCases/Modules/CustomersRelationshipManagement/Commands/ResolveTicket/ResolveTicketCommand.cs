using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ResolveTicket
{
    public record ResolveTicketCommand : ResolveTicketDto, ICommand<Result<TicketDto, Error>>;
}
