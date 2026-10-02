namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Converts PascalCase C# enum values to snake_case strings for PostgreSQL storage
/// and back. Example: <c>AwaitingDeposit ↔ "awaiting_deposit"</c>.
/// </summary>
public class SnakeCaseEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : struct, Enum
{
    public SnakeCaseEnumConverter() : base(
        v => ToSnakeCase(v.ToString()),
        v => ParseFromSnakeCase(v))
    { }

    private static string ToSnakeCase(string input) =>
        string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));

    private static TEnum ParseFromSnakeCase(string value) =>
        Enum.Parse<TEnum>(value.Replace("_", ""), ignoreCase: true);
}
