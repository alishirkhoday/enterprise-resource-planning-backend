using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.Mapping;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;
using ERP.Domain.Repositories.Modules.CorrespondenceManagement;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.ReferLetter
{
    /// <summary>
    /// Passes a letter to somebody with an instruction - ارجاع - and records that it is now their
    /// move.
    /// <para>
    /// This is the act the register exists to make traceable. The question anybody asks about a
    /// letter that went nowhere is where it stopped, and that is only answerable because each
    /// hand-off survives the next one.
    /// </para>
    /// </summary>
    public class ReferLetterCommandHandler(ILetterRepository letters)
        : IRequestHandler<ReferLetterCommand, Result<LetterDto, Error>>
    {
        public async Task<Result<LetterDto, Error>> Handle(ReferLetterCommand request, CancellationToken cancellationToken)
        {
            var letter = await letters.GetWithRecipientsAndReferralsAsync(request.LetterId, cancellationToken);
            if (letter is null)
                return Result.Failure<LetterDto, Error>(Errors.General.NotFound(request.LetterId.ToString(), nameof(Letter)));

            try
            {
                // The letter refuses a second live referral to the same person, and refuses any
                // referral once it is archived. Both are its rules, not this method's.
                letter.ReferTo(request.ToUserId, request.FromUserId, request.Instruction, request.DueDate);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<LetterDto, Error>(Errors.Correspondence.LetterCannotBeReferred(exception.Message));
            }

            await letters.UpdateAsync(letter, cancellationToken);

            return Result.Success<LetterDto, Error>(letter.ToDto());
        }
    }
}
