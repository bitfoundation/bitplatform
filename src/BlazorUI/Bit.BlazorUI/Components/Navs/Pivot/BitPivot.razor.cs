namespace Bit.BlazorUI;

public partial class BitPivot : BitComponentBase
{
    private string? selectedKey;
    private bool SelectedKeyHasBeenSet;

    private BitPivotItem? _selectedItem;
    private List<BitPivotItem> _allItems = [];



    /// <summary>
    /// The content of pivot, It can be Any custom tag
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPivot component.
    /// </summary>
    [Parameter] public BitPivotClassStyles? Classes { get; set; }

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
    [Parameter, ResetClassBuilder]
    public BitPivotLinkFormat LinkFormat { get; set; } = BitPivotLinkFormat.Links;

    /// <summary>
    /// The size of the pivot links.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? LinkSize { get; set; }

    /// <summary>
    /// Overflow behavior when there is not enough room to display all of the links/tabs
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotOverflowBehavior OverflowBehavior { get; set; }

    /// <summary>
    /// Callback for when the a pivot item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitPivotItem> OnItemClick { get; set; }

    /// <summary>
    /// Position of the pivot header
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotPosition Position { get; set; }

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

    /// <summary>
    /// Custom CSS styles for different parts of the BitPivot component.
    /// </summary>
    [Parameter] public BitPivotClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-pvt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => LinkSize switch
        {
            BitSize.Small => "bit-pvt-sm",
            BitSize.Medium => "bit-pvt-md",
            BitSize.Large => "bit-pvt-lg",
            _ => string.Empty
        }).Register(() => LinkFormat switch
        {
            BitPivotLinkFormat.Links => "bit-pvt-links",
            BitPivotLinkFormat.Tabs => "bit-pvt-tabs",
            _ => string.Empty
        }).Register(() => OverflowBehavior switch
        {
            BitPivotOverflowBehavior.Menu => "bit-pvt-menu",
            BitPivotOverflowBehavior.Scroll => "bit-pvt-scroll",
            BitPivotOverflowBehavior.None => "bit-pvt-none",
            _ => string.Empty
        }).Register(() => Position switch
        {
            BitPivotPosition.Top => "bit-pvt-top",
            BitPivotPosition.Bottom => "bit-pvt-bottom",
            BitPivotPosition.Left => "bit-pvt-left",
            BitPivotPosition.Right => "bit-pvt-right",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
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
            BitVisibility.Collapsed => "visibility:hidden",
            BitVisibility.Hidden => "display:none",
            _ => string.Empty
        };
    }

    private string GetAriaLabelledby => $"Pivot-{UniqueId}-Tab-{_allItems.FindIndex(i => i == _selectedItem)}";
}
