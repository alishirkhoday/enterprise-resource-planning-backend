namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

public sealed partial class Letter
{
    public void SetLetterRecipientContactDetails(Guid childId, string? organization, string? postalAddress, string? email)
    {
        var child = GetChild(childId, _recipients);
        child.SetContactDetails(organization, postalAddress, email);
    }

    public void IdentifyInternallyLetterRecipient(Guid childId, Guid? userId, Guid? humanId)
    {
        var child = GetChild(childId, _recipients);
        child.IdentifyInternally(userId, humanId);
    }

    public void SetLetterRecipientDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _recipients);
        child.SetDescription(description);
    }

    public void MarkLetterReferralAsSeen(Guid childId)
    {
        var child = GetChild(childId, _referrals);
        child.MarkAsSeen();
    }

    public void RespondLetterReferral(Guid childId, string response)
    {
        var child = GetChild(childId, _referrals);
        child.Respond(response);
    }

    public void WithdrawLetterReferral(Guid childId, string reason)
    {
        var child = GetChild(childId, _referrals);
        child.Withdraw(reason);
    }
}
