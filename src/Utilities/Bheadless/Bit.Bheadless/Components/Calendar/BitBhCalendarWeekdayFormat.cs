namespace Bit.Bheadless;

/// <summary>How the week-day column headers of a <see cref="BitBhCalendarGrid"/> name the days.</summary>
public enum BitBhCalendarWeekdayFormat
{
    /// <summary>The culture's shortest names: "S", "M" … in English, "So", "Mo" … in German.</summary>
    Narrow,

    /// <summary>The culture's abbreviated names: "Sun", "Mon" …</summary>
    Abbreviated,

    /// <summary>The full names: "Sunday", "Monday" …</summary>
    Full,
}
