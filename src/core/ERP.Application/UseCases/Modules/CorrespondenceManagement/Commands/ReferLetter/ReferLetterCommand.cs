using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.ReferLetter
{
    public record ReferLetterCommand : ReferLetterDto, ICommand<Result<LetterDto, Error>>;
}
