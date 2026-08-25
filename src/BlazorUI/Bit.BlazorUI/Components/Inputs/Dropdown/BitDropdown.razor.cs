using System.Text;
using System.Globalization;
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
    private Dictionary<TItem, int>? _providerPositions;
    private string? _searchText;
    private string? _searchInputText;
    private string? _comboInputText;
    private string? _foldedSearchTextKey;
    private string? _foldedSearchText;
    private int _optionsVersion;
    private int _searchedItemsCacheVersion = -1;
    private string? _searchedItemsCacheKey;
    private List<TItem>? _searchedItems;
    private HashSet<TItem>? _searchedItemsCache;
    private string? _positionsCacheKey;
    private int _positionsCacheVersion = -1;
    private int _positionsSelectionVersion = -1;
    private Dictionary<TItem, int>? _itemPositions;
    private Dictionary<TItem, string>? _itemGroupIds;
    private Dictionary<TItem, string>? _itemHeaderIds;
    private int _selectionVersion;
    private TItem? _commitTarget;
    private string? _commitTargetCacheKey;
    private int _commitTargetCacheVersion = -1;
    private int _commitTargetSelectionVersion = -1;
    private HashSet<TValue>? _valuesLookup;
    private bool _valuesLookupHasNull;
    private object? _valuesLookupSource;
    private int _valuesLookupVersion = -1;
    private string? _displayItemsCacheKey;
    private int _displayItemsCacheVersion = -1;
    private int _displayItemsSelectionVersion = -1;
    private List<TItem>? _displayItems;
    private HashSet<TItem>? _collapsedItems;
    private string? _selectAllStateCacheKey;
    private int _selectAllStateCacheVersion = -1;
    private int _selectAllStateSelectionVersion = -1;
    private (bool HasCandidates, bool AllSelected, bool AnySelected)? _selectAllState;
    private bool _isResponsiveMode;
    private int _calloutScrollOffset = -1;
    private bool _internalIsOpenChange;
    private bool _suppressOpenOnFocus;
    private bool _openedOnFocus;
    private bool _inputSearchHasFocus;
    private bool _inputComboHasFocus;
    private List<TItem> _selectedItems = [];
    private List<TItem> _lastShownItems = [];
    private ICollection<TItem>? _lastItemsReference;
    private int _lastItemsCount = -1;
    private IEqualityComparer<TValue>? _lastValueComparer;
    private Virtualize<TItem>? _virtualizeElement;
    private string _scrollContainerId = string.Empty;
    private string _dropdownTextContainerId = string.Empty;
    private DotNetObjectReference<BitDropdown<TItem, TValue>> _dotnetObj = default!;

    private readonly BitInputRateLimiter<ChangeEventArgs> _rateLimiter = new();

    private string _labelId = string.Empty;
    private string _descriptionId = string.Empty;
    private string _headerId = string.Empty;
    private string _footerId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _dropdownId = string.Empty;

    private ElementReference _searchInputRef;
    private ElementReference _comboBoxInputRef;
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
    /// Gives the focus to the dropdown as soon as it is rendered.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Enables auto-focusing of the SearchBox input when the callout is open.
    /// </summary>
    [Parameter] public bool AutoFocusSearchBox { get; set; }

    /// <summary>
    /// Makes Enter in the ComboBox mode pick the first item the typed text matches when no item matches
    /// it exactly, which is what an autocomplete does: typing "app" and pressing Enter then selects
    /// "Apple" instead of doing nothing. It takes precedence over <see cref="Dynamic"/>, so a term that
    /// matches an existing item selects that item rather than creating a new one out of it.
    /// </summary>
    [Parameter] public bool AutoSelectFirstMatch { get; set; }

    /// <summary>
    /// Removes the already selected items from the callout, which suits a multi select dropdown whose
    /// selection is visible as chips and whose list is therefore only about what is left to pick.
    /// A group header left naming nothing, and a divider left without items on one of its sides, are
    /// removed along with them.
    /// It has no effect when the items come from an <see cref="ItemsProvider"/>, which hands over the
    /// window it was asked for and is the only place that can leave the selected items out of it.
    /// </summary>
    [Parameter] public bool HideSelectedItems { get; set; }

    /// <summary>
    /// Highlights the part of the item text that matched the current search text in the callout.
    /// Only applies to the default item rendering, not to a custom <see cref="ItemTemplate"/>.
    /// The highlighted part is found by the built-in algorithm (<see cref="SearchMode"/> and
    /// <see cref="SearchIgnoreDiacritics"/>), so a custom <see cref="SearchFunction"/> that matches by
    /// some other rule can produce items with nothing to highlight.
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
    /// The composite format of the accessible name of the remove button of a chip, which receives the
    /// text of the item the chip stands for, for example "Remove {0}". Defaults to the English message.
    /// </summary>
    [Parameter] public string? ChipsRemoveButtonAriaLabel { get; set; }

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
    /// The custom template for the content of a chip in the chips display, which receives the item the
    /// chip stands for. It replaces the text of the chip only; the remove button is still rendered
    /// after it, so a chip stays removable however its content is drawn.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ChipTemplate { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitDropdown.
    /// </summary>
    [Parameter] public BitDropdownClassStyles? Classes { get; set; }

    /// <summary>
    /// Determines whether picking an item in the callout closes it. It defaults to the behavior each
    /// mode expects: a single select dropdown closes, because the pick is the whole interaction, while
    /// a multi select one stays open so the next item can be picked right away. Set it explicitly to
    /// keep a single select callout open (a long list the user keeps trying options from) or to close a
    /// multi select one after every pick.
    /// </summary>
    [Parameter] public bool? CloseOnSelect { get; set; }

    /// <summary>
    /// The general color of the dropdown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The accessible name (and the tooltip) of the clear button of the dropdown.
    /// Defaults to the English message.
    /// </summary>
    [Parameter] public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// Makes the Escape key take back the whole selection once there is nothing left for it to dismiss:
    /// the first press closes the callout (and, in the ComboBox mode, drops the text that was typed into
    /// it), and only a press with the callout already closed and nothing typed clears what is selected.
    /// It reports itself through <see cref="OnClear"/> exactly as the clear button does, and it is
    /// refused in the same places that button is - a read-only dropdown, a one-way binding.
    /// </summary>
    [Parameter] public bool ClearOnEscape { get; set; }

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
    /// The accessible name (and the tooltip) of the add button in the responsive ComboBox mode.
    /// Defaults to the English message.
    /// </summary>
    [Parameter] public string? ComboBoxAddButtonAriaLabel { get; set; }

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
    /// The description rendered below the dropdown, which is also tied to it as its accessible
    /// description, so a screen reader reads it along with the dropdown instead of leaving it as text
    /// that only happens to sit nearby.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the dropdown, which replaces <see cref="Description"/>.
    /// It is tied to the dropdown as its accessible description in the same way.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// It is allowed to add a new item in the ComboBox mode. While the typed text names no item the list
    /// offers to create one out of it, so that adding an item is something the user can see and click
    /// rather than a shortcut they have to know about (see <see cref="DynamicItemTextFormat"/>).
    /// </summary>
    [Parameter] public bool Dynamic { get; set; }

    /// <summary>
    /// The custom template for the row the callout offers to create a new item with in the
    /// <see cref="Dynamic"/> ComboBox mode, which receives the text the item would be created from.
    /// It replaces <see cref="DynamicItemTextFormat"/>.
    /// </summary>
    [Parameter] public RenderFragment<string>? DynamicItemTemplate { get; set; }

    /// <summary>
    /// The composite format of the row the callout offers to create a new item with in the
    /// <see cref="Dynamic"/> ComboBox mode, which receives the text the item would be created from,
    /// for example "Add \"{0}\"". Defaults to the English message.
    /// </summary>
    [Parameter] public string? DynamicItemTextFormat { get; set; }

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
    /// Decides whether the text committed in the ComboBox mode already stands for one of the selected
    /// items, in place of the default comparison of that text with the item texts, ignoring case. It
    /// receives the selected items and the committed text, and returning true stops the commit, so the
    /// same item cannot be selected (or created) twice under a name your data considers equivalent.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, bool>? ExistsSelectedItemFunction { get; set; }

    /// <summary>
    /// Finds the item the text committed in the ComboBox mode stands for, in place of the default
    /// comparison of that text with the item texts, ignoring case. It receives the items and the
    /// committed text; the item it returns gets selected, and only when it returns none does
    /// <see cref="AutoSelectFirstMatch"/> and then <see cref="Dynamic"/> get their turn.
    /// </summary>
    [Parameter] public Func<ICollection<TItem>, string, TItem?>? FindItemFunction { get; set; }

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
    /// Searches the items as the user types in the search box (based on the 'oninput' HTML event)
    /// instead of waiting for the search box to be committed.
    /// The ComboBox input always searches as it is typed - that is what a combo box is - so there it
    /// only decides whether <see cref="DebounceTime"/> and <see cref="ThrottleTime"/> apply.
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
    /// The function providing items to the list for virtualization. It loads the items on demand, in
    /// the windows the user actually scrolls to, and receives the current search text so the filtering
    /// happens at the source instead of over an already loaded list.
    /// It requires <see cref="Virtualize"/> to be enabled, which is what requests the windows.
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
    /// The maximum height of the scrollable item list of the callout in pixels, which is what keeps a
    /// long list from taking over the screen. It is applied on top of the space the viewport leaves, so
    /// it can only ever make the list shorter: a callout near the bottom of the window is still capped
    /// by the room it has. A value that is not greater than zero (and null) leaves the viewport alone
    /// to decide.
    /// </summary>
    [Parameter] public int? MaxHeight { get; set; }

    /// <summary>
    /// The maximum number of items that can be selected in multi select mode.
    /// A value that is not greater than zero (and null) means no limit.
    /// </summary>
    [Parameter] public int? MaxSelectedItems { get; set; }

    /// <summary>
    /// The composite format of the message announced to screen readers once <see cref="MaxSelectedItems"/>
    /// is reached, which receives that limit, for example "Maximum of {0} items selected". Reaching the
    /// limit disables the items that are not selected yet, which is a change only a sighted user can
    /// notice on their own. Defaults to the English message.
    /// </summary>
    [Parameter] public string? MaxSelectedItemsText { get; set; }

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
    /// The composite format of the hint the callout shows while the typed text is still shorter than
    /// <see cref="MinSearchLength"/>, which receives the number of characters that are still missing,
    /// for example "Type {0} more characters to search". It is what tells the user that the list they
    /// are looking at is the full one rather than the result of what they typed, and it is announced
    /// to screen readers as well. Defaults to the English message; the hint is not shown at all while
    /// nothing has been typed, where the full list needs no explaining.
    /// </summary>
    [Parameter] public string? MinSearchLengthText { get; set; }

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
    /// Stops the arrow keys at the ends of the item list instead of letting them wrap around from the
    /// last item to the first one and back, which suits a long list where the wrap is more likely to
    /// read as the focus having been lost than as a deliberate jump. The type-ahead still wraps, since
    /// it looks for the item that matches rather than for the one that comes next.
    /// It has no effect in virtualize mode, where the ends of the rendered window are not the ends of
    /// the list and the focus stops at them either way.
    /// </summary>
    [Parameter] public bool NoWrapNavigation { get; set; }

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
    /// The callback that is called when a selected item gets unselected in multi select mode, which
    /// happens by picking it again in the callout, by removing its chip, or through the
    /// <see cref="UnselectItem"/> method. Clearing the whole selection reports itself
    /// through <see cref="OnClear"/> instead.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnDeselectItem { get; set; }

    /// <summary>
    /// The callback that is called when a new item is on added Dynamic ComboBox mode.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnDynamicAdd { get; set; }

    /// <summary>
    /// The callback that is called when the dropdown (or any element inside it, like the ComboBox
    /// input) receives the focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// The callback that is called when the dropdown (or any element inside it, like the ComboBox
    /// input) loses the focus. The callout is rendered outside the dropdown so that it can escape any
    /// clipping ancestor, so moving the focus into it (with the arrow keys, or by clicking the search
    /// box) counts as leaving the dropdown here.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// The callback that is called when the callout gets opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// The callback that is called when the search text of the search box or the ComboBox input
    /// changes, with the text exactly as it was typed. It also fires while that text is still shorter
    /// than <see cref="MinSearchLength"/>, where the items are not filtered by it yet.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearch { get; set; }

    /// <summary>
    /// The callback that is called when an item gets picked in the callout. In multi select mode it
    /// reports every pick, including the one that unselects an already selected item; use
    /// <see cref="OnDeselectItem"/> to be told only about those.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// The callback that is called when the selected items change.
    /// </summary>
    [Parameter] public EventCallback<IEnumerable<TValue?>> OnValuesChange { get; set; }

    /// <summary>
    /// Opens the callout as soon as the dropdown receives the focus, so tabbing into it (or clicking
    /// any part of it) already shows the items without a further click or key press.
    /// </summary>
    [Parameter] public bool OpenOnFocus { get; set; }

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
    /// It has no effect in the ComboBox mode, where the placeholder belongs to the editable input and
    /// can only be the plain text of <see cref="Placeholder"/>.
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
    /// The accessible name (and the tooltip) of the close button in the responsive mode callout.
    /// Defaults to the English message.
    /// </summary>
    [Parameter] public string? ResponsiveCloseButtonAriaLabel { get; set; }

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
    /// The accessible name of the SearchBox input. Defaults to the English message.
    /// </summary>
    [Parameter] public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The accessible name (and the tooltip) of the clear button of the SearchBox.
    /// Defaults to the English message.
    /// </summary>
    [Parameter] public string? SearchBoxClearButtonAriaLabel { get; set; }

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
    /// Matches the search text against the item texts with the diacritics of both removed, so that
    /// "Jose" finds "José" and "Muller" finds "Müller". The item text itself is left untouched, and so
    /// is the part of it that <see cref="HighlightSearch"/> emphasizes. Ignored when a
    /// <see cref="SearchFunction"/> is provided, which does its own matching.
    /// </summary>
    [Parameter] public bool SearchIgnoreDiacritics { get; set; }

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
    /// Selects the text already in the ComboBox input whenever it takes the focus, so that typing
    /// replaces the term that is there instead of appending to it - which is what a field the user
    /// comes back to in order to search for something else needs. It has no effect outside of the
    /// ComboBox mode, and none while the input is empty, where there is nothing to select.
    /// </summary>
    [Parameter] public bool SelectTextOnFocus { get; set; }

    /// <summary>
    /// Shows the clear button when an item is selected.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Shows the SearchBox element in the callout.
    /// It has no effect in the ComboBox mode, where the input of the dropdown itself is what the items
    /// are filtered by, and a second search field would only split the typing between two places.
    /// </summary>
    [Parameter] public bool ShowSearchBox { get; set; }

    /// <summary>
    /// Shows the select all item in the callout in multi select mode.
    /// It has no effect when the items are provided by an ItemsProvider, since the items that are not
    /// loaded yet cannot be selected.
    /// </summary>
    [Parameter] public bool ShowSelectAll { get; set; }

    /// <summary>
    /// The size of the dropdown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Keeps the header of a group pinned to the top of the item list while its items are scrolled
    /// past, so a long grouped list never leaves the user looking at items whose group has scrolled
    /// out of view.
    /// </summary>
    [Parameter] public bool StickyHeaders { get; set; }

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
    /// The characters that split the text typed (or pasted) into the multi select ComboBox input into
    /// separate terms, each committed as its own selection exactly as typing it and pressing Enter
    /// would: a term naming an existing item selects it, and with <see cref="Dynamic"/> enabled a term
    /// naming none adds a new item. This is what turns a pasted "a, b, c" into three selections
    /// instead of one literal term.
    /// </summary>
    [Parameter] public char[]? TokenSeparators { get; set; }

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
    /// Renders the dropdown with only a bottom border in place of the box around it, which is the
    /// variant that suits a dense form where a full box per field would be too much furniture.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// Decides whether two values stand for the same selection, in place of the default equality of
    /// <typeparamref name="TValue"/>. This is what a value type that is not its own identity needs: a
    /// record or a class used as the value compares by reference by default, so a value that arrives
    /// from a form, a query string or a fresh fetch would never match the item it names, however equal
    /// the two look. It also decides which item a clicked value belongs to, so a comparer that treats
    /// two different values as equal makes them one and the same selection.
    /// </summary>
    [Parameter] public IEqualityComparer<TValue>? ValueComparer { get; set; }

    /// <summary>
    /// The values of the selected items in multi select mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetValues))]
    public IEnumerable<TValue?>? Values { get; set; }

    /// <summary>
    /// The expression that identifies the bound <see cref="Values"/>, which is what ties the multi
    /// select mode to an EditContext: it is what a validation message for the field is looked up by.
    /// It is supplied by the compiler for a <c>@bind-Values</c> binding, so it only has to be set by
    /// hand when the values are bound in some other way.
    /// </summary>
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
    /// The ElementReference to the combo input element, which is null outside of the ComboBox mode.
    /// The responsive panel brings an input of its own, which only exists while the panel is on the
    /// screen, so it is only reported while the callout is actually open.
    /// </summary>
    public ElementReference? ComboInputElement => Combo
                                                    ? IsOpen && _isResponsiveMode
                                                        ? _comboBoxInputResponsiveRef
                                                        : _comboBoxInputRef
                                                    : null;

    /// <summary>
    /// Gives focus to the combo input element. It does nothing outside of the ComboBox mode, which is
    /// what <see cref="ComboInputElement"/> reports.
    /// </summary>
    public ValueTask FocusComboInputAsync() => Combo ? FocusTrigger() : ValueTask.CompletedTask;

    /// <summary>
    /// The ElementReference to the search input element, which is null while the search box is not
    /// rendered - it needs <see cref="ShowSearchBox"/> and an open callout to live in, and it is
    /// replaced by the input of the dropdown itself in the ComboBox mode.
    /// </summary>
    public ElementReference? SearchInputElement => HasSearchBox && IsOpen ? _searchInputRef : null;

    /// <summary>
    /// Gives focus to the search input element. It does nothing while the search box is not rendered,
    /// which is what <see cref="SearchInputElement"/> reports.
    /// </summary>
    public ValueTask FocusSearchInputAsync() => HasSearchBox && IsOpen ? _searchInputRef.FocusAsync() : ValueTask.CompletedTask;

    /// <summary>
    /// Discards the items loaded so far and asks the <see cref="ItemsProvider"/> for them again, which
    /// is what makes a change outside of the dropdown (a filter of the page, a record added elsewhere)
    /// reach a list the dropdown only ever loads on demand. It does nothing without an ItemsProvider,
    /// where the <see cref="Items"/> collection is the source of truth and is re-read on its own.
    /// </summary>
    public Task RefreshItemsAsync() => SearchVirtualized();



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;

        // The JS side has already hidden this callout to make room for the other one, so the change
        // is marked internal to keep the OnSetIsOpen hook from toggling it again.
        if (await AssignIsOpenInternal(false) is false) return;

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
        await CloseCalloutAndRestoreFocus();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Selects the given item exactly as picking it in the callout would, so the same events fire and
    /// the same close and focus behavior follows. An item that is already selected is left alone: in
    /// multi select mode picking it again would unselect it, which <see cref="UnselectItem"/> is for.
    /// </summary>
    public async Task SelectItem(TItem? item)
    {
        if (item is null) return;

        if (GetIsSelected(item)) return;

        await HandleOnItemClick(item);
    }

    /// <summary>
    /// Unselects the given item exactly as picking an already selected one in the callout would (or, in
    /// single select mode, as the clear button would), so the same events fire. An item that is not
    /// selected is left alone.
    /// </summary>
    public async Task UnselectItem(TItem? item)
    {
        if (item is null) return;

        // Unselecting an item that is not selected must be a no-op: in multi select mode the toggle
        // below would otherwise select it, and in single select mode the clear would drop another item.
        if (GetIsSelected(item) is false) return;

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
        ResetItemCaches();

        UpdateSelectedItemsFromValues();

        StateHasChanged();
    }

    // Each option calls this during the dropdown's render cycle to decide whether its item is visible
    // for the current search. Options are refreshed explicitly (RefreshOptions in OnParametersSet and
    // after the search/selection mutations), and the search results are cached per search text; that
    // cache is reset in OnParametersSet so a change to Items cannot reuse results from a previous set.
    internal bool ShouldRenderOptionItem(TItem item)
    {
        var itemType = GetItemType(item);

        if (HideSelectedItems && itemType == BitDropdownItemType.Normal && GetIsSelected(item)) return false;

        // A header whose items were all filtered out (by a search, or because they are already selected)
        // names nothing, and a divider that lost the items on one of its sides separates nothing, so both
        // go with them. GetDisplayItems is what computes that set, and caches it along with its own result.
        if (ShouldCollapseGroups && itemType is BitDropdownItemType.Header or BitDropdownItemType.Divider)
        {
            GetDisplayItems();

            if (_collapsedItems?.Contains(item) is true) return false;
        }

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

    // Everything derived from the current set of items: the search result and the set it is looked up
    // in, the list the callout renders, the positions of the items in it and the item a commit would
    // take. They are dropped together because a change to the items invalidates all of them at once, and
    // a site that dropped only some would leave the rest answering from a list that no longer exists.
    private void ResetItemCaches()
    {
        _searchedItems = null;
        _itemPositions = null;
        _displayItems = null;
        _searchedItemsCache = null;
        _commitTargetCacheKey = null;
        _selectAllState = null;
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
        ResetItemCaches();

        if (_selectedItems.Contains(item))
        {
            _selectedItems = _selectedItems.FindAll(i => i != item);
            SetIsSelectedForSelectedItems();

            // The has-value class of the root element follows the selected items, so the cached class
            // list has to be rebuilt now that the removed option may have been the last selected one.
            ClassBuilder.Reset();
        }

        StateHasChanged();
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        if (ReadOnly) return;
        if (GetItemType(item) != BitDropdownItemType.Normal) return;
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        var wasOpen = IsOpen;

        // A one-way bound IsOpen must not block the selection itself: the selection proceeds and the
        // close attempt inside it simply becomes a no-op (AssignIsOpen refuses the change), which is
        // exactly the controlled behavior a one-way IsOpen asks for.
        await AddOrRemoveSelectedItem(item);

        // A multi select callout stays open by default so the next item can be picked right away; only
        // an explicit CloseOnSelect turns each pick into a complete interaction of its own. The single
        // select close happens inside AddOrRemoveSelectedItem, which every selection path goes through.
        if (MultiSelect && CloseOnSelect is true)
        {
            await CloseCallout();
        }

        // Closing the callout hides the focused option with it, so the focus returns to the dropdown
        // (or to its combo input) instead of falling to the document body. A callout that stays open
        // keeps the focus on the option, and one that was never open (an unselect through the API or a
        // chip) must not have the focus pulled to the dropdown at all.
        if (wasOpen && IsOpen is false)
        {
            await RestoreFocusToTrigger();
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

        if (GetIsItemDisabled(item))
        {
            stringBuilder.Append(" bit-drp-ids");
        }

        if (GetIsCommitTarget(item))
        {
            stringBuilder.Append(" bit-drp-ctg");
        }

        return stringBuilder.ToString();
    }

    // Reaching the selection limit makes the items that are not selected yet unavailable, so they are
    // disabled like any other disabled item instead of only being styled as such.
    internal bool GetIsItemDisabled(TItem item)
    {
        if (GetIsEnabled(item) is false) return true;

        return IsMaxSelectedItemsReached && GetIsSelected(item) is false;
    }

    internal bool IsMaxSelectedItemsReached => MultiSelect && MaxSelectedItems is > 0 && IsValuesCountAtLeast(MaxSelectedItems.Value);

    // This is evaluated once per rendered item, so a full enumeration of Values (which is an
    // IEnumerable and may be lazy) has to be avoided: collections expose their count directly, and
    // for anything else it is enough to know whether the limit is already reached.
    private bool IsValuesCountAtLeast(int count)
    {
        if (Values is null) return false;

        if (Values is ICollection<TValue?> collection) return collection.Count >= count;

        return Values.Skip(count - 1).Any();
    }

    // The position of each selectable item within the list as it is currently rendered, so an option can
    // report "3 of 10" instead of just being one of an unnamed set.
    private Dictionary<TItem, int> GetItemPositions()
    {
        EnsureItemMaps();

        return _itemPositions!;
    }

    // Builds, in a single pass over the rendered list, both the position of each selectable item and the
    // group each one belongs to. They are maps built once per search rather than a lookup per item,
    // which is what made an earlier attempt at the positions quadratic. The three maps share one cache
    // entry because they come out of the same pass; dropping _itemPositions invalidates all of them.
    private void EnsureItemMaps()
    {
        var search = SearchText;

        // The selection version is part of the key because GetDisplayItems drops the selected items when
        // HideSelectedItems is on, which shifts the position of every item that comes after them.
        if (_itemPositions is not null &&
            _positionsCacheKey == search &&
            _positionsCacheVersion == _optionsVersion &&
            _positionsSelectionVersion == _selectionVersion) return;

        _positionsCacheKey = search;
        _positionsCacheVersion = _optionsVersion;
        _positionsSelectionVersion = _selectionVersion;
        _itemPositions = [];
        _itemHeaderIds = [];
        _itemGroupIds = [];

        var position = 0;
        var groupIndex = 0;
        string? groupId = null;
        foreach (var item in GetDisplayItems())
        {
            var itemType = GetItemType(item);

            if (itemType == BitDropdownItemType.Header)
            {
                // A hidden header names nothing, so the items after it are left without a group rather
                // than being described by text that is not on the screen.
                if (GetIsHidden(item))
                {
                    groupId = null;
                    continue;
                }

                // A header needs an id to be referenced by the items it names; the ones without their
                // own id get a generated one, which is why this is a map and not just a lookup.
                groupId = GetId(item) ?? $"{_dropdownId}-grp-{++groupIndex}";
                _itemHeaderIds[item] = groupId;
                continue;
            }

            // A divider only draws a line between the items of a group, so it does not end one.
            if (itemType != BitDropdownItemType.Normal) continue;
            if (GetIsHidden(item)) continue;

            _itemPositions[item] = ++position;

            if (groupId is not null)
            {
                _itemGroupIds[item] = groupId;
            }
        }
    }

    // The id of the element that carries the text of the group header, so the items of the group can
    // point at it and be read as "Apple, 2 of 8, Fruits" instead of just "Apple, 2 of 8".
    internal string? GetItemHeaderId(TItem item)
    {
        EnsureItemMaps();

        return _itemHeaderIds!.GetValueOrDefault(item) ?? GetId(item);
    }

    internal string? GetItemGroupId(TItem item)
    {
        EnsureItemMaps();

        return _itemGroupIds!.GetValueOrDefault(item);
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
        // With an ItemsProvider the position cannot be counted from the loaded window, but the provider
        // is asked for a window starting at a known index, so the position within the whole set is that
        // index plus the offset inside the window. Without it an item would report a set size and no
        // place in it, which is exactly the "3 of 5000" a screen reader user needs the most in a list
        // they can only ever see a window of.
        if (ItemsProvider is not null)
        {
            return _providerPositions?.TryGetValue(item, out var providerPosition) is true ? providerPosition : null;
        }

        return GetItemPositions().TryGetValue(item, out var position) ? position : null;
    }

    internal bool GetIsSelected(TItem item)
    {
        var value = GetValue(item);

        if (value is null) return false;

        if (MultiSelect)
        {
            return ContainsValue(value);
        }
        else
        {
            return Comparer.Equals(value, CurrentValue);
        }
    }

    // The single place every value comparison in the component goes through, so a custom ValueComparer
    // governs all of them - which item a value selects, which selected item a chip removes, whether a
    // typed term is already selected - instead of only some.
    private IEqualityComparer<TValue> Comparer => ValueComparer ?? EqualityComparer<TValue>.Default;

    // Whether the given value is part of the current selection. This is asked once (often several times)
    // per rendered item, so scanning Values for each of them made a long list with a long selection cost
    // the product of the two; the values are looked up as a set instead, built once per selection.
    private bool ContainsValue(TValue? value)
    {
        EnsureValuesLookup();

        return value is null ? _valuesLookupHasNull : _valuesLookup!.Contains(value);
    }

    // The set is rebuilt whenever Values is replaced (every selection change assigns a new collection)
    // or the selection changes, and it is dropped in OnParametersSet, which is where a collection the
    // consumer mutated in place becomes visible to the component.
    private void EnsureValuesLookup()
    {
        if (_valuesLookup is not null &&
            ReferenceEquals(_valuesLookupSource, Values) &&
            _valuesLookupVersion == _selectionVersion) return;

        _valuesLookupSource = Values;
        _valuesLookupVersion = _selectionVersion;
        _valuesLookupHasNull = false;
        // Built with the same comparer every other comparison uses, so the set answers exactly what a
        // scan of Values would have - a custom ValueComparer governs the fast path too.
        _valuesLookup = new HashSet<TValue>(Comparer);

        if (Values is null) return;

        foreach (var value in Values)
        {
            if (value is null)
            {
                _valuesLookupHasNull = true;
            }
            else
            {
                _valuesLookup.Add(value);
            }
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

        ClassBuilder.Register(() => Underlined ? "bit-drp-und" : string.Empty);

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
        _descriptionId = $"{_dropdownId}-description";
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
        ResetItemCaches();

        // A parameter set is where a Values collection the consumer mutated in place (rather than
        // replacing) becomes visible to the component, so the set the selection is looked up in is
        // rebuilt from it as well.
        _valuesLookup = null;

        // A new Items collection has to resync the selected items, which still reference the previous
        // collection's instances: neither the Value nor the Values hook fires for it, and this point
        // (unlike those hooks) runs after every parameter of the batch - including a Values change
        // that arrived in the same set but was applied before the new Items - has been applied.
        // The same collection with a different number of items in it is the same story: a list the
        // consumer adds to (or removes from) in place keeps its reference, so without this the item of
        // a value that was selected before its item existed would never reach the trigger.
        // A new ValueComparer is the same story from the other side: the values did not change, but
        // which item each of them names did.
        var itemsCount = Items?.Count ?? -1;
        if (ReferenceEquals(_lastItemsReference, Items) is false ||
            _lastItemsCount != itemsCount ||
            ReferenceEquals(_lastValueComparer, ValueComparer) is false)
        {
            _lastItemsReference = Items;
            _lastItemsCount = itemsCount;
            _lastValueComparer = ValueComparer;
            UpdateSelectedItemsFromValues();
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false)
        {
            await RefreshCalloutScrollOffset();

            return;
        }

        _dotnetObj = DotNetObjectReference.Create(this);

        try
        {
            // Prevents the default behavior (scrolling) of the navigation keys handled by the
            // keydown handlers, since Blazor cannot conditionally preventDefault per key.
            await _js.BitDropdownsSetup(_Id, _calloutId);

            if (Responsive)
            {
                await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.End, Dir is BitDir.Rtl, BitSwipeOrientation.Horizontal, _dotnetObj);
            }

            // An initial IsOpen fired the OnSetIsOpen hook before the first render, when there was no
            // callout element to toggle yet, so the open state is applied here instead.
            if (IsOpen)
            {
                await ToggleCallout();
            }

            // The autofocus attribute is only honored by the browser for an element that is part of the
            // initial document, which the trigger of an interactively rendered dropdown is not.
            if (AutoFocus && IsEnabled)
            {
                await FocusTrigger();
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
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



    // Returns whether the selection actually changed, which the dynamic item flow needs: a refused
    // selection (a read-only dropdown, a one-way binding, a reached limit) must not be reported as an
    // item that was added.
    private async Task<bool> AddOrRemoveSelectedItem(TItem? item, bool addDynamic = false)
    {
        if (ReadOnly) return false;
        if (IsEnabled is false) return false;

        if (item is null) return false;

        if (MultiSelect)
        {
            if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return false;

            var isSelected = GetIsSelected(item) is false;

            if (isSelected && IsMaxSelectedItemsReached) return false;

            var tempValue = Values?.ToList() ?? [];

            if (isSelected)
            {
                tempValue.Add(GetValue(item));
            }
            else
            {
                // Not List.Remove: it compares with the default equality of TValue, which would leave the
                // value in place whenever a custom ValueComparer is what makes it equal to the picked one.
                var value = GetValue(item);
                var index = tempValue.FindIndex(v => Comparer.Equals(v, value));
                if (index > -1)
                {
                    tempValue.RemoveAt(index);
                }
            }

            await AssignValues(tempValue);

            if (Combo)
            {
                if (addDynamic && Dynamic && _selectedItems.Exists(si => Comparer.Equals(GetValue(si), GetValue(item))) is false)
                {
                    _selectedItems.Add(item);
                    ClassBuilder.Reset();
                }
                else if (addDynamic is false && isSelected is false && _selectedItems.Exists(si => Comparer.Equals(GetValue(si), GetValue(item))))
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

            if (isSelected is false)
            {
                await OnDeselectItem.InvokeAsync(item);
            }
        }
        else
        {
            if (InvalidValueBinding()) return false;

            var oldSelectedItem = _selectedItems.FirstOrDefault();

            // By value rather than by instance: the same option handed over as a fresh object (a list
            // rebuilt on every render, an item that came back from the server) is still the selection the
            // dropdown is already showing, and picking it again is still a reselection.
            var isSameItemSelected = oldSelectedItem is not null && Comparer.Equals(GetValue(oldSelectedItem), GetValue(item));

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

            // A single select pick is the whole interaction, so the callout closes with it - unless
            // CloseOnSelect says otherwise, which keeps a long list open while the user tries options.
            if (CloseOnSelect is not false)
            {
                await CloseCallout();
            }

            await ClearSearchBox();

            await ClearComboBoxInput();

            // The item is the selection either way, so this reports a selection that stands; only the
            // events of picking it again are what Reselectable holds back.
            if (isSameItemSelected && Reselectable is false) return true;

            await OnSelectItem.InvokeAsync(item);
        }

        SetIsSelectedForSelectedItems();
        RefreshOptions();

        // Values only exists in multi select mode, so in single select mode this used to hand every
        // subscriber the same empty list on every pick; the single select selection is reported by
        // OnChange/OnSelectItem instead.
        if (MultiSelect)
        {
            await OnValuesChange.InvokeAsync([.. (Values ?? [])!]);
        }

        return true;
    }

    private void UpdateSelectedItemsFromValues()
    {
        var items = ItemsProvider is null ? Items : _lastShownItems;
        if (items is null) return;

        // The selection may hold items that are not part of Items at all - the ones created in the
        // Dynamic ComboBox mode - which the rebuild below cannot find in Items. They are preserved
        // from this snapshot as long as their values are still selected, otherwise every later
        // selection change would silently drop them while their values stay selected.
        List<TItem> previousSelectedItems = ItemsProvider is null ? [.. _selectedItems] : [];

        if (ItemsProvider is null)
        {
            _selectedItems.Clear();
        }

        var comparer = Comparer;
        if (MultiSelect)
        {
            if (Values?.Any() ?? false)
            {
                foreach (var item in items)
                {
                    if (GetItemType(item) != BitDropdownItemType.Normal) continue;
                    if (ContainsValue(GetValue(item)) is false) continue;
                    if (ItemsProvider is not null && _selectedItems.Exists(si => comparer.Equals(GetValue(si), GetValue(item)))) continue;

                    _selectedItems.Add(item);
                }

                if (ItemsProvider is not null)
                {
                    _selectedItems.RemoveAll(si => ContainsValue(GetValue(si)) is false);
                }
                else
                {
                    foreach (var previousItem in previousSelectedItems)
                    {
                        if (items.Contains(previousItem)) continue;

                        var value = GetValue(previousItem);
                        if (ContainsValue(value) is false) continue;
                        if (_selectedItems.Exists(si => comparer.Equals(GetValue(si), value))) continue;

                        _selectedItems.Add(previousItem);
                    }
                }

                SortSelectedItemsByValues();
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
            else if (ItemsProvider is null)
            {
                if (comparer.Equals(CurrentValue, default) is false)
                {
                    var previousItem = previousSelectedItems.Find(si => comparer.Equals(GetValue(si), CurrentValue));
                    if (previousItem is not null)
                    {
                        _selectedItems.Add(previousItem);
                    }
                }
            }
            else if (comparer.Equals(CurrentValue, default))
            {
                // With an ItemsProvider a value that matches none of the loaded items usually just means
                // its item has not been fetched yet, so the selected item is kept. An empty value however
                // is a real deselection and has to drop the item the trigger is still showing.
                _selectedItems.Clear();
            }
            else
            {
                // The item is kept only while it still stands for the current value: a value replaced by
                // another one whose item has not been fetched yet would otherwise leave the trigger
                // naming the selection before it.
                _selectedItems.RemoveAll(si => comparer.Equals(GetValue(si), CurrentValue) is false);
            }
        }

        ClassBuilder.Reset();
        SetIsSelectedForSelectedItems();
        RefreshOptions();
    }

    // The selected items are collected by walking the item list, which would report the selection in the
    // order of that list rather than in the order it was made. Values grows as the user picks, so
    // following it puts the chips, the joined text, the overflow summary and the Backspace of the
    // ComboBox (which removes the last selected item) in the order the user actually built.
    private void SortSelectedItemsByValues()
    {
        if (_selectedItems.Count < 2) return;
        if (Values is null) return;

        // A single pass over Values (which may be long after a select all) instead of a lookup per
        // selected item, so ordering the selection stays linear in the size of the selection.
        var indexes = new Dictionary<TValue, int>(Comparer);
        var nullIndex = -1;
        var index = 0;
        foreach (var value in Values)
        {
            if (value is null)
            {
                if (nullIndex < 0)
                {
                    nullIndex = index;
                }
            }
            else
            {
                indexes.TryAdd(value, index);
            }

            index++;
        }

        // OrderBy is stable, so the items whose value is no longer in Values (which only the
        // ItemsProvider flow can produce, between a value change and the next window) keep their
        // relative order at the end instead of being shuffled.
        _selectedItems = [.. _selectedItems.OrderBy(item =>
        {
            var value = GetValue(item);

            if (value is null) return nullIndex < 0 ? int.MaxValue : nullIndex;

            return indexes.TryGetValue(value, out var i) ? i : int.MaxValue;
        })];
    }

    // See OnSetIsOpen: the flows that follow AssignIsOpen with their own awaited ToggleCallout mark
    // the change as internal, so the hook does not toggle the callout a second time.
    private async Task<bool> AssignIsOpenInternal(bool value)
    {
        _internalIsOpenChange = true;
        try
        {
            return await AssignIsOpen(value);
        }
        finally
        {
            _internalIsOpenChange = false;
        }
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (IsOpen is false) return;

        _rateLimiter.Reset();
        _typeAheadBuffer = string.Empty;

        if (await AssignIsOpenInternal(false) is false) return;

        await ToggleCallout();
    }

    // Where the focus belongs once the callout gives it up: the ComboBox input is the editable part of
    // the trigger, so it takes the focus in place of the trigger element itself. The responsive panel
    // has an input of its own, but it goes away with the panel, so it only takes the focus while the
    // panel is actually on the screen.
    private ValueTask FocusTrigger()
    {
        if (Combo is false) return InputElement.FocusAsync();

        return (IsOpen && _isResponsiveMode ? _comboBoxInputResponsiveRef : _comboBoxInputRef).FocusAsync();
    }

    // Moves the focus back to the trigger after the component itself dismissed whatever had it, without
    // letting OpenOnFocus read that move as the user coming in. The flag is consumed by the focusin the
    // move produces; a move that never happens (a disconnected circuit, an element that is no longer on
    // the page) clears it here instead, so it cannot go on to swallow the next focus the user gives.
    private async Task RestoreFocusToTrigger()
    {
        _suppressOpenOnFocus = true;

        try
        {
            await FocusTrigger();
        }
        catch
        {
            _suppressOpenOnFocus = false;

            throw;
        }
    }

    // Dismissing the callout hides whatever inside it has the focus (an option, the search box), which
    // would otherwise drop the focus to the document body and strand a keyboard user at the top of the
    // page. Every dismissal the user asks for by hand goes through here so the focus comes back to the
    // dropdown, exactly where a native select leaves it.
    private async Task CloseCalloutAndRestoreFocus()
    {
        if (IsOpen is false) return;

        await CloseCallout();

        // A refused close (a one-way bound IsOpen) leaves the callout open, so the focus stays in it.
        if (IsOpen) return;

        await RestoreFocusToTrigger();
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        // The callback reports the click itself, so it fires before (and independently of) the opening:
        // a one-way bound IsOpen refuses the change, and the click still happened.
        await OnClick.InvokeAsync(e);

        // The focus this very click gave the dropdown may have opened the callout already (OpenOnFocus),
        // which leaves the open below with nothing to do - but the focus work after it, which is the
        // rest of what opening the list by pointer means, still has to run.
        var openedOnFocus = _openedOnFocus;
        _openedOnFocus = false;

        if (openedOnFocus is false)
        {
            if (await AssignIsOpenInternal(true) is false) return;

            await ToggleCallout();
        }

        await FocusOnComboBoxInput();
        await FocusOnSearchBox();

        // A pointer open mirrors the keyboard open: the focus (and with it the scroll) goes to the
        // selected item, or the first one, unless an input already claimed the focus above.
        if (Combo is false && (HasSearchBox && AutoFocusSearchBox) is false)
        {
            await FocusItem(BitDropdownFocusMode.Selected);
        }
    }

    // The default behavior of the navigation keys handled here is prevented by the keydown listener of
    // Dropdowns.ts (Blazor cannot preventDefault per key), so the keys handled here and the ones listed
    // there have to be kept in sync.
    private async Task HandleOnTriggerKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            // The ComboBox input handles its own Escape (it has a typed term to drop first) and its
            // keydown bubbles through here, so acting on it a second time would let the very press
            // that closed the callout go on to clear the selection as well. Only a press the input
            // did not already answer - the focus sits on the trigger around it - is handled here.
            if (Combo && _inputComboHasFocus) return;

            if (IsOpen)
            {
                await CloseCallout();
            }
            else
            {
                await ClearOnEscapeKey();
            }

            return;
        }

        // Tab moves the focus out of the dropdown, and a popup the focus has left is a popup nothing
        // can dismiss any more: the callout is rendered at the end of the document, so it is not even
        // next in the tab order. It reaches here from the trigger itself (opened with Alt+ArrowDown) and
        // from the ComboBox input, whose keydown bubbles through the trigger.
        if (e.Key is "Tab")
        {
            await CloseCallout();
            return;
        }

        // Alt+ArrowUp is the "dismiss the popup" shortcut of the APG combobox pattern, so it must not
        // be treated as the plain ArrowUp that opens it; from the trigger the popup is either already
        // closed (nothing to do) or open with the focus inside it (handled by HandleOnCalloutKeyDown).
        if (e.AltKey && e.Key is "ArrowUp")
        {
            await CloseCallout();
            return;
        }

        // Alt+ArrowDown is the "reveal the popup" shortcut of the APG combobox pattern, which shows the
        // list without moving the focus into it, so the trigger (or the combo input being typed into)
        // keeps it and the plain arrows can still walk the list afterwards.
        if (e.AltKey && e.Key is "ArrowDown")
        {
            await OpenCallout();
            return;
        }

        if (Combo)
        {
            // Typing is handled by the combo input itself; only the arrow keys manage the callout here.
            if (e.Key is "ArrowDown" or "ArrowUp")
            {
                await OpenCallout();
                await FocusItem(BitDropdownFocusMode.Selected);
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

                // A keyboard open mirrors the pointer open: a search-first dropdown (AutoFocusSearchBox)
                // hands the focus to the search box instead of the selected item. The arrow keys below
                // keep focusing the items either way, since pressing them is asking to walk the list.
                if (HasSearchBox && AutoFocusSearchBox)
                {
                    await FocusOnSearchBox();
                }
                else
                {
                    await FocusItem(BitDropdownFocusMode.Selected);
                }
            }
        }
        else if (e.Key is "ArrowDown" or "ArrowUp")
        {
            await OpenCallout();
            await FocusItem(BitDropdownFocusMode.Selected);
        }
        // Home and End jump to the ends of the list from the closed dropdown as well, so reaching the
        // last of a long list does not require opening it first and then pressing a second key. There is
        // no caret to move here (the ComboBox mode, which has one, returned above).
        else if (e.Key is "Home" or "End")
        {
            await OpenCallout();
            await FocusItem(e.Key is "Home" ? BitDropdownFocusMode.First : BitDropdownFocusMode.Last);
        }
        else if (IsPrintableKey(e))
        {
            await OpenCallout();
            await FocusItem(BitDropdownFocusMode.Char, GetTypeAheadBuffer(e.Key!));
        }
    }

    // See the note on HandleOnTriggerKeyDown about keeping these keys in sync with Dropdowns.ts.
    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || IsOpen is false) return;

        switch (e.Key)
        {
            case "ArrowDown":
                await FocusItem(BitDropdownFocusMode.Next);
                break;
            case "ArrowUp":
                // Alt+ArrowUp dismisses the popup and returns to the trigger, per the APG combobox pattern.
                if (e.AltKey)
                {
                    await CloseCalloutAndRestoreFocus();
                }
                else
                {
                    await FocusItem(BitDropdownFocusMode.Prev);
                }
                break;
            case "PageDown":
                await FocusItem(BitDropdownFocusMode.NextPage);
                break;
            case "PageUp":
                await FocusItem(BitDropdownFocusMode.PrevPage);
                break;
            case "Home":
            case "End":
                // Home/End keep their caret behavior while typing in the search/combo inputs.
                if (_inputSearchHasFocus is false && _inputComboHasFocus is false)
                {
                    await FocusItem(e.Key is "Home" ? BitDropdownFocusMode.First : BitDropdownFocusMode.Last);
                }
                break;
            case "Escape":
                await CloseCalloutAndRestoreFocus();
                break;
            case "Tab":
                // The callout is rendered at the end of the document, so leaving it without moving the
                // focus back to the dropdown would continue the tab order from an unrelated place.
                await CloseCalloutAndRestoreFocus();
                break;
            default:
                // Ctrl+A (or Cmd+A) selects all the items in multi select mode - and clears them when
                // they are all selected already - per the APG listbox pattern. Inside the search/combo
                // inputs the shortcut keeps its native select-the-text behavior instead. Like the
                // select all item, it is unavailable with an ItemsProvider, where only the loaded
                // window of the items is known and "all" would silently mean an arbitrary subset.
                if (MultiSelect && ItemsProvider is null && (e.CtrlKey || e.MetaKey) && e.Key is "a" or "A" &&
                    _inputSearchHasFocus is false && _inputComboHasFocus is false)
                {
                    await HandleOnSelectAllClick();
                }
                // In Combo mode the combo input is the type-ahead, and printable keys
                // typed into the search box must keep filtering instead of moving focus.
                else if (Combo is false && _inputSearchHasFocus is false && IsPrintableKey(e))
                {
                    await FocusItem(BitDropdownFocusMode.Char, GetTypeAheadBuffer(e.Key!));
                }
                break;
        }
    }

    // Keep this in sync with the isPrintable helper of Dropdowns.ts.
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

    private ValueTask FocusItem(BitDropdownFocusMode mode, string? character = null)
    {
        return _js.BitDropdownsFocusItem(_calloutId, mode, character, Virtualize, GetSelectedItemIndex(mode), ItemSize, NoWrapNavigation);
    }

    // Where the selected item sits in the rendered list, which virtualization needs in order to reach it:
    // only the items around the visible window exist in the DOM there, so a selection further down cannot
    // be found - let alone focused - before the list has been scrolled to it, and a dropdown opened on a
    // long list would show its top rather than what is currently selected.
    // It is only knowable over a local collection; with an ItemsProvider the loaded window is all the
    // component has, and the item of a value whose page was never fetched has no index to scroll to.
    private int GetSelectedItemIndex(BitDropdownFocusMode mode)
    {
        if (mode != BitDropdownFocusMode.Selected) return -1;
        if (Virtualize is false || ItemsProvider is not null) return -1;

        var selected = _selectedItems.FirstOrDefault();
        if (selected is null) return -1;

        var index = 0;
        foreach (var item in GetDisplayItems())
        {
            if (ReferenceEquals(item, selected)) return index;

            index++;
        }

        return -1;
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        UpdateSelectedItemsFromValues();
    }

    // These follow the focus of the whole dropdown, which is why they sit on the trigger and not on a
    // single element: focusin/focusout bubble, so the inner combo input is covered by them as well.
    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        // The focus this component moves back to the trigger itself (after a dismissal, or a pick that
        // closed the callout) must not reopen what was just closed, so only a focus that was not
        // preceded by such a move counts as the user coming in.
        var suppressed = _suppressOpenOnFocus;
        _suppressOpenOnFocus = false;

        if (OpenOnFocus && suppressed is false)
        {
            var wasOpen = IsOpen;

            await OpenCallout();

            // Remembered for the click this focus may be the first half of, so that the click does not
            // read an already open callout as one it has nothing left to do about. It is cleared by the
            // next toggle of the callout, so a click that comes long after the focus (and after the
            // callout was dismissed in between) opens the callout itself as it always did.
            _openedOnFocus = IsOpen && wasOpen is false;
        }

        await OnFocusIn.InvokeAsync(e);
    }

    private Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return Task.CompletedTask;

        return OnFocusOut.InvokeAsync(e);
    }

    private Task HandleOnLabelClick()
    {
        if (IsEnabled is false) return Task.CompletedTask;

        return FocusTrigger().AsTask();
    }

    private void HandleSearchBoxFocusIn()
    {
        _inputSearchHasFocus = true;
    }

    private void HandleSearchBoxFocusOut()
    {
        _inputSearchHasFocus = false;
    }

    private async Task HandleComboInputFocusIn()
    {
        _inputComboHasFocus = true;

        await SelectComboInputText();
    }

    // The term already in the input is selected when it takes the focus, so that typing replaces it
    // instead of appending to it - which is what the user coming back to search for something else is
    // about to do. An empty input has nothing to select, and neither has a read-only one, where the
    // selection would only be a highlight over text the user cannot change.
    private async ValueTask SelectComboInputText()
    {
        if (SelectTextOnFocus is false) return;
        if (Combo is false || IsEnabled is false || ReadOnly) return;
        if (_comboInputText.HasNoValue()) return;
        if (IsRendered is false || IsDisposed) return;

        var element = ComboInputElement;
        if (element is null) return;

        try
        {
            await _js.BitUtilsSelectText(element.Value);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (InvalidOperationException) { } // an input that is not on the page has no text to select
    }

    private void HandleComboInputFocusOut()
    {
        _inputComboHasFocus = false;
    }

    private async Task HandleSearchBoxOnClear()
    {
        await ClearSearchBox();

        // The clear button only renders while there is a text, so activating it removes it from under
        // the focus; the focus moves to the input it emptied instead of dropping to the document body.
        await _searchInputRef.FocusAsync();
    }

    private async Task HandleOnSearchBoxInput(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (HasSearchBox is false) return;

        // What the input actually holds, which is what the dropdown renders back into it and what the
        // parts that describe the field rather than the result - the clear button, the too-short hint -
        // follow. It is kept apart from the search term because that term deliberately lags behind the
        // typing: by a debounce, or (without Immediate) until the input is committed.
        _searchInputText = e.Value?.ToString();

        if (Immediate is false) return;

        await _rateLimiter.Run(e, DebounceTime, ThrottleTime, async args =>
            await InvokeAsync(async () => await SearchItems(args)));
    }

    private async Task HandleOnSearchBoxChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (HasSearchBox is false) return;

        _searchInputText = e.Value?.ToString();

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
        // Not ShowSearchBox: in the ComboBox mode the search text belongs to the combo input, and
        // clearing it from here would leave ClearComboBoxInput with nothing to do and the input
        // holding the term it was supposed to empty.
        if (HasSearchBox is false) return;
        // Both are checked: without Immediate a term that was typed but never committed is only in the
        // input, and the input is exactly what this has to leave empty.
        if (_searchText.HasNoValue() && _searchInputText.HasNoValue()) return;

        _rateLimiter.Reset();

        var hadSearchText = _searchText.HasValue();

        _searchText = null;
        _searchInputText = null;

        // The term was never applied, so there is no search to re-run and nothing to report: only the
        // text the user abandoned in the input is gone, which the render that follows takes care of.
        if (hadSearchText is false) return;

        RefreshOptions();

        await OnSearch.InvokeAsync(_searchText);
        await SearchVirtualized();
    }

    private void OnSetIsOpen()
    {
        // Captured now: the lambda below runs later, so a rapid second change to IsOpen before it has
        // run must not make both invocations act on the same (latest) state.
        var isOpen = IsOpen;

        // The internal open/close flows toggle the callout themselves right after assigning IsOpen,
        // so they can await the toggle and order their focus work after it. The hook only toggles for
        // a change pushed from the outside through the IsOpen parameter (or a programmatic Assign),
        // which otherwise has no path to the JS side that actually shows and hides the callout.
        // Before the first render there is no element to toggle (and during prerendering not even a
        // JS runtime to call); an initial IsOpen is applied by OnAfterRenderAsync instead.
        var toggle = _internalIsOpenChange is false && IsRendered;

        // The hook of a [CallOnSet] parameter is synchronous, so the work is fired and forgotten.
        // Wrapped in a local async method (instead of separate discarded tasks) so the steps run in
        // order rather than racing over _searchText, and so a throwing one surfaces through Blazor's
        // normal async error handling via the renderer dispatcher instead of on an unobserved task.
        _ = InvokeAsync(async () =>
        {
            // The search text is only dropped when the callout closes: a text that was typed but never
            // committed to a selection must not survive the callout, otherwise the trigger keeps showing
            // a filter term instead of the current selection the next time the dropdown is opened.
            // On open there is nothing to drop - and in ComboBox mode the typing itself is what opens
            // the callout, so clearing here would wipe the very term the items are being filtered by.
            if (isOpen is false)
            {
                // A type-ahead that was left half typed must not continue into the next opening, the
                // same way an uncommitted search term does not survive the callout.
                _typeAheadBuffer = string.Empty;

                await ClearSearchBox();
                await ClearComboBoxInput();
            }

            if (toggle)
            {
                await ToggleCallout();
            }

            await (isOpen ? OnOpen.InvokeAsync() : OnClose.InvokeAsync());
        });
    }

    // Whether the search box element (and with it the reference to its input) actually exists. The
    // ComboBox mode filters through the input of the dropdown itself, so the search box is not
    // rendered there however ShowSearchBox is set - and focusing a reference that was never assigned
    // throws rather than doing nothing.
    private bool HasSearchBox => ShowSearchBox && Combo is false;

    private async ValueTask FocusOnSearchBox()
    {
        if (IsEnabled is false) return;
        if (HasSearchBox is false) return;
        if (AutoFocusSearchBox is false) return;
        if (IsOpen is false) return;

        await _searchInputRef.FocusAsync();
    }

    private async Task ClearComboBoxInput()
    {
        if (ReadOnly) return;
        if (Combo is false) return;
        if (IsEnabled is false) return;
        // Both are checked: a term the rate limiter has not applied yet is still in the input, and the
        // input is exactly what this has to leave empty.
        if (_searchText.HasNoValue() && _comboInputText.HasNoValue()) return;

        _rateLimiter.Reset();

        _searchText = null;
        _comboInputText = null;

        // The rendered value attribute alone cannot always empty the input: when the text was set and
        // cleared within the same event - a pasted list of terms committed through TokenSeparators, for
        // instance - the value the renderer holds never changes, so it emits no edit and the input keeps
        // what was typed into it. The element is emptied directly, which is what this method promises.
        await ClearComboInputValue();

        RefreshOptions();

        await OnSearch.InvokeAsync(_searchText);

        // The items of a virtualized combo box come from the ItemsProvider filtered by the search
        // text, so dropping the text has to re-request them just like clearing the search box does.
        await SearchVirtualized();
    }

    private async ValueTask ClearComboInputValue()
    {
        if (Combo is false || IsRendered is false || IsDisposed) return;

        try
        {
            await _js.BitUtilsSetProperty(_comboBoxInputRef, "value", string.Empty);

            // The responsive panel has an input of its own, and it only exists while the panel is on
            // the screen. Its reference is assigned a render later than the flag, so a clear that lands
            // in between finds nothing to empty - which the catch below turns into a no-op.
            if (_isResponsiveMode && IsOpen)
            {
                await _js.BitUtilsSetProperty(_comboBoxInputResponsiveRef, "value", string.Empty);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (InvalidOperationException) { } // an input that is not on the page has no value to clear
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

    // The search text with its diacritics folded away when SearchIgnoreDiacritics is on, computed once
    // per search instead of once per item since every item is matched against the same term.
    private string? FoldedSearchText
    {
        get
        {
            var search = SearchText;

            if (search is null || SearchIgnoreDiacritics is false) return search;

            if (_foldedSearchTextKey != search)
            {
                _foldedSearchTextKey = search;
                _foldedSearchText = RemoveDiacritics(search);
            }

            return _foldedSearchText;
        }
    }

    // A copy of the text with the diacritic of each character removed. It is folded one character at a
    // time on purpose: the result then has exactly as many characters as the original, so an index found
    // in it still points at the same place in the text the search highlight has to cut.
    private static string RemoveDiacritics(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            // ASCII has no diacritics to fold, and a lone surrogate half (one of the two chars an emoji
            // is made of) is not a valid string of its own, so normalizing it would throw.
            if (char.IsAscii(c) || char.IsSurrogate(c))
            {
                builder.Append(c);
                continue;
            }

            var baseChar = c;

            foreach (var decomposed in c.ToString().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(decomposed) == UnicodeCategory.NonSpacingMark) continue;

                baseChar = decomposed;
                break;
            }

            builder.Append(baseChar);
        }

        return builder.ToString();
    }

    internal bool IsItemTextMatch(string? text)
    {
        if (text is null) return false;

        var search = FoldedSearchText;
        if (search is null) return true;

        if (SearchIgnoreDiacritics)
        {
            text = RemoveDiacritics(text);
        }

        return SearchMode switch
        {
            BitDropdownSearchMode.StartsWith => text.StartsWith(search, StringComparison.OrdinalIgnoreCase),
            BitDropdownSearchMode.EndsWith => text.EndsWith(search, StringComparison.OrdinalIgnoreCase),
            BitDropdownSearchMode.ExactMatch => text.Equals(search, StringComparison.OrdinalIgnoreCase),
            _ => text.Contains(search, StringComparison.OrdinalIgnoreCase)
        };
    }

    // The index at which the item text matched the search, so the default item rendering can highlight
    // exactly the matched part. Returns -1 when there is nothing to highlight.
    internal int GetHighlightIndex(string? text)
    {
        if (HighlightSearch is false) return -1;
        if (text is null) return -1;

        var search = FoldedSearchText;
        if (search is null) return -1;

        // Folding keeps the character count, so an index found in the folded text is also the index of
        // the matched part of the original one - which is the text the highlight actually cuts.
        if (SearchIgnoreDiacritics)
        {
            text = RemoveDiacritics(text);
        }

        return SearchMode switch
        {
            BitDropdownSearchMode.StartsWith => text.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 0 : -1,
            BitDropdownSearchMode.EndsWith => text.EndsWith(search, StringComparison.OrdinalIgnoreCase) ? text.Length - search.Length : -1,
            BitDropdownSearchMode.ExactMatch => text.Equals(search, StringComparison.OrdinalIgnoreCase) ? 0 : -1,
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
        // An ItemsProvider receives the search text and returns the items it already matched, by
        // whatever rule the data source applies. Running the local algorithm over that window again
        // would drop everything the source matched in a way the local one cannot reproduce.
        if (ItemsProvider is not null) return _lastShownItems;

        var items = Items;
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
            // The headers and the dividers are kept: a group is part of what an item is, so a search
            // that flattened the result into a bare list would take that away exactly when the list is
            // hardest to read. Only the ones left standing for nothing are dropped, by GetDisplayItems.
            _searchedItems = SearchFunction is not null
                ? [.. SearchFunction.Invoke(items, search)]
                : [.. items.Where(i => GetItemType(i) != BitDropdownItemType.Normal || IsItemTextMatch(GetText(i)))];
        }

        return _searchedItems;
    }

    // Whether the headers and dividers that no longer stand for anything have to be collapsed, which is
    // the case as soon as items can be missing from between them: a search filtered them out, or they are
    // hidden because they are already selected. An ItemsProvider is left out of it because it hands over
    // one window of the list at a time, where a header at the edge of the window still names the items of
    // the next one.
    private bool ShouldCollapseGroups => ItemsProvider is null && (HideSelectedItems || HasSearchText);

    // What the callout actually renders: the search result, minus the already selected items when they
    // are meant to disappear from the list, and minus the headers and dividers those removals leave
    // standing for nothing. Kept separate from GetSearchedItems so that the select all
    // item still works over the full result rather than over the leftovers. The result is cached like
    // the search itself, so that repeated reads during one render (and Virtualize, which re-renders
    // everything when handed a new collection instance) see the same list.
    private ICollection<TItem> GetDisplayItems()
    {
        var items = GetSearchedItems();

        if (ShouldCollapseGroups is false)
        {
            // Nothing is missing from between the headers and the dividers, so nothing is collapsed -
            // and a set left over from a state where something was must not keep hiding them.
            _collapsedItems = null;

            return items;
        }

        if (_displayItems is null ||
            _displayItemsCacheKey != SearchText ||
            _displayItemsCacheVersion != _optionsVersion ||
            _displayItemsSelectionVersion != _selectionVersion)
        {
            _displayItemsCacheKey = SearchText;
            _displayItemsCacheVersion = _optionsVersion;
            _displayItemsSelectionVersion = _selectionVersion;

            var visible = HideSelectedItems
                            ? items.Where(i => GetItemType(i) != BitDropdownItemType.Normal || GetIsSelected(i) is false).ToList()
                            : [.. items];

            // Filtering the items out - by a search, or because they are already selected - can leave a
            // group header naming nothing and a divider with a side missing, so the ones that no longer
            // stand for anything go with the items they framed.
            _collapsedItems = GetCollapsedItems(visible);
            _displayItems = _collapsedItems.Count == 0 ? visible : visible.FindAll(i => _collapsedItems.Contains(i) is false);
        }

        return _displayItems;
    }

    // The headers and dividers that no longer stand for anything, found in a single pass over the list.
    // A header stays as long as a visible normal item follows it before the next header does, and a
    // divider stays only while it has a visible item on both of its sides - the ordinary rule for
    // collapsing separators, which is what keeps a list whose items were removed from opening with a
    // rule, or ending with a group name and nothing under it.
    private HashSet<TItem> GetCollapsedItems(List<TItem> items)
    {
        HashSet<TItem> collapsed = [];

        TItem? pendingHeader = null;
        TItem? pendingDivider = null;
        var hasItemSinceDivider = false;

        foreach (var item in items)
        {
            var itemType = GetItemType(item);

            if (itemType == BitDropdownItemType.Header)
            {
                if (pendingHeader is not null)
                {
                    collapsed.Add(pendingHeader);
                }

                // A hidden header names nothing to begin with, so it is not one that can be left empty.
                pendingHeader = GetIsHidden(item) ? null : item;
                continue;
            }

            if (itemType == BitDropdownItemType.Divider)
            {
                if (GetIsHidden(item)) continue;

                // Nothing visible before it (the start of the list, or another divider), so it separates
                // nothing. Otherwise it is held until a visible item proves it has a side after it too.
                if (hasItemSinceDivider is false)
                {
                    collapsed.Add(item);
                    continue;
                }

                pendingDivider = item;
                hasItemSinceDivider = false;
                continue;
            }

            if (itemType != BitDropdownItemType.Normal) continue;
            if (GetIsHidden(item)) continue;

            pendingHeader = null;
            pendingDivider = null;
            hasItemSinceDivider = true;
        }

        if (pendingHeader is not null)
        {
            collapsed.Add(pendingHeader);
        }

        // A divider that is still being held reached the end of the list without a side after it.
        if (pendingDivider is not null)
        {
            collapsed.Add(pendingDivider);
        }

        return collapsed;
    }

    private string GetSearchBoxClasses()
    {
        var className = new StringBuilder("bit-drp-sb");

        if (_searchInputText.HasValue())
        {
            className.Append(" bit-drp-shv");
        }

        if (_inputSearchHasFocus)
        {
            className.Append(" bit-drp-shf");
        }

        return className.ToString();
    }

    // Whether the label element carrying _labelId is rendered, so nothing references a missing element.
    private bool HasLabel => LabelTemplate is not null || Label.HasValue();

    // The same, for the description element carrying _descriptionId.
    private bool HasDescription => DescriptionTemplate is not null || Description.HasValue();

    private string GetDropdownAriaLabelledby()
    {
        return HasLabel ? $"{_labelId} {_dropdownTextContainerId}" : _dropdownTextContainerId;
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

            // A copy, like every other call site: handing over the live Values collection (which a
            // refused assignment can even leave null) makes the subscriber's snapshot change under it.
            await OnValuesChange.InvokeAsync([.. (Values ?? [])!]);
        }
        else
        {
            if (InvalidValueBinding()) return;

            CurrentValue = default;

            _selectedItems.Clear();
        }

        UpdateSelectedItemsFromValues();

        // In ComboBox mode the typed term is part of what the dropdown is showing, so a button that
        // says it clears the selection has to leave the input empty as well instead of leaving behind
        // a filter the user has no visible reason to expect.
        await ClearComboBoxInput();

        await OnClear.InvokeAsync();
    }

    // Tells the empty state of an unsuccessful search apart from the empty state of an empty list, so
    // the callout can show "no results for what you typed" instead of "there is nothing here".
    private bool HasSearchText => SearchText is not null;

    // Reaching the selection limit disables every item that is not selected yet, which is a change only a
    // sighted user notices on their own, so it is said out loud once instead of leaving a screen reader
    // user to discover that the list stopped responding.
    private string GetMaxSelectedItemsText()
    {
        var max = MaxSelectedItems!.Value;

        return MaxSelectedItemsText is not null
                ? string.Format(MaxSelectedItemsText, max)
                : $"Maximum of {max} items selected";
    }

    private string GetSearchResultsText()
    {
        // With an ItemsProvider only a window of the results is loaded, so the number of results is
        // the one the provider reported rather than the size of that window.
        // Otherwise it is the list as it is actually rendered that is counted, and not the raw search
        // result: HideSelectedItems takes the items that are already selected out of it, and a number
        // that counted them would tell a screen reader user about results they cannot reach.
        var count = ItemsProvider is not null
            ? _providerTotalItems ?? _lastShownItems.Count
            : GetDisplayItems().Count(i => GetItemType(i) == BitDropdownItemType.Normal && GetIsHidden(i) is false);

        return SearchResultsText is not null
                ? string.Format(SearchResultsText, count)
                : count == 1 ? "1 result available" : $"{count} results available";
    }

    // The hint the callout shows while the typed text is still too short to filter by, so the full list
    // under it is explained instead of looking like a search that matched everything. It is null while
    // there is nothing to explain: without a MinSearchLength, and before anything has been typed.
    private string? GetMinSearchLengthText()
    {
        if (MinSearchLength <= 0) return null;

        // The text of the input rather than the applied term: the hint explains what is being typed,
        // so it has to follow the typing and not the (debounced, or uncommitted) search behind it.
        var text = Combo ? _comboInputText : _searchInputText;
        if (text.HasNoValue()) return null;

        var remaining = MinSearchLength - text!.Length;
        if (remaining <= 0) return null;

        return MinSearchLengthText is not null
                ? string.Format(MinSearchLengthText, remaining)
                : remaining == 1 ? "Type 1 more character to search" : $"Type {remaining} more characters to search";
    }

    private bool HasNoVisibleItems()
    {
        return GetDisplayItems().Any(i => GetItemType(i) == BitDropdownItemType.Normal && GetIsHidden(i) is false) is false;
    }

    // Read on every render of the callout, so the items are counted in place rather than collected into
    // a list first - the state only needs the counts, not the items themselves. HasCandidates comes out
    // of the same pass because the item is only rendered when there is something for it to select: an
    // empty list (or a search that matched nothing) would otherwise be topped by a control that cannot do
    // anything at all. The result is cached like the search it is computed from, because it is asked for
    // twice per render - once by the item itself and once by the height the item takes from the list -
    // and each answer is a full pass over the search results.
    private (bool HasCandidates, bool AllSelected, bool AnySelected) GetSelectAllState()
    {
        if (_selectAllState is not null &&
            _selectAllStateCacheKey == SearchText &&
            _selectAllStateCacheVersion == _optionsVersion &&
            _selectAllStateSelectionVersion == _selectionVersion) return _selectAllState.Value;

        _selectAllStateCacheKey = SearchText;
        _selectAllStateCacheVersion = _optionsVersion;
        _selectAllStateSelectionVersion = _selectionVersion;

        _selectAllState = ComputeSelectAllState();

        return _selectAllState.Value;
    }

    private (bool HasCandidates, bool AllSelected, bool AnySelected) ComputeSelectAllState()
    {
        var count = 0;
        var selectedCount = 0;

        foreach (var item in GetSearchedItems())
        {
            if (IsSelectAllCandidate(item) is false) continue;

            count++;

            if (GetIsSelected(item))
            {
                selectedCount++;
            }
        }

        if (count == 0) return (false, false, false);

        return (true, selectedCount == count, selectedCount > 0);
    }

    // Whether the select all item is on the screen, which the callout's own layout has to agree on: its
    // height is part of the space the scrollable item list cannot use (see GetCalloutScrollOffset).
    private bool HasSelectAllItem => MultiSelect && ShowSelectAll && ItemsProvider is null && IsLoading is false && GetSelectAllState().HasCandidates;

    private bool IsSelectAllCandidate(TItem item)
    {
        return GetItemType(item) == BitDropdownItemType.Normal && GetIsHidden(item) is false && GetIsEnabled(item);
    }

    private List<TItem> GetSelectAllCandidateItems()
    {
        return [.. GetSearchedItems().Where(IsSelectAllCandidate)];
    }

    private async Task HandleOnSelectAllClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;
        if (MultiSelect is false) return;
        if (ValuesHasBeenSet && ValuesChanged.HasDelegate is false) return;

        var candidates = GetSelectAllCandidateItems();
        if (candidates.Count == 0) return;

        var comparer = Comparer;

        List<TValue?> newValues;
        // Nothing more can be added once the selection limit is reached, so the item clears there too
        // rather than being a control that does nothing at all - the list below it says the same thing,
        // since every item that is not selected yet is disabled while the limit holds.
        if (candidates.TrueForAll(GetIsSelected) || IsMaxSelectedItemsReached)
        {
            // All (searched) items are selected, so the select all item clears them, keeping
            // the selected values that are not part of the current search results.
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

        // The items added dynamically in the ComboBox mode are not part of Items, so assigning the
        // values (which rebuilds the selected items from Items) would drop them; they are kept here
        // as long as their values are still selected.
        var dynamicItems = Combo && Dynamic
            ? _selectedItems.FindAll(si => Items?.Contains(si) is not true)
            : [];

        await AssignValues(newValues);

        foreach (var item in dynamicItems)
        {
            if (newValues.Exists(v => comparer.Equals(v, GetValue(item))) is false) continue;
            if (_selectedItems.Exists(si => comparer.Equals(GetValue(si), GetValue(item)))) continue;

            _selectedItems.Add(item);
            ClassBuilder.Reset();
        }

        await OnValuesChange.InvokeAsync([.. (Values ?? [])!]);
    }

    private async Task HandleOnAddItemComboClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;

        await AddDynamicItem();

        await ClearComboBoxInput();

        if (_isResponsiveMode && MultiSelect)
        {
            await _comboBoxInputResponsiveRef.FocusAsync();

            return;
        }

        // The add button goes away with the responsive panel it lives in, so the focus is restored to
        // the trigger along with the close instead of being left on a hidden element.
        await CloseCalloutAndRestoreFocus();
    }

    // The height available to the scrollable item list is the callout's height minus the parts that sit
    // above it, so every one of those parts has to be reported here or the callout overflows the
    // viewport. The values mirror the --bit-drp-h (search box) and --bit-drp-itm-h plus its bottom
    // border (select all row) of each size in the stylesheet.
    private int GetCalloutScrollOffset()
    {
        var offset = 0;

        if (HasSearchBox)
        {
            offset += Size switch { BitSize.Small => 26, BitSize.Large => 40, _ => 32 };
        }

        if (HasSelectAllItem)
        {
            offset += Size switch { BitSize.Small => 31, BitSize.Large => 45, _ => 37 };
        }

        return offset;
    }

    // The height available to the item list is only computed when the callout is laid out, which happens
    // when it is toggled. The parts above that list can come and go while it stays open - a search that
    // matches nothing takes the select all row with it - so a change to the offset is pushed to the
    // already positioned callout instead of waiting for the next open.
    private async Task RefreshCalloutScrollOffset()
    {
        if (IsOpen is false || IsDisposed || IsEnabled is false) return;

        var scrollOffset = GetCalloutScrollOffset();
        if (scrollOffset == _calloutScrollOffset) return;

        _calloutScrollOffset = scrollOffset;

        try
        {
            await _js.BitCalloutUpdateScrollOffset(_calloutId, scrollOffset);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false || IsDisposed) return;

        // Every open and close of the callout goes through here, which is where the note the focus
        // handler left for the click it may be paired with stops being about the current state.
        _openedOnFocus = false;

        _calloutScrollOffset = GetCalloutScrollOffset();

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
            scrollOffset: _calloutScrollOffset,
            headerId: CalloutHeaderTemplate is not null ? _headerId : "",
            footerId: CalloutFooterTemplate is not null ? _footerId : "",
            setCalloutWidth: PreserveCalloutWidth is false,
            fixedCalloutWidth: false,
            maxWindowWidth: 0,
            maxHeight: MaxHeight is > 0 ? MaxHeight.Value : 0);
    }

    private async ValueTask<ItemsProviderResult<TItem>> InternalItemsProvider(ItemsProviderRequest request)
    {
        // An empty result rather than the default one, whose Items is null: Virtualize skips the result
        // of a cancelled request, but nothing guarantees it skips this one.
        if (ItemsProvider is null) return new ItemsProviderResult<TItem>([], 0);

        // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
        // The token is not passed to the delay on purpose: a cancellation is a normal outcome here (the
        // user kept scrolling or typing) and is reported by returning an empty result, not by throwing.
        if (ItemsProviderDebounceTime > 0)
        {
            await Task.Delay(ItemsProviderDebounceTime);
        }

        if (request.CancellationToken.IsCancellationRequested) return new ItemsProviderResult<TItem>([], 0);

        // Combine the query parameters from Virtualize with the ones from PaginationState
        var providerRequest = new BitDropdownItemsProviderRequest<TItem>(request.StartIndex, request.Count, SearchText, request.CancellationToken);
        var providerResult = await ItemsProvider(providerRequest);

        if (request.CancellationToken.IsCancellationRequested) return new ItemsProviderResult<TItem>([], 0);

        // The result is a struct, so a provider that hands back the default one - a guard clause, a
        // caught exception, a request it decided not to answer - carries no Items collection at all,
        // which every read of it below would otherwise dereference.
        ICollection<TItem> providerItems = providerResult.Items ?? [];

        _lastShownItems = [.. providerItems];
        _providerTotalItems = providerResult.TotalItemCount;

        // Where each item of this window sits in the whole set, so an option can report its place in it
        // (see GetItemPosInSet). A window that repeats an item keeps its first occurrence.
        _providerPositions = [];
        for (var i = 0; i < _lastShownItems.Count; i++)
        {
            _providerPositions.TryAdd(_lastShownItems[i], request.StartIndex + i + 1);
        }

        // The caches are keyed on the search text and the options version, neither of which changes when
        // the provider hands over a different window of items for the same search, so they have to be
        // dropped here or the new window would be filtered and positioned against the previous one.
        ResetItemCaches();

        UpdateSelectedItemsFromValues();
        await InvokeAsync(StateHasChanged);

        return new ItemsProviderResult<TItem>(providerItems, providerResult.TotalItemCount);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;

        if (eventArgs.Key == "Escape")
        {
            // What this press has to take back before it can reach the selection: a callout on the
            // screen, and a term the user typed into the input. Both are read before they are dropped.
            var hadSomethingToDismiss = IsOpen || _comboInputText.HasValue();

            // Dropping the text through ClearComboBoxInput (rather than by assigning it here) is what
            // re-runs the search: an ItemsProvider would otherwise keep serving the abandoned term.
            await ClearComboBoxInput();

            await CloseCallout();

            if (hadSomethingToDismiss is false)
            {
                await ClearOnEscapeKey();
            }
        }
        else if (eventArgs.Key == "Enter")
        {
            // Read from the DOM rather than from the field: the keystroke that produced the current text
            // may not have reached the input handler yet, and a debounced search is meant to lag behind
            // it anyway, while Enter has to act on exactly what the user is looking at.
            _comboInputText = await _js.BitUtilsGetProperty(_isResponsiveMode ? _comboBoxInputResponsiveRef : _comboBoxInputRef, "value");
            _searchText = _comboInputText;

            await AddDynamicItem();

            await ClearComboBoxInput();

            if (_isResponsiveMode && MultiSelect) return;

            await CloseCallout();
        }
        else if (eventArgs.Key == "Backspace" && _comboInputText.HasNoValue())
        {
            await RemoveLastSelectedItem();
        }
    }

    private async Task HandleOnClickUnselectItem(TItem? item)
    {
        await UnselectItem(item);

        // The remove button goes away with the chip it belongs to, so a removal that stood moves the
        // focus to the trigger instead of leaving it on an element that is no longer on the page. A
        // refused removal (a one-way binding) keeps the chip, and the focus with it.
        if (item is not null && GetIsSelected(item) is false)
        {
            await RestoreFocusToTrigger();
        }
    }

    // Escape with nothing left to dismiss takes back the selection, which is what a keyboard user
    // otherwise has to reach the clear button for. It goes through the very same clear, so it reports
    // itself through OnClear and is refused wherever that button would be.
    private Task ClearOnEscapeKey()
    {
        if (ClearOnEscape is false) return Task.CompletedTask;
        if (_selectedItems.Count == 0) return Task.CompletedTask;

        return HandleOnClearClick();
    }

    private async Task HandleOnClearButtonClick()
    {
        await HandleOnClearClick();

        // The clear button only renders while something is selected, so a clear that stood removes it
        // from under the focus; the focus moves to the trigger it belongs to instead of dropping to
        // the document body. A refused clear (a one-way binding) keeps the button, and the focus.
        if (_selectedItems.Count == 0)
        {
            await RestoreFocusToTrigger();
        }
    }

    private async Task HandleOnComboInput(ChangeEventArgs e)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;

        // What the input actually holds, which is what the dropdown has to render back into it and what
        // the Enter and Backspace keys act on. It is kept apart from the search term because a debounced
        // or throttled search deliberately lags behind the typing.
        _comboInputText = e.Value?.ToString();

        // A separator in the text (typed, or arriving all at once in a paste) marks it as a list of
        // finished terms, which are committed right away instead of being searched for as one string.
        if (MultiSelect && TokenSeparators is { Length: > 0 } && _comboInputText.HasValue() &&
            _comboInputText!.IndexOfAny(TokenSeparators) >= 0)
        {
            await CommitTokens(_comboInputText!);
            return;
        }

        // A rate limit only has something to govern when it actually delays the search, so it is the
        // configured debounce or throttle - and not Immediate on its own - that defers the term. The
        // list is still revealed at once: the user is typing into it, and waiting out a debounce before
        // showing anything at all would leave them typing at a closed dropdown.
        if (Immediate && (DebounceTime > 0 || ThrottleTime > 0))
        {
            await OpenCallout();

            await _rateLimiter.Run(e, DebounceTime, ThrottleTime, async args =>
                await InvokeAsync(async () => await SearchComboItems(args)));

            return;
        }

        // Otherwise the term is applied as it is typed, all of it: the local filtering, the OnSearch
        // callback and the ItemsProvider request. Filtering the list while leaving the two of them for
        // the change event used to make a locally filtered combo box and a server filtered one behave
        // like two different components.
        await SearchComboItems(e);
    }

    // Commits each separated term like Enter would: through AddDynamicItem, so an existing item gets
    // selected, a term the selection already covers is refused, and a new item is only created when
    // Dynamic allows it. Every term is committed - typing produces at most one (the separator keystroke
    // itself lands here), and in the paste that produces several the text after the last separator is
    // a term of its own, not one the user is still typing.
    private async Task CommitTokens(string text)
    {
        foreach (var token in text.Split(TokenSeparators!, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            _comboInputText = token;
            _searchText = token;

            await AddDynamicItem();
        }

        await ClearComboBoxInput();
    }

    private async Task HandleOnComboChange(ChangeEventArgs e)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;

        _comboInputText = e.Value?.ToString();

        if (Immediate) return;

        // The input handler has already searched for exactly this term, so the commit of the same text
        // has nothing left to do - and running it again would report the search twice. An emptied input
        // and a term that was never typed are the same nothing here, whichever of the two the browser
        // reports.
        if (string.Equals(_searchText ?? string.Empty, _comboInputText ?? string.Empty, StringComparison.Ordinal)) return;

        // The change event of an input is the commit of what was typed, which the browser raises when the
        // input loses the focus (and when Enter is pressed). Both of those are moments the dropdown has
        // just dismissed the callout for, so the search here must not reveal it again behind the user.
        await SearchComboItems(e, openCallout: false);
    }

    private async Task SearchComboItems(ChangeEventArgs e, bool openCallout = true)
    {
        _searchText = e.Value?.ToString();

        RefreshOptions();

        await OnSearch.InvokeAsync(_searchText);

        await SearchVirtualized();

        if (openCallout)
        {
            await OpenCallout();
        }
    }

    private async Task OpenCallout()
    {
        if (IsOpen) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpenInternal(true) is false) return;

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

        // The text of the input, not the (possibly debounced) search term: this acts on what the user
        // typed and is looking at.
        if (_comboInputText.HasNoValue()) return;

        if (IsTextAlreadySelected(_comboInputText!)) return;

        var target = FindCommitTarget(_comboInputText!);
        if (target is not null)
        {
            await AddOrRemoveSelectedItem(target);

            return;
        }

        if (Dynamic is false) return;

        var text = _comboInputText;
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

            // Only a selection that stands is an item that was added: a refused one (the selection
            // limit is reached, the binding is one-way) never became part of anything.
            if (await AddOrRemoveSelectedItem(dropdownItem as TItem, true))
            {
                await OnDynamicAdd.InvokeAsync(dropdownItem as TItem);
            }
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

            if (await AddOrRemoveSelectedItem(dropdownOption as TItem, true))
            {
                await OnDynamicAdd.InvokeAsync(dropdownOption as TItem);
            }
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

            if (await AddOrRemoveSelectedItem(customItem, true))
            {
                await OnDynamicAdd.InvokeAsync(customItem);
            }
        }
    }

    // Whether the given text already stands for one of the selected items, which is what stops the same
    // item from being selected (or created) twice under a name the data considers equivalent. Both the
    // commit itself and the row that offers to create an item go through here, so the row cannot offer
    // something the commit would refuse.
    private bool IsTextAlreadySelected(string text)
    {
        if (_selectedItems.Count == 0) return false;

        return ExistsSelectedItemFunction is not null
                ? ExistsSelectedItemFunction.Invoke(_selectedItems, text)
                : _selectedItems.Exists(i => GetText(i).HasValue() && text.Equals(GetText(i)!, StringComparison.OrdinalIgnoreCase));
    }

    // The existing item a commit of the given text would select, or null when the commit would create a
    // new item (or do nothing at all). A term that names an item exactly takes that item; failing that,
    // AutoSelectFirstMatch lets a partially typed term stand for the first item it still matches, which
    // is what lets an autocomplete be committed with a single Enter - and it comes second on purpose, so
    // a term that names an item the list already has selects it rather than creating a near-duplicate.
    // The first-match branch only applies while a term is actually filtering the list: without one (or
    // with one still shorter than MinSearchLength) the displayed items are the whole list, and its first
    // item is not a match of anything the user typed.
    // Both the commit itself and the cue the list shows go through here, so the highlighted item and the
    // one Enter takes can never disagree.
    private TItem? FindCommitTarget(string text)
    {
        var searchItems = ItemsProvider is not null ? _lastShownItems : Items;
        if (searchItems is not null && searchItems.Count > 0)
        {
            var item = FindItemFunction is not null ?
                       FindItemFunction.Invoke(searchItems, text) :
                       searchItems.FirstOrDefault(i => IsCommitCandidate(i) && GetText(i).HasValue() && text.Equals(GetText(i)!, StringComparison.OrdinalIgnoreCase));

            // The item a custom FindItemFunction picked is checked as well: what it returns is committed
            // exactly as a click on the item would be, and a click cannot take a group header, an item
            // that is not on the screen or one the list shows as unavailable either.
            if (item is not null && IsCommitCandidate(item) && GetIsSelected(item) is false) return item;
        }

        if (AutoSelectFirstMatch && HasSearchText)
        {
            return GetDisplayItems().FirstOrDefault(i => IsCommitCandidate(i) && GetIsSelected(i) is false);
        }

        return null;
    }

    // Whether an item is one a commit of the typed text may take. It is the same set of conditions
    // HandleOnItemClick enforces for a click, so typing the exact text of a group header, of an item
    // that is hidden, or of a disabled one cannot select what clicking it would refuse - and the cue
    // the list shows (and reports through aria-activedescendant) cannot point at such an item either.
    private bool IsCommitCandidate(TItem item)
    {
        return GetItemType(item) == BitDropdownItemType.Normal && GetIsHidden(item) is false && GetIsEnabled(item);
    }

    // The id the commit target is referenced by from the ComboBox input, for the items that carry no id
    // of their own. Only one item is the target at a time, so a single id is enough to stay unique.
    internal string CommitTargetId => $"{_dropdownId}-ctg";

    // Whether the given item is the one a commit would take, which the list shows so that the item Enter
    // is about to select is visible before it is pressed rather than only after. The lookup is cached on
    // the typed text (and the versions the item data and the selection are keyed by), since every
    // rendered item asks about itself.
    internal bool GetIsCommitTarget(TItem item)
    {
        var target = GetCommitTarget();

        return target is not null && ReferenceEquals(target, item);
    }

    // The id of the element the ComboBox input points at with aria-activedescendant, so the item that is
    // only visually indicated is reported to a screen reader as well. Null when nothing is indicated.
    private string? GetCommitTargetId()
    {
        var target = GetCommitTarget();

        if (target is not null) return GetId(target) ?? CommitTargetId;

        // Nothing existing is the target, but the typed text may still be about to become an item of
        // its own, and the row offering to create it is what Enter would then take.
        return GetDynamicItemText() is not null ? DynamicItemId : null;
    }

    // The id of the row that offers to create an item out of the typed text, which the ComboBox input
    // points at with aria-activedescendant while the row is what a commit would take.
    internal string DynamicItemId => $"{_dropdownId}-dyn";

    // The text the callout offers to create a new item out of, or null when there is nothing to offer:
    // outside of the Dynamic ComboBox mode, with nothing typed, while the typed text names an item the
    // commit would take instead, while it names one of the selections already, and once the selection
    // limit leaves no room for another item at all. It mirrors AddDynamicItem exactly, so the row can
    // never offer something pressing Enter would refuse.
    private string? GetDynamicItemText()
    {
        if (Combo is false || Dynamic is false) return null;
        if (ReadOnly || IsEnabled is false) return null;
        if (_comboInputText.HasNoValue()) return null;
        if (IsMaxSelectedItemsReached) return null;
        if (IsTextAlreadySelected(_comboInputText!)) return null;
        if (GetCommitTarget() is not null) return null;

        return _comboInputText;
    }

    private string GetDynamicItemDisplayText(string text)
    {
        return DynamicItemTextFormat is not null ? string.Format(DynamicItemTextFormat, text) : $"Add \"{text}\"";
    }

    // Picking the row is the pointer equivalent of pressing Enter in the input, so it goes through the
    // very same commit and follows the close behavior of the mode the dropdown is in.
    private async Task HandleOnDynamicItemClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;

        var wasOpen = IsOpen;

        await AddDynamicItem();

        await ClearComboBoxInput();

        // A multi select callout stays open so the next item can be typed right away, exactly as it
        // does after an item is picked from the list.
        if (MultiSelect && CloseOnSelect is true)
        {
            await CloseCallout();
        }

        // The row goes away with the text it stood for either way, so the focus never stays on it: it
        // returns to the combo input, which is where the next term is typed.
        if (wasOpen)
        {
            await RestoreFocusToTrigger();
        }
    }

    private TItem? GetCommitTarget()
    {
        if (Combo is false || ReadOnly || IsEnabled is false) return null;
        if (_comboInputText.HasNoValue()) return null;

        if (_commitTargetCacheKey != _comboInputText ||
            _commitTargetCacheVersion != _optionsVersion ||
            _commitTargetSelectionVersion != _selectionVersion)
        {
            _commitTargetCacheKey = _comboInputText;
            _commitTargetCacheVersion = _optionsVersion;
            _commitTargetSelectionVersion = _selectionVersion;
            _commitTarget = FindCommitTarget(_comboInputText!);
        }

        return _commitTarget;
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

    // The items the overflow indicator stands for, so they can still be read (as its tooltip and as
    // part of its accessible name) instead of being reduced to a bare number.
    private string GetOverflowItemsText()
    {
        return string.Join(MultiSelectDelimiter, _selectedItems.Skip(GetDisplayedItemsCount()).Select(GetText));
    }

    // The whole selection as one piece of text, which is what the chips and the overflow indicator show
    // between them, so the combobox can be named after it instead of after the display that also holds
    // the remove button of every chip.
    private string GetChipsAriaText()
    {
        return string.Join(MultiSelectDelimiter, _selectedItems.Select(GetText));
    }

    private string GetChipsRemoveButtonAriaLabel(TItem item)
    {
        var text = GetText(item);

        return ChipsRemoveButtonAriaLabel is not null
                ? string.Format(ChipsRemoveButtonAriaLabel, text)
                : $"Remove {text}";
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

        if (StickyHeaders)
        {
            classes.Add("bit-drp-sth");
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
        return BitCssClasses.Size(Size, "bit-drp");
    }

    private string GetColorClass()
    {
        return BitCssClasses.Color(Color, "bit-drp");
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
