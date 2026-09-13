namespace Bit.BlazorUI;

/// <summary>
/// The glyph marking an occurrence of a recurring event on the built-in event cards, badges, and agenda rows.
/// </summary>
public partial class BitFcRecurrenceIcon
{
    /// <summary>
    /// The accessible name of the glyph. When empty the glyph is decorative and hidden from assistive
    /// technology - for a host that already announces the event as recurring in its own label.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }
}
