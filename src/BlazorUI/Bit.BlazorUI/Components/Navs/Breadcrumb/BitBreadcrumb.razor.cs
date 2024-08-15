namespace Bit.BlazorUI;

public partial class BitBreadcrumb<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    private bool _isCalloutOpen;
    private uint _internalOverflowIndex;
    private uint _internalMaxDisplayedItems;
    private List<TItem> _items = [];
    private List<TItem> _internalItems = [];
    private List<TItem> _displayItems = [];
    private List<TItem> _overflowItems = [];
    private string _internalDividerIconName = default!;
    private DotNetObjectReference<BitBreadcrumb<TItem>> _dotnetObj = default!;

    private string _calloutId = default!;
    private string _overflowAnchorId = default!;
    private string _scrollContainerId = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the BitBreadcrumb, that are BitBreadOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public string? DividerIconName { get; set; }

    /// <summary>
    /// Collection of BreadLists to render.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = [];
    
    /// <summary>
    /// The maximum number of BreadLists to display before coalescing.
    /// If not specified, all BreadLists will be rendered.
    /// </summary>
    [Parameter] public uint MaxDisplayedItems { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitBreadcrumbNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Aria label for the overflow button.
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Optional index where overflow items will be collapsed.
    /// </summary>
    [Parameter] public uint OverflowIndex { get; set; }

    /// <summary>
    /// Render a custom overflow icon in place of the default icon.
    /// </summary>
    [Parameter] public string OverflowIconName { get; set; } = "More";

    /// <summary>
    /// Callback for when the BreadList item clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// The class HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemClass { get; set; }

    /// <summary>
    /// The style HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemStyle { get; set; }



    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        _isCalloutOpen = false;
        StateHasChanged();
    }



    internal void RegisterOptions(BitBreadcrumbOption option)
    {
        _items.Add((option as TItem)!);
        _internalItems = [.. _items];
        _internalMaxDisplayedItems = MaxDisplayedItems == 0 ? (uint)_items.Count : MaxDisplayedItems;
        _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedItems ? 0 : OverflowIndex;
        SetItemsToShow();
        StateHasChanged();
    }

    internal void UnregisterOptions(BitBreadcrumbOption option)
    {
        _items.Remove((option as TItem)!);
        _internalItems = [.. _items];
        SetItemsToShow();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-brc";

    protected override Task OnInitializedAsync()
    {
        _calloutId = $"BitBreadcrumb-{UniqueId}-callout";
        _overflowAnchorId = $"BitBreadcrumb-{UniqueId}-overflow-anchor";
        _scrollContainerId = $"BitBreadcrumb-{UniqueId}-scroll-container";

        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _internalDividerIconName = Dir == BitDir.Rtl ? "ChevronLeft" : "ChevronRight";

        if (ChildContent is null)
        {
            _items = [.. Items];
        }

        if (_items.Any())
        {
            bool shouldCallSetItemsToShow = false;

            shouldCallSetItemsToShow = _internalItems.Count != _items.Count || _internalItems.Any(item => _items.Contains(item) is false);
            _internalItems = [.. _items];

            shouldCallSetItemsToShow = shouldCallSetItemsToShow || _internalMaxDisplayedItems != MaxDisplayedItems;
            _internalMaxDisplayedItems = MaxDisplayedItems == 0 ? (uint)_internalItems.Count : MaxDisplayedItems;

            shouldCallSetItemsToShow = shouldCallSetItemsToShow || _internalOverflowIndex != OverflowIndex;
            _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedItems ? 0 : OverflowIndex;

            if (shouldCallSetItemsToShow)
            {
                SetItemsToShow();
            }
        }

        await base.OnParametersSetAsync();
    }



    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false) return;
        if (GetIsEnabled(item) is false) return;

        await OnItemClick.InvokeAsync(item);

        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            breadcrumbItem.OnClick?.Invoke(breadcrumbItem);
        }
        else if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            await bitBreadcrumbOption.OnClick.InvokeAsync(bitBreadcrumbOption);
        }
        else if (NameSelectors?.OnClick is not null)
        {
            NameSelectors.OnClick(item);
        }
    }

    private void SetItemsToShow()
    {
        _displayItems.Clear();
        _overflowItems.Clear();

        if (_internalMaxDisplayedItems >= _internalItems.Count)
        {
            _displayItems = _internalItems.ToList();
            return;
        }

        var overflowItemsCount = _internalItems.Count - _internalMaxDisplayedItems;

        foreach ((TItem item, int index) in _internalItems.Select((item, index) => (item, index)))
        {
            if (_internalOverflowIndex <= index && index < (overflowItemsCount + _internalOverflowIndex))
            {
                if (index == _internalOverflowIndex)
                {
                    _displayItems.Add(item);
                }

                _overflowItems.Add(item);
            }
            else
            {
                _displayItems.Add(item);
            }
        }
    }

    private string GetClasses(TItem item)
    {
        var classes = new List<string>();

        if (GetItemClass(item).HasValue())
        {
            classes.Add(GetItemClass(item)!);
        }

        if (GetIsSelected(item))
        {
            classes.Add("bit-brc-sel");
        }

        if (GetIsSelected(item) && SelectedItemClass.HasValue())
        {
            classes.Add(SelectedItemClass!);
        }

        if (GetIsEnabled(item) is false)
        {
            classes.Add("bit-brc-disi");
        }

        return string.Join(" ", classes);
    }

    private string? GetKey(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Key;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    private string GetStyles(TItem item)
    {
        var styles = new List<string>();

        if (GetItemStyle(item).HasValue())
        {
            styles.Add(GetItemStyle(item)!);
        }

        if (GetIsSelected(item) && SelectedItemStyle.HasValue())
        {
            styles.Add(SelectedItemStyle!);
        }

        return string.Join(" ", styles);
    }

    private string? GetItemHref(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Href;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Href;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Href.Selector is not null)
        {
            return NameSelectors.Href.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Href.Name);
    }

    private string? GetItemClass(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Class;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    private string? GetItemStyle(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Style;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    private string? GetItemText(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Text;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    private bool GetIsSelected(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.IsSelected;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.IsSelected;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsSelected.Selector is not null)
        {
            return NameSelectors.IsSelected.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsSelected.Name, false);
    }

    private bool GetIsEnabled(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.IsEnabled;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private async Task OpenCallout()
    {
        _isCalloutOpen = true;
        await ToggleCallout();
    }

    private async Task CloseCallout()
    {
        _isCalloutOpen = false;
        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleCallout(_dotnetObj,
                                _overflowAnchorId,
                                null,
                                _calloutId,
                                null,
                                _isCalloutOpen,
                                BitResponsiveMode.None,
                                BitDropDirection.TopAndBottom,
                                Dir is BitDir.Rtl,
                                _scrollContainerId,
                                0,
                                "",
                                "",
                                false,
                                RootElementClass);
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _dotnetObj.Dispose();

        _disposed = true;
    }
}
