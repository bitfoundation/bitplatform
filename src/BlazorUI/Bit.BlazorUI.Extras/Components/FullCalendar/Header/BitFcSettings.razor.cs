namespace Bit.BlazorUI;

public partial class BitFcSettings
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    private bool _open;

    // Unique per instance so multiple calendars on one page don't produce duplicate element IDs.
    private readonly string _menuId = $"bit-bfc-settings-menu-{Guid.NewGuid():N}";

    private void OnStartHourChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int val))
        {
            // Constrain to the same range SetStartOfDayHour accepts (0-16) so the textbox and
            // state stay in sync even when the user types an out-of-range value.
            val = Math.Clamp(val, 0, 16);
            State.SetStartOfDayHour(val);
            StateHasChanged();
        }
    }
}
