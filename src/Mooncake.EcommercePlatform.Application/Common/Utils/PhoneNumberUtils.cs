namespace Mooncake.EcommercePlatform.Application.Common.Utils;

using System.Text.RegularExpressions;

/// <summary>Normalizes Vietnamese mobile numbers to the local 0xxxxxxxxx form.</summary>
public static partial class PhoneNumberUtils
{
    public static string Normalize(string phone) =>
        NormalizePhoneRegex().Replace(phone.Trim(), string.Empty) switch
        {
            var number when number.StartsWith("+84", StringComparison.Ordinal) =>
                $"0{number[3..]}",
            var number when number.StartsWith("84", StringComparison.Ordinal) =>
                $"0{number[2..]}",
            var number => number
        };

    public static bool IsValid(string phone) => ValidPhoneRegex().IsMatch(phone);

    [GeneratedRegex(@"[\s.\-()]")]
    private static partial Regex NormalizePhoneRegex();

    [GeneratedRegex(@"^0[35789][0-9]{8}$")]
    private static partial Regex ValidPhoneRegex();
}
