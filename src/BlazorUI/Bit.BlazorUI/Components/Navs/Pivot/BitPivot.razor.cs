namespace Bit.BlazorUI;

/// <summary>
/// The Pivot control and related tabs pattern are used for navigating frequently accessed, distinct content categories. Pivots allow for navigation between two or more contentviews and relies on text headers to articulate the different sections of content.
/// </summary>
public partial class BitPivot : BitComponentBase
{
    private bool _jsSetup;
    private bool _setupRtl;
    private bool _setupVertical;
    private bool _isMenuOpen;
    private bool _slideAtEnd;
    private bool _slideAtStart = true;
    private bool _slideHasOverflow;
    private ElementReference _moreRef;
    private ElementReference _headerRef;
    private BitPivotItem? _selectedItem;
    private int[] _overflowItemIndexes = [];
    private List<BitPivotItem> _allItems = [];
    private BitPivotOverflowBehavior? _setupBehavior;
    private DotNetObjectReference<BitPivot>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    /// The general color of the pivot.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default selected key for the pivot.
    /// </summary>
    [Parameter] public string? DefaultSelectedKey { get; set; }

    /// <summary>
    /// Whether to skip rendering the tabpanel with the content of the selected tab.
    /// </summary>
    [Parameter] public bool HeaderOnly { get; set; }

    /// <summary>
    /// The type of the pivot header items.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotHeaderType? HeaderType { get; set; }

    /// <summary>
    /// Mounts all tabs at render time and hide non-selected tabs with CSS styles instead of not-rendering them (useful for processing/extracting data).
    /// </summary>
    [Parameter] public bool MountAll { get; set; }

    /// <summary>
    /// The aria-label of the next button in the Slide overflow behavior (default: Next).
    /// </summary>
    [Parameter] public string? NextAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon of the next button in the Slide overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the next button in the Slide overflow behavior from the built-in Fluent UI icons (default: ChevronRight).
    /// </summary>
    [Parameter] public string? NextIconName { get; set; }

    /// <summary>
    /// Callback for when the selected pivot item changes.
    /// </summary>
    [Parameter]
    public EventCallback<BitPivotItem> OnChange { get; set; }

    /// <summary>
    /// Callback for when a pivot header item is clicked.
    /// </summary>
    [Parameter] public EventCallback<BitPivotItem> OnItemClick { get; set; }

    /// <summary>
    /// The aria-label of the overflow menu button in the Menu overflow behavior (default: More).
    /// </summary>
    [Parameter] public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Overflow behavior when there is not enough room to display all of the links/tabs.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotOverflowBehavior? OverflowBehavior { get; set; }

    /// <summary>
    /// Gets or sets the icon of the overflow menu button in the Menu overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OverflowIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? OverflowIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the overflow menu button in the Menu overflow behavior from the built-in Fluent UI icons (default: More).
    /// </summary>
    [Parameter] public string? OverflowIconName { get; set; }

    /// <summary>
    /// Position of the pivot header.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPivotPosition? Position { get; set; }

    /// <summary>
    /// The aria-label of the previous button in the Slide overflow behavior (default: Previous).
    /// </summary>
    [Parameter] public string? PreviousAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon of the previous button in the Slide overflow behavior using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PreviousIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PreviousIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon of the previous button in the Slide overflow behavior from the built-in Fluent UI icons (default: ChevronLeft).
    /// </summary>
    [Parameter] public string? PreviousIconName { get; set; }

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

    private bool _isVertical => Position is BitPivotPosition.Start or BitPivotPosition.End;

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

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-pvt-pri",
            BitColor.Secondary => "bit-pvt-sec",
            BitColor.Tertiary => "bit-pvt-ter",
            BitColor.Info => "bit-pvt-inf",
            BitColor.Success => "bit-pvt-suc",
            BitColor.Warning => "bit-pvt-wrn",
            BitColor.SevereWarning => "bit-pvt-swr",
            BitColor.Error => "bit-pvt-err",
            BitColor.PrimaryBackground => "bit-pvt-pbg",
            BitColor.SecondaryBackground => "bit-pvt-sbg",
            BitColor.TertiaryBackground => "bit-pvt-tbg",
            BitColor.PrimaryForeground => "bit-pvt-pfg",
            BitColor.SecondaryForeground => "bit-pvt-sfg",
            BitColor.TertiaryForeground => "bit-pvt-tfg",
            BitColor.PrimaryBorder => "bit-pvt-pbr",
            BitColor.SecondaryBorder => "bit-pvt-sbr",
            BitColor.TertiaryBorder => "bit-pvt-tbr",
            _ => "bit-pvt-pri"
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
            BitPivotOverflowBehavior.Slide => "bit-pvt-sld",
            BitPivotOverflowBehavior.None => "bit-pvt-non",
            _ => "bit-pvt-non"
        });

        ClassBuilder.Register(() => Position switch
        {
            BitPivotPosition.Top => "bit-pvt-top",
            BitPivotPosition.Bottom => "bit-pvt-btm",
            BitPivotPosition.Start => "bit-pvt-sta",
            BitPivotPosition.End => "bit-pvt-end",
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsDisposed)
        {
            await base.OnAfterRenderAsync(firstRender);
            return;
        }

        var behavior = OverflowBehavior ?? BitPivotOverflowBehavior.None;
        var needsJs = behavior is BitPivotOverflowBehavior.Menu or BitPivotOverflowBehavior.Slide;
        var rtl = Dir is BitDir.Rtl;
        var vertical = Position is BitPivotPosition.Start or BitPivotPosition.End;

        if (_setupBehavior != behavior || (_jsSetup && (_setupRtl != rtl || _setupVertical != vertical)))
        {
            if (_jsSetup)
            {
                await _js.BitPivotDispose(_Id);
                _jsSetup = false;
            }

            _dotnetObj?.Dispose();
            _dotnetObj = null;

            _isMenuOpen = false;
            _slideAtEnd = false;
            _slideAtStart = true;
            _slideHasOverflow = false;
            _overflowItemIndexes = [];

            if (needsJs)
            {
                _dotnetObj = DotNetObjectReference.Create(this);

                await _js.BitPivotSetup(
                    _Id,
                    _headerRef,
                    behavior is BitPivotOverflowBehavior.Menu ? _moreRef : null,
                    behavior is BitPivotOverflowBehavior.Menu,
                    behavior is BitPivotOverflowBehavior.Slide,
                    rtl,
                    vertical,
                    _dotnetObj);

                _jsSetup = true;
            }

            _setupBehavior = behavior;
            _setupRtl = rtl;
            _setupVertical = vertical;
        }
        else if (_jsSetup)
        {
            await _js.BitPivotRefresh(_Id);
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    [JSInvokable("OnSetOverflowItems")]
    public void OnSetOverflowItems(int[] indexes)
    {
        if (IsDisposed) return;

        _overflowItemIndexes = indexes ?? [];

        if (_overflowItemIndexes.Length == 0)
        {
            _isMenuOpen = false;
        }

        StateHasChanged();
    }

    [JSInvokable("OnSetSlideState")]
    public void OnSetSlideState(bool hasOverflow, bool atStart, bool atEnd)
    {
        if (IsDisposed) return;

        _slideHasOverflow = hasOverflow;
        _slideAtStart = atStart;
        _slideAtEnd = atEnd;

        StateHasChanged();
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

    private string GetItemStyle(BitPivotItem? item)
    {
        List<string?> list =
        [
            item?.Visibility switch
            {
                BitVisibility.Collapsed => "visibility:hidden",
                BitVisibility.Hidden => "display:none",
                _ => string.Empty
            },
            Styles?.Body,
            item?.BodyStyle,
            item != _selectedItem ? "display:none" : string.Empty
        ];

        return string.Join(';', list.Where(s => s.HasValue()));
    }

    private string GetItemClass(BitPivotItem? item)
    {
        List<string?> list =
        [
            (item?.IsEnabled is false) ? "disabled" : string.Empty,
            Classes?.Body,
            item?.BodyClass
        ];

        return string.Join(' ', list.Where(s => s.HasValue()));
    }

    private void OnSetSelectedKey()
    {
        SelectItemByKey(SelectedKey);
    }

    private void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    private void CloseMenu()
    {
        _isMenuOpen = false;
    }

    private async Task SelectFromMenu(BitPivotItem item)
    {
        CloseMenu();

        if (IsEnabled is false || item.IsEnabled is false) return;

        SelectItem(item);

        await OnItemClick.InvokeAsync(item);

        if (_jsSetup)
        {
            await _js.BitPivotRefresh(_Id);
        }
    }

    private async Task Slide(bool forward)
    {
        if (IsEnabled is false || _jsSetup is false) return;

        await _js.BitPivotSlide(_Id, forward);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            try
            {
                await _js.BitPivotDispose(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here

            _dotnetObj.Dispose();
            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }
}
