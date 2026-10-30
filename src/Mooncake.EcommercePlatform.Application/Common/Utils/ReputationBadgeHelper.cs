namespace Mooncake.EcommercePlatform.Application.Common.Utils;

/// <summary>Helper to resolve supplier reputation tier badges and styling.</summary>
public static class ReputationBadgeHelper
{
    public static (string Tier, string Title, string Color) ResolveBadge(decimal score)
    {
        if (score >= 81m)
        {
            return ("GoldPartner", "Đối tác Vàng", "amber");
        }

        if (score >= 51m)
        {
            return ("Reputable", "Uy tín", "purple");
        }

        if (score >= 21m)
        {
            return ("Reliable", "Tin cậy", "blue");
        }

        return ("New", "Mới", "stone");
    }
}

