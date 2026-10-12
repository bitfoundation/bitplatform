namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.TimeZonePicker;

public partial class BitTimeZonePickerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the fill of the time zone picker. An explicit value wins over --bit-TimeZonePicker-background; left unset, the fill is the primary kind unless it says otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the frame of the time zone picker, winning over the main color of Color (which keeps the focus ring). An explicit value wins over --bit-TimeZonePicker-border-color and --bit-TimeZonePicker-hover-border-color; left unset, the frame is the primary border unless an explicit Color or those variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Classes",
            Type = "BitTimeZonePickerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitTimeZonePicker.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the time zone picker: the frame takes its main color and the focus ring its focus color. An explicit value wins over --bit-TimeZonePicker-focus-color and the --bit-TimeZonePicker-* border colors (an explicit Border still wins the frame); left unset, the field is primary unless those variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The description shown under the time zone picker, tied to the field through aria-describedby.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the description of the time zone picker.",
        },
        new()
        {
            Name = "DetectTimeZone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Preselects the time zone of the user's device when the picker has no value: the one the browser runs in, which on Blazor Server is the visitor's own rather than the server's TimeZoneInfo.Local. It is written into a bound value the way a pick in the list would be; a value the page has already set is left as it is, and so is a disabled or read-only field.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the time zone list callout.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum",
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The error message shown under the time zone picker, which also puts the field in its invalid state. It is referenced through aria-describedby and announced when it appears.",
        },
        new()
        {
            Name = "ErrorMessageTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the error message of the time zone picker, putting the field in the same invalid state.",
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the time zone picker in an invalid state without going through the validation of an EditContext.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the time zone list callout. (two-way bound)",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitTimeZone>?",
            DefaultValue = "null",
            Description = "The custom template for each time zone of the list. The offset the list shows for it is GetUtcOffset at the ReferenceDate, written by BitTimeZones.FormatUtcOffset.",
            LinkType = LinkType.Link,
            Href = "#time-zone",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label of the time zone picker shown above the field, which is also the accessible name of the field and of the time zone list.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the label of the time zone picker.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum height of the time zone list callout in pixels.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border of the time zone picker.",
        },
        new()
        {
            Name = "NoCountry",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the names of the countries the time zones are in, which the list otherwise shows beside each place.",
        },
        new()
        {
            Name = "NoOffset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the offsets from UTC of the time zones in the field and in the list.",
        },
        new()
        {
            Name = "NoResultsMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message to show, and to announce, when the search result of the time zone list is empty.",
        },
        new()
        {
            Name = "NoResultsTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to show when the search result of the time zone list is empty.",
        },
        new()
        {
            Name = "NoSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the search box of the time zone list. The field then answers to type-ahead instead, open or closed.",
        },
        new()
        {
            Name = "NoWrapNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the keyboard navigation of the time zone list from wrapping around at its two ends.",
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
            Description = "The callback that is invoked when the time zone list callout closes.",
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
            Description = "The callback that is invoked when the time zone list callout opens.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            Description = "The callback that is invoked when the search text of the time zone list changes.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text shown in the field while no time zone is selected.",
        },
        new()
        {
            Name = "PreferredTimeZones",
            Type = "ICollection<string>?",
            DefaultValue = "null",
            Description = "The time zones to pin to the top of the list, set apart from the rest of it by a rule, each written as any id BitTimeZones.Find resolves: an IANA id, an older IANA id or a Windows id.",
        },
        new()
        {
            Name = "ReferenceDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The moment the offsets of the time zones are computed for - and the list ordered by - which is now unless it is given, so a picker for an event in the future shows the offsets that will hold then.",
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the time zone list as a full height panel on small screens instead of an inline callout.",
        },
        new()
        {
            Name = "ResponsiveCloseButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the close button of the responsive panel of the time zone list.",
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
            Description = "The aria-label of the search box of the time zone list. Falls back to the search box placeholder, then to a default English value.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the search box of the time zone list.",
        },
        new()
        {
            Name = "SearchResultsAnnouncement",
            Type = "Func<int, string?>?",
            DefaultValue = "null",
            Description = "What a screen reader announces for the number of time zones a search term leaves in the list, in place of the default \"1 time zone found\" / \"{count} time zones found\". Returning null or an empty string keeps a count from being announced.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the time zone picker. An explicit value wins over the --bit-TimeZonePicker-* size variables (minimum height, font sizes, padding, row height); left unset, the field is medium unless those variables say otherwise.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTimeZonePickerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitTimeZonePicker.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "TimeZones",
            Type = "ICollection<BitTimeZone>",
            DefaultValue = "BitTimeZones.All",
            Description = "The list of the time zones the picker offers. A list of translated BitTimeZone instances is how the names are localized, and a filtered one how the choice is narrowed down. Whatever its order, the picker shows them by their offsets from UTC.",
            LinkType = LinkType.Link,
            Href = "#time-zone",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip (title attribute) of the time zone picker.",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the time zone picker with only a bottom border instead of a full one.",
        },
        new()
        {
            Name = "ValueTemplate",
            Type = "RenderFragment<BitTimeZone?>?",
            DefaultValue = "null",
            Description = "The custom template for the content of the field, receiving the selected time zone (null while none is).",
            LinkType = LinkType.Link,
            Href = "#time-zone",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitTimeZonePickerClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitTimeZonePicker." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the BitTimeZonePicker." },
                new() { Name = "Field", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the field (the button that opens the time zone list) of the BitTimeZonePicker." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content of the field (the offset and the name of the selected time zone, or the placeholder) of the BitTimeZonePicker." },
                new() { Name = "Offset", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the offset from UTC of the time zones in the field and in the list of the BitTimeZonePicker." },
                new() { Name = "Caret", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the caret down element of the field of the BitTimeZonePicker." },
                new() { Name = "ErrorMessageContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the error message of the BitTimeZonePicker." },
                new() { Name = "ErrorMessage", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error message of the BitTimeZonePicker." },
                new() { Name = "DescriptionContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the container of the description of the BitTimeZonePicker." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description of the BitTimeZonePicker." },
                new() { Name = "Overlay", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the overlay of the time zone list of the BitTimeZonePicker." },
                new() { Name = "Callout", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the callout of the time zone list of the BitTimeZonePicker." },
                new() { Name = "SearchBox", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the search box of the time zone list of the BitTimeZonePicker." },
                new() { Name = "ScrollContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the scroll container of the time zone list of the BitTimeZonePicker." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list (the listbox element) of the BitTimeZonePicker." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each item of the time zone list of the BitTimeZonePicker." },
                new() { Name = "ItemName", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the name of each item of the time zone list of the BitTimeZonePicker." },
                new() { Name = "ItemCountry", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the country name of each item of the time zone list of the BitTimeZonePicker." },
                new() { Name = "NoResults", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the no results message of the time zone list of the BitTimeZonePicker." },
                new() { Name = "ResponsiveLabelContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label container of the responsive panel of the time zone list of the BitTimeZonePicker." },
                new() { Name = "ResponsiveLabel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the responsive panel of the time zone list of the BitTimeZonePicker." },
                new() { Name = "ResponsiveCloseButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the close button of the responsive panel of the time zone list of the BitTimeZonePicker." },
                new() { Name = "ResponsiveCloseIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the close button of the responsive panel of the time zone list of the BitTimeZonePicker." },
            ]
        },
        new()
        {
            Id = "time-zone",
            Title = "BitTimeZone",
            Description = "A time zone of the IANA time zone database. BitTimeZones holds one shared instance per time zone in All, and resolves an IANA id, an older IANA id, a Windows id or a TimeZoneInfo back to it through Find.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", Description = "The canonical IANA id of the time zone (America/Los_Angeles)." },
                new() { Name = "Name", Type = "string", Description = "The name of the place the time zone is named after (Los Angeles), which the picker shows and searches." },
                new() { Name = "CountryCode", Type = "string?", DefaultValue = "null", Description = "The ISO 3166-1 alpha-2 code of the country the time zone is in, or null for UTC." },
                new() { Name = "CountryName", Type = "string?", DefaultValue = "null", Description = "The name of the country the time zone is in, shown beside the Name and searched as well. Unless it is given, it is the name BitCountries carries for the CountryCode." },
                new() { Name = "WindowsId", Type = "string?", DefaultValue = "null", Description = "The Windows id the time zone maps to (Pacific Standard Time), from the CLDR mapping, or null for one Windows has none for." },
                new() { Name = "TimeZoneInfo", Type = "TimeZoneInfo?", Description = "The TimeZoneInfo of the time zone on the running runtime, or null where the runtime knows nothing of it. It is looked up by the IANA id, then by the older ids, then by the WindowsId." },
                new() { Name = "GetUtcOffset(DateTimeOffset at)", Type = "TimeSpan?", Description = "The offset from UTC the time zone is at, at the given moment, daylight saving time included." },
            ]
        },
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
            Name = "SelectedTimeZone",
            Type = "BitTimeZone?",
            Description = "The time zone of the list the current value stands for, or null while there is none.",
            LinkType = LinkType.Link,
            Href = "#time-zone",
        },
        new()
        {
            Name = "OpenAsync",
            Type = "Task",
            Description = "Opens the time zone list. It does nothing while the field is disabled or read-only.",
        },
        new()
        {
            Name = "CloseAsync",
            Type = "Task",
            Description = "Closes the time zone list.",
        },
        new()
        {
            Name = "SelectTimeZoneAsync",
            Type = "Task",
            Description = "Selects the time zone an id (an IANA id, an older IANA id or a Windows id) or a TimeZoneInfo stands for, exactly as picking it in the list would, so the same events fire. One the list does not offer is not selected.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-TimeZonePicker-background", DefaultValue = "--bit-clr-bg-pri", Description = "Fill of the field. The Background parameter wins over it." },
        new() { Name = "--bit-TimeZonePicker-color", DefaultValue = "--bit-clr-fg-pri", Description = "Name of the selected time zone in the field." },
        new() { Name = "--bit-TimeZonePicker-placeholder-color", DefaultValue = "--bit-clr-fg-sec", Description = "Placeholder of the field and of the search box." },
        new() { Name = "--bit-TimeZonePicker-border-color", DefaultValue = "--bit-clr-brd-pri", Description = "Frame at rest. The Border and Color parameters win over it." },
        new() { Name = "--bit-TimeZonePicker-hover-border-color", DefaultValue = "--bit-clr-brd-pri-hover", Description = "Frame under a pointer, on devices that can hover. The Border and Color parameters win over it." },
        new() { Name = "--bit-TimeZonePicker-border-width", DefaultValue = "--bit-shp-brd-width", Description = "Thickness of the frame." },
        new() { Name = "--bit-TimeZonePicker-radius", DefaultValue = "--bit-shp-radius-control", Description = "Corner radius of the frame." },
        new() { Name = "--bit-TimeZonePicker-min-height", DefaultValue = "--bit-siz-ctrl-md", Description = "Smallest height of the frame. The Size parameter wins over it." },
        new() { Name = "--bit-TimeZonePicker-font-size", DefaultValue = "--bit-tpg-fs-sm", Description = "Text size of the field and of the time zone list. The Size parameter wins over it." },
        new() { Name = "--bit-TimeZonePicker-padding", DefaultValue = "0 --bit-siz-ctrl-pad-x-md", Description = "Padding of the field. The Size parameter wins over it." },
        new() { Name = "--bit-TimeZonePicker-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Focused frame and focus ring, and the keyboard cues of the active row and the close button of the list. The Color parameter wins over it. While it is unset, the focus ring is the library's own --bit-shd-focus-ring, so replacing that token re-shapes it too." },
        new() { Name = "--bit-TimeZonePicker-invalid-color", DefaultValue = "--bit-clr-err / --bit-clr-err-focus", Description = "Frame, focus ring and error message of a rejected value." },
        new() { Name = "--bit-TimeZonePicker-disabled-color", DefaultValue = "--bit-clr-fg-dis", Description = "Text and glyphs when disabled." },
        new() { Name = "--bit-TimeZonePicker-disabled-background", DefaultValue = "--bit-clr-bg-dis", Description = "Fill when disabled." },
        new() { Name = "--bit-TimeZonePicker-disabled-border-color", DefaultValue = "--bit-clr-brd-dis", Description = "Frame when disabled." },
        new() { Name = "--bit-TimeZonePicker-caret-color", DefaultValue = "--bit-clr-fg-sec", Description = "The caret of the field." },
        new() { Name = "--bit-TimeZonePicker-offset-color", DefaultValue = "--bit-clr-fg-sec", Description = "The offsets from UTC of the field and of the list." },
        new() { Name = "--bit-TimeZonePicker-label-color", DefaultValue = "--bit-clr-fg-pri", Description = "Label text." },
        new() { Name = "--bit-TimeZonePicker-label-font-size", DefaultValue = "--bit-TimeZonePicker-font-size", Description = "Label text size. The Size parameter wins over it." },
        new() { Name = "--bit-TimeZonePicker-label-font-weight", DefaultValue = "--bit-tpg-field-label-font-weight", Description = "Label weight." },
        new() { Name = "--bit-TimeZonePicker-required-color", DefaultValue = "--bit-clr-req", Description = "The asterisk of a Required field." },
        new() { Name = "--bit-TimeZonePicker-description-color", DefaultValue = "--bit-clr-fg-sec", Description = "Description text." },
        new() { Name = "--bit-TimeZonePicker-footer-font-size", DefaultValue = "--bit-tpg-fs-2xs", Description = "Text size of the description and of the error message." },
        new() { Name = "--bit-TimeZonePicker-callout-background", DefaultValue = "--bit-clr-bg-pri", Description = "The surface of the time zone list." },
        new() { Name = "--bit-TimeZonePicker-callout-border-color", DefaultValue = "--bit-clr-brd-pri", Description = "Border of the time zone list, and the rules under its search box and under the preferred time zones." },
        new() { Name = "--bit-TimeZonePicker-callout-radius", DefaultValue = "--bit-shp-radius-popup", Description = "Corner radius of the time zone list." },
        new() { Name = "--bit-TimeZonePicker-callout-shadow", DefaultValue = "--bit-shd-popup (--bit-shd-sheet for the responsive panel)", Description = "Elevation of the time zone list." },
        new() { Name = "--bit-TimeZonePicker-callout-max-height", DefaultValue = "--bit-siz-popup-max-height", Description = "Tallest the scrolling list grows. The MaxHeight parameter wins over it." },
        new() { Name = "--bit-TimeZonePicker-responsive-width", DefaultValue = "spacing(34)", Description = "Width of the side panel a Responsive picker opens on a small screen." },
        new() { Name = "--bit-TimeZonePicker-overlay-background", DefaultValue = "transparent", Description = "The layer behind an open list; give it a color for a modal-style scrim." },
        new() { Name = "--bit-TimeZonePicker-item-height", DefaultValue = "--bit-siz-item-md", Description = "Smallest height of a row of the list. The Size parameter wins over it." },
        new() { Name = "--bit-TimeZonePicker-item-color", DefaultValue = "--bit-clr-fg-pri", Description = "Place name of a row, and the text of the search box." },
        new() { Name = "--bit-TimeZonePicker-item-hover-background", DefaultValue = "--bit-clr-bg-pri-hover", Description = "Row under the pointer, and the row the arrow keys are on." },
        new() { Name = "--bit-TimeZonePicker-item-selected-background", DefaultValue = "--bit-clr-bg-sec", Description = "The selected row." },
        new() { Name = "--bit-TimeZonePicker-item-selected-color", DefaultValue = "--bit-TimeZonePicker-item-color", Description = "Text of the selected row." },
        new() { Name = "--bit-TimeZonePicker-item-country-color", DefaultValue = "--bit-clr-fg-sec", Description = "Country name of a row." },
    ];



    private string? boundTimeZoneId;
    private TimeZoneInfo? boundTimeZone;
    private string? changedTimeZoneId;
    private string? detectedTimeZoneId;
    private TimeZoneInfo? eventTimeZone;
    private string? searchedText;
    private string calloutState = "closed";
    private string focusState = "blurred";
    private bool isCalloutOpen;
    private bool validationSubmitted;
    private TimeZoneInfo? submissionTimeZone;
    private BitTimeZonePicker<string> controlledPicker = default!;
    private readonly BitTimeZonePickerValidationModel validationModel = new();
    private readonly DateTimeOffset januaryFirst = new(DateTimeOffset.UtcNow.Year, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset julyFirst = new(DateTimeOffset.UtcNow.Year, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly List<BitTimeZone> europeanTimeZones = [.. BitTimeZones.All.Where(z => z.Id.StartsWith("Europe/"))];
    private readonly List<BitTimeZone> officeTimeZones =
    [
        BitTimeZones.Find("America/New_York")!,
        BitTimeZones.Find("Europe/London")!,
        BitTimeZones.Find("Europe/Berlin")!,
        BitTimeZones.Find("Asia/Tehran")!,
        BitTimeZones.Find("Asia/Singapore")!,
    ];
    private readonly List<string> preferredTimeZones = ["Europe/Berlin", "Asia/Tehran", "America/New_York"];
    private readonly List<BitTimeZone> germanTimeZones =
    [
        new("Europe/Berlin", "Berlin", "DE", "W. Europe Standard Time", "Deutschland"),
        new("Europe/Vienna", "Wien", "AT", "W. Europe Standard Time", "Österreich"),
        new("Europe/Zurich", "Zürich", "CH", "W. Europe Standard Time", "Schweiz"),
        new("Europe/Rome", "Rom", "IT", "W. Europe Standard Time", "Italien"),
        new("Europe/Lisbon", "Lissabon", "PT", "GMT Standard Time", "Portugal"),
    ];
    private readonly List<BitTimeZone> persianTimeZones =
    [
        new("Asia/Tehran", "تهران", "IR", "Iran Standard Time", "ایران"),
        new("Asia/Kabul", "کابل", "AF", "Afghanistan Standard Time", "افغانستان"),
        new("Europe/Istanbul", "استانبول", "TR", "Turkey Standard Time", "ترکیه"),
        new("Asia/Baghdad", "بغداد", "IQ", "Arabic Standard Time", "عراق"),
        new("Asia/Dubai", "دبی", "AE", "Arabian Standard Time", "امارات"),
    ];
    private readonly BitTimeZonePickerParams[] timeZonePickerParams =
    [
        new()
        {
            PreferredTimeZones = ["Europe/Berlin", "Europe/Vienna", "Europe/Zurich"],
            Placeholder = "Select a time zone",
            NoCountry = true,
        }
    ];



    // The time it is in a time zone right now, which is what a template can add to a row.
    private static string LocalTime(BitTimeZone zone) => zone.TimeZoneInfo is { } timeZone
                                                            ? TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).ToString("t")
                                                            : string.Empty;
}
