namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    private bool _isPanelOpen;
    private List<SideRailItem> _items { get; set; } = [];
    private SideRailItem[] _sideRailItems { get; set; } = [];
    private DotNetObjectReference<SideRail>? _dotnetObj;
    private readonly string _resizeListenerId = $"SideRail-{Guid.NewGuid()}";



    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Crossing the xl breakpoint while the responsive panel is open would leave it covering
            // a layout it no longer belongs to, so any window resize closes it (see OnWindowResize).
            _dotnetObj = DotNetObjectReference.Create(this);
            await JSRuntime.RegisterWindowResizeListener(_resizeListenerId, _dotnetObj, nameof(OnWindowResize));
        }

        var sideRailItems = await JSRuntime.GetSideRailItems();

        if (ItemsChanged(sideRailItems, _sideRailItems))
        {
            // Persist the snapshot the change-check compares against; otherwise ItemsChanged stays
            // true forever and the StateHasChanged below schedules an endless render loop (which in
            // WASM runs entirely in microtasks and freezes the browser tab).
            _sideRailItems = sideRailItems;
            _items = [.. sideRailItems, new() { Id = "api-section", Title = "API" }, new() { Id = "feedback-section", Title = "Feedback" }];

            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private async Task ScrollToItem(SideRailItem targetItem)
    {
        if (targetItem.Id is null) return;

        // On small screens the link lives inside the panel; closing it here is a no-op when the
        // click came from the sticky rail.
        _isPanelOpen = false;

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



    [JSInvokable]
    public async Task OnWindowResize()
    {
        if (_isPanelOpen is false) return;

        _isPanelOpen = false;

        await InvokeAsync(StateHasChanged);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            try
            {
                await JSRuntime.UnregisterWindowResizeListener(_resizeListenerId);
            }
            catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unregister

            _dotnetObj?.Dispose();
            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }
}
