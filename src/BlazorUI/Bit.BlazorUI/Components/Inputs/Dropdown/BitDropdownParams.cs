namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitDropdown{TItem, TValue}"/> component.
/// </summary>
/// <remarks>
/// The value/selection parameters (Value, Values, DefaultValue, DefaultValues, InitialSelectedItems and
/// IsOpen), the validation state parameters (Invalid and ErrorMessage) and everything a page writes as
/// markup or as a handler (the templates, the Options content and the event callbacks) are left out on
/// purpose: they belong to a single dropdown rather than to a group of them.
/// <br />
/// The type arguments are the ones of the dropdowns this object is cascaded to, so one
/// <see cref="BitParams"/> can carry a separate object per pair of type arguments: each of them cascades
/// under the same <see cref="ParamName"/> and a dropdown picks the one its own type arguments match.
/// </remarks>
/// <typeparam name="TItem">The type of the items of the dropdowns these parameters are provided for.</typeparam>
/// <typeparam name="TValue">The type of the value of the dropdowns these parameters are provided for.</typeparam>
public class BitDropdownParams<TItem, TValue> : BitComponentBaseParams, IBitComponentParams where TItem : class, new()
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitDropdown{TItem, TValue}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitDropdown value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// <br />
    /// Every closed generic version of this class cascades under this same name, and a dropdown matches the
    /// one whose type arguments are its own, so the name does not have to name them.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitDropdown<object, object>)}";



    public string Name => ParamName;



    /// <summary>
    /// Detailed description of the dropdown for the benefit of screen readers. It is rendered into a
    /// visually hidden element that the dropdown references through its aria-describedby attribute,
    /// which is what lets a field carry an instruction too long to show next to it. It is read after
    /// <see cref="Description"/>, so the two can be used together.
    /// </summary>
    public string? AriaDescription { get; set; }

    /// <summary>
    /// Clears the typed search text after each selection in the multi select ComboBox mode, so the next
    /// item is picked from the full list instead of from the previous filter.
    /// </summary>
    public bool? AutoClearSearch { get; set; }

    /// <summary>
    /// Gives the focus to the dropdown as soon as it is rendered.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Enables auto-focusing of the SearchBox input when the callout is open.
    /// </summary>
    public bool? AutoFocusSearchBox { get; set; }

    /// <summary>
    /// Makes Enter in the ComboBox mode pick the first item the typed text matches when no item matches
    /// it exactly, which is what an autocomplete does: typing "app" and pressing Enter then selects
    /// "Apple" instead of doing nothing. It takes precedence over <see cref="Dynamic"/>, so a term that
    /// matches an existing item selects that item rather than creating a new one out of it.
    /// </summary>
    public bool? AutoSelectFirstMatch { get; set; }

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
    public BitIconInfo? CaretDownIcon { get; set; }

    /// <summary>
    /// The icon name of the chevron down element of the dropdown from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="CaretDownIcon"/> instead.
    /// </summary>
    public string? CaretDownIconName { get; set; }

    /// <summary>
    /// Shows the selected items like chips in the BitDropdown.
    /// </summary>
    public bool? Chips { get; set; }

    /// <summary>
    /// The composite format of the accessible name of the remove button of a chip, which receives the
    /// text of the item the chip stands for, for example "Remove {0}". Defaults to the English message.
    /// </summary>
    public string? ChipsRemoveButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the remove button in the chips display.
    /// Takes precedence over <see cref="ChipsRemoveIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ChipsRemoveIconName"/> instead.
    /// </summary>
    public BitIconInfo? ChipsRemoveIcon { get; set; }

    /// <summary>
    /// The icon name of the remove button in the chips display from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ChipsRemoveIcon"/> instead.
    /// </summary>
    public string? ChipsRemoveIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitDropdown.
    /// </summary>
    public BitDropdownClassStyles? Classes { get; set; }

    /// <summary>
    /// The accessible name (and the tooltip) of the clear button of the dropdown.
    /// Defaults to the English message.
    /// </summary>
    public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the clear button of the dropdown.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ClearButtonIconName"/> instead.
    /// </summary>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The icon name of the clear button of the dropdown from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ClearButtonIcon"/> instead.
    /// </summary>
    public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// Makes the Escape key take back the whole selection once there is nothing left for it to dismiss:
    /// the first press closes the callout (and, in the ComboBox mode, drops the text that was typed into
    /// it), and only a press with the callout already closed and nothing typed clears what is selected.
    /// It reports itself through <c>OnClear</c> exactly as the clear button does, and it is
    /// refused in the same places that button is - a read-only dropdown, a one-way binding.
    /// </summary>
    public bool? ClearOnEscape { get; set; }

    /// <summary>
    /// Determines whether picking an item in the callout closes it. It defaults to the behavior each
    /// mode expects: a single select dropdown closes, because the pick is the whole interaction, while
    /// a multi select one stays open so the next item can be picked right away. Set it explicitly to
    /// keep a single select callout open (a long list the user keeps trying options from) or to close a
    /// multi select one after every pick.
    /// </summary>
    public bool? CloseOnSelect { get; set; }

    /// <summary>
    /// The general color of the dropdown.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Activates the ComboBox feature in BitDropDown component.
    /// </summary>
    public bool? Combo { get; set; }

    /// <summary>
    /// The accessible name (and the tooltip) of the add button in the responsive ComboBox mode.
    /// Defaults to the English message.
    /// </summary>
    public string? ComboBoxAddButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the add button in the responsive ComboBox mode.
    /// Takes precedence over <see cref="ComboBoxAddButtonIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ComboBoxAddButtonIconName"/> instead.
    /// </summary>
    public BitIconInfo? ComboBoxAddButtonIcon { get; set; }

    /// <summary>
    /// The icon name of the add button in the responsive ComboBox mode from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ComboBoxAddButtonIcon"/> instead.
    /// </summary>
    public string? ComboBoxAddButtonIconName { get; set; }

    /// <summary>
    /// The debounce time in milliseconds for the search and combo box inputs (applied when Immediate is enabled).
    /// </summary>
    public int? DebounceTime { get; set; }

    /// <summary>
    /// The description rendered below the dropdown, which is also tied to it as its accessible
    /// description, so a screen reader reads it along with the dropdown instead of leaving it as text
    /// that only happens to sit nearby.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// It is allowed to add a new item in the ComboBox mode. While the typed text names no item the list
    /// offers to create one out of it, so that adding an item is something the user can see and click
    /// rather than a shortcut they have to know about (see <see cref="DynamicItemTextFormat"/>).
    /// </summary>
    public bool? Dynamic { get; set; }

    /// <summary>
    /// The composite format of the row the callout offers to create a new item with in the
    /// <see cref="Dynamic"/> ComboBox mode, which receives the text the item would be created from,
    /// for example "Add \"{0}\"". Defaults to the English message.
    /// </summary>
    public string? DynamicItemTextFormat { get; set; }

    /// <summary>
    /// The function for generating value in a custom item when a new item is on added Dynamic ComboBox mode.
    /// </summary>
    public Func<TItem?, TValue>? DynamicValueGenerator { get; set; }

    /// <summary>
    /// The text to render in the callout when there is no item to show.
    /// </summary>
    public string? EmptyText { get; set; }

    /// <summary>
    /// Decides whether the text committed in the ComboBox mode already stands for one of the selected
    /// items, in place of the default comparison of that text with the item texts, ignoring case. It
    /// receives the selected items and the committed text, and returning true stops the commit, so the
    /// same item cannot be selected (or created) twice under a name your data considers equivalent.
    /// </summary>
    public Func<ICollection<TItem>, string, bool>? ExistsSelectedItemFunction { get; set; }

    /// <summary>
    /// Finds the item the text committed in the ComboBox mode stands for, in place of the default
    /// comparison of that text with the item texts, ignoring case. It receives the items and the
    /// committed text; the item it returns gets selected, and only when it returns none does
    /// <see cref="AutoSelectFirstMatch"/> and then <see cref="Dynamic"/> get their turn.
    /// </summary>
    public Func<ICollection<TItem>, string, TItem?>? FindItemFunction { get; set; }

    /// <summary>
    /// Enables fit-content value for the width of the root element.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// Removes the already selected items from the callout, which suits a multi select dropdown whose
    /// selection is visible as chips and whose list is therefore only about what is left to pick.
    /// A group header left naming nothing, and a divider left without items on one of its sides, are
    /// removed along with them.
    /// It has no effect when the items come from an <see cref="ItemsProvider"/>, which hands over the
    /// window it was asked for and is the only place that can leave the selected items out of it.
    /// </summary>
    public bool? HideSelectedItems { get; set; }

    /// <summary>
    /// Highlights the part of the item text that matched the current search text in the callout.
    /// Only applies to the default item rendering, not to a custom <c>ItemTemplate</c>.
    /// The highlighted part is found by the built-in algorithm (<see cref="SearchMode"/> and
    /// <see cref="SearchIgnoreDiacritics"/>), so a custom <see cref="SearchFunction"/> that matches by
    /// some other rule can produce items with nothing to highlight.
    /// </summary>
    public bool? HighlightSearch { get; set; }

    /// <summary>
    /// Searches the items as the user types in the search box (based on the 'oninput' HTML event)
    /// instead of waiting for the search box to be committed.
    /// The ComboBox input always searches as it is typed - that is what a combo box is - so there it
    /// only decides whether <see cref="DebounceTime"/> and <see cref="ThrottleTime"/> apply.
    /// </summary>
    public bool? Immediate { get; set; }

    /// <summary>
    /// Shows a loading indicator in the callout (and in place of the caret down element) while the items are being fetched.
    /// The dropdown stays interactive, so the user can still open the callout and see the loading state.
    /// </summary>
    public bool? IsLoading { get; set; }

    /// <summary>
    /// The icon of the check mark in the multi-select items.
    /// Takes precedence over <see cref="ItemCheckIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ItemCheckIconName"/> instead.
    /// </summary>
    public BitIconInfo? ItemCheckIcon { get; set; }

    /// <summary>
    /// The icon name of the check mark in the multi-select items from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ItemCheckIcon"/> instead.
    /// </summary>
    public string? ItemCheckIconName { get; set; }

    /// <summary>
    /// The list of items to display in the callout.
    /// </summary>
    public ICollection<TItem>? Items { get; set; }

    /// <summary>
    /// The height of each item in pixels for virtualization.
    /// </summary>
    public int? ItemSize { get; set; }

    /// <summary>
    /// The function providing items to the list for virtualization. It loads the items on demand, in
    /// the windows the user actually scrolls to, and receives the current search text so the filtering
    /// happens at the source instead of over an already loaded list.
    /// It requires <see cref="Virtualize"/> to be enabled, which is what requests the windows.
    /// </summary>
    public BitDropdownItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>
    /// The delay in milliseconds before an <see cref="ItemsProvider"/> request is issued, which collapses
    /// the bursts of requests produced by fast scrolling and typing into a single one.
    /// </summary>
    public int? ItemsProviderDebounceTime { get; set; }

    /// <summary>
    /// The text of the label element of the dropdown.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The text to render in the callout in place of the items while <see cref="IsLoading"/> is enabled.
    /// </summary>
    public string? LoadingText { get; set; }

    /// <summary>
    /// The maximum number of selected items rendered in the dropdown itself. Beyond it, the chips display
    /// collapses the extra ones into an overflow indicator and the text display switches to a summary.
    /// Zero or null renders every selected item.
    /// </summary>
    public int? MaxDisplayedItems { get; set; }

    /// <summary>
    /// The maximum height of the scrollable item list of the callout in pixels, which is what keeps a
    /// long list from taking over the screen. It is applied on top of the space the viewport leaves, so
    /// it can only ever make the list shorter: a callout near the bottom of the window is still capped
    /// by the room it has. A value that is not greater than zero (and null) leaves the viewport alone
    /// to decide.
    /// </summary>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// The maximum number of items that can be selected in multi select mode.
    /// A value that is not greater than zero (and null) means no limit.
    /// </summary>
    public int? MaxSelectedItems { get; set; }

    /// <summary>
    /// The composite format of the message announced to screen readers once <see cref="MaxSelectedItems"/>
    /// is reached, which receives that limit, for example "Maximum of {0} items selected". Reaching the
    /// limit disables the items that are not selected yet, which is a change only a sighted user can
    /// notice on their own. Defaults to the English message.
    /// </summary>
    public string? MaxSelectedItemsText { get; set; }

    /// <summary>
    /// The number of characters the search text must reach before the items get filtered.
    /// While the search text is shorter, the full list is shown and no search is performed.
    /// </summary>
    public int? MinSearchLength { get; set; }

    /// <summary>
    /// The composite format of the hint the callout shows while the typed text is still shorter than
    /// <see cref="MinSearchLength"/>, which receives the number of characters that are still missing,
    /// for example "Type {0} more characters to search". It is what tells the user that the list they
    /// are looking at is the full one rather than the result of what they typed, and it is announced
    /// to screen readers as well. Defaults to the English message; the hint is not shown at all while
    /// nothing has been typed, where the full list needs no explaining.
    /// </summary>
    public string? MinSearchLengthText { get; set; }

    /// <summary>
    /// Enables the multi select mode.
    /// </summary>
    public bool? MultiSelect { get; set; }

    /// <summary>
    /// The delimiter for joining the values to create the text of the dropdown in multi select mode.
    /// </summary>
    public string? MultiSelectDelimiter { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    public BitDropdownNameSelectors<TItem, TValue>? NameSelectors { get; set; }

    /// <summary>
    /// Removes the border from the root element.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// The text to render in the callout when the current search has no result.
    /// Falls back to the <see cref="EmptyText"/> when not set.
    /// </summary>
    public string? NoResultsText { get; set; }

    /// <summary>
    /// Stops the arrow keys at the ends of the item list instead of letting them wrap around from the
    /// last item to the first one and back, which suits a long list where the wrap is more likely to
    /// read as the focus having been lost than as a deliberate jump. The type-ahead still wraps, since
    /// it looks for the item that matches rather than for the one that comes next.
    /// It has no effect in virtualize mode, where the ends of the rendered window are not the ends of
    /// the list and the focus stops at them either way.
    /// </summary>
    public bool? NoWrapNavigation { get; set; }

    /// <summary>
    /// Opens the callout as soon as the dropdown receives the focus, so tabbing into it (or clicking
    /// any part of it) already shows the items without a further click or key press.
    /// </summary>
    public bool? OpenOnFocus { get; set; }

    /// <summary>
    /// The composite format of the overflow indicator that stands for the selected items beyond
    /// <see cref="MaxDisplayedItems"/> in the chips display, for example "+{0}".
    /// </summary>
    public string? OverflowTextFormat { get; set; }

    /// <summary>
    /// Determines how many additional items are rendered before and after the visible region.
    /// </summary>
    public int? OverscanCount { get; set; }

    /// <summary>
    /// The placeholder text of the dropdown.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Prefix displayed before the BitDropdown contents. This is not included in the value.
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Disables automatic setting of the callout width and preserves its original width.
    /// </summary>
    public bool? PreserveCalloutWidth { get; set; }

    /// <summary>
    /// Enables calling the select events when the same item is selected in single select mode.
    /// </summary>
    public bool? Reselectable { get; set; }

    /// <summary>
    /// Enables the responsive mode of the component for small screens.
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// The accessible name (and the tooltip) of the close button in the responsive mode callout.
    /// Defaults to the English message.
    /// </summary>
    public string? ResponsiveCloseButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the close button in the responsive mode callout.
    /// Takes precedence over <see cref="ResponsiveCloseIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ResponsiveCloseIconName"/> instead.
    /// </summary>
    public BitIconInfo? ResponsiveCloseIcon { get; set; }

    /// <summary>
    /// The icon name of the close button in the responsive mode callout from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="ResponsiveCloseIcon"/> instead.
    /// </summary>
    public string? ResponsiveCloseIconName { get; set; }

    /// <summary>
    /// The accessible name of the SearchBox input. Defaults to the English message.
    /// </summary>
    public string? SearchBoxAriaLabel { get; set; }

    /// <summary>
    /// The accessible name (and the tooltip) of the clear button of the SearchBox.
    /// Defaults to the English message.
    /// </summary>
    public string? SearchBoxClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon of the clear icon in the SearchBox.
    /// Takes precedence over <see cref="SearchBoxClearIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="SearchBoxClearIconName"/> instead.
    /// </summary>
    public BitIconInfo? SearchBoxClearIcon { get; set; }

    /// <summary>
    /// The icon name of the clear icon in the SearchBox from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="SearchBoxClearIcon"/> instead.
    /// </summary>
    public string? SearchBoxClearIconName { get; set; }

    /// <summary>
    /// The icon of the search icon in the SearchBox.
    /// Takes precedence over <see cref="SearchBoxIconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="SearchBoxIconName"/> instead.
    /// </summary>
    public BitIconInfo? SearchBoxIcon { get; set; }

    /// <summary>
    /// The icon name of the search icon in the SearchBox from the Fluent UI icon set.
    /// For external icon libraries, use <see cref="SearchBoxIcon"/> instead.
    /// </summary>
    public string? SearchBoxIconName { get; set; }

    /// <summary>
    /// The placeholder text of the SearchBox input.
    /// </summary>
    public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// Custom search function to be used in place of the default search algorithm.
    /// Takes precedence over <see cref="SearchMode"/>, which only configures the default algorithm.
    /// </summary>
    public Func<ICollection<TItem>, string, ICollection<TItem>>? SearchFunction { get; set; }

    /// <summary>
    /// Matches the search text against the item texts with the diacritics of both removed, so that
    /// "Jose" finds "José" and "Muller" finds "Müller". The item text itself is left untouched, and so
    /// is the part of it that <see cref="HighlightSearch"/> emphasizes. Ignored when a
    /// <see cref="SearchFunction"/> is provided, which does its own matching.
    /// </summary>
    public bool? SearchIgnoreDiacritics { get; set; }

    /// <summary>
    /// Determines how the text of an item is matched against the search text by the default
    /// (case-insensitive) search algorithm. Ignored when a <see cref="SearchFunction"/> is provided.
    /// </summary>
    public BitDropdownSearchMode? SearchMode { get; set; }

    /// <summary>
    /// The composite format of the message announced to screen readers with the number of items the
    /// current search produced, for example "{0} results available". Defaults to the English message.
    /// </summary>
    public string? SearchResultsText { get; set; }

    /// <summary>
    /// The text of the select all item in multi select mode.
    /// </summary>
    public string? SelectAllText { get; set; }

    /// <summary>
    /// The composite format that replaces the joined item texts in the dropdown once more than
    /// <see cref="MaxDisplayedItems"/> items are selected, for example "{0} items selected".
    /// </summary>
    public string? SelectedItemsTextFormat { get; set; }

    /// <summary>
    /// Selects the text already in the ComboBox input whenever it takes the focus, so that typing
    /// replaces the term that is there instead of appending to it - which is what a field the user
    /// comes back to in order to search for something else needs. It has no effect outside of the
    /// ComboBox mode, and none while the input is empty, where there is nothing to select.
    /// </summary>
    public bool? SelectTextOnFocus { get; set; }

    /// <summary>
    /// Shows the clear button when an item is selected.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// Shows the SearchBox element in the callout.
    /// It has no effect in the ComboBox mode, where the input of the dropdown itself is what the items
    /// are filtered by, and a second search field would only split the typing between two places.
    /// </summary>
    public bool? ShowSearchBox { get; set; }

    /// <summary>
    /// Shows the select all item in the callout in multi select mode.
    /// It has no effect when the items are provided by an ItemsProvider, since the items that are not
    /// loaded yet cannot be selected.
    /// </summary>
    public bool? ShowSelectAll { get; set; }

    /// <summary>
    /// The size of the dropdown.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Keeps the header of a group pinned to the top of the item list while its items are scrolled
    /// past, so a long grouped list never leaves the user looking at items whose group has scrolled
    /// out of view.
    /// </summary>
    public bool? StickyHeaders { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDropdown.
    /// </summary>
    public BitDropdownClassStyles? Styles { get; set; }

    /// <summary>
    /// Suffix displayed after the BitDropdown contents. This is not included in the value. 
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// The throttle time in milliseconds for the search and combo box inputs (applied when Immediate is enabled).
    /// </summary>
    public int? ThrottleTime { get; set; }

    /// <summary>
    /// The title to show when the mouse hovers over the dropdown.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The characters that split the text typed (or pasted) into the multi select ComboBox input into
    /// separate terms, each committed as its own selection exactly as typing it and pressing Enter
    /// would: a term naming an existing item selects it, and with <see cref="Dynamic"/> enabled a term
    /// naming none adds a new item. This is what turns a pasted "a, b, c" into three selections
    /// instead of one literal term.
    /// </summary>
    public char[]? TokenSeparators { get; set; }

    /// <summary>
    /// Removes the default background color from the root element.
    /// </summary>
    public bool? Transparent { get; set; }

    /// <summary>
    /// Renders the dropdown with only a bottom border in place of the box around it, which is the
    /// variant that suits a dense form where a full box per field would be too much furniture.
    /// </summary>
    public bool? Underlined { get; set; }

    /// <summary>
    /// Decides whether two values stand for the same selection, in place of the default equality of
    /// <typeparamref name="TValue"/>. This is what a value type that is not its own identity needs: a
    /// record or a class used as the value compares by reference by default, so a value that arrives
    /// from a form, a query string or a fresh fetch would never match the item it names, however equal
    /// the two look. It also decides which item a clicked value belongs to, so a comparer that treats
    /// two different values as equal makes them one and the same selection.
    /// </summary>
    public IEqualityComparer<TValue>? ValueComparer { get; set; }

    /// <summary>
    /// Enables virtualization to render only the visible items.
    /// </summary>
    public bool? Virtualize { get; set; }


    /// <summary>
    /// Updates the properties of the specified <see cref="BitDropdown{TItem, TValue}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitDropdown{TItem, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitDropdown"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitDropdown"/>.
    /// </remarks>
    /// <param name="bitDropdown">
    /// The <see cref="BitDropdown{TItem, TValue}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitDropdown<TItem, TValue> bitDropdown)
    {
        if (bitDropdown is null) return;

        UpdateBaseParameters(bitDropdown);

        if (AriaDescription.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(AriaDescription), AriaDescription, static d => d.AriaDescription, static (d, v) => d.AriaDescription = v);
        }

        if (AutoClearSearch.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(AutoClearSearch), AutoClearSearch.Value, static d => d.AutoClearSearch, static (d, v) => d.AutoClearSearch = v);
        }

        if (AutoFocus.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static d => d.AutoFocus, static (d, v) => d.AutoFocus = v);
        }

        if (AutoFocusSearchBox.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(AutoFocusSearchBox), AutoFocusSearchBox.Value, static d => d.AutoFocusSearchBox, static (d, v) => d.AutoFocusSearchBox = v);
        }

        if (AutoSelectFirstMatch.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(AutoSelectFirstMatch), AutoSelectFirstMatch.Value, static d => d.AutoSelectFirstMatch, static (d, v) => d.AutoSelectFirstMatch = v);
        }

        if (CaretDownIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(CaretDownIcon), CaretDownIcon, static d => d.CaretDownIcon, static (d, v) => d.CaretDownIcon = v);
        }

        if (CaretDownIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(CaretDownIconName), CaretDownIconName, static d => d.CaretDownIconName, static (d, v) => d.CaretDownIconName = v);
        }

        if (Chips.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Chips), Chips.Value, static d => d.Chips, static (d, v) => d.Chips = v);
        }

        if (ChipsRemoveButtonAriaLabel.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ChipsRemoveButtonAriaLabel), ChipsRemoveButtonAriaLabel, static d => d.ChipsRemoveButtonAriaLabel, static (d, v) => d.ChipsRemoveButtonAriaLabel = v);
        }

        if (ChipsRemoveIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ChipsRemoveIcon), ChipsRemoveIcon, static d => d.ChipsRemoveIcon, static (d, v) => d.ChipsRemoveIcon = v);
        }

        if (ChipsRemoveIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ChipsRemoveIconName), ChipsRemoveIconName, static d => d.ChipsRemoveIconName, static (d, v) => d.ChipsRemoveIconName = v);
        }

        if (Classes is not null)
        {
            bitDropdown.TakeFromCascade(nameof(Classes), Classes, static d => d.Classes, static (d, v) => d.Classes = v);
        }

        if (ClearButtonAriaLabel.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ClearButtonAriaLabel), ClearButtonAriaLabel, static d => d.ClearButtonAriaLabel, static (d, v) => d.ClearButtonAriaLabel = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static d => d.ClearButtonIcon, static (d, v) => d.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static d => d.ClearButtonIconName, static (d, v) => d.ClearButtonIconName = v);
        }

        if (ClearOnEscape.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(ClearOnEscape), ClearOnEscape.Value, static d => d.ClearOnEscape, static (d, v) => d.ClearOnEscape = v);
        }

        if (CloseOnSelect.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(CloseOnSelect), CloseOnSelect.Value, static d => d.CloseOnSelect, static (d, v) => d.CloseOnSelect = v);
        }

        if (Color.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Color), Color.Value, static d => d.Color, static (d, v) => d.Color = v);
        }

        if (Combo.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Combo), Combo.Value, static d => d.Combo, static (d, v) => d.Combo = v);
        }

        if (ComboBoxAddButtonAriaLabel.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ComboBoxAddButtonAriaLabel), ComboBoxAddButtonAriaLabel, static d => d.ComboBoxAddButtonAriaLabel, static (d, v) => d.ComboBoxAddButtonAriaLabel = v);
        }

        if (ComboBoxAddButtonIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ComboBoxAddButtonIcon), ComboBoxAddButtonIcon, static d => d.ComboBoxAddButtonIcon, static (d, v) => d.ComboBoxAddButtonIcon = v);
        }

        if (ComboBoxAddButtonIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ComboBoxAddButtonIconName), ComboBoxAddButtonIconName, static d => d.ComboBoxAddButtonIconName, static (d, v) => d.ComboBoxAddButtonIconName = v);
        }

        if (DebounceTime.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(DebounceTime), DebounceTime.Value, static d => d.DebounceTime, static (d, v) => d.DebounceTime = v);
        }

        if (Description.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(Description), Description, static d => d.Description, static (d, v) => d.Description = v);
        }

        if (DropDirection.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static d => d.DropDirection, static (d, v) => d.DropDirection = v);
        }

        if (Dynamic.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Dynamic), Dynamic.Value, static d => d.Dynamic, static (d, v) => d.Dynamic = v);
        }

        if (DynamicItemTextFormat.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(DynamicItemTextFormat), DynamicItemTextFormat, static d => d.DynamicItemTextFormat, static (d, v) => d.DynamicItemTextFormat = v);
        }

        if (DynamicValueGenerator is not null)
        {
            bitDropdown.TakeFromCascade(nameof(DynamicValueGenerator), DynamicValueGenerator, static d => d.DynamicValueGenerator, static (d, v) => d.DynamicValueGenerator = v);
        }

        if (EmptyText.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(EmptyText), EmptyText, static d => d.EmptyText, static (d, v) => d.EmptyText = v);
        }

        if (ExistsSelectedItemFunction is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ExistsSelectedItemFunction), ExistsSelectedItemFunction, static d => d.ExistsSelectedItemFunction, static (d, v) => d.ExistsSelectedItemFunction = v);
        }

        if (FindItemFunction is not null)
        {
            bitDropdown.TakeFromCascade(nameof(FindItemFunction), FindItemFunction, static d => d.FindItemFunction, static (d, v) => d.FindItemFunction = v);
        }

        if (FitWidth.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(FitWidth), FitWidth.Value, static d => d.FitWidth, static (d, v) => d.FitWidth = v);
        }

        if (HideSelectedItems.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(HideSelectedItems), HideSelectedItems.Value, static d => d.HideSelectedItems, static (d, v) => d.HideSelectedItems = v);
        }

        if (HighlightSearch.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(HighlightSearch), HighlightSearch.Value, static d => d.HighlightSearch, static (d, v) => d.HighlightSearch = v);
        }

        if (Immediate.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Immediate), Immediate.Value, static d => d.Immediate, static (d, v) => d.Immediate = v);
        }

        if (IsLoading.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(IsLoading), IsLoading.Value, static d => d.IsLoading, static (d, v) => d.IsLoading = v);
        }

        if (ItemCheckIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ItemCheckIcon), ItemCheckIcon, static d => d.ItemCheckIcon, static (d, v) => d.ItemCheckIcon = v);
        }

        if (ItemCheckIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ItemCheckIconName), ItemCheckIconName, static d => d.ItemCheckIconName, static (d, v) => d.ItemCheckIconName = v);
        }

        if (Items is not null)
        {
            bitDropdown.TakeFromCascade(nameof(Items), Items, static d => d.Items, static (d, v) => d.Items = v);
        }

        if (ItemSize.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(ItemSize), ItemSize.Value, static d => d.ItemSize, static (d, v) => d.ItemSize = v);
        }

        if (ItemsProvider is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ItemsProvider), ItemsProvider, static d => d.ItemsProvider, static (d, v) => d.ItemsProvider = v);
        }

        if (ItemsProviderDebounceTime.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(ItemsProviderDebounceTime), ItemsProviderDebounceTime.Value, static d => d.ItemsProviderDebounceTime, static (d, v) => d.ItemsProviderDebounceTime = v);
        }

        if (Label.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(Label), Label, static d => d.Label, static (d, v) => d.Label = v);
        }

        if (LoadingText.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(LoadingText), LoadingText, static d => d.LoadingText, static (d, v) => d.LoadingText = v);
        }

        if (MaxDisplayedItems.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(MaxDisplayedItems), MaxDisplayedItems.Value, static d => d.MaxDisplayedItems, static (d, v) => d.MaxDisplayedItems = v);
        }

        if (MaxHeight.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(MaxHeight), MaxHeight.Value, static d => d.MaxHeight, static (d, v) => d.MaxHeight = v);
        }

        if (MaxSelectedItems.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(MaxSelectedItems), MaxSelectedItems.Value, static d => d.MaxSelectedItems, static (d, v) => d.MaxSelectedItems = v);
        }

        if (MaxSelectedItemsText.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(MaxSelectedItemsText), MaxSelectedItemsText, static d => d.MaxSelectedItemsText, static (d, v) => d.MaxSelectedItemsText = v);
        }

        if (MinSearchLength.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(MinSearchLength), MinSearchLength.Value, static d => d.MinSearchLength, static (d, v) => d.MinSearchLength = v);
        }

        if (MinSearchLengthText.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(MinSearchLengthText), MinSearchLengthText, static d => d.MinSearchLengthText, static (d, v) => d.MinSearchLengthText = v);
        }

        if (MultiSelect.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(MultiSelect), MultiSelect.Value, static d => d.MultiSelect, static (d, v) => d.MultiSelect = v);
        }

        if (MultiSelectDelimiter.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(MultiSelectDelimiter), MultiSelectDelimiter!, static d => d.MultiSelectDelimiter, static (d, v) => d.MultiSelectDelimiter = v);
        }

        if (NameSelectors is not null)
        {
            bitDropdown.TakeFromCascade(nameof(NameSelectors), NameSelectors, static d => d.NameSelectors, static (d, v) => d.NameSelectors = v);
        }

        if (NoBorder.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static d => d.NoBorder, static (d, v) => d.NoBorder = v);
        }

        if (NoResultsText.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(NoResultsText), NoResultsText, static d => d.NoResultsText, static (d, v) => d.NoResultsText = v);
        }

        if (NoWrapNavigation.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(NoWrapNavigation), NoWrapNavigation.Value, static d => d.NoWrapNavigation, static (d, v) => d.NoWrapNavigation = v);
        }

        if (OpenOnFocus.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(OpenOnFocus), OpenOnFocus.Value, static d => d.OpenOnFocus, static (d, v) => d.OpenOnFocus = v);
        }

        if (OverflowTextFormat.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(OverflowTextFormat), OverflowTextFormat, static d => d.OverflowTextFormat, static (d, v) => d.OverflowTextFormat = v);
        }

        if (OverscanCount.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(OverscanCount), OverscanCount.Value, static d => d.OverscanCount, static (d, v) => d.OverscanCount = v);
        }

        if (Placeholder.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(Placeholder), Placeholder, static d => d.Placeholder, static (d, v) => d.Placeholder = v);
        }

        if (Prefix.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(Prefix), Prefix, static d => d.Prefix, static (d, v) => d.Prefix = v);
        }

        if (PreserveCalloutWidth.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(PreserveCalloutWidth), PreserveCalloutWidth.Value, static d => d.PreserveCalloutWidth, static (d, v) => d.PreserveCalloutWidth = v);
        }

        if (Reselectable.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Reselectable), Reselectable.Value, static d => d.Reselectable, static (d, v) => d.Reselectable = v);
        }

        if (Responsive.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Responsive), Responsive.Value, static d => d.Responsive, static (d, v) => d.Responsive = v);
        }

        if (ResponsiveCloseButtonAriaLabel.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ResponsiveCloseButtonAriaLabel), ResponsiveCloseButtonAriaLabel, static d => d.ResponsiveCloseButtonAriaLabel, static (d, v) => d.ResponsiveCloseButtonAriaLabel = v);
        }

        if (ResponsiveCloseIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ResponsiveCloseIcon), ResponsiveCloseIcon, static d => d.ResponsiveCloseIcon, static (d, v) => d.ResponsiveCloseIcon = v);
        }

        if (ResponsiveCloseIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(ResponsiveCloseIconName), ResponsiveCloseIconName, static d => d.ResponsiveCloseIconName, static (d, v) => d.ResponsiveCloseIconName = v);
        }

        if (SearchBoxAriaLabel.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SearchBoxAriaLabel), SearchBoxAriaLabel, static d => d.SearchBoxAriaLabel, static (d, v) => d.SearchBoxAriaLabel = v);
        }

        if (SearchBoxClearButtonAriaLabel.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SearchBoxClearButtonAriaLabel), SearchBoxClearButtonAriaLabel, static d => d.SearchBoxClearButtonAriaLabel, static (d, v) => d.SearchBoxClearButtonAriaLabel = v);
        }

        if (SearchBoxClearIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(SearchBoxClearIcon), SearchBoxClearIcon, static d => d.SearchBoxClearIcon, static (d, v) => d.SearchBoxClearIcon = v);
        }

        if (SearchBoxClearIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SearchBoxClearIconName), SearchBoxClearIconName, static d => d.SearchBoxClearIconName, static (d, v) => d.SearchBoxClearIconName = v);
        }

        if (SearchBoxIcon is not null)
        {
            bitDropdown.TakeFromCascade(nameof(SearchBoxIcon), SearchBoxIcon, static d => d.SearchBoxIcon, static (d, v) => d.SearchBoxIcon = v);
        }

        if (SearchBoxIconName.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SearchBoxIconName), SearchBoxIconName, static d => d.SearchBoxIconName, static (d, v) => d.SearchBoxIconName = v);
        }

        if (SearchBoxPlaceholder.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SearchBoxPlaceholder), SearchBoxPlaceholder, static d => d.SearchBoxPlaceholder, static (d, v) => d.SearchBoxPlaceholder = v);
        }

        if (SearchFunction is not null)
        {
            bitDropdown.TakeFromCascade(nameof(SearchFunction), SearchFunction, static d => d.SearchFunction, static (d, v) => d.SearchFunction = v);
        }

        if (SearchIgnoreDiacritics.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(SearchIgnoreDiacritics), SearchIgnoreDiacritics.Value, static d => d.SearchIgnoreDiacritics, static (d, v) => d.SearchIgnoreDiacritics = v);
        }

        if (SearchMode.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(SearchMode), SearchMode.Value, static d => d.SearchMode, static (d, v) => d.SearchMode = v);
        }

        if (SearchResultsText.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SearchResultsText), SearchResultsText, static d => d.SearchResultsText, static (d, v) => d.SearchResultsText = v);
        }

        if (SelectAllText.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SelectAllText), SelectAllText, static d => d.SelectAllText, static (d, v) => d.SelectAllText = v);
        }

        if (SelectedItemsTextFormat.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(SelectedItemsTextFormat), SelectedItemsTextFormat, static d => d.SelectedItemsTextFormat, static (d, v) => d.SelectedItemsTextFormat = v);
        }

        if (SelectTextOnFocus.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(SelectTextOnFocus), SelectTextOnFocus.Value, static d => d.SelectTextOnFocus, static (d, v) => d.SelectTextOnFocus = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static d => d.ShowClearButton, static (d, v) => d.ShowClearButton = v);
        }

        if (ShowSearchBox.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(ShowSearchBox), ShowSearchBox.Value, static d => d.ShowSearchBox, static (d, v) => d.ShowSearchBox = v);
        }

        if (ShowSelectAll.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(ShowSelectAll), ShowSelectAll.Value, static d => d.ShowSelectAll, static (d, v) => d.ShowSelectAll = v);
        }

        if (Size.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Size), Size.Value, static d => d.Size, static (d, v) => d.Size = v);
        }

        if (StickyHeaders.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(StickyHeaders), StickyHeaders.Value, static d => d.StickyHeaders, static (d, v) => d.StickyHeaders = v);
        }

        if (Styles is not null)
        {
            bitDropdown.TakeFromCascade(nameof(Styles), Styles, static d => d.Styles, static (d, v) => d.Styles = v);
        }

        if (Suffix.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(Suffix), Suffix, static d => d.Suffix, static (d, v) => d.Suffix = v);
        }

        if (ThrottleTime.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(ThrottleTime), ThrottleTime.Value, static d => d.ThrottleTime, static (d, v) => d.ThrottleTime = v);
        }

        if (Title.HasValue())
        {
            bitDropdown.TakeFromCascade(nameof(Title), Title, static d => d.Title, static (d, v) => d.Title = v);
        }

        if (TokenSeparators is not null)
        {
            bitDropdown.TakeFromCascade(nameof(TokenSeparators), TokenSeparators, static d => d.TokenSeparators, static (d, v) => d.TokenSeparators = v);
        }

        if (Transparent.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Transparent), Transparent.Value, static d => d.Transparent, static (d, v) => d.Transparent = v);
        }

        if (Underlined.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Underlined), Underlined.Value, static d => d.Underlined, static (d, v) => d.Underlined = v);
        }

        if (ValueComparer is not null)
        {
            bitDropdown.TakeFromCascade(nameof(ValueComparer), ValueComparer, static d => d.ValueComparer, static (d, v) => d.ValueComparer = v);
        }

        if (Virtualize.HasValue)
        {
            bitDropdown.TakeFromCascade(nameof(Virtualize), Virtualize.Value, static d => d.Virtualize, static (d, v) => d.Virtualize = v);
        }
    }
}
