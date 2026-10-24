using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.Mapping;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;
using ERP.Domain.Repositories.Modules.CorrespondenceManagement;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.AnswerReferral
{
    /// <summary>
    /// Answers a referral, or takes it back.
    /// <para>
    /// Both close the same open item, which is why they are one command: a letter can only be
    /// filed once nobody is still holding it, and something has to account for every referral
    /// that was ever made.
    /// </para>
    /// </summary>
    public class AnswerReferralCommandHandler(ILetterRepository letters)
        : IRequestHandler<AnswerReferralCommand, Result<LetterDto, Error>>
    {
        public async Task<Result<LetterDto, Error>> Handle(AnswerReferralCommand request, CancellationToken cancellationToken)
        {
            var letter = await letters.GetByReferralAsync(request.ReferralId, cancellationToken);
            if (letter is null)
                return Result.Failure<LetterDto, Error>(Errors.General.NotFound(request.ReferralId.ToString(), nameof(LetterReferral)));

            var referral = letter.Referrals.FirstOrDefault(r => r.Id == request.ReferralId);
            if (referral is null)
                return Result.Failure<LetterDto, Error>(Errors.General.NotFound(request.ReferralId.ToString(), nameof(LetterReferral)));

            try
            {
                if (request.IsWithdrawal)
                    letter.WithdrawLetterReferral(referral.Id, request.Response);
                else
                    letter.RespondLetterReferral(referral.Id, request.Response);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Result.Failure<LetterDto, Error>(Errors.Correspondence.ReferralCannotBeAnswered(exception.Message));
            }

            // Once nobody is holding it any more the letter is done, so it closes itself rather
            // than waiting for somebody to remember to close it.
            if (letter.OutstandingReferrals.Count == 0 && letter.Status == LetterStatus.InProgress)
                letter.Close();

            await letters.UpdateAsync(letter, cancellationToken);

            return Result.Success<LetterDto, Error>(letter.ToDto());
        }
    }
}
