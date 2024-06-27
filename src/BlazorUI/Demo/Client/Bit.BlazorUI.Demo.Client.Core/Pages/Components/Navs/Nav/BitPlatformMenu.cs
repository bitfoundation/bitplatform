namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public List<BitPlatformMenu> Links { get; set; } = [];
    public string? Comment { get; set; }
    public RenderFragment<BitPlatformMenu>? Template { get; set; }
}
