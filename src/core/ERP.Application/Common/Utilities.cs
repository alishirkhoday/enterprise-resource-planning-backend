using System.Security.Cryptography;
using System.Text;

namespace ERP.Application.Common
{
    public static class Utilities
    {
        public static string? TrimToNull(string? value)
        {
            var v = value?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        public static string HashSha256(this string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var bytes = Encoding.UTF8.GetBytes(value);
            var hashed = SHA256.HashData(bytes);

            return Convert.ToBase64String(hashed);
        }

        public static string HMACSha256(this string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var key = Encoding.UTF8.GetBytes("keyForHashOtpCode");
            var data = Encoding.UTF8.GetBytes(value);
            var hashed = HMACSHA256.HashData(key, data);

            return Convert.ToBase64String(hashed);
        }

        public static string ToFullEnglishDifference(this DateTimeOffset from, DateTimeOffset to)
        {
            if (to < from)
                (from, to) = (to, from);

            int years = to.Year - from.Year;
            int months = to.Month - from.Month;
            int days = to.Day - from.Day;
            int hours = to.Hour - from.Hour;
            int minutes = to.Minute - from.Minute;
            int seconds = to.Second - from.Second;

            // Fix negative values (borrow)
            if (seconds < 0)
            {
                seconds += 60;
                minutes--;
            }

            if (minutes < 0)
            {
                minutes += 60;
                hours--;
            }

            if (hours < 0)
            {
                hours += 24;
                days--;
            }

            if (days < 0)
            {
                var previousMonth = to.AddMonths(-1);
                days += DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
                months--;
            }

            if (months < 0)
            {
                months += 12;
                years--;
            }

            // Build list of non-zero parts
            var parts = new List<string>();

            if (years > 0) parts.Add($"{years} year{(years > 1 ? "s" : "")}");
            if (months > 0) parts.Add($"{months} month{(months > 1 ? "s" : "")}");
            if (days > 0) parts.Add($"{days} day{(days > 1 ? "s" : "")}");
            if (hours > 0) parts.Add($"{hours} hour{(hours > 1 ? "s" : "")}");
            if (minutes > 0) parts.Add($"{minutes} minute{(minutes > 1 ? "s" : "")}");
            if (seconds > 0) parts.Add($"{seconds} second{(seconds > 1 ? "s" : "")}");

            // If all were zero
            if (!parts.Any())
                return "0 seconds";

            // Build readable output
            if (parts.Count == 1)
                return parts[0];

            return string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts.Last();
        }

        public static string ToHourMinuteDifference(this DateTimeOffset from, DateTimeOffset to)
        {
            if (to < from)
                (from, to) = (to, from);

            TimeSpan diff = to - from;

            int hours = (int)diff.TotalHours;
            int minutes = diff.Minutes;

            var parts = new List<string>();

            if (hours > 0)
                parts.Add($"{hours} hour{(hours > 1 ? "s" : "")}");

            if (minutes > 0)
                parts.Add($"{minutes} minute{(minutes > 1 ? "s" : "")}");

            if (parts.Count == 0)
                return "0 minutes";

            if (parts.Count == 1)
                return parts[0];

            return $"{parts[0]} and {parts[1]}";
        }

        public static string GetRandomCode(RandomType type, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
            var random = new Random();
            string chars = "";
            switch (type)
            {
                case RandomType.All:
                    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    break;
                case RandomType.Letters:
                    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                    break;
                case RandomType.UppercaseLetters:
                    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    break;
                case RandomType.UppercaseLettersNumbers:
                    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    break;
                case RandomType.LowercaseLetters:
                    chars = "abcdefghijklmnopqrstuvwxyz";
                    break;
                case RandomType.LowercaseLettersNumbers:
                    chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                    break;
                case RandomType.Numbers:
                    chars = "0123456789";
                    break;
                default:
                    break;
            }
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
