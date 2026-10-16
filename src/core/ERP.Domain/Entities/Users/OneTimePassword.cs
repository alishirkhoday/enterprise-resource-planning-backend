namespace ERP.Domain.Entities.Users;

/// <summary>
/// A short lived verification code. The code itself is never kept, only its hash, so a leak of
/// the store does not hand anybody a working code.
/// </summary>
public sealed class OneTimePassword
{
    public string UserId { get; private set; } = default!;

    /// <summary>The recipient the code was sent to, normalized.</summary>
    public string Destination { get; private set; } = default!;

    public string CodeHash { get; private set; } = default!;
    public DateTimeOffset ExpireAt { get; private set; }
    public OneTimePasswordChannel Channel { get; private set; }
    public OneTimePasswordPurpose Purpose { get; private set; }

    public int Attempts { get; private set; }
    public int MaximumAttempts { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private OneTimePassword() { }

    public OneTimePassword(
        string destination,
        string codeHash,
        DateTimeOffset expireAt,
        OneTimePasswordChannel channel,
        OneTimePasswordPurpose purpose = OneTimePasswordPurpose.Verification,
        int maximumAttempts = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination, nameof(destination));
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash, nameof(codeHash));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumAttempts, nameof(maximumAttempts));
        if (expireAt <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expireAt), "The expiry must be in the future.");

        Destination = destination.Trim();
        CodeHash = codeHash;
        ExpireAt = expireAt;
        Channel = channel;
        Purpose = purpose;
        MaximumAttempts = maximumAttempts;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AttachToUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        UserId = userId.Trim();
    }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpireAt;
    public bool IsUsed => UsedAt is not null;
    public bool HasAttemptsLeft => Attempts < MaximumAttempts;
    public bool IsUsable => !IsExpired && !IsUsed && HasAttemptsLeft;

    /// <summary>Records a failed attempt, which brings the code closer to being locked out.</summary>
    public void RecordFailedAttempt() => Attempts++;

    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("The code has already been used.");
        if (IsExpired)
            throw new InvalidOperationException("The code has expired.");
        UsedAt = DateTimeOffset.UtcNow;
    }
}
