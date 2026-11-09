using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// A headless segmented time input (React Aria's TimeField): hour, minute (and second) segments — plus AM / PM on a
/// 12-hour clock — each a <c>role="spinbutton"</c>, in the order of the culture's time pattern (<c>h:mm tt</c> in
/// <c>en-US</c>, <c>HH:mm</c> in <c>de-DE</c>). It types and steps like <see cref="BitBhDateField"/>; on the AM / PM
/// segment, ↑ / ↓ toggle and "a" / "p" (or the first letter of the culture's designators) set it.
///
/// <code>
/// &lt;BitBhTimeField @bind-Value="start" Granularity="BitBhTimeGranularity.Minute" aria-label="Start time" /&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> as <see cref="BitBhDateField"/>.</para>
/// </summary>
public class BitBhTimeField : BitBhDateFieldBase<TimeOnly>
{
    /// <summary>The smallest unit edited. Default: <see cref="BitBhTimeGranularity.Minute"/>.</summary>
    [Parameter]
    public BitBhTimeGranularity Granularity { get; set; } = BitBhTimeGranularity.Minute;

    /// <summary>A 12-hour (AM / PM) or a 24-hour clock. Default: the culture's (whether its time pattern has AM / PM).</summary>
    [Parameter]
    public BitBhHourCycle? HourCycle { get; set; }

    private protected override List<BitBhDateSegmentState.Spec> BuildLayout(BitBhCalendarSystem system, out bool is12Hour)
        => TimeLayout(system, Granularity, HourCycle, out is12Hour);

    /// <summary>The time segments of the culture's long time pattern, cut to <paramref name="granularity"/> and set to the hour cycle.</summary>
    internal static List<BitBhDateSegmentState.Spec> TimeLayout(BitBhCalendarSystem system, BitBhTimeGranularity granularity, BitBhHourCycle? cycle, out bool is12Hour)
    {
        var pattern = system.Format.LongTimePattern;
        var layout = BitBhDateSegmentState.Parse(pattern);
        if (!layout.Any(s => s.Type == BitBhDateSegmentType.Hour))
        {
            pattern = "HH:mm:ss";
            layout = BitBhDateSegmentState.Parse(pattern);
        }
        // Only the time: a pattern that also holds date parts drops them.
        foreach (var type in new[] { BitBhDateSegmentType.Year, BitBhDateSegmentType.Month, BitBhDateSegmentType.Day })
            BitBhDateSegmentState.Remove(layout, type);

        var cultureIs12 = layout.Any(s => s.Type == BitBhDateSegmentType.DayPeriod) && pattern.Contains('h');
        is12Hour = cycle switch
        {
            BitBhHourCycle.H12 => true,
            BitBhHourCycle.H24 => false,
            _ => cultureIs12,
        };
        if (!is12Hour) BitBhDateSegmentState.Remove(layout, BitBhDateSegmentType.DayPeriod);
        else if (!layout.Any(s => s.Type == BitBhDateSegmentType.DayPeriod))
        {
            layout.Add(new BitBhDateSegmentState.Spec(BitBhDateSegmentType.Literal, " "));
            layout.Add(new BitBhDateSegmentState.Spec(BitBhDateSegmentType.DayPeriod));
        }

        if (granularity < BitBhTimeGranularity.Second) BitBhDateSegmentState.Remove(layout, BitBhDateSegmentType.Second);
        if (granularity < BitBhTimeGranularity.Minute) BitBhDateSegmentState.Remove(layout, BitBhDateSegmentType.Minute);
        else if (!layout.Any(s => s.Type == BitBhDateSegmentType.Minute))
        {
            var hour = layout.FindIndex(s => s.Type == BitBhDateSegmentType.Hour);
            layout.Insert(hour + 1, new BitBhDateSegmentState.Spec(BitBhDateSegmentType.Literal, ":"));
            layout.Insert(hour + 2, new BitBhDateSegmentState.Spec(BitBhDateSegmentType.Minute, Pad: true));
        }
        return layout;
    }

    private protected override void Load(TimeOnly value) => State.SetTime(value);

    private protected override TimeOnly? Compose() => State.ComposeTime();

    private protected override string ToIso(TimeOnly value)
        => value.ToString(Granularity == BitBhTimeGranularity.Second ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture);

    private protected override int PlaceholderPart(BitBhDateSegmentType type)
    {
        var placeholder = PlaceholderValue ?? TimeOnly.MinValue;
        return type switch
        {
            BitBhDateSegmentType.Hour => State.Is12Hour ? (placeholder.Hour % 12 == 0 ? 12 : placeholder.Hour % 12) : placeholder.Hour,
            BitBhDateSegmentType.Minute => placeholder.Minute,
            BitBhDateSegmentType.Second => placeholder.Second,
            BitBhDateSegmentType.DayPeriod => placeholder.Hour >= 12 ? 1 : 0,
            _ => 0,
        };
    }
}
