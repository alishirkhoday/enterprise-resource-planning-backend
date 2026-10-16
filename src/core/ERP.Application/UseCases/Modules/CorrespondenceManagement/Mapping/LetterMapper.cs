using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Mapping
{
    public static class LetterMapper
    {
        public static LetterDto ToDto(this Letter letter)
        {
            ArgumentNullException.ThrowIfNull(letter, nameof(letter));

            return new LetterDto(
                letter.Id,
                letter.Number,
                letter.Direction,
                letter.Status,
                letter.Subject,
                letter.CorrespondentName,
                letter.LetterDate,
                letter.RegisteredOn,
                letter.Confidentiality,
                letter.Urgency,
                letter.DueDate,
                letter.OutstandingReferrals.Count,
                [.. letter.Referrals
                    .OrderBy(r => r.ReferredOn)
                    .Select(r => r.ToDto())]);
        }

        public static LetterReferralDto ToDto(this LetterReferral referral)
        {
            ArgumentNullException.ThrowIfNull(referral, nameof(referral));

            return new LetterReferralDto(
                referral.Id,
                referral.FromUserId,
                referral.ToUserId,
                referral.Instruction,
                referral.Status,
                referral.ReferredOn,
                referral.DueDate,
                referral.SeenOn,
                referral.RespondedOn,
                referral.Response);
        }
    }
}
