using ERP.Application.UseCases.Modules.CorrespondenceManagement.DTOs;
using ERP.Application.UseCases.Modules.CorrespondenceManagement.Mapping;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CorrespondenceManagement;

namespace ERP.Application.UseCases.Modules.CorrespondenceManagement.Commands.RegisterLetter
{
    /// <summary>
    /// Enters a letter in the register and gives it its number.
    /// <para>
    /// That number is the point of the whole module: it is how a later letter cites this one and
    /// how somebody finds it years afterwards. It is issued once here and never reissued, even if
    /// the letter is later withdrawn, because a citation that stops resolving is worse than a
    /// gap in the sequence.
    /// </para>
    /// </summary>
    public class RegisterLetterCommandHandler(
        ILetterRepository letters,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<RegisterLetterCommand, Result<LetterDto, Error>>
    {
        public async Task<Result<LetterDto, Error>> Handle(RegisterLetterCommand request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            // Inbound and outbound run in separate sequences, which is how a registry has always
            // worked: the two books are kept apart so a number always says which way it went.
            var seriesKey = $"Letter.{request.Direction}";

            var series = await numberingSeries.GetForUpdateAsync(seriesKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<LetterDto, Error>(Errors.Order.NumberingSeriesMissing(seriesKey));

            var number = series.Next(today);

            var duplicate = await letters.GetByNumberAsync(number, cancellationToken);
            if (duplicate is not null)
                return Result.Failure<LetterDto, Error>(Errors.Correspondence.LetterNumberIsUsed(number));

            var letter = new Letter(
                number,
                request.Direction,
                request.Subject,
                request.CorrespondentName,
                request.LetterDate);

            letter.SetBody(request.Body);
            letter.SetHandling(request.Confidentiality, request.Urgency);
            letter.SetCorrespondentReference(request.CorrespondentReference, request.CorrespondentReferenceDate);
            letter.IdentifyCorrespondent(request.CustomerId, request.SupplierId, request.HumanId);
            letter.SetDueDate(request.DueDate);
            letter.SetDescription(request.Description);

            if (request.InReplyToLetterId is not null || request.FollowsLetterId is not null)
                letter.SetThread(request.InReplyToLetterId, request.FollowsLetterId);

            if (request.OwnerUserId is not null)
                letter.AssignOwner(request.OwnerUserId.Value);

            if (request.BranchId is not null)
                letter.AssignToBranch(request.BranchId.Value);

            foreach (var recipient in request.Recipients)
            {
                var addressee = new LetterRecipient(letter, recipient.Kind, recipient.Name);
                letter.AddRecipient(addressee);
                letter.SetLetterRecipientContactDetails(addressee.Id, recipient.Organization, recipient.PostalAddress, recipient.Email);
                letter.IdentifyInternallyLetterRecipient(addressee.Id, recipient.UserId, recipient.HumanId);
            }

            await letters.CreateAsync(letter, cancellationToken);

            return Result.Success<LetterDto, Error>(letter.ToDto());
        }
    }
}
