using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// How a column color cycle lines up with the calendar.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitResourceTimelineStripeAlign>))]
public enum BitResourceTimelineStripeAlign
{
    /// <summary>
    /// The cycle runs across the whole timeline. Adjacent days (or hours, or
    /// weeks) take the next color, and the sequence does not restart at
    /// midnight or Monday. The default.
    /// </summary>
    Continuous,

    /// <summary>
    /// The cycle restarts at the start of each day when the unit is an hour,
    /// and at the start of each week when the unit is a day, so the same
    /// hour-of-day or weekday always gets the same color. A week unit has no
    /// larger cycle to restart on and is painted as <see cref="Continuous"/>.
    /// </summary>
    Repeat
}
