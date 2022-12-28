using System.Linq.Expressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitBreadList<TItem> : IDisposable
{
    private const string HREF_FIELD = "Href";
    private const string CLASS_FIELD = "ItemClass";
    private const string STYLE_FIELD = "ItemStyle";
    private const string TEXT_FIELD = "ItemStyle";
    private const string IS_SELECTED_FIELD = "IsSelected";

    private string hrefField = HREF_FIELD;
    private string itemClassField = CLASS_FIELD;
    private string itemStyleField = STYLE_FIELD;
    private string textField = TEXT_FIELD;
    private string isSelectedField = IS_SELECTED_FIELD;
    private Expression<Func<TItem, object>>? hrefSelector;
    private Expression<Func<TItem, object>>? itemClassSelector;
    private Expression<Func<TItem, object>>? itemStyleSelector;
    private Expression<Func<TItem, object>>? textSelector;
    private Expression<Func<TItem, bool>>? isSelectedSelector;
    private IList<TItem> items = new List<TItem>();
    private int maxDisplayedItems;
    private int overflowIndex;

    private string _internalHrefField = HREF_FIELD;
    private string _internalItemClassField = CLASS_FIELD;
    private string _internalItemStyleField = STYLE_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalIsSelectedField = IS_SELECTED_FIELD;
    private int _internalOverflowIndex;

    private bool _isCalloutOpen;
    private string _wrapperId => $"{UniqueId}-wrapper";
    private string _calloutId => $"{UniqueId}-callout";
    private string _overlayId => $"{UniqueId}-overlay";
    private string _overflowDropDownId => $"{UniqueId}-overflow-dropdown";

    private List<TItem> _displayItems = new();
    private List<TItem> _overflowItems = new();

    private DotNetObjectReference<BitBreadList<TItem>> _dotnetObj = default!;

    private bool _disposed;

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
    /// 
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
    /// 
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, bool>>? IsSelectedSelector
    {
        get => isSelectedSelector;
        set
        {
            isSelectedSelector = value;

            if (value is not null)
                _internalIsSelectedField = value.GetName();
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
            hrefField = value;
            _internalHrefField = value;
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
                _internalHrefField = value.GetName();
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
            items = value;
            SetItemsToShow();
        }
    }

    /// <summary>
    /// class HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public string ItemClassField
    {
        get => itemClassField;
        set
        {
            itemClassField = value;
            _internalItemClassField = value;
        }
    }

    /// <summary>
    /// Class HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? ItemClassSelector
    {
        get => itemClassSelector;
        set
        {
            itemClassSelector = value;

            if (value is not null)
                _internalItemClassField = value.GetName();
        }
    }

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public string ItemStyleField
    {
        get => itemStyleField;
        set
        {
            itemStyleField = value;
            _internalItemStyleField = value;
        }
    }

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    [Parameter]
    public Expression<Func<TItem, object>>? ItemStyleSelector
    {
        get => itemStyleSelector;
        set
        {
            itemStyleSelector = value;

            if (value is not null)
                _internalItemStyleField = value.GetName();
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
            _internalOverflowIndex = value;
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
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

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
                _internalTextField = value.GetName();
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

        await OnItemClick.InvokeAsync(item);
    }

    private void SetItemsToShow()
    {
        _displayItems.Clear();
        _overflowItems.Clear();

        if (MaxDisplayedItems == 0 || MaxDisplayedItems >= Items.Count)
        {
            _displayItems.AddRange(Items);
        }
        else
        {
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
            itemClasses.Append(" current-item");
        }

        if (GetIsSelected(item) && CurrentItemClass.HasValue())
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

        if (GetIsSelected(item) && CurrentItemStyle.HasValue())
        {
            itemStyles.Append(CurrentItemStyle);
        }

        return itemStyles.ToString();
    }

    private string? GetItemHref(TItem item) => item.GetValueAsObjectFromProperty(_internalHrefField)?.ToString();
    private string? GetItemClass(TItem item) => item.GetValueAsObjectFromProperty(_internalItemClassField)?.ToString();
    private string? GetItemStyle(TItem item) => item.GetValueAsObjectFromProperty(_internalItemStyleField)?.ToString();
    private string? GetItemText(TItem item) => item.GetValueAsObjectFromProperty(_internalTextField)?.ToString();
    private bool GetIsSelected(TItem item) => item.GetValueFromProperty(_internalIsSelectedField, false);

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
