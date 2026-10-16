using System.Text.RegularExpressions;

namespace ERP.Domain.Primitives;

/// <summary>
/// Turns user-entered text into the one form the system stores and compares.
/// <para>
/// Every one of these exists because the same fact arrives written several ways - a phone number
/// with or without its trunk zero, a username in mixed case - and two spellings of one fact means
/// a lookup that misses and a duplicate that gets created.
/// </para>
/// </summary>
public static partial class Normalization
{
    /// <summary>
    /// Normalizes a username format from ArAsh_EsfanDiaRi to arash_esfandiari
    /// </summary>
    public static string NormalizeUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return username.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a phone number to format like 989123456789
    /// <para>
    /// Everything that is not a digit is dropped, so a region code written as +98 or 0098 and a
    /// number written as 0912 345 6789 all normalize to the same value. The leading zero of the
    /// national number is a trunk prefix that belongs only to dialling inside the country, so it
    /// is removed; keeping it would produce 9809123456789 and never match.
    /// </para>
    /// </summary>
    public static string NormalizeMobilePhoneNumber(string regionCode, string nationalNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalNumber);

        var region = KeepDigitsOnly(regionCode).TrimStart('0');
        var national = KeepDigitsOnly(nationalNumber).TrimStart('0');

        if (region.Length == 0)
            throw new ArgumentException("The region code contains no digits.", nameof(regionCode));

        if (national.Length == 0)
            throw new ArgumentException("The phone number contains no digits.", nameof(nationalNumber));

        return $"{region}{national}";
    }

    private static string KeepDigitsOnly(string value) =>
        string.Concat(value.Where(char.IsAsciiDigit));

    /// <summary>
    /// Normalizes a email format from ArAsh_EsfanDiaRi@EmAil.cOm to arash_esfandiari@email.com
    /// </summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a text format from Ar~Ash_Esfa}}{||[][nD*(*)*^%*/-*iaRi$#@$^ to ArAsh_EsfanDiaRi
    /// </summary>
    public static string RemoveSpecialCharacters(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return SpecialCharactersRegex().Replace(value.Trim(), "");
    }

    /// <summary>
    /// Source generated, so the pattern is compiled once at build time instead of being parsed
    /// again on every call.
    /// </summary>
    [GeneratedRegex("[-!@#$%^&*()…{}'_！©®℗™°℃℉÷⁍¸‸⁁⁒µ♪¡‽․⁞⁚⁝⁘⁖⁛⁙·‥…⁗‣•‷‶‵‴″′⁑⁂‡†§⁕„‚⁄¬‘’‚‛‟„”“⁄¬⁅›⁆‹⁾¦⁽—⁌–¶⁋`´¨‼⁈⁇ª●+=¿*\\\\,/.’<>?`~\":◯;©|¶\n\r\t\\[\\]]")]
    private static partial Regex SpecialCharactersRegex();
}
