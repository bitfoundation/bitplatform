namespace Bit.BlazorUI;

/// <summary>
/// Provides details about a date range change in the calendar,
/// fired when the user navigates (prev/next/today) or switches views.
/// </summary>
public sealed class BitFullCalendarDateChangeEventArgs
{
    /// <summary>
    /// First day the calendar shows (inclusive). In the month view it is the first day of the grid, which can belong to
    /// the previous month, unless the borrowed days are left blank (<c>ShowNonCurrentDates</c> off).
    /// </summary>
    public required DateTime Start { get; init; }

    /// <summary>
    /// Last day the calendar shows (inclusive). In the month view it is the last day of the grid, which can belong to the
    /// next month, unless the borrowed days are left blank (<c>ShowNonCurrentDates</c> off).
    /// </summary>
    public required DateTime End { get; init; }

    /// <summary>The active calendar view when the change occurred.</summary>
    public required BitFullCalendarView View { get; init; }
}
