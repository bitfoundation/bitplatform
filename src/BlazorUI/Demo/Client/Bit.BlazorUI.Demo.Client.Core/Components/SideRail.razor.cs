namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    private string? activeItemId;
    [Parameter] public List<SideRailItem> Items { get; set; } = new List<SideRailItem>();

    protected override async Task OnInitAsync()
    {
        activeItemId = Items.FirstOrDefault()?.Id;

        await base.OnInitAsync();
    }

    private async Task ScrollToItem(SideRailItem targetItem)
    {
        activeItemId = targetItem.Id;
        
        if (targetItem.Id is null) return;
        
        await JSRuntime.ScrollToElement(targetItem.Id);
    }
}
