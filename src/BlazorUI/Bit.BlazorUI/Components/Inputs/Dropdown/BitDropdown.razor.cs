using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitDropdown<TItem, TValue> where TItem : class, new()
{
    private bool IsOpenHasBeenSet;
    private bool ValuesHasBeenSet;

    private bool isOpen;
    private bool isRequired;
    private bool chips;
    private ICollection<TValue?>? values = Array.Empty<TValue?>();

    private List<TItem> _selectedItems = [];
    private List<TItem> _lastShowItems = [];

    private string _dropdownId = string.Empty;
    private string _calloutId = string.Empty;
    private string _scrollContainerId = string.Empty;
    private string _headerId = string.Empty;
    private string _footerId = string.Empty;

    private string _labelId = string.Empty;
    private string _dropdownTextContainerId = string.Empty;

    private int? _totalItems;
    private string? _searchText;
    private bool _isResponsiveMode;
    private bool _inputSearchHasFocus;
    private ElementReference _searchInputRef;
    private ElementReference _comboBoxInputRef;
    private ElementReference _comboBoxInputResponsiveRef;
    private Virtualize<TItem>? _virtualizeElement;
    private DotNetObjectReference<BitDropdown<TItem, TValue>> _dotnetObj = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Enables auto-focusing of the SearchBox input when the callout is open.
    /// </summary>
    [Parameter] public bool AutoFocusSearchBox { get; set; }

    /// <summary>
    /// Custom template to render as a header in the callout.
    /// </summary>
    [Parameter] public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <summary>
    /// Custom template to render as a footer in the callout.
    /// </summary>
    [Parameter] public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// The icon name of the chevron down element of the dropdown. The default value is ChevronDown.
    /// </summary>
    [Parameter] public string CaretDownIconName { get; set; } = "ChevronDown";

    /// <summary>
    /// The custom template for the chevron down element of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment? CaretDownTemplate { get; set; }

    /// <summary>
    /// The content of the Dropdown, a list of BitDropdownOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitDropdown.
    /// </summary>
    [Parameter] public BitDropdownClassStyles? Classes { get; set; }

    /// <summary>
    /// The default key value that will be initially used to set selected item if the Value parameter is not set.
    /// </summary>
    [Parameter] public TValue? DefaultValue { get; set; }

    /// <summary>
    /// The default key value that will be initially used to set selected items in multi select mode if the Values parameter is not set.
    /// </summary>
    [Parameter] public ICollection<TValue?>? DefaultValues { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// Enables the multi select mode.
    /// </summary>
    [Parameter] public bool IsMultiSelect { get; set; }

    /// <summary>
    /// Determines the opening state of the callout. (two-way bound)
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
    public bool IsResponsive { get; set; }

    /// <summary>
    /// The list of items to display in the callout.
    /// </summary>
    [Parameter] public ICollection<TItem>? Items { get; set; }

    /// <summary>
    /// The height of each item in pixels for virtualization.
    /// </summary>
    [Parameter] public int ItemSize { get; set; } = 35;

    /// <summary>
    /// The function providing items to the list for virtualization.
    /// </summary>
    [Parameter] public BitDropdownItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// The custom template for rendering the items of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

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
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitDropdownNameSelectors<TItem, TValue>? NameSelectors { get; set; }

    /// <summary>
    /// The callback that called when selected items change.
    /// </summary>
    [Parameter] public EventCallback<TItem[]> OnChange { get; set; }

    /// <summary>
    /// The click callback for the dropdown.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The callback that called when an item gets selected.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// The callback that called when an item gets selected.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

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
    [Parameter] public RenderFragment<BitDropdown<TItem, TValue>>? PlaceholderTemplate { get; set; }

    /// <summary>
    /// The placeholder text of the SearchBox input.
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, ICollection<TItem>>? SearchFunction { get; set; }

    /// <summary>
    /// Shows the clear button when an item is selected.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Shows the SearchBox element in the callout.
    /// </summary>
    [Parameter] public bool ShowSearchBox { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDropdown.
    /// </summary>
    [Parameter] public BitDropdownClassStyles? Styles { get; set; }

    /// <summary>
    /// The title to show when the mouse hovers over the dropdown.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The custom template for the text of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment<BitDropdown<TItem, TValue>>? TextTemplate { get; set; }

    /// <summary>
    /// The values of the selected items in multi select mode. (two-way bound)
    /// </summary>
    [Parameter]
    public ICollection<TValue?>? Values
    {
        get => values;
        set
        {
            if (values == value) return;
            if (value is not null && values!.All(value.Contains) && values!.Count == value.Count) return;

            values = value;
            _ = ValuesChanged.InvokeAsync(value);

            UpdateSelectedItemsFromValues();

            EditContext?.NotifyFieldChanged(FieldIdentifier);
        }
    }
    [Parameter] public EventCallback<ICollection<TValue?>?> ValuesChanged { get; set; }
    [Parameter] public Expression<Func<ICollection<TValue?>?>>? ValuesExpression { get; set; }

    /// <summary>
    /// Enables virtualization to render only the visible items.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// The template for items that have not yet been rendered in virtualization mode.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }

    /// <summary>
    /// Activates the ComboBox feature in BitDropDown component.
    /// </summary>
    [Parameter] public bool Combo { get; set; }

    /// <summary>
    /// Shows the selected items like chips in the BitDropdown.
    /// </summary>
    [Parameter]
    public bool Chips
    {
        get => chips;
        set
        {
            if (chips == value) return;

            chips = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The callback that is called when a new item is on added Dynamic ComboBox mode.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnDynamicAdd { get; set; }

    /// <summary>
    /// It is allowed to add a new item in the ComboBox mode.
    /// </summary>
    [Parameter] public bool Dynamic { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm for checking existing an item in selected items in the ComboBox mode.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, bool>? ExistsSelectedItemFunction { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm for checking existing an item in items in the ComboBox mode.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, TItem>? FindItemFunction { get; set; }

    /// <summary>
    /// Prefix displayed before the BitDropdown contents. This is not included in the value.
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// Shows the custom prefix for BitDropdown.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// Suffix displayed after the BitDropdown contents. This is not included in the value. 
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.
    /// </summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>
    /// Shows the custom suffix for BitDropdown.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// The function for generating value in a custom item when a new item is on added Dynamic ComboBox mode.
    /// </summary>
    [Parameter] public Func<TItem, TValue>? DynamicValueGenerator { get; set; }


    /// <summary>
    /// A readonly list of the current selected items in multi-select mode.
    /// </summary>
    public IReadOnlyList<TItem> SelectedItems => IsMultiSelect ? _selectedItems : [];

    /// <summary>
    /// The current selected item in single-select mode.
    /// </summary>
    public TItem? SelectedItem => IsMultiSelect ? default : _selectedItems.FirstOrDefault();

    /// <summary>
    /// The ElementReference to the input element in combo-box mode.
    /// </summary>
    public ElementReference? InputElement => Combo ? _comboBoxInputRef : null;

    /// <summary>
    /// Gives focus to the input element in combo-box mode.
    /// </summary>
    public ValueTask FocusAsync() => Combo ? _comboBoxInputRef.FocusAsync() : ValueTask.CompletedTask;



    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
        StateHasChanged();
    }

    public async Task UnselectItem(TItem? item)
    {
        if (item is null) return;

        if (IsMultiSelect)
        {
            await HandleOnItemClick(item);
        }
        else
        {
            await HandleOnClearClick();
        }
    }

    internal void RegisterOption(BitDropdownOption<TValue> option)
    {
        Items.Add((option as TItem)!);

        UpdateSelectedItemsFromValues();

        StateHasChanged();
    }

    internal void UnregisterOption(BitDropdownOption<TValue> option)
    {
        var item = (option as TItem)!;
        Items.Remove(item);

        if (_selectedItems.Contains(item))
        {
            _selectedItems = _selectedItems.FindAll(i => i != item);
        }

        StateHasChanged();
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        if (GetItemType(item) != BitDropdownItemType.Normal) return;
        if (IsEnabled is false || GetIsEnabled(item) is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        await AddOrRemoveSelectedItem(item);

        StateHasChanged();
    }

    private async Task AddOrRemoveSelectedItem(TItem? item, bool addDynamic = false)
    {
        if (item is null) return;

        if (IsMultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

            var isSelected = GetIsSelected(item) is false;

            var tempValue = Values?.ToList() ?? [];

            if (isSelected)
            {
                tempValue.Add(GetValue(item));
            }
            else
            {
                tempValue.Remove(GetValue(item));
            }

            Values = tempValue;

            if (addDynamic && Combo && Dynamic && _selectedItems.Exists(si => EqualityComparer<TValue>.Default.Equals(GetValue(si), GetValue(item))) is false)
            {
                _selectedItems.Add(item);
            }

            await OnSelectItem.InvokeAsync(item);
        }
        else
        {
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            var oldSelectedItem = _selectedItems.FirstOrDefault();

            var isSameItemSelected = oldSelectedItem == item;

            CurrentValue = GetValue(item);

            if (addDynamic && Combo && Dynamic)
            {
                if (_selectedItems.Any())
                {
                    _selectedItems.Clear();
                }

                _selectedItems.Add(item);
            }

            await CloseCallout();

            await ClearSearchBox();

            await ClearComboBoxInput();

            if (isSameItemSelected && IsReselectable is false) return;

            await OnSelectItem.InvokeAsync(item);
        }

        await OnChange.InvokeAsync([.. _selectedItems]);
    }

    internal string GetItemWrapperCssClasses(TItem item)
    {
        var stringBuilder = new StringBuilder("bit-drp-iwr");

        if (GetIsSelected(item))
        {
            stringBuilder.Append(" bit-drp-chd");
        }

        if (GetIsEnabled(item) is false)
        {
            stringBuilder.Append(" bit-drp-ids");
        }

        return stringBuilder.ToString();
    }

    internal int? GetTotalItems()
    {
        if (Items is null) return null;

        if (_totalItems.HasValue is false)
        {
            _totalItems = Items.Count(i => GetItemType(i) == BitDropdownItemType.Normal);
        }

        return _totalItems.Value;
    }

    internal int? GetItemPosInSet(TItem item)
    {
        return null;

        // This code was removed because it caused performance issues when the items parameter had a large number of records.
        //return Items?.Where(i => GetItemType(i) == BitDropdownItemType.Normal).ToList().IndexOf(item) + 1;
    }

    internal bool GetIsSelected(TItem item)
    {
        var value = GetValue(item);

        if (value is null) return false;

        if (IsMultiSelect)
        {
            return Values?.Contains(value) ?? false;
        }
        else
        {
            return EqualityComparer<TValue>.Default.Equals(value, CurrentValue);
        }
    }


    protected override string RootElementClass => "bit-drp";
    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsRequired ? $"{RootElementClass}-req" : string.Empty);

        ClassBuilder.Register(() => _selectedItems?.Count > 0 ? $"{RootElementClass}-hvl" : string.Empty);

        ClassBuilder.Register(() => Chips ? $"{RootElementClass}-sch" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        ClassBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _dropdownId = $"Dropdown-{UniqueId}";
        _calloutId = $"{_dropdownId}-callout";
        _scrollContainerId = $"{_dropdownId}-scroll-container";
        _headerId = $"{_dropdownId}-header";
        _footerId = $"{_dropdownId}-footer";

        _labelId = $"{_dropdownId}-label";
        _dropdownTextContainerId = $"{_dropdownId}-text-container";

        if (ItemsProvider is null && Items is null)
        {
            Items = [];
        }

        _selectedItems ??= [];

        OnValueChanged += HandleOnValueChanged;

        _dotnetObj = DotNetObjectReference.Create(this);

        if (IsMultiSelect is false && EqualityComparer<TValue>.Default.Equals(CurrentValue, default) && EqualityComparer<TValue>.Default.Equals(DefaultValue, default) is false)
        {
            CurrentValue = DefaultValue;
        }

        if (IsMultiSelect && (Values is null || Values.Any() is false) && DefaultValues is not null && DefaultValues.Any())
        {
            Values = DefaultValues;
        }

        UpdateSelectedItemsFromValues();

        base.OnInitialized();
    }

    protected override bool TryParseValueFromString(string? value, out TValue? result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");

    protected override void RegisterFieldIdentifier()
    {
        if (IsMultiSelect)
        {
            RegisterFieldIdentifier(ValuesExpression, typeof(ICollection<TValue?>));
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



    private void UpdateSelectedItemsFromValues()
    {
        var items = ItemsProvider is not null ? _lastShowItems : Items;
        if (items is null) return;

        if (ItemsProvider is null)
        {
            _selectedItems.Clear();
        }

        var comparer = EqualityComparer<TValue>.Default;
        if (IsMultiSelect)
        {
            if (Values is not null)
            {
                foreach (var item in items)
                {
                    if (GetItemType(item) != BitDropdownItemType.Normal) continue;
                    if (Values.Any(v => comparer.Equals(v, GetValue(item))) is false) continue;

                    _selectedItems.Add(item);
                }
            }
        }
        else
        {
            var item = items.FirstOrDefault(i => comparer.Equals(GetValue(i), CurrentValue) && GetItemType(i) == BitDropdownItemType.Normal);

            if (item is not null)
            {
                if (_selectedItems.Any())
                {
                    _selectedItems.Clear();
                }

                _selectedItems.Add(item);
            }
        }

        ClassBuilder.Reset();
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        if (IsOpen is false) return;

        IsOpen = false;
        await ToggleCallout();

        StateHasChanged();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = true;
        await ToggleCallout();

        await OnClick.InvokeAsync(e);
        await FocusOnComboBoxInput();
        await FocusOnSearchBox();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args) => UpdateSelectedItemsFromValues();

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

        await _searchInputRef.FocusAsync();
    }

    private async Task ClearComboBoxInput()
    {
        if (IsEnabled is false) return;
        if (Combo is false) return;
        if (_searchText.HasNoValue()) return;

        _searchText = null;
    }

    private async ValueTask FocusOnComboBoxInput()
    {
        if (IsEnabled is false) return;
        if (IsOpen is false) return;
        if (Combo is false) return;
        if (_isResponsiveMode) return;

        await _comboBoxInputRef.FocusAsync();
    }

    private ICollection<TItem> GetSearchedItems()
    {
        if (Items is null) return [];

        return _searchText.HasNoValue()
                ? Items
                : SearchFunction is not null
                    ? SearchFunction.Invoke(Items, _searchText!)
                    : Items.Where(i => GetItemType(i) == BitDropdownItemType.Normal
                                       && (GetText(i)?.Contains(_searchText!, StringComparison.OrdinalIgnoreCase) ?? false))
                           .ToArray();
    }

    private string GetSearchBoxClasses()
    {
        var className = new StringBuilder("bit-drp-sb");

        if (_searchText.HasValue())
        {
            className.Append(" bit-drp-shv");
        }

        if (_inputSearchHasFocus)
        {
            className.Append(" bit-drp-shf");
        }

        return className.ToString();
    }

    private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{_labelId} {_dropdownTextContainerId}" : _dropdownTextContainerId;

    private async Task SearchVirtualized()
    {
        if (ItemsProvider is null) return;
        if (_virtualizeElement is null) return;

        await _virtualizeElement.RefreshDataAsync();
    }

    private async Task HandleOnClearClick()
    {
        if (IsEnabled is false) return;

        if (IsMultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

            Values = Array.Empty<TValue?>();
        }
        else
        {
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            CurrentValue = default;
        }

        UpdateSelectedItemsFromValues();

        await OnChange.InvokeAsync();
    }

    private async Task HandleOnAddItemComboClick()
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        await AddDynamicItem();

        _searchText = string.Empty;

        if (_isResponsiveMode && IsMultiSelect)
        {
            await _comboBoxInputResponsiveRef.FocusAsync();

            return;
        }

        await CloseCallout();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        _isResponsiveMode = await _js.ToggleCallout(_dotnetObj,
                                                    _dropdownId,
                                                    _calloutId,
                                                    IsOpen,
                                                    IsResponsive ? BitResponsiveMode.Panel : BitResponsiveMode.None,
                                                    DropDirection,
                                                    Dir is BitDir.Rtl,
                                                    _scrollContainerId,
                                                    ShowSearchBox && Combo is false ? 32 : 0,
                                                    CalloutHeaderTemplate is not null ? _headerId : "",
                                                    CalloutFooterTemplate is not null ? _footerId : "",
                                                    true,
                                                    RootElementClass);
    }

    private async ValueTask<ItemsProviderResult<TItem>> InternalItemsProvider(ItemsProviderRequest request)
    {
        if (ItemsProvider is null) return default;

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // TODO: Consider making this configurable, or smarter (e.g., doesn't delay on first call in a batch, then the amount
        // of delay increases if you rapidly issue repeated requests, such as when scrolling a long way)
        await Task.Delay(100);

        if (request.CancellationToken.IsCancellationRequested) return default;

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var providerRequest = new BitDropdownItemsProviderRequest<TItem>(request.StartIndex, request.Count, _searchText, request.CancellationToken);
        var providerResult = await ItemsProvider(providerRequest);

        if (request.CancellationToken.IsCancellationRequested) return default;

        _lastShowItems = [.. providerResult.Items];

        return new ItemsProviderResult<TItem>(providerResult.Items, providerResult.TotalItemCount);
    }

    internal string? GetAriaLabel(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.AriaLabel;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    internal string? GetClass(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.Class;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    internal string? GetId(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.Id;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.Id;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Id.Selector is not null)
        {
            return NameSelectors.Id.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Id.Name);
    }

    internal object? GetData(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.Data;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.Data;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Data.Selector is not null)
        {
            return NameSelectors.Data.Selector!(item);
        }

        return item.GetValueFromProperty<object?>(NameSelectors.Data.Name);
    }

    internal bool GetIsEnabled(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.IsEnabled;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    internal bool GetIsHidden(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.IsHidden;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.IsHidden;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsHidden.Selector is not null)
        {
            return NameSelectors.IsHidden.Selector!(item);
        }

        return item.GetValueFromProperty<bool>(NameSelectors.IsHidden.Name);
    }

    internal BitDropdownItemType GetItemType(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.ItemType;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.ItemType;
        }

        if (NameSelectors is null) return BitDropdownItemType.Normal;

        if (NameSelectors.ItemType.Selector is not null)
        {
            return NameSelectors.ItemType.Selector!(item);
        }

        return item.GetValueFromProperty<BitDropdownItemType>(NameSelectors.ItemType.Name);
    }

    internal string? GetStyle(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.Style;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal string? GetText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.Text;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    internal string? GetTitle(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.Title;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    internal TValue? GetValue(TItem? item)
    {
        if (item is null) return default;

        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.Value;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.Value;
        }

        if (NameSelectors is null) return default;

        if (NameSelectors.Value.Selector is not null)
        {
            return NameSelectors.Value.Selector!(item);
        }

        return item.GetValueFromProperty<TValue?>(NameSelectors.Value.Name);
    }

    private bool CheckInvalidForSelect(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.IsHidden is false &&
                   dropdownItem.IsEnabled &&
                   dropdownItem.ItemType == BitDropdownItemType.Normal &&
                   dropdownItem.Id.HasNoValue();
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.IsHidden is false &&
                   dropdownOption.IsEnabled &&
                   dropdownOption.ItemType == BitDropdownItemType.Normal &&
                   dropdownOption.Id.HasNoValue();
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsHidden.Selector is not null &&
            NameSelectors.IsEnabled.Selector is not null &&
            NameSelectors.ItemType.Selector is not null &&
            NameSelectors.Id.Selector is not null)
        {
            return NameSelectors.IsHidden.Selector!(item) is false &&
                   NameSelectors.IsEnabled.Selector!(item) &&
                   NameSelectors.ItemType.Selector!(item) == BitDropdownItemType.Normal &&
                   NameSelectors.Id.Selector!(item).HasNoValue();
        }

        return item.GetValueFromProperty<bool>(NameSelectors.IsHidden.Name) is false &&
               item.GetValueFromProperty<bool>(NameSelectors.IsEnabled.Name) &&
               item.GetValueFromProperty<BitDropdownItemType>(NameSelectors.ItemType.Name) == BitDropdownItemType.Normal &&
               item.GetValueFromProperty<string?>(NameSelectors.Id.Name).HasNoValue();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        if (eventArgs.Key == "Escape")
        {
            _searchText = string.Empty;

            await CloseCallout();
        }
        else if (eventArgs.Key == "Enter")
        {
            _searchText = await _js.GetProperty(_isResponsiveMode ? _comboBoxInputResponsiveRef : _comboBoxInputRef, "value");

            await AddDynamicItem();

            _searchText = string.Empty;

            if (_isResponsiveMode && IsMultiSelect) return;

            await CloseCallout();
        }
        else if (eventArgs.Key == "Backspace" && _searchText.HasNoValue())
        {
            await RemoveLastSelectedItem();
        }
    }

    private Task HandleOnClickUnselectItem(TItem? item) => UnselectItem(item);

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        _searchText = e.Value?.ToString();
        await SearchVirtualized();

        await OpenCallout();
    }

    private async Task OpenCallout()
    {
        if (IsOpen) return;
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = true;
        await ToggleCallout();
    }

    private async Task RemoveLastSelectedItem()
    {
        if (_selectedItems.Any() is false) return;

        if (IsMultiSelect)
        {
            var lastItem = _selectedItems.Last();
            await AddOrRemoveSelectedItem(lastItem);
        }
        else
        {
            await HandleOnClearClick();
        }
    }

    private async Task AddDynamicItem()
    {
        if (_searchText.HasNoValue()) return;

        if (_selectedItems.Count > 0)
        {
            var hasItem = ExistsSelectedItemFunction is not null ?
                          ExistsSelectedItemFunction.Invoke(_selectedItems, _searchText!) :
                          _selectedItems.Exists(i => GetText(i).HasValue() && _searchText!.Equals(GetText(i)!, StringComparison.OrdinalIgnoreCase));

            if (hasItem) return;
        }

        var searchItems = ItemsProvider is not null ? _lastShowItems : Items;
        if (searchItems is not null && searchItems.Count > 0)
        {
            var item = FindItemFunction is not null ?
                       FindItemFunction.Invoke(searchItems, _searchText!) :
                       (searchItems).FirstOrDefault(i => GetText(i).HasValue() && _searchText!.Equals(GetText(i)!, StringComparison.OrdinalIgnoreCase));

            if (item is not null && GetIsSelected(item) is false)
            {
                await AddOrRemoveSelectedItem(item);

                return;
            }
        }

        if (Dynamic is false) return;

        var text = _searchText;
        if (typeof(TItem) == typeof(BitDropdownItem<TValue>))
        {
            var dropdownItem = new BitDropdownItem<TValue>
            {
                Text = text,
                Title = text,
                Value = default,
                IsEnabled = true
            };

            if (DynamicValueGenerator is not null)
            {
                dropdownItem.Value = DynamicValueGenerator(dropdownItem as TItem);
            }
            await AddOrRemoveSelectedItem(dropdownItem as TItem, true);
            await OnDynamicAdd.InvokeAsync(dropdownItem as TItem);
        }
        else if (typeof(TItem) == typeof(BitDropdownOption<TValue>))
        {
            var dropdownOption = new BitDropdownOption<TValue>
            {
                Text = text,
                Title = text,
                Value = default,
                IsEnabled = true
            };

            if (DynamicValueGenerator is not null)
            {
                dropdownOption.Value = DynamicValueGenerator(dropdownOption as TItem);
            }
            await AddOrRemoveSelectedItem(dropdownOption as TItem, true);
            await OnDynamicAdd.InvokeAsync(dropdownOption as TItem);
        }
        else
        {
            var customItem = new TItem();

            if (NameSelectors?.TextSetter is not null)
            {
                NameSelectors.TextSetter(text!, customItem);
            }
            else if (NameSelectors is not null && NameSelectors.Text.Name.HasValue())
            {
                customItem.SetValueToProperty(NameSelectors.Text.Name, text!);
            }

            if (NameSelectors?.ValueSetter is not null && DynamicValueGenerator is not null)
            {
                TValue? value = DynamicValueGenerator(customItem);
                NameSelectors.ValueSetter(customItem, value);
            }
            else if (NameSelectors is not null && NameSelectors.Value.Name.HasValue() && DynamicValueGenerator is not null)
            {
                customItem.SetValueToProperty(NameSelectors.Value.Name, DynamicValueGenerator(customItem)!);
            }

            await AddOrRemoveSelectedItem(customItem, true);
            await OnDynamicAdd.InvokeAsync(customItem);
        }
    }

    private string? GetText()
    {
        return IsMultiSelect ? string.Join(MultiSelectDelimiter, _selectedItems.Select(GetText)) : GetText(_selectedItems.FirstOrDefault());
    }
}
