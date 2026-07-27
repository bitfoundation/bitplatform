namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    private bool _hadNotes;
    private bool _isPanelOpen;
    private string? _activeItemId;
    private List<SideRailItem> _items { get; set; } = [];
    private SideRailItem[] _sideRailItems { get; set; } = [];
    private DotNetObjectReference<SideRail>? _dotnetObj;
    private readonly string _scrollSpyId = $"SideRailSpy-{Guid.NewGuid()}";
    private readonly string _resizeListenerId = $"SideRail-{Guid.NewGuid()}";


    /// <summary>
    /// Whether the hosting page renders a Notes section (id "notes-section"). The rail cannot see
    /// it in the DOM the way it sees the example headings, so the page has to say so.
    /// </summary>
    [Parameter] public bool HasNotes { get; set; }


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

        if (ItemsChanged(sideRailItems, _sideRailItems) || _hadNotes != HasNotes)
        {
            // Persist the snapshot the change-check compares against; otherwise ItemsChanged stays
            // true forever and the StateHasChanged below schedules an endless render loop (which in
            // WASM runs entirely in microtasks and freezes the browser tab).
            _sideRailItems = sideRailItems;
            _hadNotes = HasNotes;

            _items = [.. sideRailItems, new() { Id = "api-section", Title = "API" }, new() { Id = "feedback-section", Title = "Feedback" }];
            if (HasNotes)
            {
                _items.Insert(0, new() { Id = "notes-section", Title = "Notes" });
            }

            StateHasChanged();

            // (Re)arm the spy with the new id list; it reports back the entry to emphasize as the
            // reader scrolls, starting with an immediate report for the current scroll position.
            await JSRuntime.RegisterSideRailScrollSpy(_scrollSpyId, _dotnetObj!, nameof(OnActiveItemChanged), [.. _items.Select(i => i.Id)]);
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
    public async Task OnActiveItemChanged(string? activeItemId)
    {
        if (_activeItemId == activeItemId) return;

        _activeItemId = activeItemId;

        await InvokeAsync(StateHasChanged);
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
                await JSRuntime.UnregisterSideRailScrollSpy(_scrollSpyId);
                await JSRuntime.UnregisterWindowResizeListener(_resizeListenerId);
            }
            catch (JSDisconnectedException) { } // the circuit is already gone, nothing left to unregister

            _dotnetObj?.Dispose();
            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }
}
