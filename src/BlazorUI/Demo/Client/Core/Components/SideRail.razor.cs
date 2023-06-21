using Bit.BlazorUI.Demo.Client.Core.Models;

namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    private string? activeItemId;
    [Parameter] public List<SideRailItem> Items { get; set; } = new List<SideRailItem>();

    protected override void OnInitialized()
    {
        activeItemId = Items.FirstOrDefault()?.Id;

        base.OnInitialized();
    }

    private async Task ScrollToItem(SideRailItem targetItem)
    {
        activeItemId = targetItem.Id;
        
        if (targetItem.Id is null) return;
        
        await JSRuntime.ScrollToElement(targetItem.Id);
    }
}
