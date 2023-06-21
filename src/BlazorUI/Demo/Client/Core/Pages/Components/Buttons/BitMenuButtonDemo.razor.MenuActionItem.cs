﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public class MenuActionItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public BitIconName Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
}
