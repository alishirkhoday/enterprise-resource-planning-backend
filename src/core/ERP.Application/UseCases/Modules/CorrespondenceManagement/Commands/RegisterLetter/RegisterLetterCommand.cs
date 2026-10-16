using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.RegisterLetter
{
    public record RegisterLetterCommand : RegisterLetterDto, ICommand<Result<LetterDto, Error>>;
}
