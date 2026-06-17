namespace Bit.BlazorUI;

public partial class BitFcSettings
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    private bool _open;

    private void OnStartHourChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int val))
            State.SetStartOfDayHour(val);
    }
}
