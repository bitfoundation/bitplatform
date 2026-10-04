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

        if (AriaDescription.HasValue() && bitDropdown.HasNotBeenSet(nameof(AriaDescription)))
        {
            bitDropdown.AriaDescription = AriaDescription;
        }

        if (AutoClearSearch.HasValue && bitDropdown.HasNotBeenSet(nameof(AutoClearSearch)))
        {
            bitDropdown.AutoClearSearch = AutoClearSearch.Value;
        }

        if (AutoFocus.HasValue && bitDropdown.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitDropdown.AutoFocus = AutoFocus.Value;
        }

        if (AutoFocusSearchBox.HasValue && bitDropdown.HasNotBeenSet(nameof(AutoFocusSearchBox)))
        {
            bitDropdown.AutoFocusSearchBox = AutoFocusSearchBox.Value;
        }

        if (AutoSelectFirstMatch.HasValue && bitDropdown.HasNotBeenSet(nameof(AutoSelectFirstMatch)))
        {
            bitDropdown.AutoSelectFirstMatch = AutoSelectFirstMatch.Value;
        }

        if (CaretDownIcon is not null && bitDropdown.HasNotBeenSet(nameof(CaretDownIcon)))
        {
            bitDropdown.CaretDownIcon = CaretDownIcon;
        }

        if (CaretDownIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(CaretDownIconName)))
        {
            bitDropdown.CaretDownIconName = CaretDownIconName;
        }

        if (Chips.HasValue && bitDropdown.HasNotBeenSet(nameof(Chips)))
        {
            bitDropdown.Chips = Chips.Value;

            bitDropdown.ClassBuilder.Reset();
        }

        if (ChipsRemoveButtonAriaLabel.HasValue() && bitDropdown.HasNotBeenSet(nameof(ChipsRemoveButtonAriaLabel)))
        {
            bitDropdown.ChipsRemoveButtonAriaLabel = ChipsRemoveButtonAriaLabel;
        }

        if (ChipsRemoveIcon is not null && bitDropdown.HasNotBeenSet(nameof(ChipsRemoveIcon)))
        {
            bitDropdown.ChipsRemoveIcon = ChipsRemoveIcon;
        }

        if (ChipsRemoveIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(ChipsRemoveIconName)))
        {
            bitDropdown.ChipsRemoveIconName = ChipsRemoveIconName;
        }

        if (Classes is not null && bitDropdown.HasNotBeenSet(nameof(Classes)))
        {
            bitDropdown.Classes = Classes;

            bitDropdown.ClassBuilder.Reset();
        }

        if (ClearButtonAriaLabel.HasValue() && bitDropdown.HasNotBeenSet(nameof(ClearButtonAriaLabel)))
        {
            bitDropdown.ClearButtonAriaLabel = ClearButtonAriaLabel;
        }

        if (ClearButtonIcon is not null && bitDropdown.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitDropdown.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitDropdown.ClearButtonIconName = ClearButtonIconName;
        }

        if (ClearOnEscape.HasValue && bitDropdown.HasNotBeenSet(nameof(ClearOnEscape)))
        {
            bitDropdown.ClearOnEscape = ClearOnEscape.Value;
        }

        if (CloseOnSelect.HasValue && bitDropdown.HasNotBeenSet(nameof(CloseOnSelect)))
        {
            bitDropdown.CloseOnSelect = CloseOnSelect.Value;
        }

        if (Color.HasValue && bitDropdown.HasNotBeenSet(nameof(Color)))
        {
            bitDropdown.Color = Color.Value;

            bitDropdown.ClassBuilder.Reset();
        }

        if (Combo.HasValue && bitDropdown.HasNotBeenSet(nameof(Combo)))
        {
            bitDropdown.Combo = Combo.Value;
        }

        if (ComboBoxAddButtonAriaLabel.HasValue() && bitDropdown.HasNotBeenSet(nameof(ComboBoxAddButtonAriaLabel)))
        {
            bitDropdown.ComboBoxAddButtonAriaLabel = ComboBoxAddButtonAriaLabel;
        }

        if (ComboBoxAddButtonIcon is not null && bitDropdown.HasNotBeenSet(nameof(ComboBoxAddButtonIcon)))
        {
            bitDropdown.ComboBoxAddButtonIcon = ComboBoxAddButtonIcon;
        }

        if (ComboBoxAddButtonIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(ComboBoxAddButtonIconName)))
        {
            bitDropdown.ComboBoxAddButtonIconName = ComboBoxAddButtonIconName;
        }

        if (DebounceTime.HasValue && bitDropdown.HasNotBeenSet(nameof(DebounceTime)))
        {
            bitDropdown.DebounceTime = DebounceTime.Value;
        }

        if (Description.HasValue() && bitDropdown.HasNotBeenSet(nameof(Description)))
        {
            bitDropdown.Description = Description;
        }

        if (DropDirection.HasValue && bitDropdown.HasNotBeenSet(nameof(DropDirection)))
        {
            bitDropdown.DropDirection = DropDirection.Value;
        }

        if (Dynamic.HasValue && bitDropdown.HasNotBeenSet(nameof(Dynamic)))
        {
            bitDropdown.Dynamic = Dynamic.Value;
        }

        if (DynamicItemTextFormat.HasValue() && bitDropdown.HasNotBeenSet(nameof(DynamicItemTextFormat)))
        {
            bitDropdown.DynamicItemTextFormat = DynamicItemTextFormat;
        }

        if (DynamicValueGenerator is not null && bitDropdown.HasNotBeenSet(nameof(DynamicValueGenerator)))
        {
            bitDropdown.DynamicValueGenerator = DynamicValueGenerator;
        }

        if (EmptyText.HasValue() && bitDropdown.HasNotBeenSet(nameof(EmptyText)))
        {
            bitDropdown.EmptyText = EmptyText;
        }

        if (ExistsSelectedItemFunction is not null && bitDropdown.HasNotBeenSet(nameof(ExistsSelectedItemFunction)))
        {
            bitDropdown.ExistsSelectedItemFunction = ExistsSelectedItemFunction;
        }

        if (FindItemFunction is not null && bitDropdown.HasNotBeenSet(nameof(FindItemFunction)))
        {
            bitDropdown.FindItemFunction = FindItemFunction;
        }

        if (FitWidth.HasValue && bitDropdown.HasNotBeenSet(nameof(FitWidth)))
        {
            bitDropdown.FitWidth = FitWidth.Value;

            bitDropdown.StyleBuilder.Reset();
        }

        if (HideSelectedItems.HasValue && bitDropdown.HasNotBeenSet(nameof(HideSelectedItems)))
        {
            bitDropdown.HideSelectedItems = HideSelectedItems.Value;
        }

        if (HighlightSearch.HasValue && bitDropdown.HasNotBeenSet(nameof(HighlightSearch)))
        {
            bitDropdown.HighlightSearch = HighlightSearch.Value;
        }

        if (Immediate.HasValue && bitDropdown.HasNotBeenSet(nameof(Immediate)))
        {
            bitDropdown.Immediate = Immediate.Value;
        }

        if (IsLoading.HasValue && bitDropdown.HasNotBeenSet(nameof(IsLoading)))
        {
            bitDropdown.IsLoading = IsLoading.Value;
        }

        if (ItemCheckIcon is not null && bitDropdown.HasNotBeenSet(nameof(ItemCheckIcon)))
        {
            bitDropdown.ItemCheckIcon = ItemCheckIcon;
        }

        if (ItemCheckIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(ItemCheckIconName)))
        {
            bitDropdown.ItemCheckIconName = ItemCheckIconName;
        }

        if (Items is not null && bitDropdown.HasNotBeenSet(nameof(Items)))
        {
            bitDropdown.Items = Items;
        }

        if (ItemSize.HasValue && bitDropdown.HasNotBeenSet(nameof(ItemSize)))
        {
            bitDropdown.ItemSize = ItemSize.Value;
        }

        if (ItemsProvider is not null && bitDropdown.HasNotBeenSet(nameof(ItemsProvider)))
        {
            bitDropdown.ItemsProvider = ItemsProvider;
        }

        if (ItemsProviderDebounceTime.HasValue && bitDropdown.HasNotBeenSet(nameof(ItemsProviderDebounceTime)))
        {
            bitDropdown.ItemsProviderDebounceTime = ItemsProviderDebounceTime.Value;
        }

        if (Label.HasValue() && bitDropdown.HasNotBeenSet(nameof(Label)))
        {
            bitDropdown.Label = Label;
        }

        if (LoadingText.HasValue() && bitDropdown.HasNotBeenSet(nameof(LoadingText)))
        {
            bitDropdown.LoadingText = LoadingText;
        }

        if (MaxDisplayedItems.HasValue && bitDropdown.HasNotBeenSet(nameof(MaxDisplayedItems)))
        {
            bitDropdown.MaxDisplayedItems = MaxDisplayedItems.Value;
        }

        if (MaxHeight.HasValue && bitDropdown.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitDropdown.MaxHeight = MaxHeight.Value;
        }

        if (MaxSelectedItems.HasValue && bitDropdown.HasNotBeenSet(nameof(MaxSelectedItems)))
        {
            bitDropdown.MaxSelectedItems = MaxSelectedItems.Value;
        }

        if (MaxSelectedItemsText.HasValue() && bitDropdown.HasNotBeenSet(nameof(MaxSelectedItemsText)))
        {
            bitDropdown.MaxSelectedItemsText = MaxSelectedItemsText;
        }

        if (MinSearchLength.HasValue && bitDropdown.HasNotBeenSet(nameof(MinSearchLength)))
        {
            bitDropdown.MinSearchLength = MinSearchLength.Value;
        }

        if (MinSearchLengthText.HasValue() && bitDropdown.HasNotBeenSet(nameof(MinSearchLengthText)))
        {
            bitDropdown.MinSearchLengthText = MinSearchLengthText;
        }

        if (MultiSelect.HasValue && bitDropdown.HasNotBeenSet(nameof(MultiSelect)))
        {
            bitDropdown.MultiSelect = MultiSelect.Value;
        }

        if (MultiSelectDelimiter.HasValue() && bitDropdown.HasNotBeenSet(nameof(MultiSelectDelimiter)))
        {
            bitDropdown.MultiSelectDelimiter = MultiSelectDelimiter;
        }

        if (NameSelectors is not null && bitDropdown.HasNotBeenSet(nameof(NameSelectors)))
        {
            bitDropdown.NameSelectors = NameSelectors;
        }

        if (NoBorder.HasValue && bitDropdown.HasNotBeenSet(nameof(NoBorder)))
        {
            bitDropdown.NoBorder = NoBorder.Value;

            bitDropdown.ClassBuilder.Reset();
        }

        if (NoResultsText.HasValue() && bitDropdown.HasNotBeenSet(nameof(NoResultsText)))
        {
            bitDropdown.NoResultsText = NoResultsText;
        }

        if (NoWrapNavigation.HasValue && bitDropdown.HasNotBeenSet(nameof(NoWrapNavigation)))
        {
            bitDropdown.NoWrapNavigation = NoWrapNavigation.Value;
        }

        if (OpenOnFocus.HasValue && bitDropdown.HasNotBeenSet(nameof(OpenOnFocus)))
        {
            bitDropdown.OpenOnFocus = OpenOnFocus.Value;
        }

        if (OverflowTextFormat.HasValue() && bitDropdown.HasNotBeenSet(nameof(OverflowTextFormat)))
        {
            bitDropdown.OverflowTextFormat = OverflowTextFormat;
        }

        if (OverscanCount.HasValue && bitDropdown.HasNotBeenSet(nameof(OverscanCount)))
        {
            bitDropdown.OverscanCount = OverscanCount.Value;
        }

        if (Placeholder.HasValue() && bitDropdown.HasNotBeenSet(nameof(Placeholder)))
        {
            bitDropdown.Placeholder = Placeholder;
        }

        if (Prefix.HasValue() && bitDropdown.HasNotBeenSet(nameof(Prefix)))
        {
            bitDropdown.Prefix = Prefix;
        }

        if (PreserveCalloutWidth.HasValue && bitDropdown.HasNotBeenSet(nameof(PreserveCalloutWidth)))
        {
            bitDropdown.PreserveCalloutWidth = PreserveCalloutWidth.Value;
        }

        if (Reselectable.HasValue && bitDropdown.HasNotBeenSet(nameof(Reselectable)))
        {
            bitDropdown.Reselectable = Reselectable.Value;
        }

        if (Responsive.HasValue && bitDropdown.HasNotBeenSet(nameof(Responsive)))
        {
            bitDropdown.Responsive = Responsive.Value;
        }

        if (ResponsiveCloseButtonAriaLabel.HasValue() && bitDropdown.HasNotBeenSet(nameof(ResponsiveCloseButtonAriaLabel)))
        {
            bitDropdown.ResponsiveCloseButtonAriaLabel = ResponsiveCloseButtonAriaLabel;
        }

        if (ResponsiveCloseIcon is not null && bitDropdown.HasNotBeenSet(nameof(ResponsiveCloseIcon)))
        {
            bitDropdown.ResponsiveCloseIcon = ResponsiveCloseIcon;
        }

        if (ResponsiveCloseIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(ResponsiveCloseIconName)))
        {
            bitDropdown.ResponsiveCloseIconName = ResponsiveCloseIconName;
        }

        if (SearchBoxAriaLabel.HasValue() && bitDropdown.HasNotBeenSet(nameof(SearchBoxAriaLabel)))
        {
            bitDropdown.SearchBoxAriaLabel = SearchBoxAriaLabel;
        }

        if (SearchBoxClearButtonAriaLabel.HasValue() && bitDropdown.HasNotBeenSet(nameof(SearchBoxClearButtonAriaLabel)))
        {
            bitDropdown.SearchBoxClearButtonAriaLabel = SearchBoxClearButtonAriaLabel;
        }

        if (SearchBoxClearIcon is not null && bitDropdown.HasNotBeenSet(nameof(SearchBoxClearIcon)))
        {
            bitDropdown.SearchBoxClearIcon = SearchBoxClearIcon;
        }

        if (SearchBoxClearIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(SearchBoxClearIconName)))
        {
            bitDropdown.SearchBoxClearIconName = SearchBoxClearIconName;
        }

        if (SearchBoxIcon is not null && bitDropdown.HasNotBeenSet(nameof(SearchBoxIcon)))
        {
            bitDropdown.SearchBoxIcon = SearchBoxIcon;
        }

        if (SearchBoxIconName.HasValue() && bitDropdown.HasNotBeenSet(nameof(SearchBoxIconName)))
        {
            bitDropdown.SearchBoxIconName = SearchBoxIconName;
        }

        if (SearchBoxPlaceholder.HasValue() && bitDropdown.HasNotBeenSet(nameof(SearchBoxPlaceholder)))
        {
            bitDropdown.SearchBoxPlaceholder = SearchBoxPlaceholder;
        }

        if (SearchFunction is not null && bitDropdown.HasNotBeenSet(nameof(SearchFunction)))
        {
            bitDropdown.SearchFunction = SearchFunction;
        }

        if (SearchIgnoreDiacritics.HasValue && bitDropdown.HasNotBeenSet(nameof(SearchIgnoreDiacritics)))
        {
            bitDropdown.SearchIgnoreDiacritics = SearchIgnoreDiacritics.Value;
        }

        if (SearchMode.HasValue && bitDropdown.HasNotBeenSet(nameof(SearchMode)))
        {
            bitDropdown.SearchMode = SearchMode.Value;
        }

        if (SearchResultsText.HasValue() && bitDropdown.HasNotBeenSet(nameof(SearchResultsText)))
        {
            bitDropdown.SearchResultsText = SearchResultsText;
        }

        if (SelectAllText.HasValue() && bitDropdown.HasNotBeenSet(nameof(SelectAllText)))
        {
            bitDropdown.SelectAllText = SelectAllText;
        }

        if (SelectedItemsTextFormat.HasValue() && bitDropdown.HasNotBeenSet(nameof(SelectedItemsTextFormat)))
        {
            bitDropdown.SelectedItemsTextFormat = SelectedItemsTextFormat;
        }

        if (SelectTextOnFocus.HasValue && bitDropdown.HasNotBeenSet(nameof(SelectTextOnFocus)))
        {
            bitDropdown.SelectTextOnFocus = SelectTextOnFocus.Value;
        }

        if (ShowClearButton.HasValue && bitDropdown.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitDropdown.ShowClearButton = ShowClearButton.Value;
        }

        if (ShowSearchBox.HasValue && bitDropdown.HasNotBeenSet(nameof(ShowSearchBox)))
        {
            bitDropdown.ShowSearchBox = ShowSearchBox.Value;
        }

        if (ShowSelectAll.HasValue && bitDropdown.HasNotBeenSet(nameof(ShowSelectAll)))
        {
            bitDropdown.ShowSelectAll = ShowSelectAll.Value;
        }

        if (Size.HasValue && bitDropdown.HasNotBeenSet(nameof(Size)))
        {
            bitDropdown.Size = Size.Value;

            bitDropdown.ClassBuilder.Reset();
        }

        if (StickyHeaders.HasValue && bitDropdown.HasNotBeenSet(nameof(StickyHeaders)))
        {
            bitDropdown.StickyHeaders = StickyHeaders.Value;
        }

        if (Styles is not null && bitDropdown.HasNotBeenSet(nameof(Styles)))
        {
            bitDropdown.Styles = Styles;

            bitDropdown.StyleBuilder.Reset();
        }

        if (Suffix.HasValue() && bitDropdown.HasNotBeenSet(nameof(Suffix)))
        {
            bitDropdown.Suffix = Suffix;
        }

        if (ThrottleTime.HasValue && bitDropdown.HasNotBeenSet(nameof(ThrottleTime)))
        {
            bitDropdown.ThrottleTime = ThrottleTime.Value;
        }

        if (Title.HasValue() && bitDropdown.HasNotBeenSet(nameof(Title)))
        {
            bitDropdown.Title = Title;
        }

        if (TokenSeparators is not null && bitDropdown.HasNotBeenSet(nameof(TokenSeparators)))
        {
            bitDropdown.TokenSeparators = TokenSeparators;
        }

        if (Transparent.HasValue && bitDropdown.HasNotBeenSet(nameof(Transparent)))
        {
            bitDropdown.Transparent = Transparent.Value;

            bitDropdown.ClassBuilder.Reset();
        }

        if (Underlined.HasValue && bitDropdown.HasNotBeenSet(nameof(Underlined)))
        {
            bitDropdown.Underlined = Underlined.Value;

            bitDropdown.ClassBuilder.Reset();
        }

        if (ValueComparer is not null && bitDropdown.HasNotBeenSet(nameof(ValueComparer)))
        {
            bitDropdown.ValueComparer = ValueComparer;
        }

        if (Virtualize.HasValue && bitDropdown.HasNotBeenSet(nameof(Virtualize)))
        {
            bitDropdown.Virtualize = Virtualize.Value;
        }
    }
}
