namespace Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Validates that a GUID value is non-null and not <see cref="Guid.Empty"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NonEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is Guid guid && guid != Guid.Empty;
}

/// <summary>Cursor-paginated query for listing the current user's wishlist items.</summary>
public sealed record WishlistPageQuery : IValidatableObject
{
    [Range(1, WishlistConstants.MaxPageSize, ErrorMessage = WishlistConstants.InvalidPageSize)]
    public int PageSize { get; init; } = WishlistConstants.DefaultPageSize;

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    [MaxLength(2048, ErrorMessage = WishlistConstants.InvalidCursor)]
    public string? Cursor { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cursor is not null && string.IsNullOrWhiteSpace(Cursor))
        {
            yield return new ValidationResult(
                WishlistConstants.InvalidCursor,
                [nameof(Cursor)]);
        }
    }
}

/// <summary>Batch lookup query containing comma-separated or repeated product IDs.</summary>
public sealed record WishlistCheckQuery : IValidatableObject
{
    [Required(ErrorMessage = WishlistConstants.EmptyBatchIds)]
    public string[]? Ids { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var count = 0;
        foreach (var part in EnumerateRawParts(Ids))
        {
            if (!Guid.TryParse(part, out var guid) || guid == Guid.Empty)
            {
                yield return new ValidationResult(
                    WishlistConstants.InvalidProductIdList,
                    [nameof(Ids)]);
                yield break;
            }

            count++;
            if (count > WishlistConstants.MaxBatchCheckIds)
            {
                yield return new ValidationResult(
                    WishlistConstants.InvalidBatchSize,
                    [nameof(Ids)]);
                yield break;
            }
        }

        if (count == 0)
        {
            yield return new ValidationResult(
                WishlistConstants.EmptyBatchIds,
                [nameof(Ids)]);
        }
    }

    /// <summary>Parses the validated product IDs into a deduplicated array.</summary>
    public IReadOnlyList<Guid> ToDistinctProductIds() =>
        EnumerateRawParts(Ids)
            .Select(Guid.Parse)
            .Distinct()
            .ToArray();

    private static IEnumerable<string> EnumerateRawParts(string[]? rawIds)
    {
        if (rawIds is null)
            yield break;

        foreach (var entry in rawIds)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            foreach (var part in entry.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return part;
            }
        }
    }
}
