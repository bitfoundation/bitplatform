namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Says how long ago something happened, the way the Progress page shows it: just now, minutes, hours, then the date.</summary>
public static class WorkbenchTimes
{
    public static string Ago(DateTimeOffset at, IStringLocalizer<WorkbenchStrings> localizer)
    {
        var elapsed = DateTimeOffset.UtcNow - at;

        if (elapsed < TimeSpan.FromMinutes(1))
            return localizer[nameof(WorkbenchStrings.TimeJustNow)];

        if (elapsed < TimeSpan.FromHours(1))
            return localizer[nameof(WorkbenchStrings.TimeMinutesAgo), (int)elapsed.TotalMinutes];

        if (elapsed < TimeSpan.FromHours(24))
            return localizer[nameof(WorkbenchStrings.TimeHoursAgo), (int)elapsed.TotalHours];

        return at.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture);
    }
}
