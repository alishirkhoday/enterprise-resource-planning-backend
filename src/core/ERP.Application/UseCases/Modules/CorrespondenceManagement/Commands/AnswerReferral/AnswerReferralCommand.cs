using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.AnswerReferral
{
    public record AnswerReferralCommand : AnswerReferralDto, ICommand<Result<LetterDto, Error>>;
}
