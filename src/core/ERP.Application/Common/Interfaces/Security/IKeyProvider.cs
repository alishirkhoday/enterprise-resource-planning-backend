namespace ERP.Application.Common.Interfaces.Security
{
    /// <summary>
    /// Abstraction to fetch keys from a secure store (KeyVault, KMS, config, etc).
    /// Should not expose keys in logs.
    /// Implementations must return base64-encoded keys.
    /// </summary>
    public interface IKeyProvider
    {
        /// <summary>Return mapping of keyId -> base64(keyBytes)</summary>
        IReadOnlyDictionary<string, string> LoadAllKeys();

        /// <summary>Return the current/active key id (optional)</summary>
        string? GetActiveKeyId();
    }
}
