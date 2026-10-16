using System.Security.Cryptography;

namespace ERP.Application.Common.Security
{
    public static class PasswordHashService
    {
        public static string HashPassword(this string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);

            var key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
        }

        public static bool VerifyPassword(string hash, string password)
        {
            var parts = hash.Split(':');
            var salt = Convert.FromBase64String(parts[0]);
            var key = Convert.FromBase64String(parts[1]);

            var candidate = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );

            return CryptographicOperations.FixedTimeEquals(candidate, key);
        }
    }
}
