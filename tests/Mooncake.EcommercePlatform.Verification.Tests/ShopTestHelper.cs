namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public static class ShopTestHelper
{
    public static async Task<(Guid TemplateId, Guid RevisionId)> EnsurePublishedTemplateRevisionAsync(
        ApplicationDbContext db,
        DateTime? now = null)
    {
        var existingRevision = await db.ShopTemplateRevisions.AsNoTracking().FirstOrDefaultAsync();
        if (existingRevision is not null)
        {
            return (existingRevision.TemplateId, existingRevision.Id);
        }

        var timestamp = now ?? DateTime.UtcNow;
        var templateId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();

        var template = new ShopTemplate
        {
            Id = templateId,
            Code = $"tpl-{Guid.NewGuid():N}"[..16],
            Version = 1,
            Name = "Default Test Shop Template",
            IsActive = false,
            SortOrder = 1,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
        db.ShopTemplates.Add(template);
        await db.SaveChangesAsync();

        var revision = new ShopTemplateRevision
        {
            Id = revisionId,
            TemplateId = templateId,
            Revision = 1,
            RendererKey = "heritage",
            SchemaVersion = 1,
            CssVariables = "{}",
            LayoutConfig = "{}",
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
        db.ShopTemplateRevisions.Add(revision);
        await db.SaveChangesAsync();

        template.CurrentRevisionId = revisionId;
        template.IsActive = true;
        await db.SaveChangesAsync();

        return (templateId, revisionId);
    }
}
