using ERP.Application.UseCases.Modules.CustomersRelationshipManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CustomersRelationshipManagement.Commands.ReplyToTicket
{
    public record ReplyToTicketCommand : ReplyToTicketDto, ICommand<Result<TicketDto, Error>>;
}
