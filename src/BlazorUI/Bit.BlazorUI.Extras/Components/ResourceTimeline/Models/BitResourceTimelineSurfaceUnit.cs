using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// What one column stripe of <see cref="BitResourceTimelineColumnStripes"/> covers.
/// The stripe a given instant falls in is decided from the calendar in
/// <c>Options.TimeZone</c> (or the viewer's zone), so it stays put as the user scrolls.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitResourceTimelineSurfaceUnit>))]
public enum BitResourceTimelineSurfaceUnit
{
    /// <summary>One stripe per local calendar day. The default.</summary>
    Day,

    /// <summary>One stripe per clock hour, continuing through midnight.</summary>
    Hour,

    /// <summary>
    /// One stripe per week. Weeks start on <c>Options.FirstDayOfWeek</c>
    /// (or the locale's week start, otherwise Monday).
    /// </summary>
    Week
}
