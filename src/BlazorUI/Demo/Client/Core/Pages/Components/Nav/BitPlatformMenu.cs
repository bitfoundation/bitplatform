namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Nav;

public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
}
