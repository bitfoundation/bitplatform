using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// The table of time zones <see cref="BitTimeZonePicker{TValue}"/> lists, and the lookups that resolve any id a time
/// zone is written down with back to one of them.
/// </summary>
/// <remarks>
/// The table is shipped with the library rather than read off the runtime, because what a runtime lists differs from
/// one to the next: <see cref="TimeZoneInfo.GetSystemTimeZones()"/> answers with about 140 Windows ids on a Windows
/// server, with every IANA id and alias of its data in the browser's WebAssembly runtime, and with something else again
/// on Android and iOS - so a picker built on it would offer a visitor of a Blazor Server app on Windows other zones, under
/// other ids, than the same visitor sees once the app moves to WebAssembly. <see cref="All"/> is the same everywhere:
/// UTC and every location time zone of the IANA database, each under its canonical IANA id.
/// <br />
/// <see cref="Find(string)"/> resolves the ways a time zone is written down in practice - its canonical IANA id, an older
/// IANA id it was renamed from (the "Asia/Calcutta" and "Europe/Kiev" a browser built on CLDR still reports), a
/// country-style id ("US/Pacific"), a Windows id ("Pacific Standard Time") and the spellings of UTC - case
/// insensitively, out of a dictionary rather than by scanning the table.
/// </remarks>
public static partial class BitTimeZones
{
    // Every id the lookups answer to, the canonical ones of All first and then the aliases and the Windows ids, which
    // never take the place of a canonical one.
    private static readonly Dictionary<string, BitTimeZone> _idMap;

    // The older ids of each time zone, which a runtime whose data predates a rename (or that ships only the old names)
    // still finds it by.
    private static readonly Dictionary<string, string[]> _formerIds;

    // The TimeZoneInfo of each time zone on the running runtime, keyed by the ids it is looked up by, since that lookup
    // reads the data of the runtime and every picker of the process asks for every zone it lists.
    private static readonly ConcurrentDictionary<(string Id, string? WindowsId), TimeZoneInfo?> _timeZoneInfos = new();



    // A static constructor rather than field initializers, which C# runs in an unspecified order across the files of a
    // partial class: the table these are built from is declared in the other half.
    static BitTimeZones()
    {
        _idMap = new(All.Length + _aliases.Count + _windowsIds.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var zone in All)
        {
            _idMap.TryAdd(zone.Id, zone);
        }

        foreach (var (alias, id) in _aliases)
        {
            if (_idMap.TryGetValue(id, out var zone))
            {
                _idMap.TryAdd(alias, zone);
            }
        }

        foreach (var (windowsId, id) in _windowsIds)
        {
            if (_idMap.TryGetValue(id, out var zone))
            {
                _idMap.TryAdd(windowsId, zone);
            }
        }

        _formerIds = _aliases.GroupBy(a => a.Value, StringComparer.OrdinalIgnoreCase)
                             .ToDictionary(g => g.Key, g => g.Select(a => a.Key).ToArray(), StringComparer.OrdinalIgnoreCase);

        Utc = All[0];
    }



    /// <summary>
    /// Coordinated Universal Time, the time zone of the table that belongs to no country.
    /// </summary>
    public static BitTimeZone Utc { get; }



    /// <summary>
    /// Finds the time zone an id stands for, case insensitively: a canonical IANA id, an older IANA id it was renamed
    /// from, a country-style id, a Windows id or a spelling of UTC.
    /// </summary>
    /// <param name="id">
    /// The id to look up. Surrounding whitespace is ignored.
    /// </param>
    /// <returns>
    /// The time zone of <see cref="All"/> the id stands for, or null where it stands for none - an Etc/GMT+n zone,
    /// which is a fixed offset rather than a place, among them.
    /// </returns>
    public static BitTimeZone? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var trimmed = id.Trim();

        if (_idMap.TryGetValue(trimmed, out var zone)) return zone;

        // A Windows id newer than the table is still answered on a runtime that can convert it.
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(trimmed, out var ianaId) && _idMap.TryGetValue(ianaId, out zone)) return zone;

        return null;
    }

    /// <summary>
    /// Finds the time zone a <see cref="TimeZoneInfo"/> stands for, by its id - an IANA id or a Windows id, depending
    /// on the runtime it came from.
    /// </summary>
    /// <returns>
    /// The time zone of <see cref="All"/> the time zone info stands for, or null where it stands for none.
    /// </returns>
    public static BitTimeZone? Find(TimeZoneInfo? timeZone) => Find(timeZone?.Id);

    /// <summary>
    /// Writes an offset from UTC the way the picker shows it: "UTC+03:30", "UTC-08:00", "UTC+00:00".
    /// </summary>
    public static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var absolute = offset.Duration();

        return $"UTC{sign}{(int)absolute.TotalHours:00}:{absolute.Minutes:00}";
    }



    // See BitTimeZone.TimeZoneInfo.
    internal static TimeZoneInfo? GetTimeZoneInfo(BitTimeZone zone)
    {
        return _timeZoneInfos.GetOrAdd((zone.Id, zone.WindowsId), static key => FindTimeZoneInfo(key.Id, key.WindowsId));
    }



    private static TimeZoneInfo? FindTimeZoneInfo(string id, string? windowsId)
    {
        // UTC is the one time zone every runtime has, even one stripped of its time zone data.
        if (ReferenceEquals(Find(id), Utc)) return TimeZoneInfo.Utc;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var info)) return info;

        // A zone of the table that the data of the runtime knows only under an older id (Europe/Kyiv before 2022b), or
        // a custom one that was handed an alias of a canonical id.
        if (Find(id) is { } canonical)
        {
            if (string.Equals(canonical.Id, id, StringComparison.OrdinalIgnoreCase) is false &&
                TimeZoneInfo.TryFindSystemTimeZoneById(canonical.Id, out info)) return info;

            if (_formerIds.TryGetValue(canonical.Id, out var formerIds))
            {
                foreach (var formerId in formerIds)
                {
                    if (TimeZoneInfo.TryFindSystemTimeZoneById(formerId, out info)) return info;
                }
            }
        }

        // A Windows runtime that cannot convert IANA ids (NLS mode, or an id newer than its ICU) still has the zone under
        // its Windows id. That id is shared by every zone of the table it stands for (W. Europe Standard Time is Berlin,
        // Vienna, Rome, ...) and Find resolves it to only one of them, so the zone is handed back under its own id.
        if (windowsId is not null && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out info)) return WithId(info, Find(id)?.Id ?? id);

        return null;
    }

    // The same time zone - its offset, its names and its adjustment rules - under another id.
    internal static TimeZoneInfo WithId(TimeZoneInfo info, string id)
    {
        if (string.Equals(info.Id, id, StringComparison.OrdinalIgnoreCase)) return info;

        try
        {
            return TimeZoneInfo.CreateCustomTimeZone(id, info.BaseUtcOffset, info.DisplayName, info.StandardName, info.DaylightName, InOrder(info.GetAdjustmentRules()));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidTimeZoneException)
        {
            // Rules a runtime hands out but would not take back: the zone is still right, only under its Windows id.
            return info;
        }
    }

    // The rules a runtime reading tzdata (Linux, macOS) hands out are cut to whole days, so one ending on the day of a
    // transition and the next starting on it share that day (Berlin's 1945 and 1947 rules, the rule after its last
    // listed year), which CreateCustomTimeZone refuses as overlapping. The later rule starts the day after instead:
    // the earlier one still covers the day of its own transition.
    private static TimeZoneInfo.AdjustmentRule[] InOrder(TimeZoneInfo.AdjustmentRule[] rules)
    {
        var inOrder = new List<TimeZoneInfo.AdjustmentRule>(rules.Length);

        foreach (var rule in rules)
        {
            if (inOrder.Count > 0 && rule.DateStart <= inOrder[^1].DateEnd)
            {
                var start = inOrder[^1].DateEnd.AddDays(1);

                if (start > rule.DateEnd) continue;

                inOrder.Add(TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(start, rule.DateEnd, rule.DaylightDelta,
                                                                             rule.DaylightTransitionStart, rule.DaylightTransitionEnd,
                                                                             rule.BaseUtcOffsetDelta));
            }
            else
            {
                inOrder.Add(rule);
            }
        }

        return [.. inOrder];
    }
}
