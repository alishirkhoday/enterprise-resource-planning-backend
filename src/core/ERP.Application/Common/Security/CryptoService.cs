using ERP.Application.Common.Interfaces.Security;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Application.Common.Security
{
    /// <summary>
    /// Provides symmetric authenticated encryption using AES-256-GCM.
    /// Keys are stored in-memory and indexed by a key identifier(keyId).
    /// Token format: "{keyId}:{Base64(nonce|ciphertext|tag)}"
    /// </summary>
    public sealed class CryptoService : ICryptoService
    {
        private const int KeyBytesLength = 32; // AES-256
        private const int NonceSize = 12;      // 96-bit nonce (recommended for AES-GCM)
        private const int TagSize = 16;        // 128-bit authentication tag

        // In-memory key store: keyId -> keyBytes
        private readonly ConcurrentDictionary<string, byte[]> _keys = new();

        // Protect active key change
        private readonly Lock _activeKeyLock = new();
        private string _activeKeyId = "";

        /// <summary>
        /// Creates the AES-GCM crypto service and loads keys from the given provider.
        /// </summary>
        public CryptoService(IKeyProvider keyProvider)
        {
            LoadKeysFromProvider(keyProvider);
        }

        /// <summary>
        /// Loads all keys from the provider and sets the active key.
        /// </summary>
        private void LoadKeysFromProvider(IKeyProvider keyProvider)
        {
            var loadedKeys = keyProvider.LoadAllKeys();

            foreach (var kv in loadedKeys)
            {
                AddKeyInternal(kv.Key, kv.Value);
            }

            var active = keyProvider.GetActiveKeyId();

            if (active != null)
            {
                if (!_keys.ContainsKey(active))
                    throw new InvalidOperationException("ActiveKeyId not found in loaded keys.");

                _activeKeyId = active;
            }
            else
            {
                if (_keys.IsEmpty)
                    throw new InvalidOperationException("No encryption keys were loaded.");

                _activeKeyId = _keys.Keys.First(); // arbitrarily pick the first
            }
        }

        /// <inheritdoc/>
        public void AddKey(string keyId, string base64Key, bool setActive = false)
        {
            AddKeyInternal(keyId, base64Key);

            if (setActive)
                SetActiveKey(keyId);
        }

        /// <summary>
        /// Validates and inserts a new key into the internal key dictionary.
        /// </summary>
        private void AddKeyInternal(string keyId, string base64Key)
        {
            if (string.IsNullOrWhiteSpace(keyId))
                throw new ArgumentNullException(nameof(keyId));
            if (string.IsNullOrWhiteSpace(base64Key))
                throw new ArgumentNullException(nameof(base64Key));

            var decoded = Convert.FromBase64String(base64Key);
            if (decoded.Length != KeyBytesLength)
                throw new ArgumentException("Key must be 32 bytes when base64-decoded (AES-256).", nameof(base64Key));

            var arr = new byte[KeyBytesLength];
            Buffer.BlockCopy(decoded, 0, arr, 0, KeyBytesLength);

            Array.Clear(decoded); // wipe temp buffer

            _keys[keyId] = arr;
        }

        /// <inheritdoc/>
        public void SetActiveKey(string keyId)
        {
            if (!_keys.ContainsKey(keyId))
                throw new InvalidOperationException("KeyId not found.");

            lock (_activeKeyLock)
            {
                _activeKeyId = keyId;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> ListKeyIds() =>
            _keys.Keys.ToList().AsReadOnly();

        /// <inheritdoc/>
        public string Encrypt(string plainText)
        {
            ArgumentNullException.ThrowIfNull(plainText);

            // snapshot current key
            string keyId;
            lock (_activeKeyLock)
                keyId = _activeKeyId;

            if (!_keys.TryGetValue(keyId, out var keyBytes))
                throw new InvalidOperationException("Active key not found.");

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            try
            {
                using var aes = new AesGcm(keyBytes, tag.Length);
                aes.Encrypt(nonce, plainBytes, cipher, tag);
            }
            finally
            {
                Array.Clear(plainBytes); // wipe sensitive data ASAP
            }

            // Build payload: nonce | cipher | tag
            var payload = new byte[nonce.Length + cipher.Length + tag.Length];

            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
            Buffer.BlockCopy(cipher, 0, payload, nonce.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, payload, nonce.Length + cipher.Length, tag.Length);

            Array.Clear(cipher);
            Array.Clear(tag);

            var base64Payload = Convert.ToBase64String(payload);
            Array.Clear(payload);

            return $"{keyId}:{base64Payload}";
        }

        /// <inheritdoc/>
        public bool TryDecrypt(string token, out string? plainText)
        {
            plainText = null;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            var colonIndex = token.IndexOf(':');
            if (colonIndex <= 0)
                return false;

            var keyId = token.Substring(0, colonIndex);
            var base64 = token.Substring(colonIndex + 1);

            if (!_keys.TryGetValue(keyId, out var keyBytes))
                return false;

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(base64);
            }
            catch
            {
                return false;
            }

            if (payload.Length < NonceSize + TagSize)
            {
                Array.Clear(payload);
                return false;
            }

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(payload, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(payload, payload.Length - TagSize, tag, 0, TagSize);

            var cipherLength = payload.Length - NonceSize - TagSize;
            var cipher = new byte[cipherLength];
            Buffer.BlockCopy(payload, NonceSize, cipher, 0, cipherLength);

            Array.Clear(payload);

            try
            {
                var plainBytes = new byte[cipherLength];
                var aes = new AesGcm(keyBytes, tag.Length);
                aes.Decrypt(nonce, cipher, tag, plainBytes);

                plainText = Encoding.UTF8.GetString(plainBytes);

                Array.Clear(cipher);
                Array.Clear(tag);
                Array.Clear(nonce);
                Array.Clear(plainBytes);

                return true;
            }
            catch
            {
                Array.Clear(cipher);
                Array.Clear(tag);
                Array.Clear(nonce);
                return false;
            }
        }

        /// <inheritdoc/>
        public string Decrypt(string token)
        {
            if (!TryDecrypt(token, out var plain))
                throw new CryptographicException("Decryption failed or authentication failed.");

            return plain!;
        }
    }
}
