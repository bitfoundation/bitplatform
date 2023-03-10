﻿using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitDropDown
{
    protected override bool UseVisual => false;

    private bool IsOpenHasBeenSet;
    private bool ValuesHasBeenSet;
    private bool SelectedItemHasBeenSet;
    private bool SelectedItemsHasBeenSet;

    private bool isRtl;
    private bool isOpen;
    private bool isRequired;
    private bool isMultiSelect;
    private bool isResponsiveModeEnabled;
    private List<string> values = new();
    private List<BitDropDownItem> selectedItems = new();

    private string? _text;
    private string? _dropDownId;
    private string? _dropdownLabelId;
    private string? _dropDownOptionId;
    private string? _dropDownCalloutId;
    private string? _dropDownOverlayId;
    private bool _isValuesChanged;
    private bool _inputSearchHasFocus;
    private string? _searchText;
    private int? _totalItems;
    private Virtualize<(int index, BitDropDownItem item)>? _virtualizeElement;
    private ElementReference _searchInputElement;
    private ElementReference _scrollWrapperElement;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Whether multiple items are allowed to be selected
    /// </summary>
    [Parameter]
    public bool IsMultiSelect
    {
        get => isMultiSelect;
        set
        {
            isMultiSelect = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not this dropdown is open
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

            ClassBuilder.Reset();
            _ = ClearSearchBox();
        }
    }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Whether the drop down items get rendered in a side panel in small screen sizes or not 
    /// </summary>
    [Parameter]
    public bool IsResponsiveModeEnabled
    {
        get => isResponsiveModeEnabled;
        set
        {
            isResponsiveModeEnabled = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Requires the end user to select an item in the dropdown.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// A list of items to display in the dropdown
    /// </summary>
    [Parameter] public List<BitDropDownItem>? Items { get; set; }

    /// <summary>
    /// Keys of the selected items for multiSelect scenarios
    /// If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed
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

    /// <summary>
    /// Gets or sets an expression that identifies the bound values.
    /// </summary>
    [Parameter] public Expression<Func<List<string>>>? ValuesExpression { get; set; }

    /// <summary>
    /// Keys that will be initially used to set selected items for multiSelect scenarios
    /// </summary>
    [Parameter] public List<string> DefaultValues { get; set; } = new();

    /// <summary>
    /// Key that will be initially used to set selected item
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Input placeholder Text, Displayed until an option is selected
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Search box input placeholder text
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// the label associated with the dropdown
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the drop down
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// When multiple items are selected, this still will be used to separate values in the dropdown title
    /// </summary>
    [Parameter] public string MultiSelectDelimiter { get; set; } = ", ";

    /// <summary>
    /// Callback for when the dropdown clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when an item is selected
    /// </summary>
    [Parameter] public EventCallback<BitDropDownItem> OnSelectItem { get; set; }

    /// <summary>
    /// Optional preference to have OnSelectItem still be called when an already selected item is clicked in single select mode
    /// </summary>
    [Parameter] public bool NotifyOnReselect { get; set; }

    /// <summary>
    /// Optional custom template for label
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Optional custom template for selected option displayed in after selection
    /// </summary>
    [Parameter] public RenderFragment<BitDropDown>? TextTemplate { get; set; }

    /// <summary>
    /// Optional custom template for placeholder Text
    /// </summary>
    [Parameter] public RenderFragment<BitDropDown>? PlaceholderTemplate { get; set; }

    /// <summary>
    /// Optional custom template for chevron icon
    /// </summary>
    [Parameter] public RenderFragment? CaretDownTemplate { get; set; }

    /// <summary>
    /// Optional chevron icon
    /// </summary>
    [Parameter] public BitIconName CaretDownIconName { get; set; } = BitIconName.ChevronDown;

    /// <summary>
    /// Optional custom template for drop-down item
    /// </summary>
    [Parameter] public RenderFragment<BitDropDownItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Search box is enabled for the end user
    /// </summary>
    [Parameter] public bool ShowSearchBox { get; set; }

    /// <summary>
    /// Auto focus on search box when dropdown is open
    /// </summary>
    [Parameter] public bool AutoFocusSearchBox { get; set; }

    /// <summary>
    /// Callback for when the search box input value changes
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// virtualize rendering the list
    /// UI rendering to just the parts that are currently visible
    /// default is false
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// determines how many additional items are rendered before and after the visible region
    /// default is 3
    /// </summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// The height of each item in pixels, default is 35
    /// </summary>
    [Parameter] public int ItemSize { get; set; } = 35;

    /// <summary>
    /// The function providing items to the list
    /// </summary>
    [Parameter] public BitDropDownItemsProvider<BitDropDownItem>? ItemsProvider { get; set; }

    /// <summary>
    /// The template for items that have not yet been loaded in memory.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }

    /// <summary>
    /// The selected items for multiSelect scenarios
    /// </summary>
    [Parameter]
    public List<BitDropDownItem> SelectedItems
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

    [Parameter] public EventCallback<List<BitDropDownItem>> SelectedItemsChanged { get; set; }

    /// <summary>
    /// The selected item for singleSelect scenarios
    /// </summary>
    [Parameter]
    public BitDropDownItem? SelectedItem
    {
        get => SelectedItems?.FirstOrDefault();
        set
        {
            if (SelectedItems?.FirstOrDefault() == value) return;

            SelectedItems.Clear();
            if (value is not null)
            {
                SelectedItems.Add(value);
            }
            ClassBuilder.Reset();
            _ = SelectedItemChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<BitDropDownItem?> SelectedItemChanged { get; set; }

    /// <summary>
    /// Change direction to RTL
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
    /// Drop-down opening direction
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;


    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
    }

    protected override string RootElementClass => "bit-drp";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => SelectedItems.Any() ? "has-value" : string.Empty);

        ClassBuilder.Register(() => IsOpen ? "opened" : string.Empty);

        ClassBuilder.Register(() => IsResponsiveModeEnabled ? "responsive" : string.Empty);

        ClassBuilder.Register(() => IsMultiSelect ? "multi" : string.Empty);

        ClassBuilder.Register(() => IsRtl ? "rtl" : string.Empty);
    }

    protected override void OnInitialized()
    {
        _dropDownId = $"Dropdown{UniqueId}";
        _dropDownOptionId = $"{_dropDownId}-option";
        _dropdownLabelId = Label.HasValue() ? $"{_dropDownId}-label" : string.Empty;
        _dropDownOverlayId = $"{_dropDownId}-overlay";
        _dropDownCalloutId = $"{_dropDownId}-list";

        if (ItemsProvider is null && Items is null)
        {
            Items = new();
        }

        if (SelectedItems is null)
        {
            SelectedItems = new();
        }

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        InitValueOrValues();
        InitText();

        await base.OnParametersSetAsync();
    }

    private void InitValueOrValues()
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
                if (Value.HasNoValue()) return;
                if (Items.Any(i => i.Value == Value)) return;

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

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitDropDown.toggleDropDownCallout",
            obj, UniqueId, _dropDownId, _dropDownCalloutId, _dropDownOverlayId, _scrollWrapperElement, DropDirection, IsOpen, IsResponsiveModeEnabled, IsRtl);

        IsOpen = false;
        StateHasChanged();
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        var obj = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, _dropDownId, _dropDownCalloutId, _dropDownOverlayId, _scrollWrapperElement, DropDirection, IsOpen, IsResponsiveModeEnabled, IsRtl);
        IsOpen = !IsOpen;
        await OnClick.InvokeAsync(e);
        await FocusOnSearchBox();
    }

    private async Task HandleItemClick(BitDropDownItem selectedItem)
    {
        if (selectedItem.ItemType != BitDropDownItemType.Normal) return;

        if (IsEnabled is false || selectedItem.IsEnabled is false) return;

        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        if (IsMultiSelect &&
                ValuesHasBeenSet &&
                ValuesChanged.HasDelegate is false) return;

        if (IsMultiSelect is false &&
            ValueHasBeenSet &&
            ValueChanged.HasDelegate is false) return;

        if (IsMultiSelect)
        {
            if (_isValuesChanged is false) _isValuesChanged = true;

            selectedItem.IsSelected = !selectedItem.IsSelected;
            if (selectedItem.IsSelected)
            {
                SelectedItems.Add(selectedItem);
            }
            else
            {
                SelectedItems.Remove(selectedItem);
            }
            ClassBuilder.Reset();

            _text = string.Join(MultiSelectDelimiter, SelectedItems.Select(i => i.Text));

            Values = SelectedItems.Select(i => i.Value).ToList();
            await OnSelectItem.InvokeAsync(selectedItem);
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
        else
        {

            var oldSelectedItem = SelectedItems.SingleOrDefault();
            var isSameItemSelected = oldSelectedItem == selectedItem;
            if (oldSelectedItem is not null)
            {
                oldSelectedItem.IsSelected = false;
            }

            selectedItem.IsSelected = true;

            SelectedItems.Clear();
            SelectedItems.Add(selectedItem);
            ClassBuilder.Reset();

            _text = selectedItem.Text;
            CurrentValueAsString = selectedItem.Value;
            var obj = DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, _dropDownId, _dropDownCalloutId, _dropDownOverlayId, _scrollWrapperElement, DropDirection, IsOpen, IsResponsiveModeEnabled, IsRtl);
            IsOpen = false;
            await ClearSearchBox();

            if (isSameItemSelected && !NotifyOnReselect) return;

            await OnSelectItem.InvokeAsync(selectedItem);
            await SelectedItemChanged.InvokeAsync(selectedItem);
        }
    }

    private void InitText()
    {
        if (Items is not null)
        {
            SelectedItems.Clear();
            ClearAllItemsIsSelected();
            if (IsMultiSelect)
            {
                if (ValuesHasBeenSet || _isValuesChanged)
                {
                    Items.FindAll(i => Values.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal).ForEach(i => i.IsSelected = true);
                }
                else if (DefaultValues.Any())
                {
                    Items.FindAll(i => DefaultValues.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal).ForEach(i => i.IsSelected = true);
                }

                SelectedItems.AddRange(Items.FindAll(i => i.IsSelected));

                _ = SelectedItemsChanged.InvokeAsync(SelectedItems);
            }
            else
            {
                if (CurrentValue.HasValue() && Items.Any(i => i.Value == CurrentValue && i.ItemType == BitDropDownItemType.Normal))
                {
                    var item = Items.Find(i => i.Value == CurrentValue && i.ItemType == BitDropDownItemType.Normal);
                    item!.IsSelected = true;
                    SelectedItems.Add(item);
                    _ = SelectedItemChanged.InvokeAsync(item);
                }
                else if (DefaultValue.HasValue() && Items.Any(i => i.Value == DefaultValue && i.ItemType == BitDropDownItemType.Normal))
                {
                    var item = Items.Find(i => i.Value == DefaultValue && i.ItemType == BitDropDownItemType.Normal);
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

    private (int index, BitDropDownItem item)[] GetItems()
    {
        if (ShowSearchBox && _searchText.HasValue())
        {
            return Items!.Where(i => i.Text.Contains(_searchText!, StringComparison.OrdinalIgnoreCase)).Select((item, index) => (index, item)).ToArray();
        }
        else
        {
            return Items!.Select((item, index) => (index, item)).ToArray();
        }
    }

    private string GetSearchBoxClasses() => $"search-box{(_searchText.HasValue() ? " has-value" : null)}{(_inputSearchHasFocus ? " focused" : null)}";

    private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{_dropDownId}-label {_dropDownId}-option" : $"{_dropDownId}-option";

    private int? GetItemPosInSet(BitDropDownItem item) => Items is null ? null : Items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).IndexOf(item) + 1;

    private int? GetTotalItems()
    {
        if (Items is null) return null;

        if (_totalItems.HasValue is false)
        {
            _totalItems = Items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).Count;
        }

        return _totalItems.Value;
    }

    private string GetCssClassForItem(BitDropDownItem item)
    {
        StringBuilder stringBuilder = new StringBuilder("checkbox-wrapper");

        if (item.IsSelected)
        {
            stringBuilder
                .Append(' ').Append("selected")
                .Append(' ').Append("checked");
        }

        stringBuilder.Append(' ').Append(item.IsEnabled ? "enabled" : "disabled");

        return stringBuilder.ToString();
    }

    private async Task SearchVirtualized()
    {
        if (ItemsProvider is null) return;
        if (_virtualizeElement is null) return;

        await _virtualizeElement.RefreshDataAsync();
    }

    // Gets called both by RefreshDataCoreAsync and directly by the Virtualize child component during scrolling
    private async ValueTask<ItemsProviderResult<(int index, BitDropDownItem item)>> ProvideVirtualizedItems(ItemsProviderRequest request)
    {
        if (ItemsProvider is null) return default;

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        await Task.Delay(100);
        if (request.CancellationToken.IsCancellationRequested) return default;

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var providerRequest = new BitDropDownItemsProviderRequest<BitDropDownItem>(request.StartIndex, request.Count, _searchText, request.CancellationToken);
        var providerResult = await ItemsProvider(providerRequest);

        if (request.CancellationToken.IsCancellationRequested) return default;

        foreach (var item in providerResult.Items)
        {
            item.IsSelected = item.ItemType == BitDropDownItemType.Normal && SelectedItems.Any(si => si.Value == item.Value);
        }

        return new ItemsProviderResult<(int, BitDropDownItem)>(
                 items: providerResult.Items.Select((x, i) => ValueTuple.Create(i + request.StartIndex + 2, x)),
                 totalItemCount: providerResult.TotalItemCount);
    }

    /// <inheritdoc />
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
}
