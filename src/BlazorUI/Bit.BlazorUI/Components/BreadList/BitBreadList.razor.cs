using System.Linq.Expressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadList<TItem> : IDisposable
{
    private const string HREF_FIELD = "Href";
    private const string CLASS_FIELD = "Class";
    private const string STYLE_FIELD = "Style";
    private const string TEXT_FIELD = "Text";
    private const string IS_SELECTED_FIELD = "IsSelected";
    private const string IS_ENABLED_FIELD = "IsEnabled";

    private string hrefField = HREF_FIELD;
    private string classField = CLASS_FIELD;
    private string styleField = STYLE_FIELD;
    private string textField = TEXT_FIELD;
    private string isSelectedField = IS_SELECTED_FIELD;
    private string isEnabledField = IS_ENABLED_FIELD;
    private Expression<Func<TItem, object>>? hrefSelector;
    private Expression<Func<TItem, object>>? classSelector;
    private Expression<Func<TItem, object>>? styleSelector;
    private Expression<Func<TItem, object>>? textSelector;
    private Expression<Func<TItem, bool>>? isSelectedSelector;
    private Expression<Func<TItem, bool>>? isEnabledSelector;

    private IList<TItem> items = new List<TItem>();
    private int maxDisplayedItems;
    private int overflowIndex;

    private string _internalHrefField = HREF_FIELD;
    private string _internalClassField = CLASS_FIELD;
    private string _internalStyleField = STYLE_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalIsSelectedField = IS_SELECTED_FIELD;
    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private int _internalOverflowIndex;

    private List<TItem> _displayItems = new();
    private List<TItem> _overflowItems = new();
    private DotNetObjectReference<BitBreadList<TItem>> _dotnetObj = default!;
    private bool _isCalloutOpen;
    private bool _disposed;

    private string _wrapperId => $"{UniqueId}-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    private string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// class HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public string ClassField
    {
        get => classField;
        set
        {
            classField = value;
            _internalClassField = value;
        }
    }

    /// <summary>
    /// Class HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? ClassSelector
    {
        get => classSelector;
        set
        {
            classSelector = value;

            if (value is not null)
            {
                _internalClassField = value.GetName();
            }
        }
    }

    /// <summary>
    /// Render a custom divider in place of the default chevron >
    /// </summary>
    [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

    /// <summary>
    /// URL to navigate to when this breadcrumb item is clicked.
    /// If provided, the breadcrumb will be rendered as a link.
    /// </summary>
    [Parameter]
    public string HrefField
    {
        get => hrefField;
        set
        {
            if (value != hrefField)
            {
                hrefField = value;
                _internalHrefField = value;
            }
        }
    }

    /// <summary>
    /// URL to navigate to when this breadcrumb item is clicked.
    /// If provided, the breadcrumb will be rendered as a link.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? HrefSelector
    {
        get => hrefSelector;
        set
        {
            hrefSelector = value;

            if (value is not null)
            {
                _internalHrefField = value.GetName();
            }
        }
    }

    /// <summary>
    /// Collection of breadcrumbs to render.
    /// </summary>
    [Parameter]
    public IList<TItem> Items
    {
        get => items;
        set
        {
            if (value != items)
            {
                items = value;
                SetItemsToShow();
            }
        }
    }

    /// <summary>
    /// Display the item as a Selected item.
    /// </summary>
    [Parameter]
    public string IsSelectedField
    {
        get => isSelectedField;
        set
        {
            isSelectedField = value;
            _internalIsSelectedField = value;
        }
    }

    /// <summary>
    /// Display the item as a Selected item.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, bool>>? IsSelectedSelector
    {
        get => isSelectedSelector;
        set
        {
            isSelectedSelector = value;

            if (value is not null)
            {
                _internalIsSelectedField = value.GetName();
            }
        }
    }

    /// <summary>
    /// Whether an item is enabled or not.
    /// </summary>
    [Parameter]
    public string IsEnabledField
    {
        get => isEnabledField;
        set
        {
            isEnabledField = value;
            _internalIsEnabledField = value;
        }
    }

    /// <summary>
    /// Whether an item is enabled or not.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, bool>>? IsEnabledSelector
    {
        get => isEnabledSelector;
        set
        {
            isEnabledSelector = value;

            if (value is not null)
            {
                _internalIsEnabledField = value.GetName();
            }
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
            if (value != maxDisplayedItems)
            {
                maxDisplayedItems = value;
                SetItemsToShow();
            }
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
            _internalOverflowIndex = value;

            if (value != overflowIndex)
            {
                overflowIndex = value;
                SetItemsToShow();
            }
        }
    }

    /// <summary>
    /// Render a custom overflow icon in place of the default icon.
    /// </summary>
    [Parameter] public BitIconName OverflowIcon { get; set; } = BitIconName.More;

    /// <summary>
    /// Callback for when the breadcrumb item clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public string StyleField
    {
        get => styleField;
        set
        {
            styleField = value;
            _internalStyleField = value;
        }
    }

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? StyleSelector
    {
        get => styleSelector;
        set
        {
            styleSelector = value;

            if (value is not null)
            {
                _internalStyleField = value.GetName();
            }
        }
    }

    /// <summary>
    /// The class HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemClass { get; set; }

    /// <summary>
    /// The style HTML attribute for Selected Item.
    /// </summary>
    [Parameter] public string? SelectedItemStyle { get; set; }

    /// <summary>
    /// Text to display in the breadcrumb item.
    /// </summary>
    [Parameter]
    public string TextField
    {
        get => textField;
        set
        {
            textField = value;
            _internalTextField = value;
        }
    }

    /// <summary>
    /// Text to display in the breadcrumb item.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? TextSelector
    {
        get => textSelector;
        set
        {
            textSelector = value;

            if (value is not null)
            {
                _internalTextField = value.GetName();
            }
        }
    }

    protected override string RootElementClass => "bit-brl";

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

        if (MaxDisplayedItems == 0 || MaxDisplayedItems >= Items.Count)
        {
            _displayItems = items.ToList();
            return;
        }

        if (OverflowIndex >= MaxDisplayedItems)
        {
            _internalOverflowIndex = 0;
        }

        var overflowItemsCount = Items.Count - MaxDisplayedItems;

        foreach ((TItem item, int index) in Items.Select((item, index) => (item, index)))
        {
            if (_internalOverflowIndex <= index && index < overflowItemsCount + _internalOverflowIndex)
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

    private string? GetItemHref(TItem item) => item.GetValueAsObjectFromProperty(_internalHrefField)?.ToString();
    private string? GetItemClass(TItem item) => item.GetValueAsObjectFromProperty(_internalClassField)?.ToString();
    private string? GetItemStyle(TItem item) => item.GetValueAsObjectFromProperty(_internalStyleField)?.ToString();
    private string? GetItemText(TItem item) => item.GetValueAsObjectFromProperty(_internalTextField)?.ToString();
    private bool GetIsSelected(TItem item) => item.GetValueFromProperty(_internalIsSelectedField, false);
    private bool GetIsEnabled(TItem item) => item.GetValueFromProperty(_internalIsEnabledField, true);

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
