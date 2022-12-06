﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace Bit.BlazorUI;

public partial class BitBreadcrumb
{
    protected override string RootElementClass => "bit-brc";

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

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
    /// Collection of breadcrumbs to render
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    [Parameter] public IList<BitBreadcrumbItem> Items { get; set; } = new List<BitBreadcrumbItem>();
#pragma warning restore CA2227 // Collection properties should be read only

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
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// Render a custom overflow icon in place of the default icon
    /// </summary>
    [Parameter] public BitIconName OnRenderOverflowIcon { get; set; } = BitIconName.More;

    /// <summary>
    /// Callback for when the breadcrumb item clicked.
    /// </summary>
    [Parameter] public EventCallback<BitBreadcrumbItem> OnItemClick { get; set; }

    public string BreadcrumbItemsWrapperId { get; set; } = string.Empty;
    public string OverflowDropDownId { get; set; } = string.Empty;
    public string OverflowDropDownMenuCalloutId { get; set; } = string.Empty;
    public string OverflowDropDownMenuOverlayId { get; set; } = string.Empty;

    private IList<BitBreadcrumbItem> _overflowItems = new List<BitBreadcrumbItem>();
    private IList<BitBreadcrumbItem> _itemsToShowInBreadcrumb = new List<BitBreadcrumbItem>();
    private bool isOpen;

    protected override async Task OnParametersSetAsync()
    {
        BreadcrumbItemsWrapperId = $"breadcrumb-items-wrapper-{UniqueId}";
        OverflowDropDownId = $"overflow-dropdown-{UniqueId}";
        OverflowDropDownMenuOverlayId = $"overflow-dropdown-overlay-{UniqueId}";
        OverflowDropDownMenuCalloutId = $"overflow-dropdown-callout{UniqueId}";

        GetBreadcrumbItemsToShow();

        await base.OnParametersSetAsync();
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitOverflowDropDownMenu.toggleOverflowDropDownMenuCallout", obj, BreadcrumbItemsWrapperId, OverflowDropDownId, OverflowDropDownMenuCalloutId, OverflowDropDownMenuOverlayId, isOpen);
        isOpen = false;
        StateHasChanged();
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false || JSRuntime is null) return;

        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitOverflowDropDownMenu.toggleOverflowDropDownMenuCallout", obj, BreadcrumbItemsWrapperId, OverflowDropDownId, OverflowDropDownMenuCalloutId, OverflowDropDownMenuOverlayId, isOpen);
        isOpen = !isOpen;
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
            OverflowIndex = 0;

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

    private string GetItemClass(BitBreadcrumbItem item)
    {
        StringBuilder itemClasses = new();

        itemClasses.Append("bit-brc-itm");

        if (IsCurrentItem(item))
        {
            itemClasses.Append(" bit-brc-crt-itm");
        }

        if (IsCurrentItem(item) && CurrentItemClass.HasValue())
        {
            itemClasses.Append($" {CurrentItemClass}");
        }

        return itemClasses.ToString();
    }

    private string GetItemStyle(BitBreadcrumbItem item)
    {
        if (IsCurrentItem(item) && CurrentItemStyle.HasValue())
        {
            return CurrentItemStyle!;
        }

        return string.Empty;
    }

    private bool IsCurrentItem(BitBreadcrumbItem item)
    {
        var currentItem = CurrentItem ?? Items[^1];

        return item == currentItem;
    }

    private bool IsLastItem(int index)
    {
        return index == _itemsToShowInBreadcrumb.Count - 1;
    }
}
