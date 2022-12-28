using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadcrumb : IDisposable
{
    private int overflowIndex;
    private int maxDisplayedItems;
    private IList<BitBreadcrumbItem> items = new List<BitBreadcrumbItem>();

    private List<BitBreadcrumbItem> _displayItems = new List<BitBreadcrumbItem>();
    private List<BitBreadcrumbItem> _overflowItems = new List<BitBreadcrumbItem>();
    private DotNetObjectReference<BitBreadcrumb> _dotnetObj = default!;
    private bool _disposed;
    private bool _isCalloutOpen;
    private int _internaloverflowIndex;

    private string _wrapperId => $"{UniqueId}-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    private string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// Collection of breadcrumbs to render.
    /// </summary>
    [Parameter] public IList<BitBreadcrumbItem> Items 
    { 
        get => items;
        set
        {
            items = value;
            SetItemsToShow();
        }
    }

    /// <summary>
    /// The maximum number of breadcrumbs to display before coalescing.
    /// If not specified, all breadcrumbs will be rendered.
    /// </summary>
    [Parameter]
    public int MaxDisplayedItems
    {
        get => maxDisplayedItems;
        set
        {
            maxDisplayedItems = value;
            SetItemsToShow();
        }
    }

    /// <summary>
    /// Aria label for the overflow button.
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Optional index where overflow items will be collapsed.
    /// </summary>
    [Parameter]
    public int OverflowIndex
    {
        get => overflowIndex;
        set
        {
            overflowIndex = value;
            _internaloverflowIndex = value;
            SetItemsToShow();
        }
    }

    /// <summary>
    /// Render a custom overflow icon in place of the default icon.
    /// </summary>
    [Parameter] public BitIconName OverflowIcon { get; set; } = BitIconName.More;

    /// <summary>
    /// Callback for when the breadcrumb item clicked.
    /// </summary>
    [Parameter] public EventCallback<BitBreadcrumbItem> OnItemClick { get; set; }

    /// <summary>
    /// The class HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemClass { get; set; }

    /// <summary>
    /// The style HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemStyle { get; set; }

    protected override string RootElementClass => "bit-brc";

    protected override Task OnInitializedAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleOverflowCallout(_dotnetObj, _wrapperId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);

        _isCalloutOpen = !_isCalloutOpen;
    }

    private async Task HandleOnItemClick(BitBreadcrumbItem item)
    {
        if (IsEnabled is false) return;
        if (item.IsEnabled is false) return;

        await OnItemClick.InvokeAsync(item);
    }

    private void SetItemsToShow()
    {
        _displayItems.Clear();
        _overflowItems.Clear();

        if (MaxDisplayedItems == 0 || MaxDisplayedItems >= Items.Count)
        {
            _displayItems = Items.ToList();
            return;
        }

        if (OverflowIndex >= MaxDisplayedItems)
        {
            _internaloverflowIndex = 0;
        }

        var overflowItemsCount = Items.Count - MaxDisplayedItems;

        foreach ((BitBreadcrumbItem item, int index) in Items.Select((item, index) => (item, index)))
        {
            if (_internaloverflowIndex <= index && index < overflowItemsCount + _internaloverflowIndex)
            {
                if (index == _internaloverflowIndex)
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

    private string GetItemClasses(BitBreadcrumbItem item)
    {
        StringBuilder itemClasses = new();

        itemClasses.Append("item");

        if (item.IsSelected)
        {
            itemClasses.Append(" selected-item");
        }

        if (item.IsSelected && SelectedItemClass.HasValue())
        {
            itemClasses.Append($" {SelectedItemClass}");
        }

        if (item.IsEnabled is false)
        {
            itemClasses.Append(" disabled-item");
        }

        return itemClasses.ToString();
    }

    private string GetItemStyles(BitBreadcrumbItem item)
    {
        return item.IsSelected ? SelectedItemStyle ?? string.Empty : string.Empty;
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
