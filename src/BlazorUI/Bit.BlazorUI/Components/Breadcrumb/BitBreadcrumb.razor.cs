﻿using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadcrumb : IDisposable
{
    private bool _disposed;
    private bool _isCalloutOpen;
    private string _wrapperId => $"{UniqueId}-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    private string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    private IList<BitBreadcrumbItem> _itemsToShowInBreadcrumb = new List<BitBreadcrumbItem>();
    private IList<BitBreadcrumbItem> _overflowItems = new List<BitBreadcrumbItem>();

    private DotNetObjectReference<BitBreadcrumb> _dotnetObj = default!;

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// The class HTML attribute for Current Item.
    /// </summary>
    [Parameter] public string? CurrentItemClass { get; set; }

    /// <summary>
    /// The style HTML attribute for Current Item.
    /// </summary>
    [Parameter] public string? CurrentItemStyle { get; set; }

    /// <summary>
    /// by default, the current item is the last item. But it can also be specified manually.
    /// </summary>
    [Parameter] public BitBreadcrumbItem? CurrentItem { get; set; }

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// Collection of breadcrumbs to render.
    /// </summary>
    [Parameter] public IList<BitBreadcrumbItem> Items { get; set; } = new List<BitBreadcrumbItem>();

    /// <summary>
    /// The maximum number of breadcrumbs to display before coalescing.
    /// If not specified, all breadcrumbs will be rendered.
    /// </summary>
    [Parameter] public int MaxDisplayedItems { get; set; }

    /// <summary>
    /// Aria label for the overflow button.
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Optional index where overflow items will be collapsed.
    /// </summary>
    [Parameter] public int OverflowIndex { get; set; }

    /// <summary>
    /// Render a custom overflow icon in place of the default icon.
    /// </summary>
    [Parameter] public BitIconName OnRenderOverflowIcon { get; set; } = BitIconName.More;

    /// <summary>
    /// Callback for when the breadcrumb item clicked.
    /// </summary>
    [Parameter] public EventCallback<BitBreadcrumbItem> OnItemClick { get; set; }

    protected override string RootElementClass => "bit-brc";

    protected override Task OnInitializedAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        GetBreadcrumbItemsToShow();

        await base.OnParametersSetAsync();
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleOverflowCallout(_dotnetObj, _wrapperId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);

        _isCalloutOpen = false;

        StateHasChanged();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await _js.ToggleOverflowCallout(_dotnetObj, _wrapperId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);

        _isCalloutOpen = !_isCalloutOpen;
    }

    private async Task HandleOnItemClick(BitBreadcrumbItem item)
    {
        if (IsEnabled is false) return;

        await OnItemClick.InvokeAsync(item);
    }

    private IList<BitBreadcrumbItem> GetBreadcrumbItemsToShow()
    {
        if (MaxDisplayedItems == 0 || MaxDisplayedItems >= Items.Count)
        {
            return _itemsToShowInBreadcrumb = Items;
        }

        _itemsToShowInBreadcrumb.Clear();
        _overflowItems.Clear();

        if (OverflowIndex >= MaxDisplayedItems)
        {
            OverflowIndex = 0;
        }

        var overflowItemsCount = Items.Count - MaxDisplayedItems;

        foreach ((BitBreadcrumbItem item, int index) in Items.Select((item, index) => (item, index)))
        {
            if (OverflowIndex <= index && index < overflowItemsCount + OverflowIndex)
            {
                if (index == OverflowIndex)
                {
                    _itemsToShowInBreadcrumb.Add(item);
                }

                _overflowItems.Add(item);
            }
            else
            {
                _itemsToShowInBreadcrumb.Add(item);
            }
        }

        return _itemsToShowInBreadcrumb;
    }

    private string GetItemClasses(BitBreadcrumbItem item)
    {
        StringBuilder itemClasses = new();

        itemClasses.Append("item");

        if (IsCurrentItem(item))
        {
            itemClasses.Append(" current-item");
        }

        if (IsCurrentItem(item) && CurrentItemClass.HasValue())
        {
            itemClasses.Append($" {CurrentItemClass}");
        }

        return itemClasses.ToString();
    }

    private string GetItemStyles(BitBreadcrumbItem item)
    {
        return IsCurrentItem(item) ? CurrentItemStyle ?? string.Empty : string.Empty;
    }

    private bool IsCurrentItem(BitBreadcrumbItem item)
    {
        return item == (CurrentItem ?? Items[^1]);
    }

    private bool IsLastItem(int index)
    {
        return index == _itemsToShowInBreadcrumb.Count - 1;
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (!disposing) return;

        _dotnetObj.Dispose();

        _disposed = true;
    }
}
