namespace Bit.BlazorUI;

/// <summary>
/// Which part of a recurring series an edit or a delete made on one of its occurrences applies to.
/// </summary>
public enum BitFullCalendarRecurrenceEditScope
{
    /// <summary>
    /// Only the chosen occurrence: the series skips its date, and an edited occurrence becomes an event of
    /// its own. The rest of the pattern is left untouched.
    /// </summary>
    ThisEvent,

    /// <summary>
    /// The chosen occurrence and every later one: the series ends the day before it, and an edited
    /// occurrence starts a new series of its own.
    /// </summary>
    ThisAndFollowing,

    /// <summary>
    /// Every occurrence: the series itself is edited or deleted.
    /// </summary>
    AllEvents
}
