namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PhoneInput;

public partial class BitPhoneInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the number input is auto focused on first render.",
        },
        new()
        {
            Name = "AutoPlaceholder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the pattern the number is currently formatted with as the placeholder of the number input, filling in only when no Placeholder of its own is given.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPhoneInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitPhoneInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ClearButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the clear button of the number input.",
        },
        new()
        {
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the clear button of the number input. Takes precedence over ClearButtonIconName when both are set, and renders icons from external libraries like FontAwesome or Bootstrap Icons.",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the clear button of the number input from the Fluent UI icon set.",
        },
        new()
        {
            Name = "ClearButtonTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the clear button of the number input.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the phone input.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Countries",
            Type = "ICollection<BitCountry>",
            DefaultValue = "BitCountries.All",
            Description = "The list of the countries to show in the country dropdown.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Country",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The currently selected country of the phone input. (two-way bound)",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "CountryName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the hidden field carrying the ISO 3166-1 alpha-2 code of the selected country, for a plain HTML form that stores the country beside the number.",
        },
        new()
        {
            Name = "DebounceTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The debounce time in milliseconds for the number input (applied when Immediate is enabled).",
        },
        new()
        {
            Name = "DefaultCountry",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The default selected country to be initially used when the Country parameter is not set.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The description shown under the phone input, tied to the number input through aria-describedby.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the description of the phone input.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the country dropdown callout.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum",
        },
        new()
        {
            Name = "DropdownAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the country selector (\"Country\" by default), which the name of the selected country is appended to (\"Country: Germany\"). It also names the country list. Set it to translate the selector for a screen reader.",
        },
        new()
        {
            Name = "DropdownPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the country dropdown when no country is selected.",
        },
        new()
        {
            Name = "DropdownTemplate",
            Type = "RenderFragment<BitCountry?>?",
            DefaultValue = "null",
            Description = "The custom template for the content of the country dropdown button, receiving the selected country.",
        },
        new()
        {
            Name = "EnterKeyHint",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the enterkeyhint html attribute of the number input, which decides the label of the return key of an on-screen keyboard.",
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The error message shown under the phone input, which also puts the field in its invalid state.",
        },
        new()
        {
            Name = "ErrorMessageTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the error message of the phone input, putting the field in the same invalid state.",
        },
        new()
        {
            Name = "ExcludeCountries",
            Type = "ICollection<BitCountry>?",
            DefaultValue = "null",
            Description = "The countries to leave out of the country dropdown and of the dialing-code lookup behind it.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "FlagUrlSelector",
            Type = "Func<BitCountry, string?>?",
            DefaultValue = "null",
            Description = "The url of the flag image of a country, replacing the flags that ship with the library. Returning null or an empty string leaves that country with the built-in flag.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the phone input to fill 100% of its container width.",
        },
        new()
        {
            Name = "Immediate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Updates the number input value as the user types (based on the 'oninput' HTML event).",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Sets the inputmode html attribute of the number input, which decides the on-screen keyboard it asks for.",
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the phone input in an invalid state without going through the validation of an EditContext.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the country dropdown callout. (two-way bound)",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitCountry>?",
            DefaultValue = "null",
            Description = "The custom template for each country of the dropdown list.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label of the phone input shown above the field.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the label of the phone input.",
        },
        new()
        {
            Name = "Mask",
            Type = "string?",
            DefaultValue = "null",
            Description = "The pattern the local number is formatted with as the user types, where every '#' is a digit slot and every other character is a literal inserted for them. The formatting is display only: the Value stays in the E.164 form.",
        },
        new()
        {
            Name = "MaskSelector",
            Type = "Func<BitCountry?, string?>?",
            DefaultValue = "null",
            Description = "The pattern to format the local number with for a given country, taking precedence over Mask whenever it returns one.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum height of the country dropdown callout in pixels.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "-1",
            Description = "Determines the maximum number of characters allowed in the number input.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border of the phone input.",
        },
        new()
        {
            Name = "NoDialCode",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the dialing code of the selected country in the country dropdown button.",
        },
        new()
        {
            Name = "NoDropdown",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the country dropdown, so the country of the phone input can only be set through its parameters.",
        },
        new()
        {
            Name = "NoFlags",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the flag images of the countries in the dropdown button and in the dropdown list.",
        },
        new()
        {
            Name = "NoFocusOnSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the focus from moving to the number input once a country has been picked.",
        },
        new()
        {
            Name = "NoResultsMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message to show, and to announce to a screen reader, when the search result of the country dropdown is empty.",
        },
        new()
        {
            Name = "NoResultsTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to show when the search result of the country dropdown is empty.",
        },
        new()
        {
            Name = "NoSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the search box of the country dropdown.",
        },
        new()
        {
            Name = "NoWrapNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the keyboard navigation of the country list from wrapping around at its two ends.",
        },
        new()
        {
            Name = "Number",
            Type = "string?",
            DefaultValue = "null",
            Description = "The local phone number exactly as the input shows it, separators and all, without the country dialing code. (two-way bound)",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "The callback that is invoked when the number input loses focus.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is invoked when the clear button of the number input is clicked.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "The callback that is invoked when the number input is clicked.",
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is invoked when the country dropdown callout closes.",
        },
        new()
        {
            Name = "OnCountryChange",
            Type = "EventCallback<BitCountry?>",
            DefaultValue = "",
            Description = "The callback that is invoked when the selected country changes.",
        },
        new()
        {
            Name = "OnEnter",
            Type = "EventCallback<KeyboardEventArgs>",
            DefaultValue = "",
            Description = "The callback that is invoked when the Enter key is pressed in the number input.",
        },
        new()
        {
            Name = "OnEscape",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "The callback that is invoked when the Escape key is pressed in the number input.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "The callback that is invoked when the number input receives focus.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "The callback that is invoked when focus moves into the phone input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "The callback that is invoked when focus moves out of the phone input.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            DefaultValue = "",
            Description = "The callback that is invoked on every key press in the number input.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is invoked when the country dropdown callout opens.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            DefaultValue = "",
            Description = "The callback that is invoked when the search text of the country dropdown changes.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the number input.",
        },
        new()
        {
            Name = "PreferredCountries",
            Type = "ICollection<BitCountry>?",
            DefaultValue = "null",
            Description = "The countries to pin to the top of the country dropdown list. They also break the tie when a typed dialing code is shared by several countries.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the country dropdown as a full height panel on small screens instead of an inline callout.",
        },
        new()
        {
            Name = "ResponsiveCloseButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the close button of the responsive panel of the country dropdown.",
        },
        new()
        {
            Name = "ResponsiveCloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button of the responsive panel of the country dropdown. Takes precedence over ResponsiveCloseIconName when both are set.",
        },
        new()
        {
            Name = "ResponsiveCloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the close button of the responsive panel of the country dropdown from the Fluent UI icon set.",
        },
        new()
        {
            Name = "SearchBoxAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label for the search box of the country dropdown.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the search box of the country dropdown.",
        },
        new()
        {
            Name = "SearchResultsAnnouncement",
            Type = "Func<int, string?>?",
            DefaultValue = "null",
            Description = "What a screen reader announces for the number of countries a search term leaves in the list, in place of the default \"1 country found\" / \"{count} countries found\". An empty result announces NoResultsMessage instead, and returning null keeps a count from being announced.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a clear button in the number input while it holds a value.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the phone input.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Strict",
            Type = "bool",
            DefaultValue = "false",
            Description = "Discards every character typed or pasted into the number input that cannot be part of a phone number, so only digits (and an optional leading plus sign) survive.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPhoneInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPhoneInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ThrottleTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The throttle time in milliseconds for the number input (applied when Immediate is enabled).",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip (title attribute) of the phone input, written on the root and on the number input so it reaches a keyboard as well as a pointer.",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the phone input with only a bottom border instead of a full one.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitPhoneInputClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitPhoneInput." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the BitPhoneInput." },
                new() { Name = "FieldGroup", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the field group (the container of the country dropdown and the number input) of the BitPhoneInput." },
                new() { Name = "Dropdown", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the country dropdown of the BitPhoneInput." },
                new() { Name = "DropdownText", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the selected country text (flag and dialing code) of the BitPhoneInput." },
                new() { Name = "Caret", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the caret down element of the country dropdown of the BitPhoneInput." },
                new() { Name = "Flag", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the flag image of the countries of the BitPhoneInput." },
                new() { Name = "Input", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the number input (text field) of the BitPhoneInput." },
                new() { Name = "ClearButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the clear button of the number input of the BitPhoneInput." },
                new() { Name = "ClearButtonIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the clear button of the number input of the BitPhoneInput." },
                new() { Name = "ErrorMessageContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the error message of the BitPhoneInput." },
                new() { Name = "ErrorMessage", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error message of the BitPhoneInput." },
                new() { Name = "DescriptionContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the description of the BitPhoneInput." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description of the BitPhoneInput." },
                new() { Name = "Overlay", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the overlay of the country dropdown of the BitPhoneInput." },
                new() { Name = "Callout", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the callout of the country dropdown of the BitPhoneInput." },
                new() { Name = "SearchBox", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the search box of the country dropdown of the BitPhoneInput." },
                new() { Name = "ScrollContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the scroll container of the country dropdown of the BitPhoneInput." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list (the listbox element) of the country dropdown of the BitPhoneInput." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each item of the country dropdown of the BitPhoneInput." },
                new() { Name = "ItemName", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the name of each item in the country dropdown of the BitPhoneInput." },
                new() { Name = "ItemCode", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the dialing code of each item in the country dropdown of the BitPhoneInput." },
                new() { Name = "NoResults", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the no results message of the country dropdown of the BitPhoneInput." },
                new() { Name = "ResponsiveLabelContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label container of the responsive panel of the country dropdown of the BitPhoneInput." },
                new() { Name = "ResponsiveLabel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the responsive panel of the country dropdown of the BitPhoneInput." },
                new() { Name = "ResponsiveCloseButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the close button of the responsive panel of the country dropdown of the BitPhoneInput." },
                new() { Name = "ResponsiveCloseIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the close button of the responsive panel of the country dropdown of the BitPhoneInput." },
            ]
        },
        new()
        {
            Id = "country",
            Title = "BitCountry",
            Description = "Represents the basic information of a specific country.",
            Parameters =
            [
                new() { Name = "Name", Type = "string", Description = "The full name of the country." },
                new() { Name = "Code", Type = "string", Description = "The dialing code of the country." },
                new() { Name = "Iso2", Type = "string", Description = "The ISO 3166-1 alpha-2 code of the country." },
                new() { Name = "Iso3", Type = "string", Description = "The ISO 3166-1 alpha-3 code of the country." },
                new() { Name = "Priority", Type = "int", DefaultValue = "0", Description = "The tie-breaking priority of the country among the ones that share its dialing code, where a higher number wins." },
                new() { Name = "ExtraCodes", Type = "string[]?", DefaultValue = "null", Description = "The other dialing codes the country answers to beyond Code (the Dominican Republic is reached on +1-809, +1-829 and +1-849 alike)." },
                new() { Name = "DigitsCode", Type = "string", Description = "The dialing code of the country reduced to its digits, as it appears in an E.164 number." },
                new() { Name = "DigitsCodes", Type = "string[]", Description = "Every dialing code of the country - Code first, then ExtraCodes - reduced to the digits each of them carries in an E.164 number." },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Value = "0" },
                new() { Name = "Secondary", Value = "1" },
                new() { Name = "Tertiary", Value = "2" },
                new() { Name = "Info", Value = "3" },
                new() { Name = "Success", Value = "4" },
                new() { Name = "Warning", Value = "5" },
                new() { Name = "SevereWarning", Value = "6" },
                new() { Name = "Error", Value = "7" },
            ]
        },
        new()
        {
            Id = "drop-direction-enum",
            Name = "BitDropDirection",
            Description = "Determines the allowed drop directions of a callout.",
            Items =
            [
                new() { Name = "All", Value = "0" },
                new() { Name = "TopAndBottom", Value = "1" },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Value = "0" },
                new() { Name = "Medium", Value = "1" },
                new() { Name = "Large", Value = "2" },
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FullNumber",
            Type = "string?",
            Description = "The full phone number in the E.164 form (\"+[code][number]\"), the same as the bound Value.",
        },
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the number input of the BitPhoneInput.",
        },
        new()
        {
            Name = "OpenAsync",
            Type = "Task",
            Description = "Opens the country list. It does nothing while the field is disabled, read-only or has no dropdown.",
        },
        new()
        {
            Name = "CloseAsync",
            Type = "Task",
            Description = "Closes the country list.",
        },
        new()
        {
            Name = "SelectCountryAsync",
            Type = "Task",
            Description = "Selects the given country exactly as picking it in the list would, so the same events fire.",
        },
        new()
        {
            Name = "SetNumberAsync",
            Type = "Task",
            Description = "Sets the local number the way typing it would: it is laid out over the current pattern, and a number carrying an international prefix ('+' or '00') selects the country that owns its dialing code.",
        },
        new()
        {
            Name = "ClearAsync",
            Type = "Task",
            Description = "Clears the number, keeping the selected country, and raises OnClear when there was something to clear.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the number input of the BitPhoneInput.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-PhoneInput-background", DefaultValue = "--bit-clr-bg-pri", Description = "Fill of the field." },
        new() { Name = "--bit-PhoneInput-color", DefaultValue = "--bit-clr-fg-pri", Description = "Text of the number." },
        new() { Name = "--bit-PhoneInput-placeholder-color", DefaultValue = "--bit-clr-fg-ter", Description = "Placeholder of the number input, of the country selector and of the search box." },
        new() { Name = "--bit-PhoneInput-border-color", DefaultValue = "The Color role, --bit-clr-brd-pri", Description = "Frame at rest." },
        new() { Name = "--bit-PhoneInput-hover-border-color", DefaultValue = "The Color role, --bit-clr-brd-pri-hover", Description = "Frame under a pointer, on devices that can hover." },
        new() { Name = "--bit-PhoneInput-border-width", DefaultValue = "--bit-shp-brd-width", Description = "Thickness of the frame and of the rule between the selector and the number." },
        new() { Name = "--bit-PhoneInput-radius", DefaultValue = "--bit-shp-radius-control", Description = "Corner radius of the frame." },
        new() { Name = "--bit-PhoneInput-min-height", DefaultValue = "Per Size, --bit-siz-ctrl-*", Description = "Smallest height of the frame." },
        new() { Name = "--bit-PhoneInput-font-size", DefaultValue = "Per Size, from the type ramp", Description = "Text size of the field and of the country list." },
        new() { Name = "--bit-PhoneInput-padding", DefaultValue = "0 per Size, --bit-siz-ctrl-pad-x-*", Description = "Padding of the number input." },
        new() { Name = "--bit-PhoneInput-focus-color", DefaultValue = "The Color role's focus color, --bit-clr-pri-focus", Description = "Focused frame and focus ring, and the keyboard cues of the selector, the clear button and the active row of the list." },
        new() { Name = "--bit-PhoneInput-invalid-color", DefaultValue = "--bit-clr-err / --bit-clr-err-focus", Description = "Frame, focus ring and error message of a rejected value." },
        new() { Name = "--bit-PhoneInput-disabled-color", DefaultValue = "--bit-clr-fg-dis", Description = "Text and glyphs when disabled." },
        new() { Name = "--bit-PhoneInput-disabled-background", DefaultValue = "--bit-clr-bg-dis", Description = "Fill when disabled." },
        new() { Name = "--bit-PhoneInput-disabled-border-color", DefaultValue = "--bit-clr-brd-dis", Description = "Frame when disabled." },
        new() { Name = "--bit-PhoneInput-dropdown-background", DefaultValue = "transparent", Description = "Fill of the country selector." },
        new() { Name = "--bit-PhoneInput-dropdown-hover-background", DefaultValue = "--bit-clr-bg-pri-hover", Description = "Fill of the country selector under a pointer." },
        new() { Name = "--bit-PhoneInput-dropdown-color", DefaultValue = "--bit-clr-fg-pri", Description = "The dialing code in the country selector." },
        new() { Name = "--bit-PhoneInput-dropdown-padding", DefaultValue = "0 per Size, --bit-siz-ctrl-pad-x-*", Description = "Padding of the country selector." },
        new() { Name = "--bit-PhoneInput-separator-color", DefaultValue = "--bit-clr-brd-pri", Description = "Rule between the country selector and the number." },
        new() { Name = "--bit-PhoneInput-caret-color", DefaultValue = "--bit-clr-fg-sec", Description = "The caret of the country selector." },
        new() { Name = "--bit-PhoneInput-flag-size", DefaultValue = "Per Size, --bit-siz-icon-*", Description = "Flags of the selector and of the list." },
        new() { Name = "--bit-PhoneInput-flag-radius", DefaultValue = "--bit-shp-radius-none", Description = "Corner radius of a flag." },
        new() { Name = "--bit-PhoneInput-button-color", DefaultValue = "--bit-clr-fg-sec", Description = "Glyph of the clear button." },
        new() { Name = "--bit-PhoneInput-button-hover-background", DefaultValue = "--bit-clr-bg-pri-hover", Description = "Fill of the clear button under a pointer." },
        new() { Name = "--bit-PhoneInput-label-color", DefaultValue = "--bit-clr-fg-pri", Description = "Label text." },
        new() { Name = "--bit-PhoneInput-label-font-size", DefaultValue = "--bit-PhoneInput-font-size", Description = "Label text size." },
        new() { Name = "--bit-PhoneInput-label-font-weight", DefaultValue = "--bit-tpg-field-label-font-weight", Description = "Label weight." },
        new() { Name = "--bit-PhoneInput-required-color", DefaultValue = "--bit-clr-req", Description = "The asterisk of a Required field." },
        new() { Name = "--bit-PhoneInput-description-color", DefaultValue = "--bit-clr-fg-sec", Description = "Description text." },
        new() { Name = "--bit-PhoneInput-footer-font-size", DefaultValue = "--bit-tpg-fs-2xs", Description = "Text size of the description and of the error message." },
        new() { Name = "--bit-PhoneInput-callout-background", DefaultValue = "--bit-clr-bg-pri", Description = "The surface of the country list." },
        new() { Name = "--bit-PhoneInput-callout-border-color", DefaultValue = "--bit-clr-brd-pri", Description = "Border of the country list, and the rules under its search box and under the preferred countries." },
        new() { Name = "--bit-PhoneInput-callout-radius", DefaultValue = "--bit-shp-radius-popup", Description = "Corner radius of the country list." },
        new() { Name = "--bit-PhoneInput-callout-shadow", DefaultValue = "--bit-shd-popup (--bit-shd-sheet for the responsive panel)", Description = "Elevation of the country list." },
        new() { Name = "--bit-PhoneInput-callout-max-height", DefaultValue = "--bit-siz-popup-max-height", Description = "Tallest the scrolling list grows. The MaxHeight parameter wins over it." },
        new() { Name = "--bit-PhoneInput-responsive-width", DefaultValue = "spacing(34)", Description = "Width of the side panel a Responsive field opens on a small screen." },
        new() { Name = "--bit-PhoneInput-overlay-background", DefaultValue = "transparent", Description = "The layer behind an open list; give it a color for a modal-style scrim." },
        new() { Name = "--bit-PhoneInput-item-height", DefaultValue = "Per Size, --bit-siz-item-*", Description = "Smallest height of a row of the list." },
        new() { Name = "--bit-PhoneInput-item-color", DefaultValue = "--bit-clr-fg-pri", Description = "Country name of a row, and the text of the search box." },
        new() { Name = "--bit-PhoneInput-item-code-color", DefaultValue = "--bit-clr-fg-sec", Description = "Dialing code of a row." },
        new() { Name = "--bit-PhoneInput-item-hover-background", DefaultValue = "--bit-clr-bg-pri-hover", Description = "Row under the pointer, and the row the arrow keys are on." },
        new() { Name = "--bit-PhoneInput-item-selected-background", DefaultValue = "--bit-clr-bg-sec", Description = "The selected row." },
        new() { Name = "--bit-PhoneInput-item-selected-color", DefaultValue = "--bit-PhoneInput-item-color", Description = "Text of the selected row." },
    ];



    private string? clearedAt;
    private string? bindingValue;
    private string? bindingNumber;
    private BitCountry? bindingCountry = BitCountries.Germany;
    private BitCountry? changedCountry;
    private string? changedValue;
    private string? searchedText;
    private string calloutState = "closed";
    private int enterPressed;
    private string? maskedValue;
    private string? maskSelectorValue;
    private string? immediateNumber;
    private string? debouncedNumber;
    private string? throttledNumber;
    private bool isCalloutOpen;
    private bool validationSubmitted;
    private string? submissionValue;
    private BitCountry? submissionCountry = BitCountries.Canada;
    private BitPhoneInput controlledInput = default!;
    private readonly BitPhoneInputValidationModel validationModel = new();
    private readonly List<BitCountry> customCountries =
    [
        BitCountries.UnitedStates,
        BitCountries.Canada,
        BitCountries.UnitedKingdom,
        BitCountries.Germany,
        BitCountries.France,
    ];
    private readonly List<BitCountry> preferredCountries =
    [
        BitCountries.Germany,
        BitCountries.France,
        BitCountries.UnitedStates,
    ];
    private readonly List<BitCountry> excludedCountries =
    [
        BitCountries.UnitedStates,
        BitCountries.UnitedKingdom,
        BitCountries.Germany,
    ];
    private readonly List<BitCountry> germanCountries =
    [
        new("Deutschland", "49", "DE", "DEU"),
        new("Österreich", "43", "AT", "AUT"),
        new("Schweiz", "41", "CH", "CHE"),
        new("Frankreich", "33", "FR", "FRA"),
        new("Italien", "39", "IT", "ITA"),
    ];
    private readonly BitPhoneInputParams[] phoneInputParams =
    [
        new()
        {
            DefaultCountry = BitCountries.Germany,
            PreferredCountries = [BitCountries.Germany, BitCountries.Austria, BitCountries.Switzerland],
            ShowClearButton = true,
            Placeholder = "Enter your number",
        }
    ];



    // Any url will do: this one asks a public flag CDN for a 24px png of the country.
    private static string? GetFlagUrl(BitCountry country) => $"https://flagcdn.com/24x18/{country.Iso2.ToLowerInvariant()}.png";

    private static string? GetMask(BitCountry? country) => country?.Iso2 switch
    {
        "US" or "CA" => "(###) ###-####",
        "FR" => "# ## ## ## ##",
        "DE" => "### ########",
        _ => null
    };



    private readonly string example1RazorCode = @"
<BitPhoneInput DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Enter your number"" />";

    private readonly string example2RazorCode = @"
<BitPhoneInput AutoFocus DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Enter your number"" />";

    private readonly string example3RazorCode = @"
<BitPhoneInput Label=""Phone number""
               DefaultCountry=""BitCountries.UnitedKingdom""
               Placeholder=""Enter your number"" />

<BitPhoneInput Label=""Mobile number"" Required
               DefaultCountry=""BitCountries.UnitedKingdom""
               Placeholder=""Enter your number"" />

<BitPhoneInput Label=""Mobile number""
               Description=""We will text you a code to confirm it.""
               DefaultCountry=""BitCountries.UnitedKingdom""
               Placeholder=""Enter your number"" />";

    private readonly string example4RazorCode = @"
<BitPhoneInput Countries=""customCountries""
               DefaultCountry=""BitCountries.Canada""
               Placeholder=""Enter your number"" />

<BitPhoneInput ExcludeCountries=""excludedCountries""
               DefaultCountry=""BitCountries.Canada""
               Placeholder=""Enter your number"" />";
    private readonly string example4CsharpCode = @"
private readonly List<BitCountry> customCountries =
[
    BitCountries.UnitedStates,
    BitCountries.Canada,
    BitCountries.UnitedKingdom,
    BitCountries.Germany,
    BitCountries.France,
];

private readonly List<BitCountry> excludedCountries =
[
    BitCountries.UnitedStates,
    BitCountries.UnitedKingdom,
    BitCountries.Germany,
];";

    private readonly string example5RazorCode = @"
<BitPhoneInput PreferredCountries=""preferredCountries""
               DefaultCountry=""BitCountries.Germany""
               Placeholder=""Enter your number"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitCountry> preferredCountries =
[
    BitCountries.Germany,
    BitCountries.France,
    BitCountries.UnitedStates,
];";

    private readonly string example6RazorCode = @"
<BitPhoneInput DefaultCountry=""BitCountries.France""
               Placeholder=""Enter your number""
               SearchBoxPlaceholder=""Country or code""
               NoResultsMessage=""No matching country"" />

<BitPhoneInput NoSearchBox DefaultCountry=""BitCountries.France"" Placeholder=""Enter your number"" />";

    private readonly string example7RazorCode = @"
<BitPhoneInput NoFlags DefaultCountry=""BitCountries.Japan"" Placeholder=""Enter your number"" />

<BitPhoneInput NoDialCode DefaultCountry=""BitCountries.Japan"" Placeholder=""Enter your number"" />

<BitPhoneInput DropdownPlaceholder=""Country"" Placeholder=""Enter your number"" />

<BitPhoneInput FlagUrlSelector=""GetFlagUrl"" DefaultCountry=""BitCountries.Switzerland"" Placeholder=""Enter your number"" />

<BitPhoneInput NoDropdown DefaultCountry=""BitCountries.Japan"" Placeholder=""Enter your number"" />";
    private readonly string example7CsharpCode = @"
// Any url will do: this one asks a public flag CDN for a 24px png of the country.
private static string? GetFlagUrl(BitCountry country)
    => $""https://flagcdn.com/24x18/{country.Iso2.ToLowerInvariant()}.png"";";

    private readonly string example8RazorCode = @"
<BitPhoneInput ShowClearButton
               DefaultCountry=""BitCountries.Italy""
               Placeholder=""Enter your number""
               OnClear=""() => clearedAt = DateTime.Now.ToLongTimeString()"" />

<div>Last cleared at: @clearedAt</div>";
    private readonly string example8CsharpCode = @"
private string? clearedAt;";

    private readonly string example9RazorCode = @"
<BitPhoneInput Strict DefaultCountry=""BitCountries.Netherlands"" Placeholder=""Digits only"" />

<BitPhoneInput MaxLength=""10"" DefaultCountry=""BitCountries.Netherlands"" Placeholder=""At most 10 characters"" />";

    private readonly string example10RazorCode = @"
<BitPhoneInput NoBorder DefaultCountry=""BitCountries.Sweden"" Placeholder=""NoBorder"" />

<BitPhoneInput Underlined DefaultCountry=""BitCountries.Sweden"" Placeholder=""Underlined"" />

<BitPhoneInput FullWidth DefaultCountry=""BitCountries.Sweden"" Placeholder=""FullWidth"" />";

    private readonly string example11RazorCode = @"
<BitPhoneInput Immediate
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""immediateNumber"" />
<div>Value: [@immediateNumber]</div>

<BitPhoneInput Immediate
               DebounceTime=""500""
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""debouncedNumber"" />
<div>Value: [@debouncedNumber]</div>

<BitPhoneInput Immediate
               ThrottleTime=""500""
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""throttledNumber"" />
<div>Value: [@throttledNumber]</div>";
    private readonly string example11CsharpCode = @"
private string? immediateNumber;
private string? debouncedNumber;
private string? throttledNumber;";

    private readonly string example12RazorCode = @"
<BitPhoneInput @bind-Value=""bindingValue""
               @bind-Number=""bindingNumber""
               @bind-Country=""bindingCountry""
               Immediate
               DebounceTime=""500""
               DefaultCountry=""BitCountries.Germany""
               Placeholder=""Enter your number"" />

<BitTextField @bind-Value=""bindingValue"" Immediate DebounceTime=""500"" Placeholder=""Full phone number"" />

<div>Value (full number): @bindingValue</div>
<div>Number (local): @bindingNumber</div>
<div>Country: @bindingCountry?.Name</div>";
    private readonly string example12CsharpCode = @"
private string? bindingValue;
private string? bindingNumber;
private BitCountry? bindingCountry = BitCountries.Germany;";

    private readonly string example13RazorCode = @"
<BitPhoneInput Mask=""(###) ###-####""
               Immediate
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""maskedValue"" />
<div>Value: [@maskedValue]</div>

<BitPhoneInput MaskSelector=""GetMask""
               Immediate
               DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               @bind-Value=""maskSelectorValue"" />
<div>Value: [@maskSelectorValue]</div>

<BitPhoneInput AutoPlaceholder
               MaskSelector=""GetMask""
               Immediate
               DefaultCountry=""BitCountries.UnitedStates"" />";
    private readonly string example13CsharpCode = @"
private string? maskedValue;
private string? maskSelectorValue;

private static string? GetMask(BitCountry? country) => country?.Iso2 switch
{
    ""US"" or ""CA"" => ""(###) ###-####"",
    ""FR"" => ""# ## ## ## ##"",
    ""DE"" => ""### ########"",
    _ => null
};";

    private readonly string example14RazorCode = @"
<BitPhoneInput Invalid Label=""Invalid"" DefaultCountry=""BitCountries.Spain"" Value=""+34123"" />

<BitPhoneInput Label=""Phone number""
               ErrorMessage=""That number is already registered.""
               Description=""Spanish mobile numbers are nine digits long.""
               DefaultCountry=""BitCountries.Spain""
               Value=""+34600123456"" />

<EditForm Model=""validationModel"" OnValidSubmit=""() => validationSubmitted = true"" novalidate>
    <DataAnnotationsValidator />
    <BitPhoneInput Label=""Phone number""
                   Required
                   Immediate
                   DefaultCountry=""BitCountries.Spain""
                   Placeholder=""Enter your number""
                   @bind-Value=""validationModel.Phone"" />
    <ValidationMessage For=""() => validationModel.Phone"" />
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>

@if (validationSubmitted)
{
    <div>Submitted: @validationModel.Phone</div>
}";
    private readonly string example14CsharpCode = @"
public class BitPhoneInputValidationModel
{
    [Required(ErrorMessage = ""Enter a phone number."")]
    [RegularExpression(@""^\+[0-9]{8,15}$"", ErrorMessage = ""Enter a valid international phone number."")]
    public string? Phone { get; set; }
}

private bool validationSubmitted;
private readonly BitPhoneInputValidationModel validationModel = new();";

    private readonly string example15RazorCode = @"
<BitPhoneInput IsEnabled=""false"" DefaultCountry=""BitCountries.Spain"" Value=""+341234567"" />

<BitPhoneInput ReadOnly DefaultCountry=""BitCountries.Spain"" Value=""+341234567"" />";

    private readonly string example16RazorCode = @"
<BitPhoneInput MaxHeight=""200""
               DropDirection=""BitDropDirection.All""
               DefaultCountry=""BitCountries.Norway""
               Placeholder=""Enter your number"" />

<BitPhoneInput NoWrapNavigation
               NoSearchBox
               DefaultCountry=""BitCountries.Norway""
               Placeholder=""Enter your number"" />

<BitPhoneInput Responsive
               Label=""Responsive""
               DefaultCountry=""BitCountries.Brazil""
               Placeholder=""Enter your number"" />";

    private readonly string example17RazorCode = @"
<BitPhoneInput Label=""Phone number""
               DefaultCountry=""BitCountries.Portugal""
               Placeholder=""Enter your number"">
    <DropdownTemplate Context=""country"">
        <span>@(country?.Iso2 ?? ""??"")</span>
    </DropdownTemplate>
    <ItemTemplate Context=""country"">
        <span style=""flex:1 1 0;overflow:hidden;text-overflow:ellipsis"">@country.Name</span>
        <b>@country.Iso3</b>
    </ItemTemplate>
    <NoResultsTemplate>
        <span>🔍 nothing here</span>
    </NoResultsTemplate>
</BitPhoneInput>";

    private readonly string example18RazorCode = @"
<BitPhoneInput DefaultCountry=""BitCountries.UnitedStates""
               Placeholder=""Enter your number""
               OnCountryChange=""c => changedCountry = c""
               OnChange=""v => changedValue = v""
               OnSearch=""s => searchedText = s""
               OnOpen='() => calloutState = ""opened""'
               OnClose='() => calloutState = ""closed""'
               OnEnter=""() => enterPressed++"" />

<div>Selected country: @changedCountry?.Name</div>
<div>Value: @changedValue</div>
<div>Search text: @searchedText</div>
<div>Callout: @calloutState</div>
<div>Enter pressed: @enterPressed</div>";
    private readonly string example18CsharpCode = @"
private BitCountry? changedCountry;
private string? changedValue;
private string? searchedText;
private string calloutState = ""closed"";
private int enterPressed;";

    private readonly string example19RazorCode = @"
<BitPhoneInput @ref=""controlledInput""
               @bind-IsOpen=""isCalloutOpen""
               DefaultCountry=""BitCountries.Australia""
               Placeholder=""Enter your number"" />

<BitStack Horizontal Wrap>
    <BitButton OnClick=""() => isCalloutOpen = !isCalloutOpen"">Toggle the list</BitButton>
    <BitButton OnClick=""() => controlledInput.SelectCountryAsync(BitCountries.Japan)"">Select Japan</BitButton>
    <BitButton OnClick='() => controlledInput.SetNumberAsync(""+81 3 1234 5678"")'>Set a Japanese number</BitButton>
    <BitButton OnClick=""() => controlledInput.ClearAsync()"">Clear</BitButton>
    <BitButton OnClick=""async () => await controlledInput.FocusAsync()"">Focus</BitButton>
</BitStack>

<div>IsOpen: @isCalloutOpen</div>";
    private readonly string example19CsharpCode = @"
private bool isCalloutOpen;
private BitPhoneInput controlledInput = default!;";

    private readonly string example20RazorCode = @"
<BitPhoneInput Name=""phone""
               CountryName=""phoneCountry""
               Label=""Phone number""
               Immediate
               DefaultCountry=""BitCountries.Canada""
               Placeholder=""Enter your number""
               @bind-Value=""submissionValue""
               @bind-Country=""submissionCountry"" />

<div>Posted as phone: @submissionValue</div>
<div>Posted as phoneCountry: @submissionCountry?.Iso2</div>";
    private readonly string example20CsharpCode = @"
private string? submissionValue;

// Country is bound here, so it is the field that carries the initial selection: a bound
// Country is the one the component reads, and DefaultCountry only fills in for a missing one.
private BitCountry? submissionCountry = BitCountries.Canada;";

    private readonly string example21RazorCode = @"
<BitPhoneInput Label=""Mobile number""
               AutoComplete=""tel""
               EnterKeyHint=""send""
               InputMode=""BitInputMode.Tel""
               DefaultCountry=""BitCountries.Ireland""
               Placeholder=""Enter your number"" />";

    private readonly string example22RazorCode = @"
<BitPhoneInput Label=""Telefonnummer""
               Countries=""germanCountries""
               DefaultCountry=""germanCountries[0]""
               Placeholder=""Nummer eingeben""
               DropdownAriaLabel=""Land""
               SearchBoxPlaceholder=""Suchen""
               SearchBoxAriaLabel=""Land suchen""
               NoResultsMessage=""Kein Land gefunden""
               SearchResultsAnnouncement=""@(count => count == 1 ? ""1 Land gefunden"" : $""{count} Länder gefunden"")""
               ShowClearButton
               ClearButtonAriaLabel=""Nummer löschen"" />";
    private readonly string example22CsharpCode = @"
private readonly List<BitCountry> germanCountries =
[
    new(""Deutschland"", ""49"", ""DE"", ""DEU""),
    new(""Österreich"", ""43"", ""AT"", ""AUT""),
    new(""Schweiz"", ""41"", ""CH"", ""CHE""),
    new(""Frankreich"", ""33"", ""FR"", ""FRA""),
    new(""Italien"", ""39"", ""IT"", ""ITA""),
];";

    private readonly string example23RazorCode = @"
<BitParams Parameters=""@phoneInputParams"">
    <BitPhoneInput Label=""Takes the market, the list order and the clear button"" />
    <BitPhoneInput Label=""So does this one"" />
    <BitPhoneInput Label=""Its own DefaultCountry, the cascaded rest"" DefaultCountry=""BitCountries.France"" />
</BitParams>

<BitPhoneInput Label=""Outside the cascade"" />";
    private readonly string example23CsharpCode = @"
private readonly BitPhoneInputParams[] phoneInputParams =
[
    new()
    {
        DefaultCountry = BitCountries.Germany,
        PreferredCountries = [BitCountries.Germany, BitCountries.Austria, BitCountries.Switzerland],
        ShowClearButton = true,
        Placeholder = ""Enter your number"",
    }
];";

    private readonly string example24RazorCode = @"
<BitPhoneInput Color=""BitColor.Primary"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Primary"" />
<BitPhoneInput Color=""BitColor.Secondary"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Secondary"" />
<BitPhoneInput Color=""BitColor.Tertiary"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Tertiary"" />
<BitPhoneInput Color=""BitColor.Info"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Info"" />
<BitPhoneInput Color=""BitColor.Success"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Success"" />
<BitPhoneInput Color=""BitColor.Warning"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Warning"" />
<BitPhoneInput Color=""BitColor.SevereWarning"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""SevereWarning"" />
<BitPhoneInput Color=""BitColor.Error"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Error"" />";

    private readonly string example25RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitPhoneInput ShowClearButton
               ClearButtonIcon=""@BitIconInfo.Css(""fa-solid fa-xmark"")""
               DefaultCountry=""BitCountries.Mexico""
               Placeholder=""Enter your number"" />";

    private readonly string example26RazorCode = @"
<BitPhoneInput Size=""BitSize.Small"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Small"" />
<BitPhoneInput Size=""BitSize.Medium"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Medium"" />
<BitPhoneInput Size=""BitSize.Large"" DefaultCountry=""BitCountries.UnitedStates"" Placeholder=""Large"" />";

    private readonly string example27RazorCode = @"
<BitPhoneInput DefaultCountry=""BitCountries.Netherlands""
               Placeholder=""Enter your number""
               Style=""--bit-PhoneInput-radius: 1.5rem;
                      --bit-PhoneInput-focus-color: #7c4dff;
                      --bit-PhoneInput-item-selected-background: #ede7f6;
                      --bit-PhoneInput-item-selected-color: #311b92;
                      --bit-PhoneInput-callout-radius: 1rem;"" />

<div class=""phi-brand-scope"">
    <BitPhoneInput DefaultCountry=""BitCountries.Netherlands"" Placeholder=""Enter your number"" />
</div>

<BitPhoneInput DefaultCountry=""BitCountries.Netherlands""
               Placeholder=""Enter your number""
               Style=""width: 300px;""
               Class=""custom-class""
               Styles=""@(new() { FieldGroup = ""border-color: blueviolet;"", ItemCode = ""color: blueviolet;"" })""
               Classes=""@(new() { Input = ""custom-input"" })"" />";
    private const string example27ScssCode = @"
// The public custom properties are inherited, so one scope re-skins every field inside it
// without a rule naming a part of the component.
.phi-brand-scope {
    --bit-PhoneInput-radius: 0;
    --bit-PhoneInput-border-width: 2px;
    --bit-PhoneInput-border-color: #00796b;
    --bit-PhoneInput-hover-border-color: #26a69a;
    --bit-PhoneInput-focus-color: #00796b;
    --bit-PhoneInput-dropdown-background: #e0f2f1;
    --bit-PhoneInput-dropdown-color: #004d40;
    --bit-PhoneInput-separator-color: #00796b;
}

::deep .custom-class {
    border-radius: 1rem;
    background: linear-gradient(90deg, rgba(138, 43, 226, 0.08), transparent);
}

::deep .custom-input {
    color: blueviolet;
    font-weight: bold;
}";
    private readonly DemoCodeFile[] example27CodeFiles =
    [
        new("BitPhoneInputDemo.razor.scss", example27ScssCode),
    ];

    private readonly string example28RazorCode = @"
<div dir=""rtl"">
    <BitPhoneInput Dir=""BitDir.Rtl"" DefaultCountry=""BitCountries.Iran"" Placeholder=""شماره خود را وارد کنید"" />
</div>";
}
