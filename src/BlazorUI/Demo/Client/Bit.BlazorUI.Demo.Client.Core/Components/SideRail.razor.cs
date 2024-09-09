namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    private List<SideRailItem> _items { get; set; } = [];

    private async Task ScrollToItem(SideRailItem targetItem)
    {
        if (targetItem.Id is null) return;

        await JSRuntime.ScrollToElement(targetItem.Id);
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        var sideRailItems = await JSRuntime.GetSideRailItems();

        _items = [.. sideRailItems, new() { Id = "api-section", Title = "API" }];

        StateHasChanged();

        await base.OnAfterFirstRenderAsync();
    }
}
