using System.Text;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A dropdown is a list in which the selected item is always visible while other items are 
/// visible on demand by clicking a dropdown button. Dropdowns are typically used for forms.
/// </summary>
public partial class BitDropdown<TItem, TValue> : BitInputBase<TValue> where TItem : class, new()
{
    private int? _providerTotalItems;
    private string? _searchText;
    private int _optionsVersion;
    private int _searchedItemsCacheVersion = -1;
    private string? _searchedItemsCacheKey;
    private List<TItem>? _searchedItems;
    private HashSet<TItem>? _searchedItemsCache;
    private string? _positionsCacheKey;
    private int _positionsCacheVersion = -1;
    private Dictionary<TItem, int>? _itemPositions;
    private int _selectionVersion;
    private string? _displayItemsCacheKey;
    private int _displayItemsCacheVersion = -1;
    private int _displayItemsSelectionVersion = -1;
    private List<TItem>? _displayItems;
    private bool _isResponsiveMode;
    private bool _inputSearchHasFocus;
    private bool _inputComboHasFocus;
    private List<TItem> _selectedItems = [];
    private List<TItem> _lastShownItems = [];
    private Virtualize<TItem>? _virtualizeElement;
    private string _scrollContainerId = string.Empty;
    private string _dropdownTextContainerId = string.Empty;
    private DotNetObjectReference<BitDropdown<TItem, TValue>> _dotnetObj = default!;

    private readonly BitInputRateLimiter<ChangeEventArgs> _rateLimiter = new();

    private string _labelId = string.Empty;
    private string _headerId = string.Empty;
    private string _footerId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _dropdownId = string.Empty;

    private ElementReference _searchInputRef;
    private ElementReference _comboBoxInputRef;
    private ElementReference _dropdownWrapperRef;
    private ElementReference _comboBoxInputResponsiveRef;

    private string _typeAheadBuffer = string.Empty;
    private DateTimeOffset _lastTypeAheadStamp;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Clears the typed search text after each selection in the multi select ComboBox mode, so the next
    /// item is picked from the full list instead of from the previous filter.
    /// </summary>
    [Parameter] public bool AutoClearSearch { get; set; }

    /// <summary>
    /// Enables auto-focusing of the SearchBox input when the callout is open.
    /// </summary>
    [Parameter] public bool AutoFocusSearchBox { get; set; }

    /// <summary>
    /// Removes the already selected items from the callout, which suits a multi select dropdown whose
    /// selection is visible as chips and whose list is therefore only about what is left to pick.
    /// </summary>
    [Parameter] public bool HideSelectedItems { get; set; }

    /// <summary>
    /// Highlights the part of the item text that matched the current search text in the callout.
    /// Only applies to the default item rendering, not to a custom <see cref="ItemTemplate"/>.
    /// </summary>
    [Parameter] public bool HighlightSearch { get; set; }

    /// <summary>
    /// Custom template to render as a footer in the callout.
    /// </summary>
    [Parameter] public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// Custom template to render as a header in the callout.
    /// </summary>
    [Parameter] public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <summary>
    /// The icon of the chevron down element of the dropdown.
    /// Takes precedence over <see cref="CaretDownIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CaretDownIconName"/> instead.
    /// </summary>
    /// <example>
    /// Bootstrap: CaretDownIcon="BitIconInfo.Bi("chevron-down")"
    /// FontAwesome: CaretDownIcon="BitIconInfo.Fa("solid chevron-down")"
    /// Custom CSS: CaretDownIcon="BitIconInfo.Css("my-chevron-class")"
    /// </example>
    [Parameter] public BitIconInfo? CaretDownIcon { get; set; }

    /// <summary>
    /// The icon name of the chevron down element of the dropdown from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="CaretDownIcon"/> instead.
    /// </summary>
    [Parameter] public string? CaretDownIconName { get; set; }

    /// <summary>
    /// The custom template for the chevron down element of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment? CaretDownTemplate { get; set; }

    /// <summary>
    /// The content of the Dropdown, a list of BitDropdownOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Shows the selected items like chips in the BitDropdown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Chips { get; set; }

    /// <summary>
    /// The icon of the remove button in the chips display.
    /// Takes precedence over <see cref="ChipsRemoveIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ChipsRemoveIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? ChipsRemoveIcon { get; set; }

    /// <summary>
    /// The icon name of the remove button in the chips display from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ChipsRemoveIcon"/> instead.
    /// </summary>
    [Parameter] public string? ChipsRemoveIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitDropdown.
    /// </summary>
    [Parameter] public BitDropdownClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the dropdown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The icon of the clear button of the dropdown.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ClearButtonIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The icon name of the clear button of the dropdown from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ClearButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// Activates the ComboBox feature in BitDropDown component.
    /// </summary>
    [Parameter] public bool Combo { get; set; }

    /// <summary>
    /// The icon of the add button in the responsive ComboBox mode.
    /// Takes precedence over <see cref="ComboBoxAddButtonIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ComboBoxAddButtonIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? ComboBoxAddButtonIcon { get; set; }

    /// <summary>
    /// The icon name of the add button in the responsive ComboBox mode from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ComboBoxAddButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? ComboBoxAddButtonIconName { get; set; }

    /// <summary>
    /// The debounce time in milliseconds for the search and combo box inputs (applied when Immediate is enabled).
    /// </summary>
    [Parameter] public int DebounceTime { get; set; }

    /// <summary>
    /// The default values that will be initially used to set selected items in multi select mode if the Values parameter is not set.
    /// </summary>
    [Parameter] public IEnumerable<TValue?>? DefaultValues { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// It is allowed to add a new item in the ComboBox mode.
    /// </summary>
    [Parameter] public bool Dynamic { get; set; }

    /// <summary>
    /// The function for generating value in a custom item when a new item is on added Dynamic ComboBox mode.
    /// </summary>
    [Parameter] public Func<TItem?, TValue>? DynamicValueGenerator { get; set; }

    /// <summary>
    /// The custom template to render in the callout when there is no item to show.
    /// </summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// The text to render in the callout when there is no item to show.
    /// </summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm for checking existing an item in selected items in the ComboBox mode.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, bool>? ExistsSelectedItemFunction { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm for checking existing an item in items in the ComboBox mode.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, TItem>? FindItemFunction { get; set; }

    /// <summary>
    /// Enables fit-content value for the width of the root element.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// The custom template for rendering the header items of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// The initial items that will be used to set selected items when using an ItemProvider.
    /// </summary>
    [Parameter] public IEnumerable<TItem>? InitialSelectedItems { get; set; }

    /// <summary>
    /// Searches the items immediately as the user types in the search box or combo box input (based on the 'oninput' HTML event).
    /// </summary>
    [Parameter] public bool Immediate { get; set; }

    /// <summary>
    /// Shows a loading indicator in the callout (and in place of the caret down element) while the items are being fetched.
    /// The dropdown stays interactive, so the user can still open the callout and see the loading state.
    /// </summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>
    /// Determines the opening state of the callout. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The icon of the check mark in the multi-select items.
    /// Takes precedence over <see cref="ItemCheckIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ItemCheckIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? ItemCheckIcon { get; set; }

    /// <summary>
    /// The icon name of the check mark in the multi-select items from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ItemCheckIcon"/> instead.
    /// </summary>
    [Parameter] public string? ItemCheckIconName { get; set; }

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
    /// The delay in milliseconds before an <see cref="ItemsProvider"/> request is issued, which collapses
    /// the bursts of requests produced by fast scrolling and typing into a single one.
    /// </summary>
    [Parameter] public int ItemsProviderDebounceTime { get; set; } = 100;

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
    /// The custom template to render in the callout in place of the items while <see cref="IsLoading"/> is enabled.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The text to render in the callout in place of the items while <see cref="IsLoading"/> is enabled.
    /// </summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>
    /// The maximum number of items that can be selected in multi select mode. Zero or null means no limit.
    /// </summary>
    [Parameter] public int? MaxSelectedItems { get; set; }

    /// <summary>
    /// The maximum number of selected items rendered in the dropdown itself. Beyond it, the chips display
    /// collapses the extra ones into an overflow indicator and the text display switches to a summary.
    /// Zero or null renders every selected item.
    /// </summary>
    [Parameter] public int? MaxDisplayedItems { get; set; }

    /// <summary>
    /// The number of characters the search text must reach before the items get filtered.
    /// While the search text is shorter, the full list is shown and no search is performed.
    /// </summary>
    [Parameter] public int MinSearchLength { get; set; }

    /// <summary>
    /// Enables the multi select mode.
    /// </summary>
    [Parameter] public bool MultiSelect { get; set; }

    /// <summary>
    /// The delimiter for joining the values to create the text of the dropdown in multi select mode.
    /// </summary>
    [Parameter] public string MultiSelectDelimiter { get; set; } = ", ";

    /// <summary>
    /// The composite format of the overflow indicator that stands for the selected items beyond
    /// <see cref="MaxDisplayedItems"/> in the chips display, for example "+{0}".
    /// </summary>
    [Parameter] public string? OverflowTextFormat { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitDropdownNameSelectors<TItem, TValue>? NameSelectors { get; set; }

    /// <summary>
    /// Removes the border from the root element.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// The custom template to render in the callout when the current search has no result.
    /// Falls back to the <see cref="EmptyTemplate"/> when not set.
    /// </summary>
    [Parameter] public RenderFragment? NoResultsTemplate { get; set; }

    /// <summary>
    /// The text to render in the callout when the current search has no result.
    /// Falls back to the <see cref="EmptyText"/> when not set.
    /// </summary>
    [Parameter] public string? NoResultsText { get; set; }

    /// <summary>
    /// The callback that is called when the selection gets cleared by the clear button.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// The click callback for the dropdown.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// The callback that is called when the callout gets closed.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// The callback that is called when a new item is on added Dynamic ComboBox mode.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnDynamicAdd { get; set; }

    /// <summary>
    /// The callback that is called when the callout gets opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// The callback that called when an item gets selected.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// The callback that called when an item gets selected.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// The callback that called when selected items change.
    /// </summary>
    [Parameter] public EventCallback<IEnumerable<TValue?>> OnValuesChange { get; set; }

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
    /// Prefix displayed before the BitDropdown contents. This is not included in the value.
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// Shows the custom prefix for BitDropdown.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// Disables automatic setting of the callout width and preserves its original width.
    /// </summary>
    [Parameter] public bool PreserveCalloutWidth { get; set; }

    /// <summary>
    /// Enables calling the select events when the same item is selected in single select mode.
    /// </summary>
    [Parameter] public bool Reselectable { get; set; }

    /// <summary>
    /// Enables the responsive mode of the component for small screens.
    /// </summary>
    [Parameter] public bool Responsive { get; set; }

    /// <summary>
    /// The icon of the close button in the responsive mode callout.
    /// Takes precedence over <see cref="ResponsiveCloseIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ResponsiveCloseIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? ResponsiveCloseIcon { get; set; }

    /// <summary>
    /// The icon name of the close button in the responsive mode callout from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ResponsiveCloseIcon"/> instead.
    /// </summary>
    [Parameter] public string? ResponsiveCloseIconName { get; set; }

    /// <summary>
    /// The icon of the clear icon in the SearchBox.
    /// Takes precedence over <see cref="SearchBoxClearIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="SearchBoxClearIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? SearchBoxClearIcon { get; set; }

    /// <summary>
    /// The icon name of the clear icon in the SearchBox from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="SearchBoxClearIcon"/> instead.
    /// </summary>
    [Parameter] public string? SearchBoxClearIconName { get; set; }

    /// <summary>
    /// The icon of the search icon in the SearchBox.
    /// Takes precedence over <see cref="SearchBoxIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="SearchBoxIconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? SearchBoxIcon { get; set; }

    /// <summary>
    /// The icon name of the search icon in the SearchBox from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="SearchBoxIcon"/> instead.
    /// </summary>
    [Parameter] public string? SearchBoxIconName { get; set; }

    /// <summary>
    /// The placeholder text of the SearchBox input.
    /// </summary>
    [Parameter] public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm.
    /// Takes precedence over <see cref="SearchMode"/>, which only configures the default algorithm.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, ICollection<TItem>>? SearchFunction { get; set; }

    /// <summary>
    /// Determines how the text of an item is matched against the search text by the default
    /// (case-insensitive) search algorithm. Ignored when a <see cref="SearchFunction"/> is provided.
    /// </summary>
    [Parameter] public BitDropdownSearchMode SearchMode { get; set; } = BitDropdownSearchMode.Contains;

    /// <summary>
    /// The composite format of the message announced to screen readers with the number of items the
    /// current search produced, for example "{0} results available". Defaults to the English message.
    /// </summary>
    [Parameter] public string? SearchResultsText { get; set; }

    /// <summary>
    /// The text of the select all item in multi select mode.
    /// </summary>
    [Parameter] public string? SelectAllText { get; set; }

    /// <summary>
    /// The composite format that replaces the joined item texts in the dropdown once more than
    /// <see cref="MaxDisplayedItems"/> items are selected, for example "{0} items selected".
    /// </summary>
    [Parameter] public string? SelectedItemsTextFormat { get; set; }

    /// <summary>
    /// Shows the clear button when an item is selected.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Shows the SearchBox element in the callout.
    /// </summary>
    [Parameter] public bool ShowSearchBox { get; set; }

    /// <summary>
    /// Shows the select all item in the callout in multi select mode.
    /// </summary>
    [Parameter] public bool ShowSelectAll { get; set; }

    /// <summary>
    /// The size of the dropdown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDropdown.
    /// </summary>
    [Parameter] public BitDropdownClassStyles? Styles { get; set; }

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
    /// The custom template for the text of the dropdown.
    /// </summary>
    [Parameter] public RenderFragment<BitDropdown<TItem, TValue>>? TextTemplate { get; set; }

    /// <summary>
    /// The throttle time in milliseconds for the search and combo box inputs (applied when Immediate is enabled).
    /// </summary>
    [Parameter] public int ThrottleTime { get; set; }

    /// <summary>
    /// The title to show when the mouse hovers over the dropdown.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Removes the default background color from the root element.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Transparent { get; set; }

    /// <summary>
    /// The values of the selected items in multi select mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetValues))]
    public IEnumerable<TValue?>? Values { get; set; }

    [Parameter] public Expression<Func<IEnumerable<TValue?>?>>? ValuesExpression { get; set; }

    /// <summary>
    /// Enables virtualization to render only the visible items.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// The template for items that have not yet been rendered in virtualization mode.
    /// </summary>
    [Parameter] public RenderFragment<PlaceholderContext>? VirtualizePlaceholder { get; set; }



    /// <summary>
    /// A readonly list of the current selected items in multi-select mode.
    /// </summary>
    public IReadOnlyList<TItem> SelectedItems => MultiSelect ? _selectedItems : [];

    /// <summary>
    /// The current selected item in single-select mode.
    /// </summary>
    public TItem? SelectedItem => MultiSelect ? default : _selectedItems.FirstOrDefault();

    /// <summary>
    /// The ElementReference to the combo input element.
    /// </summary>
    public ElementReference? ComboInputElement => Combo
                                                    ? _isResponsiveMode
                                                        ? _comboBoxInputResponsiveRef
                                                        : _comboBoxInputRef
                                                    : null;

    /// <summary>
    /// Gives focus to the combo input element.
    /// </summary>
    public ValueTask FocusComboInputAsync() => Combo
                                                ? (_isResponsiveMode
                                                    ? _comboBoxInputResponsiveRef
                                                    : _comboBoxInputRef).FocusAsync()
                                                : ValueTask.CompletedTask;

    /// <summary>
    /// The ElementReference to the search input element.
    /// </summary>
    public ElementReference? SearchInputElement => _searchInputRef;

    /// <summary>
    /// Gives focus to the search input element.
    /// </summary>
    public ValueTask FocusSearchInputAsync() => _searchInputRef.FocusAsync();



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        await InvokeAsync(StateHasChanged);
    }

    // The swipe interop of the responsive panel expects all four callbacks to exist, but only the
    // close one has a behavior here: the panel is dismissed, it does not follow the finger.
    [JSInvokable("OnStart")]
    public Task _OnStart(decimal startX, decimal startY) => Task.CompletedTask;

    [JSInvokable("OnMove")]
    public Task _OnMove(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnEnd")]
    public Task _OnEnd(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await CloseCallout();
        await InvokeAsync(StateHasChanged);
    }

    public async Task UnselectItem(TItem? item)
    {
        if (item is null) return;

        if (MultiSelect)
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
        Items!.Add((option as TItem)!);
        _searchedItems = null;
        _itemPositions = null;
        _displayItems = null;
        _searchedItemsCache = null;

        UpdateSelectedItemsFromValues();

        StateHasChanged();
    }

    // Each option calls this during the dropdown's render cycle to decide whether its item is visible
    // for the current search. Options are refreshed explicitly (RefreshOptions in OnParametersSet and
    // after the search/selection mutations), and the search results are cached per search text; that
    // cache is reset in OnParametersSet so a change to Items cannot reuse results from a previous set.
    internal bool ShouldRenderOptionItem(TItem item)
    {
        if (HideSelectedItems && GetItemType(item) == BitDropdownItemType.Normal && GetIsSelected(item)) return false;

        if (SearchText is null) return true;

        // Every option asks this during the render cycle, so the searched items are looked up as a set
        // instead of scanning the result list once per option. GetSearchedItems keeps the underlying
        // results keyed on the search text and _optionsVersion (which the options bump when their own
        // parameters change) and drops this set whenever it recomputes them, so an option whose data
        // changed without the dropdown itself re-rendering cannot be matched against a stale result.
        _searchedItemsCache ??= [.. GetSearchedItems()];

        return _searchedItemsCache.Contains(item);
    }

    internal string? GetItemCheckIconCss()
    {
        return BitIconInfo.From(ItemCheckIcon, ItemCheckIconName ?? "Accept")?.GetCssClasses();
    }

    // Called by an option when its own parameters change, so the cached search results (which hold
    // the option instances themselves) cannot be reused after the data they were computed from changed.
    internal void NotifyOptionParametersChanged()
    {
        _optionsVersion++;
    }

    private void RefreshOptions()
    {
        // Only options that render their item in place need the push re-render. In virtualize mode the
        // options render nothing (the dropdown renders the items from its Items collection), and in the
        // Items API there are no options at all, so there is nothing to refresh in either case.
        if (Items is null || Virtualize || (Options ?? ChildContent) is null) return;

        foreach (var item in Items)
        {
            (item as BitDropdownOption<TValue>)?.InternalStateHasChanged();
        }
    }

    internal void UnregisterOption(BitDropdownOption<TValue> option)
    {
        if (IsDisposed) return;

        var item = (option as TItem)!;
        Items!.Remove(item);
        _searchedItems = null;
        _itemPositions = null;
        _displayItems = null;
        _searchedItemsCache = null;

        if (_selectedItems.Contains(item))
        {
            _selectedItems = _selectedItems.FindAll(i => i != item);
            SetIsSelectedForSelectedItems();
        }

        StateHasChanged();
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        if (ReadOnly) return;
        if (GetItemType(item) != BitDropdownItemType.Normal) return;
        if (IsEnabled is false || GetIsEnabled(item) is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        await AddOrRemoveSelectedItem(item);

        if (MultiSelect is false)
        {
            // Selecting an item hides the callout along with the focused option,
            // so return the focus to the dropdown (or its combo input).
            if (Combo)
            {
                await FocusComboInputAsync();
            }
            else
            {
                await _dropdownWrapperRef.FocusAsync();
            }
        }

        StateHasChanged();
    }

    internal string GetItemWrapperCssClasses(TItem item)
    {
        var stringBuilder = new StringBuilder("bit-drp-iwr");

        var isSelected = GetIsSelected(item);

        if (isSelected)
        {
            stringBuilder.Append(" bit-drp-chd");
        }

        if (GetIsEnabled(item) is false || (isSelected is false && IsMaxSelectedItemsReached))
        {
            stringBuilder.Append(" bit-drp-ids");
        }

        return stringBuilder.ToString();
    }

    internal bool IsMaxSelectedItemsReached => MultiSelect && MaxSelectedItems is > 0 && (Values?.Count() ?? 0) >= MaxSelectedItems.Value;

    // The position of each selectable item within the list as it is currently rendered, so an option can
    // report "3 of 10" instead of just being one of an unnamed set. It is a map built once per search
    // rather than an index lookup per item, which is what made an earlier attempt at this quadratic.
    private Dictionary<TItem, int> GetItemPositions()
    {
        var search = SearchText;

        if (_itemPositions is null ||
            _positionsCacheKey != search ||
            _positionsCacheVersion != _optionsVersion)
        {
            _positionsCacheKey = search;
            _positionsCacheVersion = _optionsVersion;
            _itemPositions = [];

            var position = 0;
            foreach (var item in GetDisplayItems())
            {
                if (GetItemType(item) != BitDropdownItemType.Normal) continue;
                if (GetIsHidden(item)) continue;

                _itemPositions[item] = ++position;
            }
        }

        return _itemPositions;
    }

    internal int? GetTotalItems()
    {
        // With an ItemsProvider only a window of the list is loaded, so the size of the whole set is the
        // one the provider reported, and the position within it is not knowable from the window alone.
        if (ItemsProvider is not null) return _providerTotalItems;

        if (Items is null) return null;

        return GetItemPositions().Count;
    }

    internal int? GetItemPosInSet(TItem item)
    {
        if (ItemsProvider is not null) return null;

        return GetItemPositions().TryGetValue(item, out var position) ? position : null;
    }

    internal bool GetIsSelected(TItem item)
    {
        var value = GetValue(item);

        if (value is null) return false;

        if (MultiSelect)
        {
            return Values?.Contains(value) ?? false;
        }
        else
        {
            return EqualityComparer<TValue>.Default.Equals(value, CurrentValue);
        }
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

    internal BitIconInfo? GetIcon(TItem item)
    {
        if (item is BitDropdownItem<TValue> dropdownItem)
        {
            return BitIconInfo.From(dropdownItem.Icon, dropdownItem.IconName);
        }

        if (item is BitDropdownOption<TValue> dropdownOption)
        {
            return BitIconInfo.From(dropdownOption.Icon, dropdownOption.IconName);
        }

        if (NameSelectors is null) return null;

        BitIconInfo? icon = null;
        if (NameSelectors.Icon.Selector is not null)
        {
            icon = NameSelectors.Icon.Selector!(item);
        }
        else
        {
            icon = item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
        }

        string? iconName = null;
        if (NameSelectors.IconName.Selector is not null)
        {
            iconName = NameSelectors.IconName.Selector!(item);
        }
        else
        {
            iconName = item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
        }

        return BitIconInfo.From(icon, iconName);
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



    protected override string RootElementClass => "bit-drp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => GetColorClass());

        ClassBuilder.Register(() => GetSizeClass());

        ClassBuilder.Register(() => Required ? "bit-drp-req" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-drp-rol" : string.Empty);

        ClassBuilder.Register(() => _selectedItems?.Count > 0 ? "bit-drp-hvl" : string.Empty);

        ClassBuilder.Register(() => Chips ? "bit-drp-sch" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-drp-nbd" : string.Empty);

        ClassBuilder.Register(() => Transparent ? "bit-drp-trn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => FitWidth ? "width:fit-content" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _dropdownId = $"Dropdown-{UniqueId}";
        _calloutId = $"{_dropdownId}-callout";
        _overlayId = $"{_dropdownId}-overlay";
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

        if (MultiSelect)
        {
            if (ItemsProvider is not null && (InitialSelectedItems?.Any() ?? false))
            {
                _selectedItems.AddRange(InitialSelectedItems);

                if (ValuesHasBeenSet is false)
                {
                    await AssignValues(_selectedItems.Select(s => GetValue(s)));
                }
            }
            else if (ValuesHasBeenSet is false && DefaultValues is not null)
            {
                await AssignValues(DefaultValues);
            }
        }
        else
        {
            if (ItemsProvider is not null && (InitialSelectedItems?.Any() ?? false))
            {
                _selectedItems.Add(InitialSelectedItems.First());

                if (ValueHasBeenSet is false)
                {
                    Value = GetValue(_selectedItems.First());
                }
            }
            else if (ValueHasBeenSet is false && DefaultValue is not null)
            {
                Value = DefaultValue;
            }
        }

        UpdateSelectedItemsFromValues();

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        // Options render their items themselves and Blazor skips re-rendering them when only the
        // dropdown's own parameters (Styles, ItemTemplate, ...) change, so push a re-render to each one.
        RefreshOptions();

        // Items (or the search inputs) may have changed with this parameter set, so drop any cached
        // search results (and item count) computed for the previous one; they get rebuilt on demand.
        _searchedItems = null;
        _itemPositions = null;
        _displayItems = null;
        _searchedItemsCache = null;

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        try
        {
            // Prevents the default behavior (scrolling) of the navigation keys handled by the
            // keydown handlers, since Blazor cannot conditionally preventDefault per key.
            await _js.BitDropdownsSetup(_Id, _calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        if (Responsive is false) return;

        await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.End, Dir is BitDir.Rtl, BitSwipeOrientation.Horizontal, _dotnetObj);
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? parsingErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");

    protected override void CreateFieldIdentifier()
    {
        if (MultiSelect)
        {
            CreateFieldIdentifier(ValuesExpression, typeof(ICollection<TValue?>));
        }
        else
        {
            base.CreateFieldIdentifier();
        }
    }



    private async Task AddOrRemoveSelectedItem(TItem? item, bool addDynamic = false)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (item is null) return;

        if (MultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

            var isSelected = GetIsSelected(item) is false;

            if (isSelected && IsMaxSelectedItemsReached) return;

            var tempValue = Values?.ToList() ?? [];

            if (isSelected)
            {
                tempValue.Add(GetValue(item));
            }
            else
            {
                tempValue.Remove(GetValue(item));
            }

            await AssignValues(tempValue);

            if (Combo)
            {
                if (addDynamic && Dynamic && _selectedItems.Exists(si => EqualityComparer<TValue>.Default.Equals(GetValue(si), GetValue(item))) is false)
                {
                    _selectedItems.Add(item);
                    ClassBuilder.Reset();
                }
                else if (addDynamic is false && isSelected is false && _selectedItems.Exists(si => EqualityComparer<TValue>.Default.Equals(GetValue(si), GetValue(item))))
                {
                    _selectedItems.Remove(item);
                    ClassBuilder.Reset();
                }
            }

            if (AutoClearSearch)
            {
                // The callout stays open after a multi select pick, so without this the next item has to
                // be found through the filter left over from the previous one.
                await ClearSearchBox();
                await ClearComboBoxInput();
            }

            await OnSelectItem.InvokeAsync(item);
        }
        else
        {
            if (InvalidValueBinding()) return;

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

                ClassBuilder.Reset();
            }

            await CloseCallout();

            await ClearSearchBox();

            await ClearComboBoxInput();

            if (isSameItemSelected && Reselectable is false) return;

            await OnSelectItem.InvokeAsync(item);
        }

        SetIsSelectedForSelectedItems();
        RefreshOptions();
        await OnValuesChange.InvokeAsync([.. (Values ?? [])!]);
    }

    private void UpdateSelectedItemsFromValues()
    {
        var items = ItemsProvider is null ? Items : _lastShownItems;
        if (items is null) return;

        if (ItemsProvider is null)
        {
            _selectedItems.Clear();
        }

        var comparer = EqualityComparer<TValue>.Default;
        if (MultiSelect)
        {
            if (Values?.Any() ?? false)
            {
                foreach (var item in items)
                {
                    if (GetItemType(item) != BitDropdownItemType.Normal) continue;
                    if (Values.Any(v => comparer.Equals(v, GetValue(item))) is false) continue;
                    if (ItemsProvider is not null && _selectedItems.Exists(si => EqualityComparer<TValue>.Default.Equals(GetValue(si), GetValue(item)))) continue;

                    _selectedItems.Add(item);
                }

                if (ItemsProvider is not null)
                {
                    _selectedItems.RemoveAll(si => Values.Contains(GetValue(si)) is false);
                }
            }
            else
            {
                _selectedItems.Clear();
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
            else if (ItemsProvider is not null && comparer.Equals(CurrentValue, default))
            {
                // With an ItemsProvider a value that matches none of the loaded items usually just means
                // its item has not been fetched yet, so the selected item is kept. An empty value however
                // is a real deselection and has to drop the item the trigger is still showing.
                _selectedItems.Clear();
            }
        }

        ClassBuilder.Reset();
        SetIsSelectedForSelectedItems();
        RefreshOptions();
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (IsOpen is false) return;

        _rateLimiter.Reset();

        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(true) is false) return;

        await ToggleCallout();

        await OnClick.InvokeAsync(e);
        await FocusOnComboBoxInput();
        await FocusOnSearchBox();
    }

    private async Task HandleOnTriggerKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            await CloseCallout();
            return;
        }

        if (Combo)
        {
            // Typing is handled by the combo input itself; only the arrow keys manage the callout here.
            if (e.Key is "ArrowDown" or "ArrowUp")
            {
                await OpenCallout();
                await FocusItem("selected");
            }
            return;
        }

        if (e.Key is "Enter" or " ")
        {
            if (IsOpen)
            {
                await CloseCallout();
            }
            else
            {
                await OpenCallout();
                await FocusItem("selected");
            }
        }
        else if (e.Key is "ArrowDown" or "ArrowUp")
        {
            await OpenCallout();
            await FocusItem("selected");
        }
        else if (IsPrintableKey(e))
        {
            await OpenCallout();
            await FocusItem("char", GetTypeAheadBuffer(e.Key!));
        }
    }

    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        switch (e.Key)
        {
            case "ArrowDown":
                await FocusItem("next");
                break;
            case "ArrowUp":
                // Alt+ArrowUp dismisses the popup and returns to the trigger, per the APG combobox pattern.
                if (e.AltKey)
                {
                    await CloseCallout();
                    await _dropdownWrapperRef.FocusAsync();
                }
                else
                {
                    await FocusItem("prev");
                }
                break;
            case "PageDown":
                await FocusItem("nextPage");
                break;
            case "PageUp":
                await FocusItem("prevPage");
                break;
            case "Home":
            case "End":
                // Home/End keep their caret behavior while typing in the search/combo inputs.
                if (_inputSearchHasFocus is false && _inputComboHasFocus is false)
                {
                    await FocusItem(e.Key is "Home" ? "first" : "last");
                }
                break;
            case "Escape":
                await CloseCallout();
                await _dropdownWrapperRef.FocusAsync();
                break;
            case "Tab":
                await CloseCallout();
                break;
            default:
                // In Combo mode the combo input is the type-ahead, and printable keys
                // typed into the search box must keep filtering instead of moving focus.
                if (Combo is false && _inputSearchHasFocus is false && IsPrintableKey(e))
                {
                    await FocusItem("char", GetTypeAheadBuffer(e.Key!));
                }
                break;
        }
    }

    private static bool IsPrintableKey(KeyboardEventArgs e)
    {
        return e.Key?.Length is 1 && e.Key != " " && e.CtrlKey is false && e.AltKey is false && e.MetaKey is false;
    }

    private string GetTypeAheadBuffer(string key)
    {
        // Accumulates the keys typed in quick succession so the type-ahead matches the
        // full string, and starts over after a pause (the common 500ms convention).
        var now = DateTimeOffset.UtcNow;

        if ((now - _lastTypeAheadStamp).TotalMilliseconds > 500)
        {
            _typeAheadBuffer = string.Empty;
        }

        _lastTypeAheadStamp = now;
        _typeAheadBuffer += key;

        return _typeAheadBuffer;
    }

    private ValueTask FocusItem(string mode, string? character = null)
    {
        return _js.BitDropdownsFocusItem(_calloutId, mode, character);
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        UpdateSelectedItemsFromValues();
    }

    private void HandleSearchBoxFocusIn()
    {
        _inputSearchHasFocus = true;
    }

    private void HandleSearchBoxFocusOut()
    {
        _inputSearchHasFocus = false;
    }

    private void HandleComboInputFocusIn()
    {
        _inputComboHasFocus = true;
    }

    private void HandleComboInputFocusOut()
    {
        _inputComboHasFocus = false;
    }

    private Task HandleSearchBoxOnClear()
    {
        return ClearSearchBox();
    }

    private async Task HandleOnSearchBoxInput(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ShowSearchBox is false) return;

        if (Immediate is false) return;

        await _rateLimiter.Run(e, DebounceTime, ThrottleTime, async args =>
            await InvokeAsync(async () => await SearchItems(args)));
    }

    private async Task HandleOnSearchBoxChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ShowSearchBox is false) return;

        if (Immediate) return;

        await SearchItems(e);
    }

    private async Task SearchItems(ChangeEventArgs e)
    {
        _searchText = e.Value?.ToString();

        RefreshOptions();

        await OnSearch.InvokeAsync(_searchText);
        await SearchVirtualized();
    }

    private async Task ClearSearchBox()
    {
        if (IsEnabled is false) return;
        if (ShowSearchBox is false) return;
        if (_searchText.HasNoValue()) return;

        _rateLimiter.Reset();

        _searchText = null;

        RefreshOptions();

        await OnSearch.InvokeAsync(_searchText);
        await SearchVirtualized();
    }

    private void OnSetIsOpen()
    {
        _ = ClearSearchBox();

        // The combo input doubles as the search input, so a text that was typed but never committed
        // to a selection must not survive the callout, otherwise the trigger keeps showing a filter
        // term instead of the current selection the next time the dropdown is opened.
        _ = ClearComboBoxInput();

        _ = IsOpen ? OnOpen.InvokeAsync() : OnClose.InvokeAsync();
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
        if (ReadOnly) return;
        if (Combo is false) return;
        if (IsEnabled is false) return;
        if (_searchText.HasNoValue()) return;

        _rateLimiter.Reset();

        _searchText = null;

        RefreshOptions();

        // The items of a virtualized combo box come from the ItemsProvider filtered by the search
        // text, so dropping the text has to re-request them just like clearing the search box does.
        await SearchVirtualized();
    }

    private async ValueTask FocusOnComboBoxInput()
    {
        if (IsEnabled is false) return;
        if (IsOpen is false) return;
        if (Combo is false) return;
        if (_isResponsiveMode) return;

        await _comboBoxInputRef.FocusAsync();
    }

    // The term the items are actually filtered by. It is null until the typed text is long enough for
    // MinSearchLength, so a short term shows the full list instead of a nearly unfiltered one (and, with
    // an ItemsProvider, does not turn every first keystroke into a query).
    private string? SearchText => _searchText.HasValue() && _searchText!.Length >= MinSearchLength ? _searchText : null;

    internal bool IsItemTextMatch(string? text)
    {
        if (text is null) return false;

        var search = SearchText;
        if (search is null) return true;

        return SearchMode switch
        {
            BitDropdownSearchMode.StartsWith => text.StartsWith(search, StringComparison.OrdinalIgnoreCase),
            BitDropdownSearchMode.EndsWith => text.EndsWith(search, StringComparison.OrdinalIgnoreCase),
            BitDropdownSearchMode.Equals => text.Equals(search, StringComparison.OrdinalIgnoreCase),
            _ => text.Contains(search, StringComparison.OrdinalIgnoreCase)
        };
    }

    // The index at which the item text matched the search, so the default item rendering can highlight
    // exactly the matched part. Returns -1 when there is nothing to highlight.
    internal int GetHighlightIndex(string? text)
    {
        if (HighlightSearch is false) return -1;
        if (text is null) return -1;

        var search = SearchText;
        if (search is null) return -1;

        return SearchMode switch
        {
            BitDropdownSearchMode.StartsWith => text.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 0 : -1,
            BitDropdownSearchMode.EndsWith => text.EndsWith(search, StringComparison.OrdinalIgnoreCase) ? text.Length - search.Length : -1,
            BitDropdownSearchMode.Equals => text.Equals(search, StringComparison.OrdinalIgnoreCase) ? 0 : -1,
            _ => text.IndexOf(search, StringComparison.OrdinalIgnoreCase)
        };
    }

    internal int GetHighlightLength() => SearchText?.Length ?? 0;

    // The searched items are read several times while the callout renders (the empty state check, the
    // item list itself and the select all state), and the search may be a user-provided SearchFunction
    // over the whole item set, so the result is computed once per search text and reused. An unfiltered
    // list needs no work at all, so it keeps returning the live collection and cannot go stale.
    private ICollection<TItem> GetSearchedItems()
    {
        var items = ItemsProvider is null ? Items : _lastShownItems;
        if (items is null) return [];

        var search = SearchText;
        if (search is null) return items;

        if (_searchedItems is null ||
            _searchedItemsCacheKey != search ||
            _searchedItemsCacheVersion != _optionsVersion)
        {
            _searchedItemsCacheKey = search;
            _searchedItemsCacheVersion = _optionsVersion;
            _searchedItemsCache = null;
            _searchedItems = SearchFunction is not null
                ? [.. SearchFunction.Invoke(items, search)]
                : [.. items.Where(i => GetItemType(i) == BitDropdownItemType.Normal && IsItemTextMatch(GetText(i)))];
        }

        return _searchedItems;
    }

    // What the callout actually renders: the search result, minus the already selected items when they
    // are meant to disappear from the list. Kept separate from GetSearchedItems so that the select all
    // item still works over the full result rather than over the leftovers. The result is cached like
    // the search itself, so that repeated reads during one render (and Virtualize, which re-renders
    // everything when handed a new collection instance) see the same list.
    private ICollection<TItem> GetDisplayItems()
    {
        var items = GetSearchedItems();

        if (HideSelectedItems is false) return items;

        if (_displayItems is null ||
            _displayItemsCacheKey != SearchText ||
            _displayItemsCacheVersion != _optionsVersion ||
            _displayItemsSelectionVersion != _selectionVersion)
        {
            _displayItemsCacheKey = SearchText;
            _displayItemsCacheVersion = _optionsVersion;
            _displayItemsSelectionVersion = _selectionVersion;
            _displayItems = [.. items.Where(i => GetItemType(i) != BitDropdownItemType.Normal || GetIsSelected(i) is false)];
        }

        return _displayItems;
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

    private string GetDropdownAriaLabelledby()
    {
        return Label.HasValue() ? $"{_labelId} {_dropdownTextContainerId}" : _dropdownTextContainerId;
    }

    private async Task SearchVirtualized()
    {
        if (ItemsProvider is null) return;
        if (_virtualizeElement is null) return;

        await _virtualizeElement.RefreshDataAsync();
    }

    private async Task HandleOnClearClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (MultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

            await AssignValues([]);
            await OnValuesChange.InvokeAsync(Values);
        }
        else
        {
            if (InvalidValueBinding()) return;

            CurrentValue = default;

            _selectedItems.Clear();
        }

        UpdateSelectedItemsFromValues();

        await OnClear.InvokeAsync();
    }

    // Tells the empty state of an unsuccessful search apart from the empty state of an empty list, so
    // the callout can show "no results for what you typed" instead of "there is nothing here".
    private bool HasSearchText => SearchText is not null;

    private string GetSearchResultsText()
    {
        var count = GetSearchedItems().Count(i => GetItemType(i) == BitDropdownItemType.Normal && GetIsHidden(i) is false);

        return SearchResultsText is not null
                ? string.Format(SearchResultsText, count)
                : count == 1 ? "1 result available" : $"{count} results available";
    }

    private bool HasNoVisibleItems()
    {
        return GetDisplayItems().Any(i => GetItemType(i) == BitDropdownItemType.Normal && GetIsHidden(i) is false) is false;
    }

    private (bool AllSelected, bool AnySelected) GetSelectAllState()
    {
        var candidates = GetSelectAllCandidateItems();
        if (candidates.Count == 0) return (false, false);

        var selectedCount = candidates.Count(GetIsSelected);

        return (selectedCount == candidates.Count, selectedCount > 0);
    }

    private List<TItem> GetSelectAllCandidateItems()
    {
        return [.. GetSearchedItems().Where(i => GetItemType(i) == BitDropdownItemType.Normal &&
                                                 GetIsHidden(i) is false &&
                                                 GetIsEnabled(i))];
    }

    private async Task HandleOnSelectAllClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;
        if (MultiSelect is false) return;
        if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

        var candidates = GetSelectAllCandidateItems();
        if (candidates.Count == 0) return;

        List<TValue?> newValues;
        if (candidates.TrueForAll(GetIsSelected))
        {
            // All (searched) items are selected, so the select all item clears them, keeping
            // the selected values that are not part of the current search results.
            var comparer = EqualityComparer<TValue>.Default;
            var candidateValues = candidates.Select(GetValue).ToList();
            newValues = [.. (Values ?? []).Where(v => candidateValues.Exists(cv => comparer.Equals(cv, v)) is false)];
        }
        else
        {
            newValues = Values?.ToList() ?? [];
            foreach (var item in candidates)
            {
                if (GetIsSelected(item)) continue;
                if (MaxSelectedItems is > 0 && newValues.Count >= MaxSelectedItems.Value) break;

                newValues.Add(GetValue(item));
            }
        }

        await AssignValues(newValues);
        await OnValuesChange.InvokeAsync([.. (Values ?? [])!]);
    }

    private async Task HandleOnAddItemComboClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;

        await AddDynamicItem();

        _searchText = string.Empty;

        RefreshOptions();

        if (_isResponsiveMode && MultiSelect)
        {
            await _comboBoxInputResponsiveRef.FocusAsync();

            return;
        }

        await CloseCallout();
    }

    // The height available to the scrollable item list is the callout's height minus the parts that sit
    // above it, so every one of those parts has to be reported here or the callout overflows the
    // viewport. The values mirror the --bit-drp-h (search box) and --bit-drp-itm-h plus its bottom
    // border (select all row) of each size in the stylesheet.
    private int GetCalloutScrollOffset()
    {
        var offset = 0;

        if (ShowSearchBox && Combo is false)
        {
            offset += Size switch { BitSize.Small => 26, BitSize.Large => 40, _ => 32 };
        }

        if (MultiSelect && ShowSelectAll && ItemsProvider is null)
        {
            offset += Size switch { BitSize.Small => 31, BitSize.Large => 45, _ => 37 };
        }

        return offset;
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        _isResponsiveMode = await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: _dropdownId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: IsOpen,
            responsiveMode: Responsive ? BitResponsiveMode.Panel : BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: Dir is BitDir.Rtl,
            scrollContainerId: _scrollContainerId,
            scrollOffset: GetCalloutScrollOffset(),
            headerId: CalloutHeaderTemplate is not null ? _headerId : "",
            footerId: CalloutFooterTemplate is not null ? _footerId : "",
            setCalloutWidth: PreserveCalloutWidth is false,
            fixedCalloutWidth: false,
            maxWindowWidth: 0);
    }

    private async ValueTask<ItemsProviderResult<TItem>> InternalItemsProvider(ItemsProviderRequest request)
    {
        if (ItemsProvider is null) return default;

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // The token is not passed to the delay on purpose: a cancellation is a normal outcome here (the
        // user kept scrolling or typing) and is reported by returning an empty result, not by throwing.
        if (ItemsProviderDebounceTime > 0)
        {
            await Task.Delay(ItemsProviderDebounceTime);
        }

        if (request.CancellationToken.IsCancellationRequested) return default;

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var providerRequest = new BitDropdownItemsProviderRequest<TItem>(request.StartIndex, request.Count, SearchText, request.CancellationToken);
        var providerResult = await ItemsProvider(providerRequest);

        if (request.CancellationToken.IsCancellationRequested) return default;

        _lastShownItems = [.. providerResult.Items];
        _providerTotalItems = providerResult.TotalItemCount;

        UpdateSelectedItemsFromValues();
        await InvokeAsync(StateHasChanged);

        return new ItemsProviderResult<TItem>(providerResult.Items, providerResult.TotalItemCount);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;

        if (eventArgs.Key == "Escape")
        {
            _searchText = string.Empty;

            RefreshOptions();

            await CloseCallout();
        }
        else if (eventArgs.Key == "Enter")
        {
            _searchText = await _js.BitUtilsGetProperty(_isResponsiveMode ? _comboBoxInputResponsiveRef : _comboBoxInputRef, "value");

            await AddDynamicItem();

            _searchText = string.Empty;

            RefreshOptions();

            if (_isResponsiveMode && MultiSelect) return;

            await CloseCallout();
        }
        else if (eventArgs.Key == "Backspace" && _searchText.HasNoValue())
        {
            await RemoveLastSelectedItem();
        }
    }

    private Task HandleOnClickUnselectItem(TItem? item)
    {
        return UnselectItem(item);
    }

    private async Task HandleOnComboInput(ChangeEventArgs e)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;

        _searchText = e.Value?.ToString();

        RefreshOptions();

        if (Immediate is false) return;

        await _rateLimiter.Run(e, DebounceTime, ThrottleTime, async args =>
            await InvokeAsync(async () => await SearchComboItems(args)));
    }

    private async Task HandleOnComboChange(ChangeEventArgs e)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;

        if (Immediate) return;

        await SearchComboItems(e);
    }

    private async Task SearchComboItems(ChangeEventArgs e)
    {
        _searchText = e.Value?.ToString();

        RefreshOptions();

        await SearchVirtualized();

        await OpenCallout();
    }

    private async Task OpenCallout()
    {
        if (IsOpen) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpen(true) is false) return;

        await ToggleCallout();
    }

    private async Task RemoveLastSelectedItem()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (_selectedItems.Any() is false) return;

        if (MultiSelect)
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
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (_searchText.HasNoValue()) return;

        if (_selectedItems.Count > 0)
        {
            var hasItem = ExistsSelectedItemFunction is not null ?
                          ExistsSelectedItemFunction.Invoke(_selectedItems, _searchText!) :
                          _selectedItems.Exists(i => GetText(i).HasValue() && _searchText!.Equals(GetText(i)!, StringComparison.OrdinalIgnoreCase));

            if (hasItem) return;
        }

        var searchItems = ItemsProvider is not null ? _lastShownItems : Items;
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

    // The number of selected items the dropdown itself renders, the rest being collapsed into the
    // overflow indicator (chips) or the summary text.
    private int GetDisplayedItemsCount()
    {
        return MaxDisplayedItems is > 0 && _selectedItems.Count > MaxDisplayedItems.Value
                ? MaxDisplayedItems.Value
                : _selectedItems.Count;
    }

    private int GetOverflowItemsCount() => _selectedItems.Count - GetDisplayedItemsCount();

    private string GetOverflowText()
    {
        var count = GetOverflowItemsCount();

        return OverflowTextFormat is not null ? string.Format(OverflowTextFormat, count) : $"+{count}";
    }

    private string? GetText()
    {
        if (MultiSelect is false) return GetText(_selectedItems.FirstOrDefault());

        // Past the display limit the individual texts stop being readable in the width of a dropdown,
        // so a count of the selection says more than a truncated list of names.
        if (GetOverflowItemsCount() > 0)
        {
            return SelectedItemsTextFormat is not null
                    ? string.Format(SelectedItemsTextFormat, _selectedItems.Count)
                    : $"{_selectedItems.Count} items selected";
        }

        return string.Join(MultiSelectDelimiter, _selectedItems.Select(GetText));
    }

    private void OnSetValues()
    {
        UpdateSelectedItemsFromValues();

        EditContext?.NotifyFieldChanged(FieldIdentifier);
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

        item.SetValueToProperty(NameSelectors.IsSelected, value);
    }

    private void SetIsSelectedForSelectedItems()
    {
        // Every selection change passes through here, so this is where the caches that depend on which
        // items are selected (the hidden-when-selected list) learn that they are out of date.
        _selectionVersion++;

        var items = ItemsProvider is null ? Items : _lastShownItems;
        if (items is null) return;

        foreach (var it in items)
        {
            SetIsSelected(it, false);
        }

        foreach (var it in _selectedItems)
        {
            SetIsSelected(it, true);
        }
    }

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-drp-cal"];

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Responsive)
        {
            classes.Add("bit-drp-res");
        }

        if (ReadOnly)
        {
            // The callout is rendered outside the root element, so it needs its own read-only marker to
            // stop the items from advertising an interaction the component silently ignores.
            classes.Add("bit-drp-rol");
        }

        if (Dir is BitDir.Rtl)
        {
            classes.Add("bit-drp-rtl");
        }

        classes.Add(GetColorClass());

        // The callout renders outside the root element, so the size class has to be repeated on it for
        // the items to follow the size of the dropdown they belong to.
        classes.Add(GetSizeClass());

        return string.Join(' ', classes).Trim();
    }

    private string GetSizeClass()
    {
        return Size switch
        {
            BitSize.Small => "bit-drp-sm",
            BitSize.Medium => "bit-drp-md",
            BitSize.Large => "bit-drp-lg",
            _ => string.Empty
        };
    }

    private string GetColorClass()
    {
        return Color switch
        {
            BitColor.Primary => "bit-drp-pri",
            BitColor.Secondary => "bit-drp-sec",
            BitColor.Tertiary => "bit-drp-ter",
            BitColor.Info => "bit-drp-inf",
            BitColor.Success => "bit-drp-suc",
            BitColor.Warning => "bit-drp-wrn",
            BitColor.SevereWarning => "bit-drp-swr",
            BitColor.Error => "bit-drp-err",
            BitColor.PrimaryBackground => "bit-drp-pbg",
            BitColor.SecondaryBackground => "bit-drp-sbg",
            BitColor.TertiaryBackground => "bit-drp-tbg",
            BitColor.PrimaryForeground => "bit-drp-pfg",
            BitColor.SecondaryForeground => "bit-drp-sfg",
            BitColor.TertiaryForeground => "bit-drp-tfg",
            BitColor.PrimaryBorder => "bit-drp-pbr",
            BitColor.SecondaryBorder => "bit-drp-sbr",
            BitColor.TertiaryBorder => "bit-drp-tbr",
            _ => "bit-drp-pri"
        };
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        _rateLimiter.Reset();

        await base.DisposeAsync(disposing);

        try
        {
            await _js.BitDropdownsDispose(_Id);
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            _dotnetObj?.Dispose();
        }
    }
}
