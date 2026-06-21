namespace Bit.BlazorUI;

/// <summary>
/// Supported calendar view types that control how the date range and events are laid out.
/// </summary>
public enum BitFullCalendarView
{
    /// <summary>Single-day time grid showing hourly slots for the selected date.</summary>
    Day,

    /// <summary>Seven-day time grid for the week containing the selected date.</summary>
    Week,

    /// <summary>Month grid with one cell per day for the selected month.</summary>
    Month,

    /// <summary>Twelve-month overview of the selected year.</summary>
    Year,

    /// <summary>Scrollable, grouped list of the selected month's events.</summary>
    Agenda
}

