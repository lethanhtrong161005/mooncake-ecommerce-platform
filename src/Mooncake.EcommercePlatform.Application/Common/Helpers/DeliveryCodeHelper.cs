namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using System.Security.Cryptography;

/// <summary>Generates secure short codes for public driver trip lookup.</summary>
public static class DeliveryCodeHelper
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    public static string GenerateShareCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}
