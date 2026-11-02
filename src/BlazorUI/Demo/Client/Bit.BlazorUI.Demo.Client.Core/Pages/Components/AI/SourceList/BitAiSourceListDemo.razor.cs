using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.SourceList;

public partial class BitAiSourceListDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Citations",
            Type = "IReadOnlyList<BitAiCitation>?",
            DefaultValue = "null",
            Description = "The citations to list the sources of - usually the Citations of a message. A source is listed once (by its SourceId), under the lowest Number any of its citations carries, ordered by those numbers; a source cited without a number goes last. Nothing is rendered while there is none.",
            LinkType = LinkType.Link,
            Href = "/components/ai/citationmarker#citation",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiSourceListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the source list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "OnSourceClick",
            Type = "EventCallback<BitAiCitationSource>",
            DefaultValue = "",
            Description = "The callback for when the user activates a source card, with its source. When set, the cards become buttons instead of links - for revealing the source in place. A disabled list hands over nothing.",
            LinkType = LinkType.Link,
            Href = "/components/ai/sourcecard#citation-source",
        },
        new()
        {
            Name = "ShowHeading",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the heading (BitAiStrings.Sources, \"Sources\") above the cards, which names the list for assistive technologies. Left out, the list is named by the same words all the same.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiSourceListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the source list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiSourceListClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAiSourceList." },
                new() { Name = "Header", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the heading of the BitAiSourceList." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list (the grid of the cards) of the BitAiSourceList." },
                new()
                {
                    Name = "SourceCard",
                    Type = "BitAiSourceCardClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for every BitAiSourceCard of the BitAiSourceList.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/sourcecard#class-styles",
                },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiSourceList-gap", DefaultValue = "spacing(1)", Description = "Room between the heading and the cards, and between two cards." },
        new() { Name = "--bit-AiSourceList-column-min-width", DefaultValue = "spacing(26)", Description = "Width a column of cards does not shrink below, which decides how many columns fit; 100% stacks the cards." },
        new() { Name = "--bit-AiSourceList-header-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The heading." },
        new() { Name = "--bit-AiSourceList-header-font-size", DefaultValue = "var(--bit-tpg-fs-xs)", Description = "Size of the heading, which is cased and tracked like the overline style of the theme." },
    ];



    // Basic.
    private static readonly BitAiCitationSource renderModesSource = new("render-modes", "Blazor render modes")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
        SiteName = "Microsoft Learn",
        Description = "How interactivity is chosen per component or per page.",
    };

    private static readonly BitAiCitationSource circuitsSource = new("circuits", "SignalR circuit lifetime")
    {
        Kind = BitAiSourceKind.Document,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
        SiteName = "Microsoft Learn",
        Description = "What happens to state when a circuit drops.",
    };

    private static readonly BitAiCitation[] citations =
    [
        new("c1", renderModesSource) { Number = 1 },
        new("c2", circuitsSource) { Number = 2 },
    ];



    // Deduplication & order.
    private static readonly BitAiCitationSource reportSource = new("q3-report", "Q3 report.pdf") { Kind = BitAiSourceKind.File };

    private static readonly BitAiCitation[] mixedCitations =
    [
        new("c1", circuitsSource) { Number = 2 },
        new("c2", renderModesSource) { Number = 1 },
        new("c3", circuitsSource) { Number = 3, Quote = "Another passage of the same document." },
        new("c4", reportSource),
    ];



    // Source click.
    private BitAiCitationSource? clicked;



    // From a message: the citations a scripted transport of this site streams.
    private BitAiChatSession? citedSession;
    private BitAiMessage? citedMessage;
    private bool isAsking;

    private async Task AskCited()
    {
        isAsking = true;

        if (citedSession is not null) await citedSession.DisposeAsync();

        citedSession = new BitAiChatSession(AiDemoContentTransports.Cited());
        citedMessage = await citedSession.SubmitAsync("What does Interactive Server trade away?");

        isAsking = false;
    }



    // Disabled.
    private BitAiCitationSource? disabledClicked;



    // Cascading parameters.
    private readonly BitAiSourceListParams[] listParams =
    [
        new() { ShowHeading = false, Styles = new() { SourceCard = new() { Title = "font-weight: 600" } } }
    ];



    // RTL.
    private static readonly BitAiCitation[] rtlCitations =
    [
        new("c1", new BitAiCitationSource("render-modes", "حالت‌های رندر بلیزر")
        {
            Kind = BitAiSourceKind.Web,
            Url = "https://learn.microsoft.com/fa-ir/aspnet/core/blazor/components/render-modes",
            SiteName = "مایکروسافت لرن",
        }) { Number = 1 },
        new("c2", new BitAiCitationSource("circuits", "طول عمر اتصال SignalR")
        {
            Kind = BitAiSourceKind.Document,
            Url = "https://learn.microsoft.com/fa-ir/aspnet/core/blazor/fundamentals/signalr",
            SiteName = "مایکروسافت لرن",
        }) { Number = 2 },
    ];



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (citedSession is not null) await citedSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
