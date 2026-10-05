namespace Bit.BlazorUI;

/// <summary>
/// One day of a month grid, as <see cref="BitFullCalendar.MonthCellTemplate"/> receives it.
/// </summary>
public class BitFullCalendarCell
{
    /// <summary>The day number in the calendar's active calendar system (a Persian date shows its own day).</summary>
    public int Day { get; set; }

    /// <summary>True for a day of the month being shown; false for one the grid borrows from a neighbouring month.</summary>
    public bool CurrentMonth { get; set; }

    /// <summary>The date of the day.</summary>
    public DateTime Date { get; set; }
}
