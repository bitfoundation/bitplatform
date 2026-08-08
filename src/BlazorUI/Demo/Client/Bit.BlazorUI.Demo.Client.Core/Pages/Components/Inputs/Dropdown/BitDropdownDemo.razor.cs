namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class BitDropdownDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoClearSearch",
            Type = "bool",
            DefaultValue = "false",
            Description = "Clears the typed search text after each selection in the multi select ComboBox mode, so the next item is picked from the full list instead of from the previous filter.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives the focus to the dropdown as soon as it is rendered.",
        },
        new()
        {
            Name = "AutoFocusSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables auto-focusing of the SearchBox input when the callout is open.",
        },
        new()
        {
            Name = "AutoSelectFirstMatch",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes Enter in the ComboBox mode pick the first item the typed text matches when no item matches it exactly, which is what an autocomplete does: typing \"app\" and pressing Enter then selects \"Apple\" instead of doing nothing. It takes precedence over Dynamic, so a term that matches an existing item selects that item rather than creating a new one out of it.",
        },
        new()
        {
            Name = "CalloutFooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render as a footer in the callout.",
        },
        new()
        {
            Name = "CalloutHeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render as a header in the callout.",
        },
        new()
        {
            Name = "CaretDownIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the chevron down element. Takes precedence over CaretDownIconName when both are set. Use for external icon libraries (e.g. BitIconInfo.Fa(\"solid chevron-down\"), BitIconInfo.Bi(\"chevron-down\"), BitIconInfo.Css(\"my-class\")).",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CaretDownIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the chevron down element of the dropdown from the Fluent UI icon set.",
        },
        new()
        {
            Name = "CaretDownTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the chevron down element of the dropdown.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Dropdown, a list of BitDropdownOption components.",
        },
        new()
        {
            Name = "Chips",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the selected items like chips in the BitDropdown.",
        },
        new()
        {
            Name = "ChipsRemoveButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format of the accessible name of the remove button of a chip, which receives the text of the item the chip stands for, for example \"Remove {0}\". Defaults to the English message.",
        },
        new()
        {
            Name = "ChipsRemoveIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the remove button in the chips display. Takes precedence over ChipsRemoveIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ChipsRemoveIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the remove button in the chips display from the Fluent UI icon set.",
        },
        new()
        {
            Name = "ChipTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template for the content of a chip in the chips display, which receives the item the chip stands for. It replaces the text of the chip only; the remove button is still rendered after it.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitDropdownClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitDropdown.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "ClearButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name (and the tooltip) of the clear button of the dropdown. Defaults to the English message.",
        },
        new()
        {
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the clear button of the dropdown. Takes precedence over ClearButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the clear button of the dropdown from the Fluent UI icon set.",
        },
        new()
        {
            Name = "CloseOnSelect",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Determines whether picking an item in the callout closes it. It defaults to the behavior each mode expects: a single select dropdown closes, because the pick is the whole interaction, while a multi select one stays open so the next item can be picked right away. Set it explicitly to keep a single select callout open (a long list the user keeps trying options from) or to close a multi select one after every pick.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the dropdown.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Combo",
            Type = "bool",
            DefaultValue = "false",
            Description = "Activates the ComboBox feature in BitDropDown component.",
        },
        new()
        {
            Name = "ComboBoxAddButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name (and the tooltip) of the add button in the responsive ComboBox mode. Defaults to the English message.",
        },
        new()
        {
            Name = "ComboBoxAddButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the add button in the responsive ComboBox mode. Takes precedence over ComboBoxAddButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ComboBoxAddButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the add button in the responsive ComboBox mode from the Fluent UI icon set.",
        },
        new()
        {
            Name = "DefaultValues",
            Type = "IEnumerable<TValue?>?",
            DefaultValue = "null",
            Description = "The default values that will be initially used to set selected items in multi select mode if the Values parameter is not set.",
        },
        new()
        {
            Name = "DebounceTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The debounce time in milliseconds for the search and combo box inputs (applied when Immediate is enabled).",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The description rendered below the dropdown, which is also tied to it as its accessible description through aria-describedby.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the description of the dropdown, which replaces Description and is tied to the dropdown in the same way.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout.",
        },
        new()
        {
            Name = "Dynamic",
            Type = "bool",
            DefaultValue = "false",
            Description = "It is allowed to add a new item in the ComboBox mode. While the typed text names no item the list offers to create one out of it.",
        },
        new()
        {
            Name = "DynamicItemTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "The custom template for the row the callout offers to create a new item with in the Dynamic ComboBox mode, which receives the text the item would be created from.",
        },
        new()
        {
            Name = "DynamicItemTextFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format of the row the callout offers to create a new item with in the Dynamic ComboBox mode, which receives the text the item would be created from, for example \"Add \\\"{0}\\\"\".",
        },
        new()
        {
            Name = "DynamicValueGenerator",
            Type = "Func<TItem?, TValue>?",
            DefaultValue = "null",
            Description = "The function for generating value in a custom item when a new item is on added Dynamic ComboBox mode.",
        },
        new()
        {
            Name = "EmptyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render in the callout when there is no item to show.",
        },
        new()
        {
            Name = "EmptyText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to render in the callout when there is no item to show.",
        },
        new()
        {
            Name = "ExistsSelectedItemFunction",
            Type = "Func<ICollection<TItem>, string, bool>?",
            DefaultValue = "null",
            Description = "Decides whether the text committed in the ComboBox mode already stands for one of the selected items, in place of the default comparison of that text with the item texts, ignoring case. It receives the selected items and the committed text, and returning true stops the commit, so the same item cannot be selected (or created) twice under a name your data considers equivalent.",
        },
        new()
        {
            Name = "FindItemFunction",
            Type = "Func<ICollection<TItem>, string, TItem?>?",
            DefaultValue = "null",
            Description = "Finds the item the text committed in the ComboBox mode stands for, in place of the default comparison of that text with the item texts, ignoring case. It receives the items and the committed text; the item it returns gets selected, and only when it returns none does AutoSelectFirstMatch and then Dynamic get their turn.",
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables fit-content value for the width of the root element.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the header items of the dropdown.",
        },
        new()
        {
            Name = "HideSelectedItems",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the already selected items from the callout, which suits a multi select dropdown whose selection is visible as chips and whose list is therefore only about what is left to pick. A group header left naming nothing, and a divider left without items on one of its sides, are removed along with them. It has no effect when the items come from an ItemsProvider, which hands over the window it was asked for and is the only place that can leave the selected items out of it.",
        },
        new()
        {
            Name = "HighlightSearch",
            Type = "bool",
            DefaultValue = "false",
            Description = "Highlights the part of the item text that matched the current search text in the callout. Only applies to the default item rendering, not to a custom ItemTemplate. The highlighted part is found by the built-in algorithm (SearchMode and SearchIgnoreDiacritics), so a custom SearchFunction that matches by some other rule can produce items with nothing to highlight.",
        },
        new()
        {
            Name = "InitialSelectedItems",
            Type = "IEnumerable<TItem>?",
            DefaultValue = "null",
            Description = "The initial items that will be used to set selected items when using an ItemProvider.",
        },
        new()
        {
            Name = "Immediate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Searches the items as the user types in the search box (based on the 'oninput' HTML event) instead of waiting for the search box to be committed. The ComboBox input always searches as it is typed, so there it only decides whether DebounceTime and ThrottleTime apply.",
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a loading indicator in the callout (and in place of the caret down element) while the items are being fetched. The dropdown stays interactive, so the user can still open the callout and see the loading state.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout. (two-way bound)",
        },
        new()
        {
            Name = "ItemCheckIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the check mark in the multi-select items. Takes precedence over ItemCheckIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ItemCheckIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the check mark in the multi-select items from the Fluent UI icon set.",
        },
        new()
        {
            Name = "Items",
            Type = "ICollection<TItem>?",
            DefaultValue = "null",
            Description = "The list of items to display in the callout.",
            LinkType = LinkType.Link,
            Href = "#dropdown-item"
        },
        new()
        {
            Name = "ItemSize",
            Type = "int",
            DefaultValue = "35",
            Description = "The height of each item in pixels for virtualization.",
        },
        new()
        {
            Name = "ItemsProvider",
            Type = "BitDropdownItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list for virtualization. It loads the items on demand, in the windows the user actually scrolls to, and receives the current search text so the filtering happens at the source instead of over an already loaded list. It requires Virtualize to be enabled, which is what requests the windows.",
        },
        new()
        {
            Name = "ItemsProviderDebounceTime",
            Type = "int",
            DefaultValue = "100",
            Description = "The delay in milliseconds before an ItemsProvider request is issued, which collapses the bursts of requests produced by fast scrolling and typing into a single one.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the items of the dropdown.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the label element of the dropdown.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the label of the dropdown.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render in the callout in place of the items while IsLoading is enabled.",
        },
        new()
        {
            Name = "LoadingText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to render in the callout in place of the items while IsLoading is enabled. Defaults to \"Loading...\".",
        },
        new()
        {
            Name = "MaxDisplayedItems",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum number of selected items rendered in the dropdown itself. Beyond it, the chips display collapses the extra ones into an overflow indicator and the text display switches to a summary. Zero or null renders every selected item.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum height of the scrollable item list of the callout in pixels. It is applied on top of the space the viewport leaves, so it can only ever make the list shorter. A value that is not greater than zero (and null) leaves the viewport alone to decide.",
        },
        new()
        {
            Name = "MaxSelectedItems",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum number of items that can be selected in multi select mode. A value that is not greater than zero (and null) means no limit.",
        },
        new()
        {
            Name = "MaxSelectedItemsText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format of the message announced to screen readers once MaxSelectedItems is reached, which receives that limit, for example \"Maximum of {0} items selected\". Defaults to the English message.",
        },
        new()
        {
            Name = "MinSearchLength",
            Type = "int",
            DefaultValue = "0",
            Description = "The number of characters the search text must reach before the items get filtered. While the search text is shorter, the full list is shown and no search is performed.",
        },
        new()
        {
            Name = "MultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the multi select mode.",
        },
        new()
        {
            Name = "MultiSelectDelimiter",
            Type = "string",
            DefaultValue = ", ",
            Description = "The delimiter for joining the values to create the text of the dropdown in multi select mode.",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the hidden native select element that mirrors the selection, so the value takes part in a plain HTML form post. It is intended for use with forms and is not displayed in the UI.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitDropdownNameSelectors<TItem, TValue>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors"
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border from the root element.",
        },
        new()
        {
            Name = "NoResultsTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render in the callout when the current search has no result. Falls back to the EmptyTemplate when not set.",
        },
        new()
        {
            Name = "NoResultsText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to render in the callout when the current search has no result. Falls back to the EmptyText when not set.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "The callback that is called when the selection gets cleared by the clear button.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The click callback for the dropdown.",
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            Description = "The callback that is called when the callout gets closed.",
        },
        new()
        {
            Name = "OnDeselectItem",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when a selected item gets unselected in multi select mode, by picking it again in the callout, by removing its chip, or through the UnselectItem method. Clearing the whole selection reports itself through OnClear instead.",
        },
        new()
        {
            Name = "OnDynamicAdd",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when a new item is on added Dynamic ComboBox mode.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "The callback that is called when the dropdown (or any element inside it, like the ComboBox input) receives the focus.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "The callback that is called when the dropdown (or any element inside it, like the ComboBox input) loses the focus. The callout is rendered outside the dropdown so that it can escape any clipping ancestor, so moving the focus into it (with the arrow keys, or by clicking the search box) counts as leaving the dropdown here.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "The callback that is called when the callout gets opened.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            Description = "The callback that is called when the search text of the search box or combo box input changes, with the term the items are getting filtered by.",
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when an item gets picked in the callout. In multi select mode it reports every pick, including the one that unselects an already selected item; use OnDeselectItem to be told only about those.",
        },
        new()
        {
            Name = "OnValuesChange",
            Type = "EventCallback<IEnumerable<TValue?>>",
            Description = "The callback that is called when the selected items change.",
        },
        new()
        {
            Name = "OpenOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the callout as soon as the dropdown receives the focus, so tabbing into it (or clicking any part of it) already shows the items without a further click or key press.",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent.",
        },
        new()
        {
            Name = "OverflowTextFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format of the overflow indicator that stands for the selected items beyond MaxDisplayedItems in the chips display, for example \"+{0}\".",
        },
        new()
        {
            Name = "OverscanCount",
            Type = "int",
            DefaultValue = "3",
            Description = "Determines how many additional items are rendered before and after the visible region.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the dropdown.",
        },
        new()
        {
            Name = "PlaceholderTemplate",
            Type = "RenderFragment<BitDropdown<TItem, TValue>>?",
            DefaultValue = "null",
            Description = "The custom template for the placeholder of the dropdown.",
        },
        new()
        {
            Name = "Prefix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Prefix displayed before the dropdown contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom prefix for dropdown.",
        },
        new()
        {
            Name = "PreserveCalloutWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables automatic setting of the callout width and preserves its original width.",
        },
        new()
        {
            Name = "Reselectable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables calling the select events when the same item is selected in single select mode.",
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the responsive mode of the component for small screens.",
        },
        new()
        {
            Name = "ResponsiveCloseButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name (and the tooltip) of the close button in the responsive mode callout. Defaults to the English message.",
        },
        new()
        {
            Name = "ResponsiveCloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button in the responsive mode callout. Takes precedence over ResponsiveCloseIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ResponsiveCloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the close button in the responsive mode callout from the Fluent UI icon set.",
        },
        new()
        {
            Name = "SearchBoxAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the SearchBox input. Defaults to the English message.",
        },
        new()
        {
            Name = "SearchBoxClearButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name (and the tooltip) of the clear button of the SearchBox. Defaults to the English message.",
        },
        new()
        {
            Name = "SearchBoxClearIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the clear icon in the SearchBox. Takes precedence over SearchBoxClearIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "SearchBoxClearIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the clear icon in the SearchBox from the Fluent UI icon set.",
        },
        new()
        {
            Name = "SearchBoxIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the search icon in the SearchBox. Takes precedence over SearchBoxIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "SearchBoxIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the search icon in the SearchBox from the Fluent UI icon set.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the SearchBox input.",
        },
        new()
        {
            Name = "SearchFunction",
            Type = "Func<ICollection<TItem>, string, ICollection<TItem>>?",
            DefaultValue = "null",
            Description = "Custom search function to be used in place of the default search algorithm. Takes precedence over SearchMode, which only configures the default algorithm.",
        },
        new()
        {
            Name = "SearchIgnoreDiacritics",
            Type = "bool",
            DefaultValue = "false",
            Description = "Matches the search text against the item texts with the diacritics of both removed, so that \"Jose\" finds \"José\" and \"Muller\" finds \"Müller\". The item text itself is left untouched, and so is the part of it that HighlightSearch emphasizes. Ignored when a SearchFunction is provided, which does its own matching.",
        },
        new()
        {
            Name = "SearchMode",
            Type = "BitDropdownSearchMode",
            DefaultValue = "BitDropdownSearchMode.Contains",
            Description = "Determines how the text of an item is matched against the search text by the default (case-insensitive) search algorithm. Ignored when a SearchFunction is provided.",
            LinkType = LinkType.Link,
            Href = "#search-mode-enum",
        },
        new()
        {
            Name = "SearchResultsText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format of the message announced to screen readers with the number of items the current search produced, for example \"{0} results available\". Defaults to the English message.",
        },
        new()
        {
            Name = "SelectAllText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the select all item in multi select mode.",
        },
        new()
        {
            Name = "SelectedItemsTextFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format that replaces the joined item texts in the dropdown once more than MaxDisplayedItems items are selected, for example \"{0} items selected\".",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the clear button when an item is selected.",
        },
        new()
        {
            Name = "ShowSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the SearchBox element in the callout.",
        },
        new()
        {
            Name = "ShowSelectAll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the select all item in the callout in multi select mode. It has no effect when the items are provided by an ItemsProvider, since the items that are not loaded yet cannot be selected.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the dropdown.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StickyHeaders",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the header of a group pinned to the top of the item list while its items are scrolled past, so a long grouped list never leaves the user looking at items whose group has scrolled out of view.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitDropdownClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitDropdown.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Suffix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Suffix displayed after the dropdown contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom suffix for dropdown.",
        },
        new()
        {
            Name = "TextTemplate",
            Type = "RenderFragment<BitDropdown<TItem, TValue>>?",
            DefaultValue = "null",
            Description = "The custom template for the text of the dropdown.",
        },
        new()
        {
            Name = "ThrottleTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The throttle time in milliseconds for the search and combo box inputs (applied when Immediate is enabled).",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse hovers over the dropdown.",
        },
        new()
        {
            Name = "TokenSeparators",
            Type = "char[]?",
            DefaultValue = "null",
            Description = "The characters that split the text typed (or pasted) into the multi select ComboBox input into separate terms, each committed as its own selection exactly as typing it and pressing Enter would: a term naming an existing item selects it, and with Dynamic enabled a term naming none adds a new item.",
        },
        new()
        {
            Name = "Transparent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default background color from the root element.",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the dropdown with only a bottom border in place of the box around it.",
        },
        new()
        {
            Name = "ValueComparer",
            Type = "IEqualityComparer<TValue>?",
            DefaultValue = "null",
            Description = "Decides whether two values stand for the same selection, in place of the default equality of TValue. It governs every value comparison the component makes: which item a value selects, which selected item a chip removes, and whether a typed term is already selected.",
        },
        new()
        {
            Name = "Values",
            Type = "IEnumerable<TValue?>?",
            DefaultValue = "null",
            Description = "The values of the selected items in multi select mode. (two-way bound)",
        },
        new()
        {
            Name = "Virtualize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables virtualization to render only the visible items.",
        },
        new()
        {
            Name = "VirtualizePlaceholder",
            Type = "RenderFragment<PlaceholderContext>?",
            DefaultValue = "null",
            Description = "The template for items that have not yet been rendered in virtualization mode.",
        }
    ];
    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "dropdown-item",
            Title = "BitDropdownItem<TValue>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The aria label attribute for the dropdown item."
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the dropdown item."
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The id for the dropdown item."
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon name from the Fluent UI icon set. For external icon libraries, use Icon instead."
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the dropdown item to provide state for the item template."
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Determines if the dropdown item is enabled."
               },
               new()
               {
                   Name = "IsHidden",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the dropdown item is hidden."
               },
               new()
               {
                   Name = "ItemType",
                   Type = "BitDropdownItemType",
                   DefaultValue = "BitDropdownItemType.Normal",
                   Description = "The type of the dropdown item.",
                   LinkType = LinkType.Link,
                   Href = "#item-type-enum"
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the dropdown item."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text to render for the dropdown item."
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title attribute for the dropdown item."
               },
               new()
               {
                   Name = "Value",
                   Type = "TValue?",
                   DefaultValue = "null",
                   Description = "The value of the dropdown item."
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the item is selected. This property's value is assigned by the component."
               },
            ],
        },
        new()
        {
            Id = "dropdown-option",
            Title = "BitDropdownOption<TValue>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The aria label attribute for the dropdown option."
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the dropdown option."
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The id for the dropdown option."
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the dropdown option to provide extra state for the template."
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Determines if the dropdown option is enabled."
               },
               new()
               {
                   Name = "IsHidden",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the dropdown option is hidden."
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon name from the Fluent UI icon set. For external icon libraries, use Icon instead."
               },
               new()
               {
                   Name = "ItemType",
                   Type = "BitDropdownItemType",
                   DefaultValue = "BitDropdownItemType.Normal",
                   Description = "The type of the dropdown option.",
                   LinkType = LinkType.Link,
                   Href = "#item-type-enum"
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the dropdown option."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text to render for the dropdown option."
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title attribute for the dropdown option."
               },
               new()
               {
                   Name = "Value",
                   Type = "TValue?",
                   DefaultValue = "null",
                   Description = "The value of the dropdown option."
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the option is selected. This property's value is assigned by the component."
               },
            ],
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitDropdownNameSelectors<TItem, TValue>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.AriaLabel))",
                   Description = "The AriaLabel field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Class))",
                   Description = "The CSS Class field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Id",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Id))",
                   Description = "The Id field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Data",
                   Type = "BitNameSelectorPair<TItem, object?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Data))",
                   Description = "The Data field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.IsEnabled))",
                   Description = "The IsEnabled field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsHidden",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.IsHidden))",
                   Description = "The IsHidden field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "ItemType",
                   Type = "BitNameSelectorPair<TItem, BitDropdownItemType>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.ItemType))",
                   Description = "The ItemType field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#item-type-enum"
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Icon))",
                   Description = "The Icon field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info"
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.IconName))",
                   Description = "The IconName field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Style))",
                   Description = "The CSS Style field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Text))",
                   Description = "The Text field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Title",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Title))",
                   Description = "The Title field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Value",
                   Type = "BitNameSelectorPair<TItem, TValue?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Value))",
                   Description = "The Value field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "TextSetter",
                   Type = "Action<string, TItem>?",
                   Description = "The setter function for updating Text property of custom item in Dynamic ComboBox mode upon new item addition.",
               },
               new()
               {
                   Name = "ValueSetter",
                   Type = "Action<TItem, TItem>?",
                   Description = "The setter function for updating Value property of custom item in Dynamic ComboBox mode upon new item addition.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "string",
                   Description = "The IsSelected field name of the custom input class. This property's value is assigned by the component.",
               }
            ],
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair<TItem, TProp>",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string",
                   Description = "Custom class property name."
               },
               new()
               {
                   Name = "Selector",
                   Type = "Func<TItem, TProp?>?",
                   Description = "Custom class property selector."
               }
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitDropdownClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitDropdown."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitDropdown."
               },
               new()
               {
                   Name = "DescriptionContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description container of the BitDropdown."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description of the BitDropdown."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the main container of the BitDropdown."
               },
               new()
               {
                   Name = "TextContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text container of the BitDropdown."
               },
               new()
               {
                   Name = "ClearButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the clear button of the BitDropdown."
               },
               new()
               {
                   Name = "CaretDownIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the caret down icon of the BitDropdown."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitDropdown."
               },
               new()
               {
                   Name = "Callout",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout of the BitDropdown."
               },
               new()
               {
                   Name = "CalloutHeader",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the callout header template of the BitDropdown."
               },
               new()
               {
                   Name = "CalloutFooter",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the callout footer template of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveLabelContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel's label container of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel label of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveCloseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel's close button of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveCloseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel's close icon of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box container of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxIconContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's icon container of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box icon of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxInput",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box input of the BitDropdown."
               },
               new()
               {
                   Name = "ComboBoxInput",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the ComboBox input of the BitDropdown."
               },
               new()
               {
                   Name = "Chips",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chips container of the BitDropdown."
               },
               new()
               {
                   Name = "OverflowChip",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow chip of the BitDropdown."
               },
               new()
               {
                   Name = "ChipsRemoveButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chips's remove button of the BitDropdown."
               },
               new()
               {
                   Name = "ChipsRemoveIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chips's remove icon of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxClearButtonContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's clear button container of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxClearButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's clear button of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxClearIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's clear icon of the BitDropdown."
               },
               new()
               {
                   Name = "SelectAllContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the select all item container of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "SelectAllButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the select all item button of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "SelectAllCheckBox",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the select all item check box of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "SelectAllCheckIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the select all item check icon of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "SelectAllText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the select all item text of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "ScrollContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the scroll container of the BitDropdown."
               },
               new()
               {
                   Name = "ItemHeader",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item header of the BitDropdown."
               },
               new()
               {
                   Name = "ItemWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item wrapper of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "ItemButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item button of the BitDropdown."
               },
               new()
               {
                   Name = "ItemCheckBox",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item check box of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "ItemCheckIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item check icon of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item icon of the BitDropdown."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item text of the BitDropdown."
               },
               new()
               {
                   Name = "ItemHighlight",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the highlighted part of the item text of the BitDropdown."
               },
               new()
               {
                   Name = "ItemDivider",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item divider of the BitDropdown."
               },
               new()
               {
                   Name = "DynamicItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the row that offers to create a new item out of the typed text in the Dynamic ComboBox mode of the BitDropdown."
               },
               new()
               {
                   Name = "EmptyContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the empty state content of the BitDropdown."
               },
               new()
               {
                   Name = "LoadingContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading state container of the BitDropdown."
               },
               new()
               {
                   Name = "LoadingText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading state text of the BitDropdown."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading spinner of the BitDropdown."
               },
               new()
               {
                   Name = "PrefixContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dropdown's prefix container."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dropdown's prefix."
               },
               new()
               {
                   Name = "SuffixContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dropdown's suffix container."
               },
               new()
               {
                   Name = "Suffix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dropdown's suffix."
               },
               new()
               {
                   Name = "ResponsiveComboInputContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the ComboBox input container in responsive mode of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveComboAddButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the add new item combo box button in responsive mode of the BitDropdown."
               },
            ],
        }
    ];
    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "item-type-enum",
            Name = "BitDropdownItemType",
            Items =
            [
                new()
                {
                    Name = "Normal",
                    Description = "Dropdown items are being rendered as a normal item.",
                    Value = "0",
                },
                new()
                {
                    Name = "Header",
                    Description = "Dropdown items are being rendered as a header, they cannot be selected.",
                    Value = "1",
                },
                new()
                {
                    Name = "Divider",
                    Description = "Dropdown items are being rendered as a divider, just draw a line.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "search-mode-enum",
            Name = "BitDropdownSearchMode",
            Description = "Determines how the text of an item is matched against the search text.",
            Items =
            [
                new()
                {
                    Name = "Contains",
                    Description = "An item matches when its text contains the search text.",
                    Value = "0",
                },
                new()
                {
                    Name = "StartsWith",
                    Description = "An item matches when its text starts with the search text.",
                    Value = "1",
                },
                new()
                {
                    Name = "EndsWith",
                    Description = "An item matches when its text ends with the search text.",
                    Value = "2",
                },
                new()
                {
                    Name = "ExactMatch",
                    Description = "An item matches when its text is equal to the search text.",
                    Value = "3",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "The small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "The medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "The large size.",
                    Value = "2",
                }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
    ];
    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "SelectedItems",
            Type = "IReadOnlyList<TItem>",
            Description = "A readonly list of the current selected items in multi-select mode.",
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            Description = "The current selected item in single-select mode.",
        },
        new()
        {
            Name = "SelectItem",
            Type = "Task SelectItem(TItem? item)",
            Description = "Selects the given item exactly as picking it in the callout would, so the same events fire and the same close and focus behavior follows. An item that is already selected is left alone: in multi select mode picking it again would unselect it, which UnselectItem is for.",
        },
        new()
        {
            Name = "UnselectItem",
            Type = "Task UnselectItem(TItem? item)",
            Description = "Unselects the given item exactly as picking an already selected one in the callout would (or, in single select mode, as the clear button would), so the same events fire. An item that is not selected is left alone.",
        },
        new()
        {
            Name = "RefreshItemsAsync",
            Type = "Task RefreshItemsAsync()",
            Description = "Discards the items loaded so far and asks the ItemsProvider for them again, which is what makes a change outside of the dropdown (a filter of the page, a record added elsewhere) reach a list the dropdown only ever loads on demand. It does nothing without an ItemsProvider, where the Items collection is the source of truth and is re-read on its own.",
        },
        new()
        {
            Name = "AssignIsOpen",
            Type = "Task<bool> AssignIsOpen(bool value)",
            Description = "Opens or closes the callout programmatically, without having to bind the IsOpen parameter. It returns false when the change was refused, which is what a one-way bound IsOpen does.",
        },
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the combobox element of the dropdown, which is the element the user focuses and operates the component with.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask FocusAsync(bool preventScroll = false)",
            Description = "Gives focus to the combobox element of the dropdown.",
        },
        new()
        {
            Name = "ComboInputElement",
            Type = "ElementReference?",
            Description = "The ElementReference to the combo input element.",
        },
        new()
        {
            Name = "FocusComboInputAsync",
            Type = "ValueTask FocusComboInputAsync()",
            Description = "Gives focus to the combo input element.",
        },
        new()
        {
            Name = "SearchInputElement",
            Type = "ElementReference?",
            Description = "The ElementReference to the search input element.",
        },
        new()
        {
            Name = "FocusSearchInputAsync",
            Type = "ValueTask FocusSearchInputAsync()",
            Description = "Gives focus to the search input element.",
        }
    ];
}
