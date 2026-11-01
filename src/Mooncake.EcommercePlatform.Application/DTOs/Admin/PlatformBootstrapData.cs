namespace Mooncake.EcommercePlatform.Application.DTOs.Admin;

public sealed record PlatformBootstrapData(string Email, string PasswordHash, string FullName,
    DateTime VerifiedAtUtc, IReadOnlyList<(string Name, int SortOrder)> DefaultCategories,
    string TemplateName, string TemplateDescription, string CssVariables, string LayoutConfig, int TemplateSortOrder);
