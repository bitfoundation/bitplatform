namespace Bit.BlazorUI;

/// <summary>
/// Controls how overlapping event cards are positioned in the day and week views.
/// </summary>
public enum BitFullCalendarEventLayout
{
    /// <summary>
    /// Overlapping cards are cascaded on top of each other: each successive card is offset to the
    /// right and extends to the column edge, so most of every card stays visible behind the others.
    /// </summary>
    Overlap,

    /// <summary>
    /// Overlapping cards are placed side by side in equal-width columns, stacked close together with
    /// no overlap (similar to Google Calendar).
    /// </summary>
    Stack
}
