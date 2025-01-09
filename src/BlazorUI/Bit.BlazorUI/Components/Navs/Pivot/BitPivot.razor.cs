namespace Bit.BlazorUI;

/// <summary>
/// The Pivot control and related tabs pattern are used for navigating frequently accessed, distinct content categories. Pivots allow for navigation between two or more contentviews and relies on text headers to articulate the different sections of content.
/// </summary>
public partial class BitPivot : BitComponentBase
{
    private BitPivotItem? _selectedItem;
    private List<BitPivotItem> _allItems = [];



    /// <summary>
    /// Determines the alignment of the header section of the pivot.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// The content of pivot.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the pivot.
    /// </summary>
    [Parameter] public BitPivotClassStyles? Classes { get; set; }

    /// <summary>
    /// Default selected key for the pivot.
    /// </summary>
    [Parameter] public string? DefaultSelectedKey { get; set; }

    /// <summary>
    /// Whether to skip rendering the tabpanel with the content of the selected tab.
    /// </summary>
    [Parameter] public bool HeaderOnly { get; set; } = false;

    /// <summary>
    /// The type of the pivot header items.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotHeaderType? HeaderType { get; set; }

    /// <summary>
    /// Callback for when a pivot header item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitPivotItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback for when the selected pivot item changes.
    /// </summary>
    [Parameter]
    public EventCallback<BitPivotItem> OnChange { get; set; }

    /// <summary>
    /// Overflow behavior when there is not enough room to display all of the links/tabs.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotOverflowBehavior? OverflowBehavior { get; set; }

    /// <summary>
    /// Position of the pivot header.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotPosition? Position { get; set; }

    /// <summary>
    /// Key of the selected pivot item.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetSelectedKey))]
    public string? SelectedKey { get; set; }

    /// <summary>
    /// The size of the pivot header items.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the pivot.
    /// </summary>
    [Parameter] public BitPivotClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-pvt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-pvt-sm",
            BitSize.Medium => "bit-pvt-md",
            BitSize.Large => "bit-pvt-lg",
            _ => "bit-pvt-md"
        });

        ClassBuilder.Register(() => HeaderType switch
        {
            BitPivotHeaderType.Link => "bit-pvt-lnk",
            BitPivotHeaderType.Tab => "bit-pvt-tab",
            _ => "bit-pvt-lnk"
        });

        ClassBuilder.Register(() => OverflowBehavior switch
        {
            BitPivotOverflowBehavior.Menu => "bit-pvt-mnu",
            BitPivotOverflowBehavior.Scroll => "bit-pvt-scr",
            BitPivotOverflowBehavior.None => "bit-pvt-non",
            _ => "bit-pvt-non"
        });

        ClassBuilder.Register(() => Position switch
        {
            BitPivotPosition.Top => "bit-pvt-top",
            BitPivotPosition.Bottom => "bit-pvt-btm",
            BitPivotPosition.Left => "bit-pvt-lft",
            BitPivotPosition.Right => "bit-pvt-rgt",
            _ => "bit-pvt-top"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Alignment switch
        {
            BitAlignment.Start => "--bit-pvt-hal:flex-start",
            BitAlignment.End => "--bit-pvt-hal:flex-end",
            BitAlignment.Center => "--bit-pvt-hal:center",
            BitAlignment.SpaceBetween => "--bit-pvt-hal:space-between",
            BitAlignment.SpaceAround => "--bit-pvt-hal:space-around",
            BitAlignment.SpaceEvenly => "--bit-pvt-hal:space-evenly",
            BitAlignment.Baseline => "--bit-pvt-hal:baseline",
            BitAlignment.Stretch => "--bit-pvt-hal:stretch",
            _ => "--bit-pvt-hal:flex-start"
        });
    }

    protected override async Task OnInitializedAsync()
    {
        if (SelectedKeyHasBeenSet is false && DefaultSelectedKey is not null)
        {
            await AssignSelectedKey(DefaultSelectedKey);
        }

        await base.OnInitializedAsync();
    }



    internal int GetPivotItemTabIndex(BitPivotItem item) => item.IsSelected ? 0 : _allItems.FindIndex(i => i == item) == 0 ? 0 : -1;

    internal async void SelectItem(BitPivotItem item)
    {
        if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

        _selectedItem?.SetIsSelected(false);
        item.SetIsSelected(true);

        _selectedItem = item;
        _ = AssignSelectedKey(item.Key);

        await OnChange.InvokeAsync(item);

        StateHasChanged();
    }

    internal void RegisterItem(BitPivotItem item)
    {
        if (SelectedKey is null)
        {
            if (_allItems.Count == 0)
            {
                item.SetIsSelected(true);
                _selectedItem = item;
                StateHasChanged();
            }
        }
        else if (SelectedKey == item.Key)
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
            _ = SelectedKeyChanged.InvokeAsync(SelectedKey);
            return;
        }

        SelectItem(newItem);
    }

    private string GetSelectedItemStyle()
    {
        List<string?> list =
        [
            _selectedItem?.Visibility switch
            {
                BitVisibility.Collapsed => "visibility:hidden",
                BitVisibility.Hidden => "display:none",
                _ => string.Empty
            },
            Styles?.Body,
            _selectedItem?.BodyStyle
        ];

        return string.Join(';', list.Where(s => s.HasValue()));
    }

    private string GetSelectedItemClass()
    {
        List<string?> list =
        [
            (_selectedItem?.IsEnabled is false) ? "disabled" : string.Empty,
            Classes?.Body,
            _selectedItem?.BodyClass
        ];

        return string.Join(' ', list.Where(s => s.HasValue()));
    }

    private void OnSetSelectedKey()
    {
        SelectItemByKey(SelectedKey);
    }
}
