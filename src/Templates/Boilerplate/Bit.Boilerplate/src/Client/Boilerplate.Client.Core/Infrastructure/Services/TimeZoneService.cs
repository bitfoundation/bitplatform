namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Manages the user's preferred time zone (See AppMenu), which wins over the device's time zone wherever the app
/// needs one. The preference is stored on the device itself, so it works the same whether the user is signed in or
/// not. <see cref="CurrentTimeZone"/> carries the resolved value - per circuit on Blazor Server, per app elsewhere -
/// and <see cref="ToLocalTime"/> is how display code renders date/times with it; <see cref="TimeZoneInfo.Local"/>
/// itself stays the device's zone everywhere, because on Blazor Server it is process-wide state shared by every
/// visitor's circuit and could never carry a per-user preference.
/// </summary>
public partial class TimeZoneService
{
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;

    private const string TimeZoneStoreKey = "time-zone";

    /// <summary>
    /// The zone the app currently renders date/times in: the stored preference when there is one, the device's zone
    /// otherwise. Resolved by <see cref="ApplyPreferredTimeZone"/> at startup and kept fresh by
    /// <see cref="ChangeTimeZone"/>; scoped, so on Blazor Server each circuit carries its own visitor's value.
    /// </summary>
    public TimeZoneInfo CurrentTimeZone { get; private set; } = TimeZoneInfo.Local;

    /// <summary>
    /// Renders a moment in <see cref="CurrentTimeZone"/> - the replacement for `.ToLocalTime()` in display code,
    /// because <see cref="TimeZoneInfo.Local"/> is the device's (on Blazor Server: the server's) zone, not the
    /// user's preference.
    /// </summary>
    public DateTimeOffset ToLocalTime(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, CurrentTimeZone);

    public async Task<TimeZoneInfo> GetCurrentTimeZone()
    {
        var storedTimeZoneId = await storageService.GetItem(TimeZoneStoreKey);

        if (string.IsNullOrEmpty(storedTimeZoneId) is false
            && TimeZoneInfo.TryFindSystemTimeZoneById(NormalizeToPlatformId(storedTimeZoneId), out var storedTimeZone))
        {
            return storedTimeZone;
        }

        // An id this runtime cannot resolve at all falls back to the device's zone - failing every consumer would
        // be worse than ignoring a stale preference.
        return TimeZoneInfo.Local;
    }

    /// <summary>
    /// The same site runs on two id families: Windows ids on a Windows-hosted Blazor Server circuit, IANA ids in the
    /// browser's WebAssembly runtime - and Blazor Auto walks a visitor through both. A preference stored under the
    /// other family's id resolves (FindSystemTimeZoneById converts), but keeps the REQUESTED id on the returned zone,
    /// which then matches nothing in AppMenu's list of this runtime's own zones. Converting the id first keeps the
    /// stored preference working on whichever runtime reads it.
    /// </summary>
    private static string NormalizeToPlatformId(string timeZoneId)
    {
        if (OperatingSystem.IsWindows())
        {
            return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId) ? windowsId : timeZoneId;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId) ? ianaId : timeZoneId;
    }

    /// <summary>
    /// Resolves <see cref="CurrentTimeZone"/> from the stored preference - called by AppClientCoordinator at startup
    /// and by <see cref="ChangeTimeZone"/>.
    /// </summary>
    public async Task ApplyPreferredTimeZone()
    {
        CurrentTimeZone = await GetCurrentTimeZone();

        telemetryContext.TimeZone = CurrentTimeZone.Id;
    }

    public async Task ChangeTimeZone(string timeZoneId)
    {
        // Stored canonically as the IANA id whenever the platform can produce one: every runtime can read an IANA id
        // back (NormalizeToPlatformId converts it on Windows), while the browser's WebAssembly runtime cannot convert
        // a Windows id at all - its trimmed ICU data ships without the windowsZones mapping - and would silently fall
        // back to the device's zone.
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId))
        {
            timeZoneId = ianaId;
        }

        await storageService.SetItem(TimeZoneStoreKey, timeZoneId, persistent: true);

        await ApplyPreferredTimeZone();

        pubSubService.Publish(ClientAppMessages.TIME_ZONE_CHANGED, timeZoneId);
    }
}
