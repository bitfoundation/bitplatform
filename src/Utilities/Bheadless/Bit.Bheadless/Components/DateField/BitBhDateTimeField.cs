using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// A headless segmented date-and-time input (React Aria's DateField with <c>granularity="minute"</c>): the date segments
/// of <see cref="BitBhDateField"/> followed by the time segments of <see cref="BitBhTimeField"/> — month, day, year, hour,
/// minute (and second), plus AM / PM on a 12-hour clock — each a <c>role="spinbutton"</c>, in the order of the culture's
/// short date and time patterns (<c>M/d/yyyy h:mm tt</c> in <c>en-US</c>, <c>dd.MM.yyyy HH:mm</c> in <c>de-DE</c>).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Typing and keys</b> — as <see cref="BitBhDateField"/> and <see cref="BitBhTimeField"/>: digits fill a segment
///     and move on (from the year into the hour), ↑ / ↓ step, ← / → move, Backspace deletes. Every segment is a tab stop.</item>
///   <item><b>Value</b> — <c>@bind-Value</c> a <c>DateTime?</c> (<see cref="DateTimeKind.Unspecified"/>): set once every
///     segment is filled, null while one is empty. <see cref="Granularity"/> decides the smallest unit edited (the units
///     below it are 0) and <see cref="HourCycle"/> the clock. A value outside <c>Min</c> / <c>Max</c> is kept and marks the
///     field invalid.</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> wiring and a hidden input (<c>Name</c>)
///     carrying <c>yyyy-MM-ddTHH:mm</c> (<c>yyyy-MM-ddTHH:mm:ss</c> with seconds), what <c>&lt;input type="datetime-local"&gt;</c> posts.</item>
/// </list>
///
/// <code>
/// &lt;BitBhField&gt;
///     &lt;BitBhLabel&gt;Meeting&lt;/BitBhLabel&gt;
///     &lt;BitBhDateTimeField @bind-Value="meeting" class="date-field" /&gt;
/// &lt;/BitBhField&gt;
/// </code>
///
/// <para>With a calendar to pick the date from, use <see cref="BitBhDateTimePicker"/>.</para>
///
/// <para><b>Styling hooks:</b> as <see cref="BitBhDateField"/>.</para>
/// </summary>
public class BitBhDateTimeField : BitBhDateFieldBase<DateTime>
{
    /// <summary>The smallest unit of time edited. Default: <see cref="BitBhTimeGranularity.Minute"/>.</summary>
    [Parameter]
    public BitBhTimeGranularity Granularity { get; set; } = BitBhTimeGranularity.Minute;

    /// <summary>A 12-hour (AM / PM) or a 24-hour clock. Default: the culture's (whether its time pattern has AM / PM).</summary>
    [Parameter]
    public BitBhHourCycle? HourCycle { get; set; }

    private protected override List<BitBhDateSegmentState.Spec> BuildLayout(BitBhCalendarSystem system, out bool is12Hour)
        => DateTimeLayout(system, Granularity, HourCycle, out is12Hour);

    /// <summary>The date segments of the culture's short date pattern, then its time segments, a space between them.</summary>
    internal static List<BitBhDateSegmentState.Spec> DateTimeLayout(BitBhCalendarSystem system, BitBhTimeGranularity granularity, BitBhHourCycle? cycle, out bool is12Hour)
    {
        var layout = BitBhDateField.DateLayout(system);
        layout.Add(new BitBhDateSegmentState.Spec(BitBhDateSegmentType.Literal, " "));
        layout.AddRange(BitBhTimeField.TimeLayout(system, granularity, cycle, out is12Hour));
        return layout;
    }

    private protected override void Load(DateTime value)
    {
        State.SetDate(DateOnly.FromDateTime(value));
        State.SetTime(TimeOnly.FromDateTime(value));
    }

    private protected override DateTime? Compose()
        => State.ComposeDate() is { } date && State.ComposeTime() is { } time ? date.ToDateTime(time) : null;

    private protected override string ToIso(DateTime value)
        => Normalize(value).ToString(Granularity == BitBhTimeGranularity.Second ? "yyyy-MM-ddTHH:mm:ss" : "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    // The units below the granularity are 0, as the segments compose them: a value from outside, the placeholder's time.
    private protected override DateTime Normalize(DateTime value)
    {
        var ticks = Granularity switch
        {
            BitBhTimeGranularity.Hour => TimeSpan.TicksPerHour,
            BitBhTimeGranularity.Minute => TimeSpan.TicksPerMinute,
            _ => TimeSpan.TicksPerSecond,
        };
        return new DateTime(value.Ticks - (value.Ticks % ticks), value.Kind);
    }

    /// <summary>The placeholder: <see cref="BitBhDateFieldBase{TValue}.PlaceholderValue"/>, else today at midnight.</summary>
    private DateTime Placeholder => PlaceholderValue ?? DateTime.Today;

    private protected override int PlaceholderYear => System.Year(DateOnly.FromDateTime(Placeholder));

    private protected override int PlaceholderPart(BitBhDateSegmentType type)
    {
        var placeholder = Placeholder;
        var date = DateOnly.FromDateTime(placeholder);
        return type switch
        {
            BitBhDateSegmentType.Year => System.Year(date),
            BitBhDateSegmentType.Month => System.Month(date),
            BitBhDateSegmentType.Day => System.Day(date),
            BitBhDateSegmentType.Hour => State.Is12Hour ? (placeholder.Hour % 12 == 0 ? 12 : placeholder.Hour % 12) : placeholder.Hour,
            BitBhDateSegmentType.Minute => placeholder.Minute,
            BitBhDateSegmentType.Second => placeholder.Second,
            BitBhDateSegmentType.DayPeriod => placeholder.Hour >= 12 ? 1 : 0,
            _ => 0,
        };
    }
}
