﻿namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    [Parameter] public List<SideRailItem> Items { get; set; } = [];

    private async Task ScrollToItem(SideRailItem targetItem)
    {
        if (targetItem.Id is null) return;

        await JSRuntime.ScrollToElement(targetItem.Id);
    }
}
