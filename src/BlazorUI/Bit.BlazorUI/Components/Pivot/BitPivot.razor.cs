namespace Bit.BlazorUI;

public partial class BitPivot
{
    protected override bool UseVisual => false;


    private bool SelectedKeyHasBeenSet;
    private BitPivotLinkFormat linkFormat = BitPivotLinkFormat.Links;
    private BitPivotLinkSize linkSize = BitPivotLinkSize.Normal;
    private BitPivotOverflowBehavior overflowBehavior = BitPivotOverflowBehavior.None;
    private BitPivotPosition position = BitPivotPosition.Top;
    private string? selectedKey;

    private BitPivotItem? _selectedItem;
    private List<BitPivotItem> _allItems = new();

    /// <summary>
    /// The content of pivot, It can be Any custom tag
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Default selected key for the pivot
    /// </summary>
    [Parameter] public string? DefaultSelectedKey { get; set; }

    /// <summary>
    /// Whether to skip rendering the tabpanel with the content of the selected tab
    /// </summary>
    [Parameter] public bool HeadersOnly { get; set; } = false;

    /// <summary>
    /// Pivot link format, display mode for the pivot links
    /// </summary>
    [Parameter]
    public BitPivotLinkFormat LinkFormat
    {
        get => linkFormat;
        set
        {
            if (value == linkFormat) return;

            linkFormat = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Pivot link size
    /// </summary>
    [Parameter]
    public BitPivotLinkSize LinkSize
    {
        get => linkSize;
        set
        {
            if (value == linkSize) return;

            linkSize = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Overflow behavior when there is not enough room to display all of the links/tabs
    /// </summary>
    [Parameter]
    public BitPivotOverflowBehavior OverflowBehavior
    {
        get => overflowBehavior;
        set
        {
            if (value == overflowBehavior) return;

            overflowBehavior = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Callback for when the a pivot item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitPivotItem> OnItemClick { get; set; }

    /// <summary>
    /// Position of the pivot header
    /// </summary>
    [Parameter]
    public BitPivotPosition Position
    {
        get => position;
        set
        {
            if (value == position) return;

            position = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Key of the selected pivot item. Updating this will override the Pivot's selected item state
    /// </summary>
    [Parameter]
    public string? SelectedKey
    {
        get => selectedKey;
        set
        {
            if (value == selectedKey) return;

            SelectItemByKey(value);
        }
    }

    [Parameter] public EventCallback<string?> SelectedKeyChanged { get; set; }

    protected override string RootElementClass => "bit-pvt";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => LinkSize switch
        {
            BitPivotLinkSize.Large => "large",
            BitPivotLinkSize.Normal => "normal",
            _ => string.Empty
        }).Register(() => LinkFormat switch
        {
            BitPivotLinkFormat.Links => "links",
            BitPivotLinkFormat.Tabs => "tabs",
            _ => string.Empty
        }).Register(() => OverflowBehavior switch
        {
            BitPivotOverflowBehavior.Menu => "menu",
            BitPivotOverflowBehavior.Scroll => "scroll",
            BitPivotOverflowBehavior.None => "none",
            _ => string.Empty
        }).Register(() => Position switch
        {
            BitPivotPosition.Top => "position-top",
            BitPivotPosition.Bottom => "position-bottom",
            BitPivotPosition.Left => "position-left",
            BitPivotPosition.Right => "position-right",
            _ => string.Empty
        });
    }

    protected override Task OnInitializedAsync()
    {
        selectedKey ??= DefaultSelectedKey;

        return base.OnInitializedAsync();
    }

    internal string GetPivotItemId(BitPivotItem item) => $"Pivot-{UniqueId}-Tab-{_allItems.FindIndex(i => i == item)}";

    internal int GetPivotItemTabIndex(BitPivotItem item) => item.IsSelected ? 0 : _allItems.FindIndex(i => i == item) == 0 ? 0 : -1;

    internal void SelectItem(BitPivotItem item)
    {
        if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

        _selectedItem?.SetIsSelected(false);
        item.SetIsSelected(true);

        _selectedItem = item;
        selectedKey = item.Key;

        _ = SelectedKeyChanged.InvokeAsync(selectedKey);

        StateHasChanged();
    }

    internal void RegisterItem(BitPivotItem item)
    {
        if (selectedKey is null)
        {
            if (_allItems.Count == 0)
            {
                item.SetIsSelected(true);
                _selectedItem = item;
                StateHasChanged();
            }
        }
        else if (selectedKey == item.Key)
        {
            item.SetIsSelected(true);
            _selectedItem = item;
            StateHasChanged();
        }

        _allItems.Add(item);
    }

    internal void UnregisterItem(BitPivotItem item)
    {
        _allItems.Remove(item);
    }

    internal void Refresh()
    {
        StateHasChanged();
    }

    private void SelectItemByKey(string? key)
    {
        var newItem = _allItems.FirstOrDefault(i => i.Key == key);

        if (newItem == null || newItem == _selectedItem || newItem.IsEnabled is false)
        {
            _ = SelectedKeyChanged.InvokeAsync(selectedKey);
            return;
        }

        SelectItem(newItem);
    }

    private string GetSelectedItemStyle()
    {
        return _selectedItem?.Visibility switch
        {
            BitComponentVisibility.Collapsed => "visibility:hidden",
            BitComponentVisibility.Hidden => "display:none",
            _ => string.Empty
        };
    }

    private string GetAriaLabelledby => $"Pivot-{UniqueId}-Tab-{_allItems.FindIndex(i => i == _selectedItem)}";
}
