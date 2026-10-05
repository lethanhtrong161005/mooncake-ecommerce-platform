namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Responses;

/// <summary>Public category information.</summary>
public sealed record CategoryResponse(Guid Id, Guid? ParentId, string Name, string? Description, string? IconUrl, int SortOrder);
