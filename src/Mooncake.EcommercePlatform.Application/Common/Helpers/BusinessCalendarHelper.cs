namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

/// <summary>Calculates business-day SLA deadlines in UTC (Monday through Friday).</summary>
public static class BusinessCalendarHelper
{
    /// <summary>Adds the specified number of business days (skipping Saturday and Sunday) to a UTC timestamp.</summary>
    public static DateTime AddBusinessDays(DateTime startUtc, int businessDays)
    {
        if (businessDays <= 0)
            return startUtc;

        var current = startUtc;
        var remaining = businessDays;
        while (remaining > 0)
        {
            current = current.AddDays(1);
            if (current.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                remaining--;
        }

        return current;
    }
}

