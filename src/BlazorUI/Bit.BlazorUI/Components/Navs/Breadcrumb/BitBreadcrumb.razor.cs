namespace Bit.BlazorUI;

/// <summary>
/// Breadcrumbs should be used as a navigational aid in your app or site. They indicate the current page’s location within a hierarchy and help the user understand where they are in relation to the rest of that hierarchy.
/// </summary>
public partial class BitBreadcrumb<TItem> : BitComponentBase, IAsyncDisposable where TItem : class
{
    private bool _disposed;
    private bool _isCalloutOpen;
    private uint _internalOverflowIndex;
    private uint _internalMaxDisplayedItems;
    private List<TItem> _items = [];
    private List<TItem> _internalItems = [];
    private List<TItem> _displayItems = [];
    private List<TItem> _overflowItems = [];
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
    /// Custom CSS classes for different parts of the breadcrumb.
    /// </summary>
    [Parameter] public BitBreadcrumbClassStyles? Classes { get; set; }

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public string? DividerIconName { get; set; }

    /// <summary>
    /// The custom template content to render divider icon.
    /// </summary>
    [Parameter] public RenderFragment? DividerIconTemplate { get; set; }

    /// <summary>
    /// Collection of BreadLists to render.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// The custom template content to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

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
    /// Callback for when the BreadList item clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

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
    [Parameter] public string? OverflowIconName { get; set; }

    /// <summary>
    /// The custom template content to render each overflow icon.
    /// </summary>
    [Parameter] public RenderFragment? OverflowIconTemplate { get; set; }

    /// <summary>
    /// The custom template content to render each item in overflow list.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? OverflowTemplate { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the item text of the item content.
    /// </summary>
    [Parameter] public bool ReversedIcon { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the breadcrumb.
    /// </summary>
    [Parameter] public BitBreadcrumbClassStyles? Styles { get; set; }



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

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        _calloutId = $"BitBreadcrumb-{UniqueId}-callout";
        _overflowAnchorId = $"BitBreadcrumb-{UniqueId}-overflow-anchor";
        _scrollContainerId = $"BitBreadcrumb-{UniqueId}-scroll-container";

        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        if (ChildContent is null && Options is null)
        {
            _items = Items is not null ? [.. Items] : [];
        }

        if (_items.Any() is false) return;

        bool shouldCallSetItemsToShow = _internalItems.Count != _items.Count || _internalItems.Any(item => _items.Contains(item) is false);
        _internalItems = [.. _items];

        shouldCallSetItemsToShow = shouldCallSetItemsToShow || _internalMaxDisplayedItems != MaxDisplayedItems;
        _internalMaxDisplayedItems = MaxDisplayedItems == 0 ? (uint)_internalItems.Count : MaxDisplayedItems;

        shouldCallSetItemsToShow = shouldCallSetItemsToShow || _internalOverflowIndex != OverflowIndex;
        _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedItems ? 0 : OverflowIndex;

        if (shouldCallSetItemsToShow is false) return;

        SetItemsToShow();

        base.OnParametersSet();
    }



    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false) return;
        if (GetIsEnabled(item) is false) return;
        if (OnItemClick.HasDelegate is false) return;

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
            _displayItems = [.. _internalItems];
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

    private string GetClasses(TItem item, bool isOverflowItem)
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

        if (isOverflowItem)
        {
            if (Classes?.OverflowItem.HasValue() ?? false)
            {
                classes.Add(Classes.OverflowItem!);
            }

            if (GetIsSelected(item) && (Classes?.OverflowSelectedItem.HasValue() ?? false))
            {
                classes.Add(Classes.OverflowSelectedItem!);
            }
        }
        else
        {
            if (Classes?.Item.HasValue() ?? false)
            {
                classes.Add(Classes.Item!);
            }

            if (GetIsSelected(item) && (Classes?.SelectedItem.HasValue() ?? false))
            {
                classes.Add(Classes.SelectedItem!);
            }
        }

        if (GetIsEnabled(item) is false)
        {
            classes.Add("bit-brc-dis");
        }

        if (GetReversedIcon(item))
        {
            classes.Add("bit-brc-rvi");
        }

        return string.Join(" ", classes);
    }

    private string GetStyles(TItem item, bool isOverflowItem)
    {
        var styles = new List<string>();

        if (GetItemStyle(item).HasValue())
        {
            styles.Add(GetItemStyle(item)!.Trim(';'));
        }

        if (isOverflowItem)
        {
            if (Styles?.OverflowItem.HasValue() ?? false)
            {
                styles.Add(Styles.OverflowItem!.Trim(';'));
            }

            if (GetIsSelected(item) && (Styles?.OverflowSelectedItem.HasValue() ?? false))
            {
                styles.Add(Styles.OverflowSelectedItem!.Trim(';'));
            }
        }
        else
        {
            if (Styles?.Item.HasValue() ?? false)
            {
                styles.Add(Styles.Item!.Trim(';'));
            }

            if (GetIsSelected(item) && (Styles?.SelectedItem.HasValue() ?? false))
            {
                styles.Add(Styles.SelectedItem!.Trim(';'));
            }
        }

        return string.Join(';', styles);
    }

    private string? GetItemHref(TItem item)
    {
        if (GetIsEnabled(item) is false) return null;

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

    private string? GetIconName(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.IconName;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    private bool GetReversedIcon(TItem item)
    {
        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.ReversedIcon ?? ReversedIcon;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.ReversedIcon ?? ReversedIcon;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.ReversedIcon.Selector is not null)
        {
            return NameSelectors.ReversedIcon.Selector!(item) ?? ReversedIcon;
        }

        return item.GetValueFromProperty(NameSelectors.ReversedIcon.Name, ReversedIcon);
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
        if (IsEnabled is false) return false;

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

    private RenderFragment<TItem>? GetOverflowTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.OverflowTemplate as RenderFragment<TItem>;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.OverflowTemplate as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OverflowTemplate.Selector is not null)
        {
            return NameSelectors.OverflowTemplate.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.OverflowTemplate.Name);
    }

    private RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitBreadcrumbItem breadcrumbItem)
        {
            return breadcrumbItem.Template as RenderFragment<TItem>;
        }

        if (item is BitBreadcrumbOption bitBreadcrumbOption)
        {
            return bitBreadcrumbOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
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

        await _js.BitCalloutToggleCallout(_dotnetObj,
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
                                false);
    }

    private string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitCalloutClearCallout(_calloutId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _disposed = true;
    }
}
