namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CountryPicker;

public partial class BitCountryPickerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the fill of the country picker. An explicit value wins over --bit-CountryPicker-background; left unset, the fill is the primary kind unless it says otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the frame of the country picker, winning over the main color of Color (which keeps the focus ring). An explicit value wins over --bit-CountryPicker-border-color and --bit-CountryPicker-hover-border-color; left unset, the frame is the primary border unless an explicit Color or those variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCountryPickerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitCountryPicker.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the country picker: the frame takes its main color and the focus ring its focus color. An explicit value wins over --bit-CountryPicker-focus-color and the --bit-CountryPicker-* border colors (an explicit Border still wins the frame); left unset, the field is primary unless those variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Countries",
            Type = "ICollection<BitCountry>",
            DefaultValue = "BitCountries.All",
            Description = "The list of the countries the picker offers.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "DefaultCountry",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The country an unbound picker starts on when no DefaultValue is given - the market of an app, which a BitCountryPickerParams can hand every picker under it. A bound Value is left as it is.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The description shown under the country picker, tied to the field through aria-describedby.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the description of the country picker.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the country list callout.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum",
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The error message shown under the country picker, which also puts the field in its invalid state. It is referenced through aria-describedby and announced when it appears.",
        },
        new()
        {
            Name = "ErrorMessageTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the error message of the country picker, putting the field in the same invalid state.",
        },
        new()
        {
            Name = "ExcludeCountries",
            Type = "ICollection<BitCountry>?",
            DefaultValue = "null",
            Description = "The countries to leave out of the list, which is how a list built from Countries is trimmed without being written out in full.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "FlagUrlSelector",
            Type = "Func<BitCountry, string?>?",
            DefaultValue = "null",
            Description = "The url of the flag image of a country, replacing the flags that ship with the library. Returning null or an empty string leaves that country with the built-in flag.",
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the country picker in an invalid state without going through the validation of an EditContext.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the country list callout. (two-way bound)",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitCountry>?",
            DefaultValue = "null",
            Description = "The custom template for each country of the list.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label of the country picker shown above the field, which is also the accessible name of the field and of the country list.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the label of the country picker.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum height of the country list callout in pixels.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border of the country picker.",
        },
        new()
        {
            Name = "NoFlags",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the flag images of the countries in the field and in the list.",
        },
        new()
        {
            Name = "NoResultsMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message to show, and to announce, when the search result of the country list is empty.",
        },
        new()
        {
            Name = "NoResultsTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to show when the search result of the country list is empty.",
        },
        new()
        {
            Name = "NoSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the search box of the country list. The field then answers to type-ahead instead, open or closed.",
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
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            Description = "The callback that is invoked when the field loses focus.",
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            Description = "The callback that is invoked when the country list callout closes.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "The callback that is invoked when the field receives focus.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "The callback that is invoked when the country list callout opens.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            Description = "The callback that is invoked when the search text of the country list changes.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text shown in the field while no country is selected.",
        },
        new()
        {
            Name = "PreferredCountries",
            Type = "ICollection<BitCountry>?",
            DefaultValue = "null",
            Description = "The countries to pin to the top of the list, set apart from the rest of it by a rule.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the country list as a full height panel on small screens instead of an inline callout.",
        },
        new()
        {
            Name = "ResponsiveCloseButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the close button of the responsive panel of the country list.",
        },
        new()
        {
            Name = "ResponsiveCloseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the close button of the responsive panel. Takes precedence over ResponsiveCloseIconName when both are set, and renders icons from external libraries like FontAwesome or Bootstrap Icons.",
        },
        new()
        {
            Name = "ResponsiveCloseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the close button of the responsive panel from the Fluent UI icon set.",
        },
        new()
        {
            Name = "SearchBoxAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the search box of the country list. Falls back to the search box placeholder, then to a default English value.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the search box of the country list.",
        },
        new()
        {
            Name = "SearchResultsAnnouncement",
            Type = "Func<int, string?>?",
            DefaultValue = "null",
            Description = "What a screen reader announces for the number of countries a search term leaves in the list, in place of the default \"1 country found\" / \"{count} countries found\". Returning null or an empty string keeps a count from being announced.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the country picker. An explicit value wins over the --bit-CountryPicker-* size variables (minimum height, font sizes, padding, flag size, row height); left unset, the field is medium unless those variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCountryPickerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitCountryPicker.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip (title attribute) of the country picker.",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the country picker with only a bottom border instead of a full one.",
        },
        new()
        {
            Name = "ValueTemplate",
            Type = "RenderFragment<BitCountry?>?",
            DefaultValue = "null",
            Description = "The custom template for the content of the field, receiving the selected country (null while none is).",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCountryPickerClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitCountryPicker." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the BitCountryPicker." },
                new() { Name = "Field", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the field (the button that opens the country list) of the BitCountryPicker." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content of the field (the flag and the name of the selected country, or the placeholder) of the BitCountryPicker." },
                new() { Name = "Flag", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the flag image of the countries of the BitCountryPicker." },
                new() { Name = "Caret", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the caret down element of the field of the BitCountryPicker." },
                new() { Name = "ErrorMessageContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the error message of the BitCountryPicker." },
                new() { Name = "ErrorMessage", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error message of the BitCountryPicker." },
                new() { Name = "DescriptionContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the description of the BitCountryPicker." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description of the BitCountryPicker." },
                new() { Name = "Overlay", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the overlay of the country list of the BitCountryPicker." },
                new() { Name = "Callout", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the callout of the country list of the BitCountryPicker." },
                new() { Name = "SearchBox", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the search box of the country list of the BitCountryPicker." },
                new() { Name = "ScrollContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the scroll container of the country list of the BitCountryPicker." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list (the listbox element) of the BitCountryPicker." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each item of the country list of the BitCountryPicker." },
                new() { Name = "ItemName", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the name of each item of the country list of the BitCountryPicker." },
                new() { Name = "NoResults", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the no results message of the country list of the BitCountryPicker." },
                new() { Name = "ResponsiveLabelContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label container of the responsive panel of the country list of the BitCountryPicker." },
                new() { Name = "ResponsiveLabel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the responsive panel of the country list of the BitCountryPicker." },
                new() { Name = "ResponsiveCloseButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the close button of the responsive panel of the country list of the BitCountryPicker." },
                new() { Name = "ResponsiveCloseIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the close button of the responsive panel of the country list of the BitCountryPicker." },
            ]
        },
        new()
        {
            Id = "country",
            Title = "BitCountry",
            Description = "The basic information of a country. BitCountries holds one shared instance per country.",
            Parameters =
            [
                new() { Name = "Name", Type = "string", Description = "The full name of the country." },
                new() { Name = "Code", Type = "string", Description = "The dialing code of the country." },
                new() { Name = "Iso2", Type = "string", Description = "The ISO 3166-1 alpha-2 code of the country." },
                new() { Name = "Iso3", Type = "string", Description = "The ISO 3166-1 alpha-3 code of the country." },
                new() { Name = "Priority", Type = "int", DefaultValue = "0", Description = "The tie-breaking priority of the country among the ones that share its dialing code, where a higher number wins." },
                new() { Name = "ExtraCodes", Type = "string[]?", DefaultValue = "null", Description = "The other dialing codes the country answers to beyond Code." },
                new() { Name = "Emoji", Type = "string?", Description = "The flag of the country as a Unicode emoji, built from its Iso2." },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitColorKind(),
        DemoSharedEnums.BitDropDirection(),
        DemoSharedEnums.BitSize(),
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "OpenAsync",
            Type = "Task",
            Description = "Opens the country list. It does nothing while the field is disabled or read-only.",
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
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-CountryPicker-background", DefaultValue = "--bit-clr-bg-pri", Description = "Fill of the field. The Background parameter wins over it." },
        new() { Name = "--bit-CountryPicker-color", DefaultValue = "--bit-clr-fg-pri", Description = "Name of the selected country in the field." },
        new() { Name = "--bit-CountryPicker-placeholder-color", DefaultValue = "--bit-clr-fg-sec", Description = "Placeholder of the field and of the search box." },
        new() { Name = "--bit-CountryPicker-border-color", DefaultValue = "--bit-clr-brd-pri", Description = "Frame at rest. The Border and Color parameters win over it." },
        new() { Name = "--bit-CountryPicker-hover-border-color", DefaultValue = "--bit-clr-brd-pri-hover", Description = "Frame under a pointer, on devices that can hover. The Border and Color parameters win over it." },
        new() { Name = "--bit-CountryPicker-border-width", DefaultValue = "--bit-shp-brd-width", Description = "Thickness of the frame." },
        new() { Name = "--bit-CountryPicker-radius", DefaultValue = "--bit-shp-radius-control", Description = "Corner radius of the frame." },
        new() { Name = "--bit-CountryPicker-min-height", DefaultValue = "--bit-siz-ctrl-md", Description = "Smallest height of the frame. The Size parameter wins over it." },
        new() { Name = "--bit-CountryPicker-font-size", DefaultValue = "--bit-tpg-fs-sm", Description = "Text size of the field and of the country list. The Size parameter wins over it." },
        new() { Name = "--bit-CountryPicker-padding", DefaultValue = "0 --bit-siz-ctrl-pad-x-md", Description = "Padding of the field. The Size parameter wins over it." },
        new() { Name = "--bit-CountryPicker-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Focused frame and focus ring, and the keyboard cues of the active row and the close button of the list. The Color parameter wins over it. While it is unset, the focus ring is the library's own --bit-shd-focus-ring, so replacing that token re-shapes it too." },
        new() { Name = "--bit-CountryPicker-invalid-color", DefaultValue = "--bit-clr-err / --bit-clr-err-focus", Description = "Frame, focus ring and error message of a rejected value." },
        new() { Name = "--bit-CountryPicker-disabled-color", DefaultValue = "--bit-clr-fg-dis", Description = "Text and glyphs when disabled." },
        new() { Name = "--bit-CountryPicker-disabled-background", DefaultValue = "--bit-clr-bg-dis", Description = "Fill when disabled." },
        new() { Name = "--bit-CountryPicker-disabled-border-color", DefaultValue = "--bit-clr-brd-dis", Description = "Frame when disabled." },
        new() { Name = "--bit-CountryPicker-caret-color", DefaultValue = "--bit-clr-fg-sec", Description = "The caret of the field." },
        new() { Name = "--bit-CountryPicker-flag-size", DefaultValue = "--bit-siz-icon-md", Description = "Flags of the field and of the list. The Size parameter wins over it." },
        new() { Name = "--bit-CountryPicker-flag-radius", DefaultValue = "--bit-shp-radius-none", Description = "Corner radius of a flag." },
        new() { Name = "--bit-CountryPicker-label-color", DefaultValue = "--bit-clr-fg-pri", Description = "Label text." },
        new() { Name = "--bit-CountryPicker-label-font-size", DefaultValue = "--bit-CountryPicker-font-size", Description = "Label text size. The Size parameter wins over it." },
        new() { Name = "--bit-CountryPicker-label-font-weight", DefaultValue = "--bit-tpg-field-label-font-weight", Description = "Label weight." },
        new() { Name = "--bit-CountryPicker-required-color", DefaultValue = "--bit-clr-req", Description = "The asterisk of a Required field." },
        new() { Name = "--bit-CountryPicker-description-color", DefaultValue = "--bit-clr-fg-sec", Description = "Description text." },
        new() { Name = "--bit-CountryPicker-footer-font-size", DefaultValue = "--bit-tpg-fs-2xs", Description = "Text size of the description and of the error message." },
        new() { Name = "--bit-CountryPicker-callout-background", DefaultValue = "--bit-clr-bg-pri", Description = "The surface of the country list." },
        new() { Name = "--bit-CountryPicker-callout-border-color", DefaultValue = "--bit-clr-brd-pri", Description = "Border of the country list, and the rules under its search box and under the preferred countries." },
        new() { Name = "--bit-CountryPicker-callout-radius", DefaultValue = "--bit-shp-radius-popup", Description = "Corner radius of the country list." },
        new() { Name = "--bit-CountryPicker-callout-shadow", DefaultValue = "--bit-shd-popup (--bit-shd-sheet for the responsive panel)", Description = "Elevation of the country list." },
        new() { Name = "--bit-CountryPicker-callout-max-height", DefaultValue = "--bit-siz-popup-max-height", Description = "Tallest the scrolling list grows. The MaxHeight parameter wins over it." },
        new() { Name = "--bit-CountryPicker-responsive-width", DefaultValue = "spacing(34)", Description = "Width of the side panel a Responsive picker opens on a small screen." },
        new() { Name = "--bit-CountryPicker-overlay-background", DefaultValue = "transparent", Description = "The layer behind an open list; give it a color for a modal-style scrim." },
        new() { Name = "--bit-CountryPicker-item-height", DefaultValue = "--bit-siz-item-md", Description = "Smallest height of a row of the list. The Size parameter wins over it." },
        new() { Name = "--bit-CountryPicker-item-color", DefaultValue = "--bit-clr-fg-pri", Description = "Country name of a row, and the text of the search box." },
        new() { Name = "--bit-CountryPicker-item-hover-background", DefaultValue = "--bit-clr-bg-pri-hover", Description = "Row under the pointer, and the row the arrow keys are on." },
        new() { Name = "--bit-CountryPicker-item-selected-background", DefaultValue = "--bit-clr-bg-sec", Description = "The selected row." },
        new() { Name = "--bit-CountryPicker-item-selected-color", DefaultValue = "--bit-CountryPicker-item-color", Description = "Text of the selected row." },
    ];



    private BitCountry? boundCountry;
    private BitCountry? changedCountry;
    private BitCountry? eventCountry;
    private string? searchedText;
    private string calloutState = "closed";
    private string focusState = "blurred";
    private bool isCalloutOpen;
    private bool validationSubmitted;
    private BitCountry? submissionCountry;
    private BitCountryPicker controlledPicker = default!;
    private readonly BitCountryPickerValidationModel validationModel = new();
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
    ];
    private readonly List<BitCountry> preferredCountries =
    [
        BitCountries.Germany,
        BitCountries.France,
        BitCountries.UnitedStates,
    ];
    private readonly List<BitCountry> germanCountries =
    [
        new("Deutschland", "49", "DE", "DEU"),
        new("Frankreich", "33", "FR", "FRA"),
        new("Italien", "39", "IT", "ITA"),
        new("Österreich", "43", "AT", "AUT"),
        new("Schweiz", "41", "CH", "CHE"),
    ];
    private readonly List<BitCountry> persianCountries =
    [
        new("ایران", "98", "IR", "IRN"),
        new("افغانستان", "93", "AF", "AFG"),
        new("ترکیه", "90", "TR", "TUR"),
        new("عراق", "964", "IQ", "IRQ"),
        new("عمان", "968", "OM", "OMN"),
    ];
    private readonly BitCountryPickerParams[] countryPickerParams =
    [
        new()
        {
            DefaultCountry = BitCountries.Germany,
            PreferredCountries = [BitCountries.Germany, BitCountries.Austria, BitCountries.Switzerland],
            Placeholder = "Select a country",
        }
    ];



    // Any url will do: this one asks a public flag CDN for a 24px png of the country.
    private static string? GetFlagUrl(BitCountry country) => $"https://flagcdn.com/24x18/{country.Iso2.ToLowerInvariant()}.png";
}
