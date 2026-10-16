namespace ERP.Application.Common.Interfaces.Security
{
    public interface ICryptoService
    {
        /// <summary>
        /// Encrypt plain text and return token: "{keyId}:{base64(nonce|ciphertext|tag)}"
        /// </summary>
        string Encrypt(string plainText);

        /// <summary>
        /// Try to decrypt token produced by Encrypt. Returns false if decryption/authentication fails.
        /// </summary>
        bool TryDecrypt(string token, out string? plainText);

        /// <summary>
        /// Decrypt or throw CryptographicException on failure.
        /// </summary>
        string Decrypt(string token);

        /// <summary>
        /// Add a new key into the key store (in-memory) and (optionally) set it active.
        /// base64Key must be 32 bytes when decoded (AES-256).
        /// </summary>
        void AddKey(string keyId, string base64Key, bool setActive = false);

        /// <summary>
        /// Set the active key id used for encryption.
        /// </summary>
        void SetActiveKey(string keyId);

        /// <summary>
        /// List known key ids (in memory).
        /// </summary>
        IReadOnlyCollection<string> ListKeyIds();
    }
}
