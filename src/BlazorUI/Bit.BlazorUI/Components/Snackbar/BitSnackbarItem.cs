
namespace Bit.BlazorUI;

internal class BitSnackbarItem
{
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public BitSnackbarType? Type { get; set; }
    public System.Timers.Timer? Timerr { get; set; }
    public int Counter { get; set; }
}
