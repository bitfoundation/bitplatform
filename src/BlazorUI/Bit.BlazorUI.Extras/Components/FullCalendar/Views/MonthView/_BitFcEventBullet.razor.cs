using System.ComponentModel;

namespace Bit.BlazorUI;

/// <summary>
/// A part of <see cref="BitFullCalendar"/>, rendered by the calendar alone: it is not meant to be used on its own.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class _BitFcEventBullet
{
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;

    [Parameter] public string? Color { get; set; }
}
