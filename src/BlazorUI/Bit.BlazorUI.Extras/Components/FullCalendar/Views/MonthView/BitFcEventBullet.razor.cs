namespace Bit.BlazorUI;

public partial class BitFcEventBullet
{
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;

    [Parameter] public string? Color { get; set; }
}
