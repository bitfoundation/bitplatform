using System.Linq.Expressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadList<TItem>
{
    private bool _isCalloutOpen;
    private string _breadListId => $"{UniqueId}-items-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    private string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    private IList<TItem> _itemsToShowInBreadcrumb = new List<TItem>();
    private IList<TItem> _overflowItems = new List<TItem>();

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
    [Parameter] public TItem? CurrentItem { get; set; }

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// URL to navigate to when this breadcrumb item is clicked.
    /// If provided, the breadcrumb will be rendered as a link.
    /// </summary>
    [Parameter] public string HrefField { get; set; } = "Href";

    /// <summary>
    /// URL to navigate to when this breadcrumb item is clicked.
    /// If provided, the breadcrumb will be rendered as a link.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? HrefSelector { get; set; }

    /// <summary>
    /// Collection of breadcrumbs to render.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// class HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter] public string ItemClassField { get; set; } = "ItemClass";

    /// <summary>
    /// Class HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? ItemClassSelector { get; set; }

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter] public string ItemStyleField { get; set; } = "ItemStyle";

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? ItemStyleSelector { get; set; }

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
    [Parameter] public BitIconName OverflowIcon { get; set; } = BitIconName.More;

    /// <summary>
    /// Callback for when the breadcrumb item clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Text to display in the breadcrumb item.
    /// </summary>
    [Parameter] public string TextField { get; set; } = "Text";

    /// <summary>
    /// Text to display in the breadcrumb item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? TextSelector { get; set; }

    protected override string RootElementClass => "bit-brl";

    protected override async Task OnInitializedAsync()
    {
        if (HrefSelector is not null)
        {
            HrefField = HrefSelector.GetName();
        }

        if (ItemClassSelector is not null)
        {
            ItemClassField = ItemClassSelector.GetName();
        }

        if (ItemStyleSelector is not null)
        {
            ItemStyleField = ItemStyleSelector.GetName();
        }

        if (TextSelector is not null)
        {
            TextField = TextSelector.GetName();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        GetBreadcrumbItemsToShow();

        await base.OnParametersSetAsync();
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitOverflowDropDownMenu.toggleOverflowDropDownMenuCallout", obj, _breadListId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);
        _isCalloutOpen = false;
        StateHasChanged();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false || _js is null) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitOverflowDropDownMenu.toggleOverflowDropDownMenuCallout", obj, _breadListId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);
        _isCalloutOpen = !_isCalloutOpen;
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false) return;

        await OnItemClick.InvokeAsync(item);
    }

    private IList<TItem> GetBreadcrumbItemsToShow()
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

        foreach ((TItem item, int index) in Items.Select((item, index) => (item, index)))
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

    private string GetItemClasses(TItem item)
    {
        StringBuilder itemClasses = new();

        itemClasses.Append("item");

        if (GetItemClass(item).HasValue())
        {
            itemClasses.Append($" {GetItemClass(item)}");
        }

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

    private string GetItemStyles(TItem item)
    {
        StringBuilder itemStyles = new();

        if (GetItemStyle(item).HasValue())
        {
            itemStyles.Append(GetItemStyle(item));
        }

        if (IsCurrentItem(item) && CurrentItemStyle.HasValue())
        {
            itemStyles.Append(CurrentItemStyle);
        }

        return itemStyles.ToString();
    }

    private bool IsCurrentItem(TItem item)
    {
        var currentItem = CurrentItem ?? Items[^1];

        return GetItemHref(item) == GetItemHref(currentItem) &&
               GetItemClass(item) == GetItemClass(currentItem) &&
               GetItemStyle(item) == GetItemStyle(currentItem) &&
               GetItemText(item) == GetItemText(currentItem);
    }

    private bool IsLastItem(int index)
    {
        return index == _itemsToShowInBreadcrumb.Count - 1;
    }

    private string? GetItemHref(TItem item) => item.GetValueAsObjectFromProperty(HrefField)?.ToString();
    private string? GetItemClass(TItem item) => item.GetValueAsObjectFromProperty(ItemClassField)?.ToString();
    private string? GetItemStyle(TItem item) => item.GetValueAsObjectFromProperty(ItemStyleField)?.ToString();
    private string? GetItemText(TItem item) => item.GetValueAsObjectFromProperty(TextField)?.ToString();
}
