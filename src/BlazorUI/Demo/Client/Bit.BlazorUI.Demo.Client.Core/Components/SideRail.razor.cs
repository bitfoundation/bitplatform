namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    private List<SideRailItem> _items { get; set; } = [];
    private SideRailItem[] _sideRailItems { get; set; } = [];
    


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var sideRailItems = await JSRuntime.GetSideRailItems();

        if (ItemsChanged(sideRailItems, _sideRailItems))
        {
            _items = [.. sideRailItems, new() { Id = "api-section", Title = "API" }, new() { Id = "feedback-section", Title = "Feedback" }];

            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private async Task ScrollToItem(SideRailItem targetItem)
    {
        if (targetItem.Id is null) return;

        await JSRuntime.ScrollToElement(targetItem.Id);
    }

    private static bool ItemsChanged(SideRailItem[] newItems, SideRailItem[] oldItems)
    {
        if(newItems is null || oldItems is null) return false;

        if (newItems.Length != oldItems.Length) return true;

        for (int i = 0; i < newItems.Length; i++)
        {
            if (newItems[i].Id != oldItems[i].Id) return true;
        }

        return false;
    }
}
