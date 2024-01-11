using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

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
    private bool showChips;
    private List<TItem> selectedItems = [];
    private ICollection<TValue?>? values = Array.Empty<TValue?>();

    private List<TItem> _items = [];
    private List<TItem> _lastShowItems = [];

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
    private string? _selectedItemId;
    private bool _valueTypeIsString;
    private bool _inputSearchHasFocus;
    private ElementReference _searchInputRef;
    private ElementReference _comboBoxInputRef;
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
            if (selectedItems.TrueForAll(value.Contains) && selectedItems.Count == value.Count) return;

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
            if (value is not null && values!.All(value.Contains) && values!.Count == value.Count) return;

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

    /// <summary>
    /// Activates the ComboBox feature in BitDropDown component.
    /// </summary>
    [Parameter] public bool IsComboBox { get; set; }

    /// <summary>
    /// Shows the selected items like chips in the BitDropdown.
    /// </summary>
    [Parameter]
    public bool ShowChips
    {
        get => showChips;
        set
        {
            if (showChips == value) return;

            showChips = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The callback that is called when a new item is added in ComboBox mode.
    /// </summary>
    [Parameter] public EventCallback<string> OnAddedComboBox { get; set; }

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


    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        _selectedItemId = null;
        IsOpen = false;
        StateHasChanged();
    }

    [JSInvokable("SetSelectedItemId")]
    public void SetSelectedItemId(string? id)
    {
        if (id.HasNoValue()) return;

        _selectedItemId = id;
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
            await HandleOnClickClear();
        }
    }

    internal void RegisterOption(BitDropdownOption<TValue> option)
    {
        if (IsComboBox && option.ItemType == BitDropdownItemType.Normal && option.Id.HasNoValue())
        {
            option.Id = Guid.NewGuid().ToString();
        }

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
                    var item = _items.Find(i => comparer.Equals(GetValue(i), Value));
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
                var item = _items.Find(i => comparer.Equals(GetValue(i), DefaultValue));
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

            await ClearComboBoxInput();

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

        if (IsComboBox && _selectedItemId.HasValue() && GetIsHidden(item) is false && GetIsEnabled(item) && GetItemType(item) == BitDropdownItemType.Normal)
        {
            var id = GetId(item);
            if (id.HasValue() && id == _selectedItemId)
            {
                stringBuilder.Append($" {RootElementClass}-sli");
            }
        }

        return stringBuilder.ToString();
    }

    internal string? GetItemCssClasses(TItem item)
    {
        if (IsComboBox && _selectedItemId.HasValue() && GetIsHidden(item) is false && GetIsEnabled(item) && GetItemType(item) == BitDropdownItemType.Normal)
        {
            var id = GetId(item);
            if (id.HasValue() && id == _selectedItemId)
            {
                return $"{RootElementClass}-sli";
            }
        }

        return null;
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

        ClassBuilder.Register(() => ShowChips ? $"{RootElementClass}-sch" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        ClassBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        Type _valueType = typeof(TValue);
        _valueType = Nullable.GetUnderlyingType(_valueType) ?? _valueType;

        _valueTypeIsString = _valueType == typeof(string);

        _dropdownId = $"Dropdown-{UniqueId}";
        _calloutId = $"{_dropdownId}-callout";
        _scrollContainerId = $"{_dropdownId}-scroll-container";
        _headerId = $"{_dropdownId}-header";
        _footerId = $"{_dropdownId}-footer";

        _labelId = $"{_dropdownId}-label";
        _dropdownTextContainerId = $"{_dropdownId}-text-container";

        if (ItemsProvider is null && Items is null)
        {
            _items = [];
        }

        SelectedItems ??= [];

        _dotnetObj = DotNetObjectReference.Create(this);

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (ChildContent is not null) return;

        if (Items is not null)
        {
            _items = [.. Items];

            if (IsComboBox)
            {
                _items.ForEach(item =>
                {
                    if (GetItemType(item) == BitDropdownItemType.Normal && GetId(item).HasNoValue())
                    {
                        SetId(item);
                    }
                });
            }
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

                Values = Values.Where(v => _items.Exists(i => comparer.Equals(GetValue(i), v))).
                            Union(IsComboBox && Dynamic ? SelectedItems.Where(GetIsSelectedAndIsDynamic).Select(GetValue) : Array.Empty<TValue>()).
                            ToArray();
            }
            else
            {
                if (CurrentValue is null) return;
                if (_items.Exists(i => comparer.Equals(GetValue(i), CurrentValue))) return;
                if (SelectedItems.Exists(GetIsSelectedAndIsDynamic)) return;

                CurrentValue = default;
            }
        }
        else if (ItemsProvider is not null && (SelectedItems?.Any() ?? false))
        {
            if (IsMultiSelect)
            {
                bool isEqual = SelectedItems.Select(GetValue).OrderBy(i => i).SequenceEqual(Values!.OrderBy(v => v));
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
            if (IsComboBox && Dynamic)
            {
                SelectedItems.RemoveAll(i => GetIsDynamic(i) is false);
            }
            else
            {
                SelectedItems.Clear();
            }
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

        _selectedItemId = null;
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

    private async Task ClearComboBoxInput()
    {
        if (IsEnabled is false) return;
        if (IsComboBox is false) return;
        if (_searchText.HasNoValue()) return;

        _searchText = null;
    }

    private async ValueTask FocusOnComboBoxInput()
    {
        if (IsEnabled is false) return;
        if (IsComboBox is false) return;
        if (IsOpen is false) return;

        await _comboBoxInputRef.FocusAsync();
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

    private async Task HandleOnClickClear()
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
            SetIsSelected(item, GetItemType(item) == BitDropdownItemType.Normal && SelectedItems.Exists(si => comparer.Equals(GetValue(si), GetValue(item))));
        }

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

    private bool GetIsDynamic(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.IsDynamic;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.IsDynamic;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsDynamic.Selector is not null)
        {
            return NameSelectors.IsDynamic.Selector!(item);
        }

        return item.GetValueFromProperty<bool>(NameSelectors.IsDynamic.Name);
    }

    private bool GetIsSelectedAndIsDynamic(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return dropdownItem.IsSelected && dropdownItem.IsDynamic;
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return dropdownOption.IsSelected && dropdownOption.IsDynamic;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSelected.Selector is not null && NameSelectors.IsDynamic.Selector is not null)
        {
            return NameSelectors.IsSelected.Selector!(item) && NameSelectors.IsDynamic.Selector!(item);
        }

        return item.GetValueFromProperty<bool>(NameSelectors.IsSelected.Name) && item.GetValueFromProperty<bool>(NameSelectors.IsDynamic.Name);
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

    private void SetId(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            dropdownItem.Id = Guid.NewGuid().ToString();
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            dropdownOption.Id = Guid.NewGuid().ToString();
        }

        if (NameSelectors is null) return;

        // we need to think about a proper solution to use Selector for setting the value!
        //if (NameSelectors.Id.Selector is not null)
        //{
        //    NameSelectors.Id.Selector!(item);
        //}

        item.SetValueToProperty(NameSelectors.Id.Name, Guid.NewGuid().ToString());
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
            _searchText = await _js.GetProperty(_comboBoxInputRef, "value");

            await AddDynamicItem();

            _searchText = string.Empty;
            await CloseCallout();
        }
        else if (eventArgs.Key == "Backspace")
        {
            if (_searchText.HasNoValue())
            {
                await RemoveLastSelectedItem();
            }
        }
        else if (eventArgs.Key == "ArrowUp")
        {
            await ChangeSelectedItem(true);
        }
        else if (eventArgs.Key == "ArrowDown")
        {
            await ChangeSelectedItem(false);
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
        if (SelectedItems.Any() is false) return;

        if (IsMultiSelect)
        {
            var lastItem = SelectedItems.Last();
            await HandleOnItemClick(lastItem);
        }
        else
        {
            await HandleOnClickClear();
        }
    }

    private async Task AddDynamicItem()
    {
        if (_selectedItemId.HasValue())
        {
            TItem? selectItem = null;
            if (ItemsProvider is not null)
            {
                selectItem = _lastShowItems.Find(i => GetId(i) == _selectedItemId!);
            }
            else if (_items is not null)
            {
                selectItem = _items.Find(i => GetId(i) == _selectedItemId!);
            }

            if (selectItem is not null)
            {
                await HandleOnItemClick(selectItem);

                return;
            }
        }

        if (_searchText.HasNoValue()) return;

        if (SelectedItems.Count > 0)
        {
            var hasItem = ExistsSelectedItemFunction is not null ?
                        ExistsSelectedItemFunction.Invoke(SelectedItems, _searchText!) :
                        SelectedItems.Exists(i => GetText(i).HasValue() && _searchText!.Equals(GetText(i)!, StringComparison.OrdinalIgnoreCase));

            if (hasItem) return;
        }

        var searchItems = ItemsProvider is not null ? _lastShowItems : _items;
        if (searchItems is not null && searchItems.Count > 0)
        {
            var item = FindItemFunction is not null ?
                    FindItemFunction.Invoke(searchItems, _searchText!) :
                    (searchItems).Find(i => GetText(i).HasValue() && _searchText!.Equals(GetText(i)!, StringComparison.OrdinalIgnoreCase));

            if (item is not null && GetIsSelected(item) is false)
            {
                await HandleOnItemClick(item);

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
                Value = _valueTypeIsString ? text.ConvertTo<TValue>() : default,
                IsEnabled = true,
                IsDynamic = true
            };

            await HandleOnItemClick(dropdownItem as TItem);
        }
        else if (typeof(TItem) == typeof(BitDropdownOption<TValue>))
        {
            var dropdownOption = new BitDropdownOption<TValue>
            {
                Text = text,
                Title = text,
                Value = _valueTypeIsString ? text.ConvertTo<TValue>() : default,
                IsEnabled = true,
                IsDynamic = true
            };

            await HandleOnItemClick(dropdownOption as TItem);
        }

        await OnAddedComboBox.InvokeAsync(text);
    }

    private async Task ChangeSelectedItem(bool isArrowUp)
    {
        if (IsOpen is false) return;
        if (ItemsProvider is not null && _lastShowItems.Exists(CheckInvalidForSelect)) return;
        if (_items is not null && _items.Exists(CheckInvalidForSelect)) return;

        await _js.InvokeVoidAsync("BitDropdown.changeSelectedItem", _dotnetObj, _comboBoxInputRef, _scrollContainerId, IsMultiSelect, isArrowUp);
    }
}
