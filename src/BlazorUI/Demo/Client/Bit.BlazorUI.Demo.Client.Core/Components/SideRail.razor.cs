namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class SideRail
{
    private bool _isPanelOpen;
    private bool _shouldScrollPanelToActiveItem;
    private bool _shouldScanSections = true;
    private string? _activeItemId;
    private List<SideRailItem> _items { get; set; } = [];
    private SideRailItem[] _sideRailItems { get; set; } = [];
    private DotNetObjectReference<SideRail>? _dotnetObj;
    private readonly string _scrollSpyId = $"SideRailSpy-{Guid.NewGuid()}";
    private readonly string _resizeListenerId = $"SideRail-{Guid.NewGuid()}";



    protected override Task OnParamsSetAsync()
    {
        // A render the hosting page drives can bring a different set of sections with it, so it is
        // the one kind of render worth re-reading the DOM after. The renders the rail schedules for
        // itself never are - and those are the frequent ones - so scanning is gated on this flag
        // instead of running after every render.
        _shouldScanSections = true;

        return base.OnParamsSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Crossing the xl breakpoint while the responsive panel is open would leave it covering
            // a layout it no longer belongs to, so any window resize closes it (see OnWindowResize).
            _dotnetObj = DotNetObjectReference.Create(this);
            await JSRuntime.RegisterWindowResizeListener(_resizeListenerId, _dotnetObj, nameof(OnWindowResize));
        }

        if (_shouldScanSections)
        {
            _shouldScanSections = false;
            await ScanSections();
        }

        // The panel's list is a second copy of the rail, rendered only now, so the spy has never had
        // the chance to bring its active entry into view the way it does for the sticky rail.
        if (_shouldScrollPanelToActiveItem)
        {
            _shouldScrollPanelToActiveItem = false;
            await JSRuntime.ScrollSideRailToActiveItem();
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    /// <summary>
    /// Reads the section headings out of the DOM and rebuilds the rail from them. Only called when
    /// the sections can actually have changed: a render driven by the page, or the spy reporting
    /// that the elements it was watching have left the document (see <see cref="OnSectionsChanged"/>).
    /// </summary>
    private async Task ScanSections()
    {
        var sideRailItems = await JSRuntime.GetSideRailItems();

        // A null read is the JS runtime saying it could not be asked (prerendering, a disconnected
        // circuit) rather than the page saying it has no sections, so the rail keeps what it has -
        // spreading it into _items below would throw.
        if (sideRailItems is null) return;

        if (ItemsChanged(sideRailItems, _sideRailItems) is false) return;

        // Persist the snapshot the change-check compares against; otherwise ItemsChanged stays
        // true forever and the StateHasChanged below schedules an endless render loop (which in
        // WASM runs entirely in microtasks and freezes the browser tab).
        _sideRailItems = sideRailItems;

        _items = [.. sideRailItems];

        StateHasChanged();

        // (Re)arm the spy with the new id list; it moves the highlight in the DOM itself and reports
        // back where the reader is, starting with an immediate report for the current scroll position.
        await JSRuntime.RegisterSideRailScrollSpy(_scrollSpyId, _dotnetObj!, nameof(OnActiveItemChanged),
                                                  nameof(OnSectionsChanged), [.. _items.Select(i => i.Id)]);
    }

    private void OpenPanel()
    {
        _isPanelOpen = true;
        _shouldScrollPanelToActiveItem = true;
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
        if (newItems is null || oldItems is null) return true;

        if (newItems.Length != oldItems.Length) return true;

        for (int i = 0; i < newItems.Length; i++)
        {
            if (newItems[i].Id != oldItems[i].Id) return true;
        }

        return false;
    }



    /// <summary>
    /// The spy has already emphasized the entry in the DOM - moving a class is not worth a render of
    /// the whole list on every section the reader scrolls past - so this only keeps the C# copy of
    /// the state in step, for the next list the rail does render (the panel opening, the sections
    /// changing) to come up already pointing at the right entry.
    /// </summary>
    [JSInvokable]
    public void OnActiveItemChanged(string? activeItemId)
    {
        _activeItemId = activeItemId;
    }

    /// <summary>
    /// The spy reports that the sections it was watching have left the document - a pivot tab swap,
    /// which nothing else announces to the rail.
    /// </summary>
    [JSInvokable]
    public Task OnSectionsChanged()
    {
        return InvokeAsync(ScanSections);
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
