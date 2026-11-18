using System.ComponentModel;

namespace Bit.BlazorUI;

/// <summary>
/// A part of <see cref="BitFullCalendar"/>, rendered by the calendar alone: it is not meant to be used on its own.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class _BitFcFilterEvents
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;

    private string SelectedColorValue => State.SelectedColors.Count == 1
        ? State.SelectedColors[0]
        : string.Empty;

    private void HandleColorChange(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        State.SetColorFilter(string.IsNullOrWhiteSpace(value) ? null : value);
    }

    private void HandleAttendeeChange(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        State.SetAttendeeFilter(string.IsNullOrWhiteSpace(value) ? null : value);
    }
}
