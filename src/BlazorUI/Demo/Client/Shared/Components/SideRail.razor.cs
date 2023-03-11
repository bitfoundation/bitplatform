using Bit.BlazorUI.Demo.Client.Shared.Models;

namespace Bit.BlazorUI.Demo.Client.Shared.Components;

public partial class SideRail
{
    private string activeItem;

    [Inject] public IJSRuntime JSRuntime { get; set; }
    [Parameter] public List<SideRailItem> Items { get; set; } = new List<SideRailItem>();

    protected override void OnInitialized()
    {
        activeItem = Items.FirstOrDefault().Id;

        base.OnInitialized();
    }

    private async Task ScrollToItem(SideRailItem targetItem)
    {
        activeItem = targetItem.Id;

        await JSRuntime.ScrollToElement(targetItem.Id);
    }
}
