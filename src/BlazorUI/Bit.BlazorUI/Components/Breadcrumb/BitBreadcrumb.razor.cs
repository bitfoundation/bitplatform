using System.Linq.Expressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadcrumb<TItem> : IDisposable where TItem : class 
{
    protected override bool UseVisual => false;

    private const string CLASS_FIELD = nameof(BitBreadcrumbItem.Class);
    private const string HREF_FIELD = nameof(BitBreadcrumbItem.Href);
    private const string IS_SELECTED_FIELD = nameof(BitBreadcrumbItem.IsSelected);
    private const string IS_ENABLED_FIELD = nameof(BitBreadcrumbItem.IsEnabled);
    private const string TEXT_FIELD = nameof(BitBreadcrumbItem.Text);
    private const string STYLE_FIELD = nameof(BitBreadcrumbItem.Style);

    private string _internalClassField = CLASS_FIELD;
    private string _internalHrefField = HREF_FIELD;
    private string _internalIsSelectedField = IS_SELECTED_FIELD;
    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalStyleField = STYLE_FIELD;

    private DotNetObjectReference<BitBreadcrumb<TItem>> _dotnetObj = default!;
    private List<TItem> _internalItems = new();
    private List<TItem> _displayItems = new();
    private List<TItem> _overflowItems = new();
    private uint _internalOverflowIndex;
    private uint _internalMaxDisplayedItems;
    private bool _isCalloutOpen;
    private bool _disposed;

    private string _wrapperId => $"{UniqueId}-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    private string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// class HTML attribute for BreadList item.
    /// </summary>
    [Parameter] public string ClassField { get; set; } = CLASS_FIELD;

    /// <summary>
    /// Class HTML attribute for BreadList item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? ClassFieldSelector { get; set; }

    /// <summary>
    /// The content of the BitBreadcrumb, that are BitBreadOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// URL to navigate to when this BreadList item is clicked.
    /// If provided, the BreadList will be rendered as a link.
    /// </summary>
    [Parameter]
    public string HrefField { get; set; } = HREF_FIELD;

    /// <summary>
    /// URL to navigate to when this BreadList item is clicked.
    /// If provided, the BreadList will be rendered as a link.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? HrefFieldSelector { get; set; }

    /// <summary>
    /// Collection of BreadLists to render.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// Display the item as a Selected item.
    /// </summary>
    [Parameter] public string IsSelectedField { get; set; } = IS_SELECTED_FIELD;

    /// <summary>
    /// Display the item as a Selected item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsSelectedFieldSelector { get; set; }

    /// <summary>
    /// Whether an item is enabled or not.
    /// </summary>
    [Parameter] public string IsEnabledField { get; set; } = IS_ENABLED_FIELD;

    /// <summary>
    /// Whether an item is enabled or not.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsEnabledFieldSelector { get; set; }

    /// <summary>
    /// The maximum number of BreadLists to display before coalescing.
    /// If not specified, all BreadLists will be rendered.
    /// </summary>
    [Parameter] public uint MaxDisplayedItems { get; set; }

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
    [Parameter] public BitIconName OverflowIcon { get; set; } = BitIconName.More;

    /// <summary>
    /// Callback for when the BreadList item clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Style HTML attribute for BreadList item.
    /// </summary>
    [Parameter] public string StyleField { get; set; } = STYLE_FIELD;

    /// <summary>
    /// Style HTML attribute for BreadList item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? StyleFieldSelector { get; set; }

    /// <summary>
    /// The class HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemClass { get; set; }

    /// <summary>
    /// The style HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemStyle { get; set; }

    /// <summary>
    /// Text to display in the BreadList item.
    /// </summary>
    [Parameter] public string TextField { get; set; } = TEXT_FIELD;

    /// <summary>
    /// Text to display in the BreadList item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? TextFieldSelector { get; set; }

    protected override string RootElementClass => "bit-brc";

    internal void RegisterOptions(BitBreadcrumbOption option)
    {
        _internalItems.Add((option as TItem)!);
        _internalMaxDisplayedItems = MaxDisplayedItems == 0 ? (uint)_internalItems.Count : MaxDisplayedItems;
        _internalOverflowIndex = OverflowIndex >= _internalMaxDisplayedItems ? 0 : OverflowIndex;
        SetItemsToShow();
        StateHasChanged();
    }

    internal void UnregisterOptions(BitBreadcrumbOption option)
    {
        _internalItems.Remove((option as TItem)!);
        SetItemsToShow();
        StateHasChanged();
    }

    internal void InternalStateHasChanged()
    {
        StateHasChanged();
    }

    protected override Task OnInitializedAsync()
    {
        _internalClassField = ClassFieldSelector?.GetName() ?? ClassField;
        _internalHrefField = HrefFieldSelector?.GetName() ?? HrefField;
        _internalIsSelectedField = IsSelectedFieldSelector?.GetName() ?? IsSelectedField;
        _internalIsEnabledField = IsEnabledFieldSelector?.GetName() ?? IsEnabledField;
        _internalTextField = TextFieldSelector?.GetName() ?? TextField;
        _internalStyleField = StyleFieldSelector?.GetName() ?? StyleField;

        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ChildContent is null && Items.Any())
        {
            bool shouldCallSetItemsToShow = false;

            shouldCallSetItemsToShow = _internalItems.Count != Items.Count || _internalItems.Any(item => Items.Contains(item) is false);
            _internalItems = Items.ToList();

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

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleOverflowCallout(_dotnetObj, _wrapperId, _overflowDropDownId, _calloutId, _overlayId, _isCalloutOpen);
        _isCalloutOpen = !_isCalloutOpen;
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false) return;
        if (GetIsEnabled(item) is false) return;

        await OnItemClick.InvokeAsync(item);
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

    private string GetItemClasses(TItem item)
    {
        StringBuilder itemClasses = new();

        itemClasses.Append("item");

        if (GetItemClass(item).HasValue())
        {
            itemClasses.Append($" {GetItemClass(item)}");
        }

        if (GetIsSelected(item))
        {
            itemClasses.Append(" selected-item");
        }

        if (GetIsSelected(item) && SelectedItemClass.HasValue())
        {
            itemClasses.Append($" {SelectedItemClass}");
        }

        if (GetIsEnabled(item) is false)
        {
            itemClasses.Append(" disabled-item");
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

        if (GetIsSelected(item) && SelectedItemStyle.HasValue())
        {
            itemStyles.Append(SelectedItemStyle);
        }

        return itemStyles.ToString();
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

        return item.GetValueAsObjectFromProperty(_internalHrefField)?.ToString();
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

        return item.GetValueAsObjectFromProperty(_internalClassField)?.ToString();
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

        return item.GetValueAsObjectFromProperty(_internalStyleField)?.ToString();
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

        return item.GetValueAsObjectFromProperty(_internalTextField)?.ToString();
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

        return item.GetValueFromProperty(_internalIsSelectedField, false);
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

        return item.GetValueFromProperty(_internalIsEnabledField, true);
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
