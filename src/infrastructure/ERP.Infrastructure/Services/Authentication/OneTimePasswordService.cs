using ERP.Application.Common.Interfaces.Authentication;
using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Users;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Infrastructure.Services.Authentication
{
    public sealed class OneTimePasswordService : IOneTimePasswordService
    {
        private readonly ICacheDbContext _context;
        private readonly IDatabase _db;

        private const int OtpTTLMinutes = 2;
        private const int RequestLimit = 5;
        private const int RateLimitMinutes = 10;

        public OneTimePasswordService(ICacheDbContext context)
        {
            _context = context.WithPrefix("otp:");
            _db = _context.Db;
        }

        private string CodeKey(string normalizeMobilePhoneNumberOrEmail) => $"{_context.Prefix}code:{normalizeMobilePhoneNumberOrEmail}";
        private string CountKey(string normalizeMobilePhoneNumberOrEmail) => $"{_context.Prefix}count:{normalizeMobilePhoneNumberOrEmail}";

        public async Task<Tuple<string?, bool>> GenerateAsync(string normalizeMobilePhoneNumberOrEmail, OneTimePasswordChannel channel, CancellationToken cancellationToken = default)
        {
            var codeKey = CodeKey(normalizeMobilePhoneNumberOrEmail);
            var countKey = CountKey(normalizeMobilePhoneNumberOrEmail);

            // Atomic increment
            long count = await _db.StringIncrementAsync(countKey);

            // Set TTL on first request
            if (count == 1)
                await _db.KeyExpireAsync(countKey, TimeSpan.FromMinutes(RateLimitMinutes));

            if (count > RequestLimit)
                return new Tuple<string?, bool>(null, false);

            // Secure OTP 6-digit
            string code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Only the hash is stored. Anyone who reads the cache still cannot sign in with what
            // they find there, and the plain code exists only long enough to be sent.
            var otp = new OneTimePassword(
                normalizeMobilePhoneNumberOrEmail,
                Hash(code),
                DateTimeOffset.UtcNow.AddMinutes(OtpTTLMinutes),
                channel);

            string json = JsonConvert.SerializeObject(otp);

            // Save OTP with TTL
            await _db.StringSetAsync(codeKey, json, TimeSpan.FromMinutes(OtpTTLMinutes));

            return new Tuple<string?, bool>(code, true);
        }

        public async Task<OneTimePassword?> GetByMobilePhoneNumberOrEmailAsync(string normalizeMobilePhoneNumberOrEmail, CancellationToken cancellationToken = default)
        {
            var codeKey = CodeKey(normalizeMobilePhoneNumberOrEmail);
            var value = await _db.StringGetAsync(codeKey);
            if (value.IsNullOrEmpty)
                return null;

            var json = value.ToString();
            return json is null ? null : JsonConvert.DeserializeObject<OneTimePassword>(json);
        }

        public async Task<bool> VerifyAsync(string normalizeMobilePhoneNumberOrEmail, string inputCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputCode))
                return false;

            var codeKey = CodeKey(normalizeMobilePhoneNumberOrEmail);

            // LUA Script – get + delete atomically
            string script = @"
            local v = redis.call('GET', KEYS[1])
            if v then
                redis.call('DEL', KEYS[1])
            end
            return v
            ";

            var scriptEvaluate = await _db.ScriptEvaluateAsync(script, [codeKey], []);
            if (scriptEvaluate.IsNull)
                return false;

            var result = scriptEvaluate.ToString();
            if (string.IsNullOrEmpty(result))
                return false;

            var otp = JsonConvert.DeserializeObject<OneTimePassword>(result);

            if (otp is null)
                return false;

            if (!otp.IsUsable)
                return false;

            // Compared in constant time so the answer cannot be guessed a character at a time.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(otp.CodeHash),
                Encoding.UTF8.GetBytes(Hash(inputCode)));
        }

        public async Task<int?> GetUserOTPCountAsync(string normalizeMobilePhoneNumberOrEmail, CancellationToken cancellationToken = default)
        {
            var countKey = CountKey(normalizeMobilePhoneNumberOrEmail);
            var val = await _db.StringGetAsync(countKey);
            if (val.IsNullOrEmpty)
                return 0;

            return int.TryParse(val.ToString(), out var count) ? count : 0;
        }

        private static string Hash(string code) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    }
}
