using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.CloseTicket
{
    public record CloseTicketCommand : CloseTicketDto, ICommand<Result<TicketDto, Error>>;
}
