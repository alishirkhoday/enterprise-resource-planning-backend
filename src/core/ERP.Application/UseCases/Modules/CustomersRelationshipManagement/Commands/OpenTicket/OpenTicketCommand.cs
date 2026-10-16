using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.OpenTicket
{
    public record OpenTicketCommand : OpenTicketDto, ICommand<Result<TicketDto, Error>>;
}
