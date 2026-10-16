namespace ERP.Domain.Entities.Integration;

/// <summary>
/// Where an inbound message has got to. A message is stored before it is processed, so this
/// is what separates "arrived" from "acted on".
/// </summary>
public enum InboxMessageStatus
{
    /// <summary>Stored, not yet acted on. Also the state a failed message returns to while it still has attempts left.</summary>
    Received = 0,

    Processing = 1,
    Processed = 2,

    /// <summary>Out of attempts. Kept for a human to resolve, because the sender considers it delivered.</summary>
    Abandoned = 3,

    /// <summary>Understood and deliberately not acted on.</summary>
    Discarded = 4
}
