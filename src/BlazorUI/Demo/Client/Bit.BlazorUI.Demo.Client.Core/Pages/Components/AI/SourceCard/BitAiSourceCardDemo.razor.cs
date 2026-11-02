namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.SourceCard;

public partial class BitAiSourceCardDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiSourceCardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the source card.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Number",
            Type = "int?",
            DefaultValue = "null",
            Description = "The number shown beside the title, which matches the inline citation markers of the source. Left out when null (or int.MaxValue, the number a source cited without one is sorted under).",
        },
        new()
        {
            Name = "OnActivate",
            Type = "EventCallback<BitAiCitationSource>",
            DefaultValue = "",
            Description = "The callback for when the card is activated, with its source. When set, the card becomes a button instead of a link - for revealing the source in place. A disabled card hands over nothing.",
            LinkType = LinkType.Link,
            Href = "#citation-source",
        },
        new()
        {
            Name = "Source",
            Type = "BitAiCitationSource",
            DefaultValue = "",
            Description = "The source to render (required). Its Url becomes the link of the card only when it is an absolute http or https address, and its IconUrl the favicon only when it is a web address or an inline raster image the image policy of the options shows outright.",
            LinkType = LinkType.Link,
            Href = "#citation-source",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiSourceCardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the source card.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiSourceCardClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAiSourceCard." },
                new()
                {
                    Name = "Card",
                    Type = "BitCardClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitCard of the BitAiSourceCard.",
                    LinkType = LinkType.Link,
                    Href = "/components/card#card-class-styles",
                },
                new() { Name = "Content", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content (the number and the body) of the BitAiSourceCard." },
                new() { Name = "Index", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the marker number of the BitAiSourceCard." },
                new() { Name = "Body", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the body (the title and the site) of the BitAiSourceCard." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title of the BitAiSourceCard." },
                new() { Name = "Site", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the site line (the favicon and the site name) of the BitAiSourceCard." },
                new() { Name = "Favicon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the favicon of the BitAiSourceCard." },
            ]
        },
        new()
        {
            Id = "citation-source",
            Title = "BitAiCitationSource",
            Description = "A referenceable source one or more citations point at: new BitAiCitationSource(sourceId, title). Sources are deduplicated by SourceId across a message, so several citations quoting different passages of one document share one entry of the source list and one number. A sealed record.",
            Parameters =
            [
                new() { Name = "SourceId", Type = "string", DefaultValue = "", Description = "The stable identifier of the source, used to deduplicate it and to group its citations." },
                new() { Name = "Title", Type = "string", DefaultValue = "", Description = "The display title, the title of the card." },
                new()
                {
                    Name = "Kind",
                    Type = "BitAiSourceKind",
                    DefaultValue = "BitAiSourceKind.Unknown",
                    Description = "The kind of source. The card names it on its site line when the source has neither a SiteName nor a web address.",
                    LinkType = LinkType.Link,
                    Href = "#source-kind-enum",
                },
                new() { Name = "Url", Type = "string?", DefaultValue = "null", Description = "The absolute URL of the source, when it is addressable. Only an http or https one becomes a link; its host is the site line when there is no SiteName." },
                new() { Name = "SiteName", Type = "string?", DefaultValue = "null", Description = "The host or publication name shown under the title." },
                new() { Name = "IconUrl", Type = "string?", DefaultValue = "null", Description = "The URL of a favicon or thumbnail for the card: loaded only when it is a web address or an inline raster image (data:image/, not SVG), lazily and without a referrer - and only when the image policy of the options (BitAiUiOptions.Images) shows it outright: a favicon is not worth a placeholder of its own, so one the policy would load on request or block is left out." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A short description or excerpt of the source, for a preview of the application's own." },
                new() { Name = "Author", Type = "string?", DefaultValue = "null", Description = "The author or byline." },
                new() { Name = "PublishedAt", Type = "DateTimeOffset?", DefaultValue = "null", Description = "The publication date." },
                new() { Name = "Score", Type = "double?", DefaultValue = "null", Description = "The retrieval score, normalized to 0..1 where the retriever reports one." },
                new() { Name = "Metadata", Type = "IReadOnlyDictionary<string, object?>?", DefaultValue = "null", Description = "Application-defined values carried through to custom source renderers." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "source-kind-enum",
            Name = "BitAiSourceKind",
            Description = "Where a citation points (BitAiCitationSource.Kind).",
            Items =
            [
                new() { Name = "Unknown", Description = "Kind not stated.", Value = "0" },
                new() { Name = "Web", Description = "A page on the public web.", Value = "1" },
                new() { Name = "Document", Description = "A document in a corpus or knowledge base.", Value = "2" },
                new() { Name = "File", Description = "A file the user attached to the conversation.", Value = "3" },
                new() { Name = "Dataset", Description = "A row, record, or query result from structured data.", Value = "4" },
                new() { Name = "Message", Description = "A message earlier in this conversation.", Value = "5" },
                new() { Name = "Tool", Description = "The output of a tool.", Value = "6" },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiSourceCard-gap", DefaultValue = "spacing(1)", Description = "Room between the number and the body." },
        new() { Name = "--bit-AiSourceCard-index-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The number." },
        new() { Name = "--bit-AiSourceCard-title-color", DefaultValue = "the text color of the card", Description = "The title." },
        new() { Name = "--bit-AiSourceCard-title-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the title." },
        new() { Name = "--bit-AiSourceCard-site-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The site line." },
        new() { Name = "--bit-AiSourceCard-site-font-size", DefaultValue = "var(--bit-tpg-fs-xs)", Description = "Size of the site line and of the number." },
        new() { Name = "--bit-AiSourceCard-favicon-size", DefaultValue = "var(--bit-siz-icon-sm)", Description = "Size of the favicon." },
    ];



    // Basic.
    private static readonly BitAiCitationSource renderModes = new("render-modes", "Blazor render modes")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
        SiteName = "Microsoft Learn",
        Description = "How interactivity is chosen per component or per page.",
        Author = "Microsoft",
        PublishedAt = new DateTimeOffset(2025, 11, 11, 0, 0, 0, TimeSpan.Zero),
        Score = 0.94,
    };



    // Number.
    private static readonly BitAiCitationSource circuits = new("circuits", "SignalR circuit lifetime")
    {
        Kind = BitAiSourceKind.Document,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
        SiteName = "Microsoft Learn",
        Description = "What happens to state when a circuit drops.",
        Author = "Microsoft",
        PublishedAt = new DateTimeOffset(2025, 11, 11, 0, 0, 0, TimeSpan.Zero),
        Score = 0.81,
    };



    // Site & favicon.
    private static readonly BitAiCitationSource withFavicon = new("bitplatform", "bitfoundation/bitplatform")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://github.com/bitfoundation/bitplatform",
        SiteName = "GitHub",
        IconUrl = "https://github.com/favicon.ico",
    };

    private static readonly BitAiCitationSource hostOnly = new("signalr", "ASP.NET Core SignalR overview")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/aspnet/core/signalr/introduction",
    };

    private static readonly BitAiCitationSource attachedFile = new("q3-report", "Q3 report.pdf")
    {
        Kind = BitAiSourceKind.File,
        Description = "The quarterly report you attached to the conversation.",
        Author = "Finance team",
        Score = 0.72,
    };

    private bool blockImages;

    private static readonly BitAiUiOptions blockedImageOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block };



    // Activation.
    private BitAiCitationSource? activated;



    // Untrusted URLs.
    private static readonly BitAiCitationSource untrusted = new("untrusted", "A page that says it is the docs")
    {
        Kind = BitAiSourceKind.Web,
        Url = "javascript:alert('Gotcha')",
        SiteName = "docs.example.com",
        IconUrl = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnLz4=",
    };



    // Cascading parameters.
    private readonly BitAiSourceCardParams[] cardParams =
    [
        new() { Styles = new() { Title = "font-weight: 600", Index = "color: var(--bit-clr-pri)" } }
    ];



    // RTL.
    private static readonly BitAiCitationSource rtlSource = new("render-modes", "حالت‌های رندر بلیزر")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/fa-ir/aspnet/core/blazor/components/render-modes",
        SiteName = "مایکروسافت لرن",
    };
}
