namespace Bit.BlazorUI;

public partial class BitFcFilterEvents
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

    private void HandleAttendeeChange(ChangeEventArgs e) =>
        State.SetAttendeeFilter(e.Value?.ToString());
}
