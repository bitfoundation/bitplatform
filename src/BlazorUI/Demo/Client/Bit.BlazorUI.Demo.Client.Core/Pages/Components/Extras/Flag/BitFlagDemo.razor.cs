namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Flag;

public partial class BitFlagDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alt",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the flag. Without one the flag is decorative; with OnClick it names the button, so say what the click does.",
        },
        new()
        {
            Name = "AspectRatio",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aspect ratio of the frame, as any CSS aspect-ratio value (e.g. \"4/3\"). The height stays and the width follows - the other way round where only a Width is given. A packaged or ImageSet flag is cropped to fill it (\"1\" draws a square flag) unless a Fit other than Cover is set.",
        },
        new()
        {
            Name = "AutoAlt",
            Type = "bool",
            DefaultValue = "false",
            Description = "Names the flag to assistive technologies with the full name of the country it resolved to. An Alt of the page's own wins over it.",
        },
        new()
        {
            Name = "AutoTitle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sets the tooltip of the flag to the full name of the country it resolved to. A Title of the page's own wins over it.",
        },
        new()
        {
            Name = "Bordered",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a hairline border inside the frame, which keeps a mostly white flag visible on a white surface.",
        },
        new()
        {
            Name = "Circular",
            Type = "bool",
            DefaultValue = "false",
            Description = "Clips the flag into a circle. It wins over Rounded where both are set.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitFlagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the flag.",
            LinkType = LinkType.Link,
            Href = "#flag-class-styles",
        },
        new()
        {
            Name = "Code",
            Type = "string?",
            DefaultValue = "null",
            Description = "The dialing code of the country, read like a phone number (\"+31\", \"0031\", \"31\"). A shared code resolves to the country owning it (\"1\" is the United States), so prefer an ISO code where that matters.",
        },
        new()
        {
            Name = "Country",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The country of the flag, taken as given rather than looked up. A country of the page's own works too, and one the packaged images do not cover (the European Union) is drawn by a Src or SrcPattern. It wins over Iso2, Iso3, Code and Name.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Emoji",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the Unicode emoji flag instead of an image: no request, crisp at any size, but drawn by the platform (Windows draws the two letters of the code). It wins over Src, SrcPattern and ImageSet.",
        },
        new()
        {
            Name = "FallbackTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "What to render when there is no flag to draw: an unknown country, or an image that failed after the packaged flag stood in for it.",
        },
        new()
        {
            Name = "Fit",
            Type = "BitImageFit?",
            DefaultValue = "null",
            Description = "How the image fits a frame of another shape (a Src, SrcPattern or AspectRatio). Unset, it covers the frame and is cropped - a packaged or ImageSet flag to the flag inside its image.",
            LinkType = LinkType.Link,
            Href = "#image-fit-enum",
        },
        new()
        {
            Name = "Grayscale",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the flag in shades of grey, for a country that is not in play.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the flag, as any CSS length; also the width where no Width is set, and the emoji's size. It wins over Size.",
        },
        new()
        {
            Name = "ImageAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Additional HTML attributes for the img element rather than the frame, e.g. a crossorigin or referrerpolicy for a CDN. The flag's own src, alt and loading win, and a srcset only goes with a Src or SrcPattern.",
        },
        new()
        {
            Name = "ImageSet",
            Type = "BitFlagImageSet?",
            DefaultValue = "null",
            Description = "Draws the flag from the flat or shiny set of the Bit.BlazorUI.Assets package (16-64px), at the size the flag and the screen density need - read off Width/Height in px or rem, else Size. Also taken from a CascadingValue of a BitFlagImageSet. Emoji, Src and SrcPattern win over it.",
            LinkType = LinkType.Link,
            Href = "#flag-image-set-enum",
        },
        new()
        {
            Name = "ImageSize",
            Type = "BitFlagImageSize?",
            DefaultValue = "null",
            Description = "Pins the ImageSet image to one size instead of letting the browser pick; it is still scaled to the frame. Only applies with an ImageSet.",
            LinkType = LinkType.Link,
            Href = "#flag-image-size-enum",
        },
        new()
        {
            Name = "Iso2",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ISO 3166-1 alpha-2 code of the country, case insensitive; \"UK\" is answered with the United Kingdom (\"GB\"). An unknown code draws nothing rather than a broken image.",
        },
        new()
        {
            Name = "Iso3",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ISO 3166-1 alpha-3 code of the country, matched case insensitively.",
        },
        new()
        {
            Name = "Loading",
            Type = "BitImageLoading?",
            DefaultValue = "null",
            Description = "How the browser loads the flag image. Lazy unless set.",
            LinkType = LinkType.Link,
            Href = "#image-loading-enum",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "The full English name of the country, case insensitive. Everyday names and abbreviations (\"Czechia\", \"Holland\", \"USA\") resolve too, ignoring accents and punctuation.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback for when the flag is clicked. It makes the flag a button: in the tab order, activated by Enter and Space, with a 24px pointer target, named by its Alt or else its country.",
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback",
            Description = "The callback for when the flag image fails to load; fired again if the packaged flag standing in for a failed Src fails too.",
        },
        new()
        {
            Name = "OnLoad",
            Type = "EventCallback",
            Description = "The callback for when the flag image has loaded. Never fired for an emoji flag.",
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Rounds the corners of the flag with the theme's surface radius, kept in proportion to the flag's size (--bit-Flag-radius overrides it). Circular wins over it where both are set.",
        },
        new()
        {
            Name = "Shadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the card shadow of the theme under the flag.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the flag, out of the theme's icon sizes; unset, it is --bit-Flag-size, else Medium (16px). Width and Height win over it.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Src",
            Type = "string?",
            DefaultValue = "null",
            Description = "The url of an image of the page's own to draw instead of the packaged flag. A failed one falls back to the packaged flag, then to the FallbackTemplate. It wins over SrcPattern and ImageSet; Emoji wins over it.",
        },
        new()
        {
            Name = "SrcPattern",
            Type = "string?",
            DefaultValue = "null",
            Description = "The url of the image of every country, with {iso2}/{iso3} (lower case) or {ISO2}/{ISO3} (upper case) written in - e.g. \"https://flagcdn.com/{iso2}.svg\". Falls back like a Src; Src and Emoji win over it, and it wins over ImageSet.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitFlagClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the flag.",
            LinkType = LinkType.Link,
            Href = "#flag-class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the flag. It is not an accessible name, so a flag that must be named wants an Alt as well.",
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the flag, as any CSS length. A Height alone usually sets both; with a Height that is not a square, a packaged or ImageSet flag is cropped to fill the frame. It wins over Size.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the flag element. Only a flag the browser can focus takes it: one with an OnClick handler, or one given a TabIndex of its own.",
        },
        new()
        {
            Name = "FocusAsync(bool preventScroll)",
            Type = "ValueTask",
            Description = "Gives focus to the flag element, leaving the page scrolled where it is instead of bringing the flag into view.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Flag-size",
            DefaultValue = "--bit-siz-icon-md",
            Description = "Size of a flag with no Size, Width or Height - also the size the emoji is drawn at.",
        },
        new()
        {
            Name = "--bit-Flag-radius",
            DefaultValue = "min(--bit-shp-radius-surface, 1/8 of the size)",
            Description = "Corner radius of a Rounded flag. The default is capped so a theme's large card corner does not turn a small flag into a pill; a value set here is taken as given.",
        },
        new()
        {
            Name = "--bit-Flag-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Border width of a Bordered flag.",
        },
        new()
        {
            Name = "--bit-Flag-border-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Border color of a Bordered flag.",
        },
        new()
        {
            Name = "--bit-Flag-shadow",
            DefaultValue = "--bit-shd-card",
            Description = "Elevation of a Shadow flag.",
        },
        new()
        {
            Name = "--bit-Flag-grayscale-filter",
            DefaultValue = "grayscale(1)",
            Description = "Filter of a Grayscale flag, e.g. grayscale(1) opacity(0.5).",
        },
        new()
        {
            Name = "--bit-Flag-hover-opacity",
            DefaultValue = "0.8",
            Description = "Opacity of a clickable flag under the pointer.",
        },
        new()
        {
            Name = "--bit-Flag-active-opacity",
            DefaultValue = "0.6",
            Description = "Opacity of a clickable flag while pressed.",
        },
        new()
        {
            Name = "--bit-Flag-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Color of the keyboard focus ring.",
        },
        new()
        {
            Name = "--bit-Flag-emoji-font-family",
            DefaultValue = "'Apple Color Emoji', 'Segoe UI Emoji', 'Noto Color Emoji', 'Segoe UI Symbol', sans-serif",
            Description = "Font stack of the Emoji flag. Put a flag emoji font of the page's own first to draw flags where the platform draws letters (Windows).",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "countries",
            Title = "BitCountries",
            Description = "The table of countries the flag images and the country lookups of the library are built on. Every country is a shared BitCountry instance named after itself, so it can be written straight into markup, and the lookups below resolve any of the ways one is written down back to it - out of a dictionary rather than by scanning the table.",
            Parameters =
            [
                new()
                {
                    Name = "All",
                    Type = "BitCountry[]",
                    Description = "Every country the packaged flag images cover, in alphabetical order."
                },
                new()
                {
                    Name = "FindByIso2(string? iso2)",
                    Type = "BitCountry?",
                    Description = "The country carrying the ISO 3166-1 alpha-2 code, case insensitively and ignoring surrounding whitespace. \"UK\" is answered with the United Kingdom, whose own code is \"GB\"."
                },
                new()
                {
                    Name = "FindByIso3(string? iso3)",
                    Type = "BitCountry?",
                    Description = "The country carrying the ISO 3166-1 alpha-3 code, case insensitively."
                },
                new()
                {
                    Name = "FindByName(string? name)",
                    Type = "BitCountry?",
                    Description = "The country carrying the English name, matched against the whole name rather than part of it. The alternative names and abbreviations a country is as widely known by are answered too, and the accents, punctuation and spacing another source spells it with are ignored."
                },
                new()
                {
                    Name = "FindByCode(string? code)",
                    Type = "BitCountry?",
                    Description = "The country carrying the dialing code, read the way a telephone number is written: \"+31\", \"00 31\" and \"31\" all reach the Netherlands. A shared code resolves to the country with the highest Priority (\"1\" is the United States, \"7\" Russia), else the first alphabetically."
                },
                new()
                {
                    Name = "Find(string? value)",
                    Type = "BitCountry?",
                    Description = "The country a single value stands for, read as an alpha-2 code, an alpha-3 code, a name and a dialing code in that order."
                },
                new()
                {
                    Name = "HasFlag(string? iso2)",
                    Type = "bool",
                    Description = "Whether a packaged flag image ships for the given alpha-2 code, answered without asking the network for it."
                },
                new()
                {
                    Name = "GetEmoji(string? iso2)",
                    Type = "string?",
                    Description = "The Unicode emoji flag of the given alpha-2 code, built from the pair of regional indicator symbols its letters stand for rather than looked up - so it answers for codes the packaged images do not cover, and for \"GB-ENG\", \"GB-SCT\" and \"GB-WLS\"."
                },
            ]
        },
        new()
        {
            Id = "country",
            Title = "BitCountry",
            Description = "Represents the basic information of a specific country. BitCountries holds one shared instance per country - build a new one rather than editing it - and its Find methods resolve its Name, Code, Iso2 or Iso3 back to it.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    Description = "The full name of the country."
                },
                new()
                {
                    Name = "Code",
                    Type = "string",
                    Description = "The dialing code of the country, written without its leading plus sign. It is not unique: Canada and the United States both carry \"1\"."
                },
                new()
                {
                    Name = "Iso2",
                    Type = "string",
                    Description = "The ISO 3166-1 alpha-2 code of the country, which is what the flag image and the emoji flag are keyed by."
                },
                new()
                {
                    Name = "Iso3",
                    Type = "string",
                    Description = "The ISO 3166-1 alpha-3 code of the country."
                },
                new()
                {
                    Name = "Priority",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The tie-breaker among the countries sharing a dialing code; the higher one wins (the United States over Canada for \"1\")."
                },
                new()
                {
                    Name = "ExtraCodes",
                    Type = "string[]?",
                    DefaultValue = "null",
                    Description = "The other dialing codes the country answers to beyond Code (the Dominican Republic's \"1-829\" and \"1-849\" beside its \"1-809\")."
                },
                new()
                {
                    Name = "DigitsCode",
                    Type = "string",
                    Description = "The Code reduced to its digits, as it appears in an E.164 number."
                },
                new()
                {
                    Name = "DigitsCodes",
                    Type = "string[]",
                    Description = "Every dialing code of the country - Code first, then ExtraCodes - reduced to its digits."
                },
                new()
                {
                    Name = "Emoji",
                    Type = "string?",
                    Description = "The flag of the country as a Unicode emoji, built from the pair of regional indicator symbols its Iso2 stands for. Null where the Iso2 is not two ASCII letters."
                },
            ]
        },
        new()
        {
            Id = "flag-class-styles",
            Title = "BitFlagClassStyles",
            Description = "Custom CSS classes/styles for the different parts of the BitFlag component.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the flag, which is the frame carrying the size, the shape and the border."
                },
                new()
                {
                    Name = "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the img element, which is only rendered while the flag is drawn as an image."
                },
                new()
                {
                    Name = "Emoji",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element the emoji flag is written in."
                },
                new()
                {
                    Name = "Fallback",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element wrapping the FallbackTemplate."
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
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
                    Description = "The medium size, which is the 16 pixels the packaged flag images are drawn at.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "The large size.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "flag-image-set-enum",
            Name = "BitFlagImageSet",
            Description = "The image sets of the Bit.BlazorUI.Assets package a BitFlag can be drawn out of, each carrying every flag at 16, 24, 32, 48 and 64 pixels.",
            Items =
            [
                new()
                {
                    Name = "Flat",
                    Description = "The flat artwork, in the same style as the packaged 16 pixel image.",
                    Value = "0",
                },
                new()
                {
                    Name = "Shiny",
                    Description = "The shiny artwork, with a gloss and a soft edge over the flag.",
                    Value = "1",
                },
            ]
        },
        new()
        {
            Id = "flag-image-size-enum",
            Name = "BitFlagImageSize",
            Description = "The pixel sizes the image sets of the Bit.BlazorUI.Assets package draw every flag at, which a BitFlag with an ImageSet can be pinned to.",
            Items =
            [
                new()
                {
                    Name = "Size16",
                    Description = "The 16 pixel image.",
                    Value = "0",
                },
                new()
                {
                    Name = "Size24",
                    Description = "The 24 pixel image.",
                    Value = "1",
                },
                new()
                {
                    Name = "Size32",
                    Description = "The 32 pixel image.",
                    Value = "2",
                },
                new()
                {
                    Name = "Size48",
                    Description = "The 48 pixel image.",
                    Value = "3",
                },
                new()
                {
                    Name = "Size64",
                    Description = "The 64 pixel image.",
                    Value = "4",
                },
            ]
        },
        new()
        {
            Id = "image-fit-enum",
            Name = "BitImageFit",
            Description = "Determines how the flag image is scaled and cropped to fit the frame around it. It only matters where the image and the frame turn out to be different shapes, which a Src of the page's own or an AspectRatio is what makes possible.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Description = "Neither the image nor the frame are scaled. Whatever of the image does not fit is cropped away from the right and the bottom.",
                    Value = "0",
                },
                new()
                {
                    Name = "Center",
                    Description = "The image is not scaled, and is centered within the frame with the overflow cropped.",
                    Value = "1",
                },
                new()
                {
                    Name = "CenterContain",
                    Description = "The image keeps its aspect ratio, is scaled down where needed so that all of it fits, and is centered in the frame.",
                    Value = "2",
                },
                new()
                {
                    Name = "CenterCover",
                    Description = "The image keeps its aspect ratio, is scaled up where needed so that it covers the frame, and is centered in it.",
                    Value = "3",
                },
                new()
                {
                    Name = "Contain",
                    Description = "The image keeps its aspect ratio and is fully contained within the frame. Nothing is cropped, and whatever of the frame it does not reach is left empty.",
                    Value = "4",
                },
                new()
                {
                    Name = "Cover",
                    Description = "The image keeps its aspect ratio and fills the frame, with whatever falls outside it cropped away. This is what a flag with no Fit set does.",
                    Value = "5",
                },
                new()
                {
                    Name = "Fill",
                    Description = "The image is stretched to fill the frame exactly, at the cost of distorting the flag where the two shapes disagree.",
                    Value = "6",
                },
                new()
                {
                    Name = "ScaleDown",
                    Description = "The image is contained within the frame but never scaled up, so one smaller than the frame keeps its natural size.",
                    Value = "7",
                },
            ]
        },
        new()
        {
            Id = "image-loading-enum",
            Name = "BitImageLoading",
            Description = "Represents the img loading attribute values.",
            Items =
            [
                new()
                {
                    Name = "Eager",
                    Description = "Tells the browser to load the image as soon as the element is processed.",
                    Value = "0",
                },
                new()
                {
                    Name = "Lazy",
                    Description = "Tells the browser to hold off on loading the image until it estimates that it will be needed imminently. This is what a flag with no Loading set does.",
                    Value = "1",
                },
            ]
        },
    ];



    // A vector source of the same flag, inline so the example carries its own file: it is the point of
    // Src - the packaged images are 16 pixels of raster and blur past the sizes of the theme, where a
    // vector one stays sharp at any size at all.
    private const string japanSvg = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 900 600'%3E%3Crect width='900' height='600' fill='%23fff'/%3E%3Ccircle cx='450' cy='300' r='180' fill='%23bc002d'/%3E%3C/svg%3E";

    // A 3:2 vector source, which is the shape the flag itself is drawn in and the shape the vector sets
    // of the world ship: the packaged images are square, so this is the case AspectRatio is for.
    private const string netherlandsSvg = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 900 600'%3E%3Crect width='900' height='600' fill='%231e4785'/%3E%3Crect width='900' height='400' fill='%23fff'/%3E%3Crect width='900' height='200' fill='%23ae1c28'/%3E%3C/svg%3E";

    // One url for every country: {iso2} is written in as the lower-cased alpha-2 code.
    private const string flagCdnPattern = "https://flagcdn.com/{iso2}.svg";

    // A country the table does not carry, which a source of the page's own covers: the European Union has an
    // exceptionally reserved alpha-2 code and no alpha-3 one.
    private static readonly BitCountry europeanUnion = new("European Union", "", "EU", "");

    private static readonly BitCountry[] clickableCountries =
    [
        BitCountries.France,
        BitCountries.Germany,
        BitCountries.Italy,
        BitCountries.Spain
    ];

    private BitCountry? selectedCountry;

    private readonly BitFlagParams[] flagParams =
    [
        new()
        {
            Height = "2rem",
            Rounded = true,
            Bordered = true,
            AutoAlt = true,
            AutoTitle = true,
            ImageSet = BitFlagImageSet.Shiny,
        }
    ];

    private int eventFlagsRenderCount;
    private int loadedCount;
    private int failedCount;

    // Rendering the flags only on demand is what lets their load and error events be watched at all:
    // rendered with the page, they would have fired before the section was ever scrolled to. A new key
    // renders them anew, so each press fetches and counts from scratch.
    private void RenderEventFlags()
    {
        loadedCount = 0;
        failedCount = 0;
        eventFlagsRenderCount++;
    }
}
