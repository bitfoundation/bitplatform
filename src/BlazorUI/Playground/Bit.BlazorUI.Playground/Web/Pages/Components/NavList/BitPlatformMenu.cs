using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
}
