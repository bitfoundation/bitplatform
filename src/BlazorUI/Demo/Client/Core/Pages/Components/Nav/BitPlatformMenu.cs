﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Nav;

public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
    public string? Comment { get; set; }
}
