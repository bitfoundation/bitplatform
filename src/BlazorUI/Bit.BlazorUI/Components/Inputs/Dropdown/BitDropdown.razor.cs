using System.Text;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitDropdown<TItem, TValue> where TItem : class
{
    private bool IsOpenHasBeenSet;
    private bool ValuesHasBeenSet;
    private bool SelectedItemHasBeenSet;
    private bool SelectedItemsHasBeenSet;

    private bool isRtl;
    private bool isOpen;
    private bool isRequired;
    private List<TItem> selectedItems = new();
    private ICollection<TValue?>? values = Array.Empty<TValue?>();

    private List<TItem> _items = new();

    private string _dropdownId = string.Empty;
    private string _calloutId = string.Empty;
    private string _scrollContainerId = string.Empty;
    private string _headerId = string.Empty;
    private string _footerId = string.Empty;

    private string _labelId = string.Empty;
    private string _dropdownTextContainerId = string.Empty;

    private string? _text;
    private int? _totalItems;
    private string? _searchText;
    private bool _isValuesChanged;
    private bool _inputSearchHasFocus;
    private ElementReference _searchInputRef;
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
    /// The callback that is called when the search value changes.
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
    /// The selected item in single select mode. (two-way bound)
    /// </summary>
    [Parameter]
    public TItem? SelectedItem
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
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>
    /// The selected items in multi select mode. (two-way bound)
    /// </summary>
    [Parameter]
    public List<TItem> SelectedItems
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
    [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }

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
            if (value is not null && values!.All(value.Contains) && values!.Count() == value.Count()) return;

            values = value;
            _ = ValuesChanged.InvokeAsync(value);

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



    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
        StateHasChanged();
    }



    internal void RegisterOption(BitDropdownOption<TValue> option)
    {
        _items.Add((option as TItem)!);

        var comparer = EqualityComparer<TValue>.Default;
        if (IsMultiSelect)
        {
            if (ValuesHasBeenSet || SelectedItemsHasBeenSet)
            {
                Values ??= SelectedItems.Select(GetValue).ToArray();
                if (Values is not null && Values.Any())
                {
                    //var validValues = Values.Where(dv => _items.Any(i => comparer.Equals(dv, GetValue(i))));
                    //Values = validValues.ToArray();
                    var items = _items.FindAll(i => Values.Any(vv => comparer.Equals(vv, GetValue(i))));
                    ClearAllItemsIsSelected();
                    items.ForEach(i => SetIsSelected(i, true));
                    SelectedItems = items;
                    _text = string.Join(MultiSelectDelimiter, SelectedItems.Select(GetText));
                }
            }
            else if (DefaultValues is not null)
            {
                var validValues = DefaultValues.Where(dv => _items.Any(i => comparer.Equals(dv, GetValue(i))));
                Values = validValues.ToArray();
                if (Values is not null && Values.Any())
                {
                    var items = _items.FindAll(i => validValues.Any(vv => comparer.Equals(vv, GetValue(i))));
                    ClearAllItemsIsSelected();
                    items.ForEach(i => SetIsSelected(i, true));
                    SelectedItems = items;
                    _text = string.Join(MultiSelectDelimiter, SelectedItems.Select(GetText));
                }
            }
        }
        else
        {
            if (ValueHasBeenSet || SelectedItemHasBeenSet)
            {
                Value ??= GetValue(SelectedItem);
                if (Value is not null)
                {
                    var item = _items.FirstOrDefault(i => comparer.Equals(GetValue(i), Value));
                    if (item is not null)
                    {
                        CurrentValue = Value;
                        ClearAllItemsIsSelected();
                        SetIsSelected(item, true);
                        SelectedItem = item;
                        _text = GetText(SelectedItem) ?? string.Empty;
                    }
                }
            }
            else if (CurrentValue is null && DefaultValue is not null)
            {
                var item = _items.FirstOrDefault(i => comparer.Equals(GetValue(i), DefaultValue));
                if (item is not null)
                {
                    CurrentValue = DefaultValue;
                    ClearAllItemsIsSelected();
                    SetIsSelected(item, true);
                    SelectedItem = item;
                    _text = GetText(SelectedItem) ?? string.Empty;
                }
            }
        }

        StateHasChanged();
    }

    internal void UnregisterOption(BitDropdownOption<TValue> option)
    {
        var item = (option as TItem)!;
        _items.Remove(item);

        if (SelectedItems.Contains(item))
        {
            SelectedItems = SelectedItems.FindAll(i => i != item);
        }

        var value = GetValue(item);

        if (Values?.Contains(value) ?? false)
        {
            Values = Values.Where(v => EqualityComparer<TValue>.Default.Equals(v, value)).ToArray();
        }

        StateHasChanged();
    }



    internal async Task HandleOnItemClick(TItem item)
    {
        if (GetItemType(item) != BitDropdownItemType.Normal) return;
        if (IsEnabled is false || GetIsEnabled(item) is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        if (IsMultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

            _isValuesChanged = true;

            var isSelected = GetIsSelected(item) is false;

            SetIsSelected(item, isSelected);

            if (isSelected)
            {
                SelectedItems.Add(item);
            }
            else
            {
                SelectedItems.Remove(item);
            }

            ClassBuilder.Reset();

            _text = string.Join(MultiSelectDelimiter, SelectedItems.Select(GetText));

            CurrentValue = GetValue(SelectedItems.FirstOrDefault());

            Values = SelectedItems.Select(GetValue).ToArray();

            await OnSelectItem.InvokeAsync(item);
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
        else
        {
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            var oldSelectedItem = SelectedItem;

            var isSameItemSelected = oldSelectedItem == item;

            if (oldSelectedItem is not null)
            {
                SetIsSelected(oldSelectedItem, false);
            }

            SetIsSelected(item, true);

            SelectedItems.Clear();

            SelectedItems.Add(item);

            ClassBuilder.Reset();

            _text = GetText(item);

            CurrentValue = GetValue(item);

            await CloseCallout();

            await ClearSearchBox();

            if (isSameItemSelected && IsReselectable is false) return;

            await OnSelectItem.InvokeAsync(item);
            await SelectedItemChanged.InvokeAsync(item);
        }

        await OnChange.InvokeAsync(SelectedItems.ToArray());

        StateHasChanged();
    }

    internal string GetItemWrapperCssClasses(TItem item)
    {
        var stringBuilder = new StringBuilder(RootElementClass);

        stringBuilder.Append("-iwr");

        if (GetIsSelected(item))
        {
            stringBuilder.Append($" {RootElementClass}-chd");
        }

        if (GetIsEnabled(item) is false)
        {
            stringBuilder.Append($" {RootElementClass}-ids");
        }

        return stringBuilder.ToString();
    }

    internal int? GetTotalItems()
    {
        if (_items is null) return null;

        if (_totalItems.HasValue is false)
        {
            _totalItems = _items.FindAll(i => GetItemType(i) == BitDropdownItemType.Normal).Count;
        }

        return _totalItems.Value;
    }

    internal int? GetItemPosInSet(TItem item)
    {
        return _items?.FindAll(i => GetItemType(i) == BitDropdownItemType.Normal).IndexOf(item) + 1;
    }



    protected override string RootElementClass => "bit-drp";
    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsRequired ? $"{RootElementClass}-req" : string.Empty);

        ClassBuilder.Register(() => IsRtl ? $"{RootElementClass}-rtl" : string.Empty);

        ClassBuilder.Register(() => SelectedItems?.Count > 0 ? $"{RootElementClass}-hvl" : string.Empty);
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
            _items = new List<TItem>();
        }

        SelectedItems ??= new();

        _dotnetObj = DotNetObjectReference.Create(this);

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (ChildContent is not null) return;

        if (Items is not null)
        {
            _items = Items.ToList();
        }

        InitValues();
        InitSelectedItemsAndText();
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



    private void InitValues()
    {
        var comparer = EqualityComparer<TValue>.Default;

        if (Items is not null)
        {
            if (IsMultiSelect)
            {
                if (Values is null || Values.Any() is false) return;

                Values = Values.Where(v => _items.Any(i => comparer.Equals(GetValue(i), v))).ToArray();
            }
            else
            {
                if (CurrentValue is null) return;
                if (_items.Any(i => comparer.Equals(GetValue(i), CurrentValue))) return;

                CurrentValue = default;
            }
        }
        else if (ItemsProvider is not null && (SelectedItems?.Any() ?? false))
        {
            if (IsMultiSelect)
            {
                bool isEqual = SelectedItems.Select(GetValue).OrderBy(i => i).SequenceEqual(Values.OrderBy(v => v));
                if (isEqual) return;

                Values = SelectedItems.Select(GetValue).ToArray();
            }
            else
            {
                if (comparer.Equals(CurrentValue, GetValue(SelectedItem))) return;

                CurrentValue = GetValue(SelectedItem);
            }
        }
    }

    private void InitSelectedItemsAndText()
    {
        var comparer = EqualityComparer<TValue>.Default;

        if (Items is not null)
        {
            SelectedItems.Clear();
            ClearAllItemsIsSelected();

            if (IsMultiSelect)
            {
                if (ValuesHasBeenSet || _isValuesChanged)
                {
                    foreach (var item in _items)
                    {
                        if (GetItemType(item) != BitDropdownItemType.Normal) continue;
                        if (Values is null || Values.Any(v => comparer.Equals(v, GetValue(item))) is false) continue;

                        SetIsSelected(item, true);
                        SelectedItems.Add(item);
                    }
                }
                else if (DefaultValues is not null && DefaultValues.Any())
                {
                    foreach (var item in _items)
                    {
                        if (GetItemType(item) != BitDropdownItemType.Normal) continue;
                        if (DefaultValues!.Any(v => comparer.Equals(v, GetValue(item))) is false) continue;

                        SetIsSelected(item, true);
                        SelectedItems.Add(item);
                    }
                }
                _ = SelectedItemsChanged.InvokeAsync(SelectedItems);
            }
            else
            {
                if (CurrentValue is not null)
                {
                    var item = _items.Find(i => comparer.Equals(GetValue(i), CurrentValue) && GetItemType(i) == BitDropdownItemType.Normal);
                    if (item is not null)
                    {
                        SetIsSelected(item, true);
                        SelectedItems.Add(item);
                    }
                }
                else if (DefaultValue is not null)
                {
                    var item = _items.Find(i => comparer.Equals(GetValue(i), DefaultValue) && GetItemType(i) == BitDropdownItemType.Normal)!;
                    if (item is not null)
                    {
                        SetIsSelected(item, true);
                        SelectedItems.Add(item);
                    }
                }
                _ = SelectedItemChanged.InvokeAsync(SelectedItem);
            }
            ClassBuilder.Reset();
        }

        if (SelectedItems.Any())
        {
            if (IsMultiSelect)
            {
                _text = string.Join(MultiSelectDelimiter, SelectedItems.Select(GetText));
            }
            else
            {
                _text = GetText(SelectedItem) ?? string.Empty;
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
        await FocusOnSearchBox();
    }

    private void ClearAllItemsIsSelected() => _items?.ForEach(i => SetIsSelected(i, false));

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

    private List<TItem> GetSearchedItems()
    {
        return _searchText.HasNoValue()
                ? _items
                : SearchFunction is not null
                    ? [.. SearchFunction.Invoke(_items, _searchText!)]
                    : _items.FindAll(i => GetItemType(i) == BitDropdownItemType.Normal
                                          && (GetText(i)?.Contains(_searchText!, StringComparison.OrdinalIgnoreCase) ?? false));
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

    private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{_labelId} {_dropdownTextContainerId}" : $"{_dropdownTextContainerId}";

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

            SelectedItems.ForEach(i => SetIsSelected(i, false));
            SelectedItems.Clear();
            ClassBuilder.Reset();

            Values = Array.Empty<TValue?>();
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
        else
        {
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            if (SelectedItems.Count > 0)
            {
                SetIsSelected(SelectedItem!, false);
                SelectedItems.Clear();
                ClassBuilder.Reset();
            }

            await SelectedItemChanged.InvokeAsync(SelectedItem);
        }

        _text = string.Empty;
        CurrentValue = default;

        StateHasChanged();
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleCallout(_dotnetObj,
                                _dropdownId,
                                _calloutId,
                                IsOpen,
                                IsResponsive ? BitResponsiveMode.Panel : BitResponsiveMode.None,
                                DropDirection,
                                IsRtl,
                                _scrollContainerId,
                                ShowSearchBox ? 32 : 0,
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

        var comparer = EqualityComparer<TValue>.Default;
        foreach (var item in providerResult.Items)
        {
            SetIsSelected(item, GetItemType(item) == BitDropdownItemType.Normal && SelectedItems.Any(si => comparer.Equals(GetValue(si), GetValue(item))));
        }

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

    internal bool GetIsSelected(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.IsSelected;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.IsSelected;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSelected.Selector is not null)
        {
            return NameSelectors.IsSelected.Selector!(item);
        }

        return item.GetValueFromProperty<bool>(NameSelectors.IsSelected.Name);
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



    private void SetIsSelected(TItem item, bool value)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            dropdownItem.IsSelected = value;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            dropdownOption.IsSelected = value;
        }

        if (NameSelectors is null) return;

        // we need to think about a proper solution to use Selector for setting the value!
        //if (NameSelectors.IsSelected.Selector is not null)
        //{
        //    NameSelectors.IsSelected.Selector!(item);
        //}

        item.SetValueToProperty(NameSelectors.IsSelected.Name, value);
    }
}
