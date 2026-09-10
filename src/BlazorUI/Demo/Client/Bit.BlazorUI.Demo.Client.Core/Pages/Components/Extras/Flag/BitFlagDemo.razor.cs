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
            Description = "The alternative text of the flag image, which is also its accessible name. A flag with none is decorative and is not announced at all. Where OnClick makes the flag a button, this is the name of the button.",
        },
        new()
        {
            Name = "AspectRatio",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aspect ratio of the frame of the flag, as any CSS aspect-ratio value (e.g. \"4/3\" or \"1\"). The height stays and the width follows from the ratio - or the other way round where a Width is what was given - which is what gives a Src of a page's own the proportions the flag itself is drawn in.",
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
            Description = "Draws a hairline border around the flag, which is what keeps a mostly white flag off a white surface. It is drawn inside the frame, so it costs no layout.",
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
            Description = "The dialing code of the country, read the way a telephone number is written: \"+31\", \"0031\" and \"31\" all reach the Netherlands. Dialing codes are not unique, and the first country of BitCountries.All carrying the code wins.",
        },
        new()
        {
            Name = "Country",
            Type = "BitCountry?",
            DefaultValue = "null",
            Description = "The country to render the flag. It is taken exactly as given rather than looked up, so a country of the page's own is as valid as one out of BitCountries, and it wins over every other way of naming one.",
            LinkType = LinkType.Link,
            Href = "#country",
        },
        new()
        {
            Name = "Emoji",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the flag as its Unicode emoji instead of as an image, which costs no request and stays crisp at any size. Windows draws the two letters of the country code instead of the flag. It wins over Src.",
        },
        new()
        {
            Name = "FallbackTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "What to render in place of the flag when there is none to draw - a country that resolved to nothing, or an image that failed to load after the packaged flag has stood in for it.",
        },
        new()
        {
            Name = "Fit",
            Type = "BitImageFit?",
            DefaultValue = "null",
            Description = "How the flag image is scaled and cropped to fit the frame around it, which only matters where a Src of the page's own or an AspectRatio makes the two different shapes. Unset, the image covers the frame and the overflow is cropped.",
            LinkType = LinkType.Link,
            Href = "#image-fit-enum",
        },
        new()
        {
            Name = "Grayscale",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the flag in shades of grey, which is how a flag says the country is not the one in play while staying recognisable.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the flag, as any CSS length. It also sets the size the emoji flag is drawn at, and the width where no Width is set. It wins over Size.",
        },
        new()
        {
            Name = "ImageAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Additional HTML attributes to render on the img element rather than on the frame - a crossorigin or a referrerpolicy for a Src pointing at a CDN. The src, the alt and the loading of the flag itself still win over them.",
        },
        new()
        {
            Name = "Iso2",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ISO 3166-1 alpha-2 code of the country, matched case insensitively. A code no country of BitCountries.All carries draws nothing rather than a broken image. The reserved \"UK\" is answered with the United Kingdom, whose own code is \"GB\".",
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
            Description = "How the browser should load the flag image. It defaults to loading lazily, which is what a list of two hundred flags wants.",
            LinkType = LinkType.Link,
            Href = "#image-loading-enum",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "The full English name of the country, matched case insensitively against the whole name rather than part of it. The alternative names and abbreviations a country is as widely known by are answered too - \"Czechia\", \"Türkiye\", \"Holland\", \"USA\" - and so are the accents and the punctuation another source spells it with.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback for when the flag is clicked. Setting it turns the flag into a button that joins the tab order, grows its target to the 24 pixels WCAG asks of one and answers Enter and Space as well as the pointer, named by its Alt or, without one, by the country it shows.",
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback",
            Description = "The callback for when the flag image fails to load. A Src of the page's own that failed raises it once for that image and once more if the packaged flag standing in for it fails as well.",
        },
        new()
        {
            Name = "OnLoad",
            Type = "EventCallback",
            Description = "The callback for when the flag image has loaded. It is the browser's own load event, so it never fires for the emoji flag, which is text.",
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Rounds the corners of the flag. Circular wins over it where both are set.",
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
            Description = "The size of the flag, out of the icon sizes of the theme. Medium is the 16 pixels the packaged images are drawn at. Width and Height win over it.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Src",
            Type = "string?",
            DefaultValue = "null",
            Description = "The url of the image to render instead of the packaged flag image, for a set of images of the page's own. Emoji wins over it, and a source that fails falls back to the packaged flag of the same country and then to the FallbackTemplate.",
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
            Description = "The tooltip value of the flag element. A tooltip is a pointer affordance rather than an accessible name, so a flag that has to be named to everyone wants an Alt as well.",
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the flag, as any CSS length. The flag images are square, so a Height alone usually sets both. It wins over Size.",
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
                    Description = "The first country carrying the dialing code, read the way a telephone number is written: \"+31\", \"00 31\" and \"31\" all reach the Netherlands."
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
            Description = "Represents the basic information of a specific country. BitCountries holds one shared instance per country, and its Find methods resolve any of the four values below back to it.",
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

    private static readonly BitCountry[] clickableCountries =
    [
        BitCountries.France,
        BitCountries.Germany,
        BitCountries.Italy,
        BitCountries.Spain
    ];

    private BitCountry? selectedCountry;

    private int loadedCount;
    private int failedCount;
}
