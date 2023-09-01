using System.Text;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitDropdown
{
    private bool IsOpenHasBeenSet;
    private bool ValuesHasBeenSet;
    private bool SelectedItemHasBeenSet;
    private bool SelectedItemsHasBeenSet;

    private bool isRtl;
    private bool isOpen;
    private bool isRequired;
    private bool isResponsive;
    private List<string> values = new();
    private List<BitDropdownItem> selectedItems = new();

    private string? _text;
    private string? _dropdownId;
    private string? _dropdownLabelId;
    private string? _dropdownTextContainerId;
    private string? _dropdownCalloutId;
    private string? _dropdownOverlayId;
    private int? _totalItems;
    private string? _searchText;
    private bool _isValuesChanged;
    private bool _inputSearchHasFocus;
    private ElementReference _searchInputElement;
    private ElementReference _scrollWrapperElement;
    private Virtualize<BitDropdownItem>? _virtualizeElement;
    private DotNetObjectReference<BitDropdown> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Enables auto-focusing of the SearchBox input when the callout is open.
    /// </summary>
    [Parameter] public bool AutoFocusSearchBox { get; set; }

    /// <summary>
    /// The icon name of the chevron down element of the dropdown. The default value is ChevronDown.
    /// </summary>
    [Parameter] public string CaretDownIconName { get; set; } = "ChevronDown";

    /// <summary>
    /// The custom template for the chevron down element of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment? CaretDownTemplate { get; set; }

    /// <summary>
    /// The default key value that will be initially used to set selected item if the Value parameter is not set.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// The default key value that will be initially used to set selected items in multi select mode if the Values parameter is not set.
    /// </summary>
    [Parameter] public List<string> DefaultValues { get; set; } = new();

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// Enables the multi select mode.
    /// </summary>
    [Parameter] public bool IsMultiSelect { get; set; }

    /// <summary>
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (isOpen == value) return;

            isOpen = value;

            _ = IsOpenChanged.InvokeAsync(value);

            _ = ClearSearchBox();
        }
    }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Enables the required mode of the dropdown.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            if (isRequired == value) return;

            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Enables calling the select events when the same item is selected in single select mode.
    /// </summary>
    [Parameter] public bool IsReselectable { get; set; }

    /// <summary>
    /// Enables the responsive mode of the component for small screens.
    /// </summary>
    [Parameter]
    public bool IsResponsive
    {
        get => isResponsive;
        set
        {
            if (isResponsive == value) return;

            isResponsive = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Enables the RTL direction for the component.
    /// </summary>
    [Parameter]
    public bool IsRtl
    {
        get => isRtl;
        set
        {
            if (isRtl == value) return;

            isRtl = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The list of items to display in the callout.
    /// </summary>
    [Parameter] public List<BitDropdownItem>? Items { get; set; }

    /// <summary>
    /// The height of each item in pixels for virtualization.
    /// </summary>
    [Parameter] public int ItemSize { get; set; } = 35;

    /// <summary>
    /// The function providing items to the list for virtualization.
    /// </summary>
    [Parameter] public BitDropdownItemsProvider<BitDropdownItem>? ItemsProvider { get; set; }

    /// <summary>
    /// The custom template for rendering the items of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment<BitDropdownItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The text of the label element of the dropdown.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The custom template for the label of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The delimiter for joining the values to create the text of the dropdown in multi select mode.
    /// </summary>
    [Parameter] public string MultiSelectDelimiter { get; set; } = ", ";

    /// <summary>
    /// The callback that called when selected items change.
    /// </summary>
    [Parameter] public EventCallback<BitDropdownItem[]> OnChange { get; set; }

    /// <summary>
    /// The click callback for the dropdown.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The callback that is called when the search value changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// The callback that called when an item gets selected.
    /// </summary>
    [Parameter] public EventCallback<BitDropdownItem> OnSelectItem { get; set; }

    /// <summary>
    /// Determines how many additional items are rendered before and after the visible region.
    /// </summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// The placeholder text of the dropdown.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// The custom template for the placeholder of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment<BitDropdown>? PlaceholderTemplate { get; set; }

    /// <summary>
    /// The placeholder text of the SearchBox input.
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// The selected item in single select mode.
    /// </summary>
    [Parameter]
    public BitDropdownItem? SelectedItem
    {
        get => SelectedItems?.FirstOrDefault();
        set
        {
            if (SelectedItems?.FirstOrDefault() == value) return;

            SelectedItems?.Clear();
            if (value is not null)
            {
                SelectedItems?.Add(value);
            }
            ClassBuilder.Reset();
            _ = SelectedItemChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<BitDropdownItem?> SelectedItemChanged { get; set; }

    /// <summary>
    /// The selected items in multi select mode.
    /// </summary>
    [Parameter]
    public List<BitDropdownItem> SelectedItems
    {
        get => selectedItems;
        set
        {
            if (value == null || selectedItems == value) return;
            if (selectedItems.All(value.Contains) && selectedItems.Count == value.Count) return;

            selectedItems = value;
            ClassBuilder.Reset();
            _ = SelectedItemsChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<List<BitDropdownItem>> SelectedItemsChanged { get; set; }

    /// <summary>
    /// Shows the clear button when an item is selected.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Shows the SearchBox element in the callout.
    /// </summary>
    [Parameter] public bool ShowSearchBox { get; set; }

    /// <summary>
    /// The title to show when the mouse hovers over the dropdown.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The custom template for the text of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment<BitDropdown>? TextTemplate { get; set; }

    /// <summary>
    /// The key values of the selected items in multi select mode.
    /// </summary>
    [Parameter]
    public List<string> Values
    {
        get => values;
        set
        {
            if (value == null || values == value) return;
            if (values.All(value.Contains) && values.Count == value.Count) return;

            values = value;
            _ = ValuesChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<List<string>> ValuesChanged { get; set; }
    [Parameter] public Expression<Func<List<string>>>? ValuesExpression { get; set; }

    /// <summary>
    /// Enables virtualization to render only the visible items.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// The template for items that have not yet been rendered in virtualization mode.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }



    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
    }



    internal async Task HandleOnItemClick(BitDropdownItem item)
    {
        if (item.ItemType != BitDropdownItemType.Normal) return;
        if (IsEnabled is false || item.IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        if (IsMultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

            _isValuesChanged = true;

            item.IsSelected = !item.IsSelected;

            if (item.IsSelected)
            {
                SelectedItems.Add(item);
            }
            else
            {
                SelectedItems.Remove(item);
            }

            ClassBuilder.Reset();

            _text = string.Join(MultiSelectDelimiter, SelectedItems.Select(i => i.Text));

            CurrentValueAsString = string.Join(MultiSelectDelimiter, SelectedItems.Select(i => i.Value));

            Values = SelectedItems.Select(i => i.Value).ToList();

            if (item.IsSelected)
            {
                await OnSelectItem.InvokeAsync(item);
            }
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
        else
        {
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            var oldSelectedItem = SelectedItems.SingleOrDefault();

            var isSameItemSelected = oldSelectedItem == item;

            if (oldSelectedItem is not null)
            {
                oldSelectedItem.IsSelected = false;
            }

            item.IsSelected = true;

            SelectedItems.Clear();

            SelectedItems.Add(item);

            ClassBuilder.Reset();

            _text = item.Text;

            CurrentValueAsString = item.Value;

            await ToggleCallout();

            IsOpen = false;

            await ClearSearchBox();

            if (isSameItemSelected && IsReselectable is false) return;

            await OnSelectItem.InvokeAsync(item);
            await SelectedItemChanged.InvokeAsync(item);
        }

        await OnChange.InvokeAsync(SelectedItems.ToArray());

        StateHasChanged();
    }

    internal string GetItemCssClasses(BitDropdownItem item)
    {
        StringBuilder stringBuilder = new StringBuilder(RootElementClass);

        stringBuilder.Append("-iwr");

        if (item.IsSelected)
        {
            stringBuilder.Append($" {RootElementClass}-chd");
        }

        if (item.IsEnabled is false)
        {
            stringBuilder.Append($" {RootElementClass}-ids");
        }

        return stringBuilder.ToString();
    }

    internal int? GetTotalItems()
    {
        if (Items is null) return null;

        if (_totalItems.HasValue is false)
        {
            _totalItems = Items.FindAll(i => i.ItemType == BitDropdownItemType.Normal).Count;
        }

        return _totalItems.Value;
    }

    internal int? GetItemPosInSet(BitDropdownItem item)
    {
        return Items?.FindAll(i => i.ItemType == BitDropdownItemType.Normal).IndexOf(item) + 1;
    }



    protected override string RootElementClass => "bit-drp";
    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsRequired ? $"{RootElementClass}-req" : string.Empty);

        ClassBuilder.Register(() => IsResponsive ? $"{RootElementClass}-rsp" : string.Empty);

        ClassBuilder.Register(() => IsRtl ? $"{RootElementClass}-rtl" : string.Empty);

        ClassBuilder.Register(() => SelectedItems?.Count > 0 ? $"{RootElementClass}-hvl" : string.Empty);
    }

    protected override void OnInitialized()
    {
        _dropdownId = $"Dropdown-{UniqueId}";
        _dropdownLabelId = $"{_dropdownId}-label";
        _dropdownOverlayId = $"{_dropdownId}-overlay";
        _dropdownCalloutId = $"{_dropdownId}-callout";
        _dropdownTextContainerId = $"{_dropdownId}-text-container";

        if (ItemsProvider is null && Items is null)
        {
            Items = new();
        }

        SelectedItems ??= new();

        _dotnetObj = DotNetObjectReference.Create(this);

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        InitValues();

        InitSelectedItemsAndText();

        await base.OnParametersSetAsync();
    }

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override void RegisterFieldIdentifier()
    {
        if (IsMultiSelect)
        {
            RegisterFieldIdentifier(ValuesExpression, typeof(List<string>));
        }
        else
        {
            base.RegisterFieldIdentifier();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dotnetObj.Dispose();
        }

        base.Dispose(disposing);
    }



    private void InitValues()
    {
        if (Items is not null)
        {
            if (IsMultiSelect)
            {
                if (Values.Any() is false) return;

                var intersectValues = Values.Intersect(Items.Select(i => i.Value)).ToList();

                bool isEqual = intersectValues.OrderBy(i => i).SequenceEqual(Values.OrderBy(v => v));

                if (isEqual) return;

                Values = intersectValues;
            }
            else
            {
                if (CurrentValue.HasNoValue()) return;
                if (Items.Any(i => i.Value == CurrentValue)) return;

                CurrentValue = null;
            }
        }
        else if (ItemsProvider is not null && (SelectedItems?.Any() ?? false))
        {
            if (IsMultiSelect)
            {
                bool isEqual = SelectedItems.Select(si => si.Value).OrderBy(i => i).SequenceEqual(Values.OrderBy(v => v));
                if (isEqual) return;

                Values = SelectedItems.Select(si => si.Value).ToList();
            }
            else
            {
                if (CurrentValue == SelectedItem!.Value) return;

                CurrentValue = SelectedItem!.Value;
            }
        }
    }

    private void InitSelectedItemsAndText()
    {
        if (Items is not null)
        {
            SelectedItems.Clear();
            ClearAllItemsIsSelected();
            if (IsMultiSelect)
            {
                if (ValuesHasBeenSet || _isValuesChanged)
                {
                    Items.FindAll(i => Values.Contains(i.Value) && i.ItemType == BitDropdownItemType.Normal).ForEach(i => i.IsSelected = true);
                }
                else if (DefaultValues.Any())
                {
                    Items.FindAll(i => DefaultValues.Contains(i.Value) && i.ItemType == BitDropdownItemType.Normal).ForEach(i => i.IsSelected = true);
                }

                SelectedItems.AddRange(Items.FindAll(i => i.IsSelected));

                _ = SelectedItemsChanged.InvokeAsync(SelectedItems);
            }
            else
            {
                if (CurrentValue.HasValue() && Items.Any(i => i.Value == CurrentValue && i.ItemType == BitDropdownItemType.Normal))
                {
                    var item = Items.Find(i => i.Value == CurrentValue && i.ItemType == BitDropdownItemType.Normal);
                    item!.IsSelected = true;
                    SelectedItems.Add(item);
                    _ = SelectedItemChanged.InvokeAsync(item);
                }
                else if (DefaultValue.HasValue() && Items.Any(i => i.Value == DefaultValue && i.ItemType == BitDropdownItemType.Normal))
                {
                    var item = Items.Find(i => i.Value == DefaultValue && i.ItemType == BitDropdownItemType.Normal);
                    item!.IsSelected = true;
                    SelectedItems.Add(item);
                    _ = SelectedItemChanged.InvokeAsync(item);
                }
            }
            ClassBuilder.Reset();
        }

        if (SelectedItems.Any())
        {
            if (IsMultiSelect)
            {
                _text = string.Join(MultiSelectDelimiter, SelectedItems.Select(i => i.Text));
            }
            else
            {
                _text = SelectedItems.SingleOrDefault()?.Text ?? string.Empty;
            }
        }
        else
        {
            _text = string.Empty;
        }
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        if (IsOpen is false) return;

        await ToggleCallout();

        IsOpen = false;

        StateHasChanged();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        await ToggleCallout();

        IsOpen = !IsOpen;

        await OnClick.InvokeAsync(e);
        await FocusOnSearchBox();
    }

    private void ClearAllItemsIsSelected() => Items?.ForEach(i => i.IsSelected = false);

    private void HandleSearchBoxFocusIn() => _inputSearchHasFocus = true;

    private void HandleSearchBoxFocusOut() => _inputSearchHasFocus = false;

    private Task HandleSearchBoxOnClear() => ClearSearchBox();

    private async Task HandleFilterChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ShowSearchBox is false) return;

        _searchText = e.Value?.ToString();

        await OnSearch.InvokeAsync(_searchText);
        await SearchVirtualized();
    }

    private async Task ClearSearchBox()
    {
        if (IsEnabled is false) return;
        if (ShowSearchBox is false) return;
        if (_searchText.HasNoValue()) return;

        _searchText = null;

        await OnSearch.InvokeAsync(_searchText);
        await SearchVirtualized();
    }

    private async ValueTask FocusOnSearchBox()
    {
        if (IsEnabled is false) return;
        if (ShowSearchBox is false) return;
        if (AutoFocusSearchBox is false) return;
        if (IsOpen is false) return;

        await _searchInputElement.FocusAsync();
    }

    private BitDropdownItem[] GetSearchedItems()
    {
        if (ShowSearchBox && _searchText.HasValue())
        {
            return Items!.Where(i => i.Text.Contains(_searchText!, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        else
        {
            return Items!.ToArray();
        }
    }

    private string GetSearchBoxClasses()
    {
        StringBuilder className = new StringBuilder(RootElementClass);
        className.Append("-sb");

        if (_searchText.HasValue())
        {
            className.Append($" {RootElementClass}-shv");
        }

        if (_inputSearchHasFocus)
        {
            className.Append($" {RootElementClass}-shf");
        }

        return className.ToString();
    }

    private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{_dropdownLabelId} {_dropdownTextContainerId}" : $"{_dropdownTextContainerId}";

    private async Task SearchVirtualized()
    {
        if (ItemsProvider is null) return;
        if (_virtualizeElement is null) return;

        await _virtualizeElement.RefreshDataAsync();
    }

    private async Task Clear()
    {
        if (IsEnabled is false) return;

        if (IsMultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

            SelectedItems.ForEach(i => i.IsSelected = false);
            SelectedItems.Clear();
            ClassBuilder.Reset();

            Values = new();
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
        else
        {
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            if (SelectedItems.Count > 0)
            {
                var currentSelectedItem = SelectedItems.SingleOrDefault();
                currentSelectedItem!.IsSelected = false;

                SelectedItems.Clear();
                ClassBuilder.Reset();
            }

            CurrentValue = string.Empty;
            await SelectedItemChanged.InvokeAsync(SelectedItem);
        }

        _text = string.Empty;
        CurrentValueAsString = null;

        StateHasChanged();
    }

    private async Task ToggleCallout()
    {
        await _js.InvokeVoidAsync("BitDropdown.toggleDropdownCallout",
            _dotnetObj, UniqueId, _dropdownId, _dropdownCalloutId, _dropdownOverlayId, _scrollWrapperElement,
            DropDirection, IsOpen, IsResponsive, IsRtl, ShowSearchBox);
    }


    private async ValueTask<ItemsProviderResult<BitDropdownItem>> InternalItemsProvider(ItemsProviderRequest request)
    {
        if (ItemsProvider is null) return default;

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        await Task.Delay(100);

        if (request.CancellationToken.IsCancellationRequested) return default;

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var providerRequest = new BitDropdownItemsProviderRequest<BitDropdownItem>(request.StartIndex, request.Count, _searchText, request.CancellationToken);
        var providerResult = await ItemsProvider(providerRequest);

        if (request.CancellationToken.IsCancellationRequested) return default;

        foreach (var item in providerResult.Items)
        {
            item.IsSelected = item.ItemType == BitDropdownItemType.Normal && SelectedItems.Any(si => si.Value == item.Value);
        }

        return new ItemsProviderResult<BitDropdownItem>(providerResult.Items, providerResult.TotalItemCount);
    }
}
