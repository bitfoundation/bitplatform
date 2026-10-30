using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.CitationMarker;

public partial class BitAiCitationMarkerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Citation",
            Type = "BitAiCitation",
            DefaultValue = "",
            Description = "The citation this marker points at (required). The marker shows its Number, previews the title of its source and its Quote in the native tooltip, and is named after both for assistive technologies.",
            LinkType = LinkType.Link,
            Href = "#citation",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiCitationMarkerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the citation marker.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "OnActivate",
            Type = "EventCallback<BitAiCitation>",
            DefaultValue = "",
            Description = "The callback for when the user activates the marker (a click, or Enter / Space while it has the focus), with its citation - the place to reveal the source in place. A disabled marker raises nothing.",
            LinkType = LinkType.Link,
            Href = "#citation",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiCitationMarkerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the citation marker.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiCitationMarkerClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element (the button) of the BitAiCitationMarker." },
            ]
        },
        new()
        {
            Id = "citation",
            Title = "BitAiCitation",
            Description = "A claim of a response attributed to a BitAiCitationSource: new BitAiCitation(citationId, source). A transport streams one as a BitAiCitationEvent; the message collects them in BitAiMessage.Citations. A sealed record.",
            Parameters =
            [
                new() { Name = "CitationId", Type = "string", DefaultValue = "", Description = "The identifier of the citation, unique within its message." },
                new()
                {
                    Name = "Source",
                    Type = "BitAiCitationSource",
                    DefaultValue = "",
                    Description = "The source being cited. Several citations of one source (the same SourceId) share one entry of the source list.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/sourcecard#citation-source",
                },
                new() { Name = "PartId", Type = "string?", DefaultValue = "null", Description = "The identifier of the part the span refers to, when the citation is anchored." },
                new() { Name = "StartIndex", Type = "int?", DefaultValue = "null", Description = "The start of the cited span of the text of the part, in UTF-16 code units. With EndIndex, it is what lets the text of a message place the marker at exactly the right position; during streaming a citation whose span runs past the text received so far is held back until the text catches up." },
                new() { Name = "EndIndex", Type = "int?", DefaultValue = "null", Description = "The end of the cited span, exclusive, in UTF-16 code units." },
                new() { Name = "Quote", Type = "string?", DefaultValue = "null", Description = "The exact text quoted from the source, shown in the preview of the marker (cut short at 220 characters)." },
                new() { Name = "Locator", Type = "string?", DefaultValue = "null", Description = "A page number, a section or another location within the source." },
                new() { Name = "Number", Type = "int?", DefaultValue = "null", Description = "The one-based number of the marker - what the marker shows, what an [n] of the Markdown resolves against and what the source list numbers the source with. A session assigns it when the message is finalised, in document order whatever the order the events arrived in; set it yourself on citations you build." },
                new() { Name = "IsAnchored", Type = "bool", DefaultValue = "false", Description = "Whether the citation addresses a span of text: both StartIndex and EndIndex are set, and EndIndex is past StartIndex." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiCitationMarker-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "The number at rest." },
        new() { Name = "--bit-AiCitationMarker-background", DefaultValue = "var(--bit-clr-pri-tint)", Description = "The chip at rest." },
        new() { Name = "--bit-AiCitationMarker-hover-color", DefaultValue = "var(--bit-clr-pri-text)", Description = "The number under the pointer." },
        new() { Name = "--bit-AiCitationMarker-hover-background", DefaultValue = "var(--bit-clr-pri)", Description = "The chip under the pointer." },
        new() { Name = "--bit-AiCitationMarker-radius", DefaultValue = "var(--bit-shp-radius-xs)", Description = "Corner of the chip." },
    ];



    // Basic: two citations of two sources.
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

    private static readonly BitAiCitation renderModes = new("c1", renderModesSource)
    {
        Number = 1,
        Quote = "Interactive Server renders the component on the server and sends UI updates over a circuit.",
    };

    private static readonly BitAiCitation circuits = new("c2", circuitsSource)
    {
        Number = 2,
        Quote = "When a circuit is lost, the associated component state is disposed.",
    };



    // Preview: a quote in the tooltip, and an accessible name of the page's own.
    private static readonly BitAiCitationSource prerenderingSource = new("prerendering", "Prerendering")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/components/prerender",
        SiteName = "Microsoft Learn",
    };

    private static readonly BitAiCitation prerendering = new("c3", prerenderingSource)
    {
        Number = 3,
        Quote = "Prerendering is the process of initially rendering page content on the server without enabling event handlers for rendered controls.",
    };

    private static readonly BitAiCitation prerenderingNoQuote = new("c4", prerenderingSource) { Number = 4 };



    // Activation.
    private BitAiCitation? activated;



    // In Markdown: the [n] of the text resolved against the citations as it streams.
    private static readonly BitAiCitation[] markdownCitations = [renderModes, circuits];
    private string streamedText = "";
    private bool isStreaming;
    private BitAiCitation? markdownActivated;

    private async Task StreamAnswer()
    {
        isStreaming = true;
        markdownActivated = null;

        const string answer = """
            **Interactive Server** keeps component state on the server and streams UI diffs over a SignalR circuit [1].
            If that circuit drops, the state goes with it unless you have persisted it yourself [2].

            That is the trade the render mode makes: a fast first paint, in exchange for a dependency on a live connection [1].
            """;

        for (var i = 6; i < answer.Length; i += 6)
        {
            streamedText = answer[..i];
            StateHasChanged();
            await Task.Delay(30);
        }

        streamedText = answer;
        isStreaming = false;
    }



    // Anchored in a message: citations streamed with the span they support, by a scripted transport of this site.
    private BitAiChatSession? citedSession;
    private BitAiMessage? citedMessage;

    private async Task AskCited()
    {
        if (citedSession is not null) await citedSession.DisposeAsync();

        var current = new BitAiChatSession(AiDemoContentTransports.Cited());
        citedSession = current;
        citedMessage = null;

        // The answer appears in the conversation once the transport starts it; from then on the message follows itself.
        current.StateChanged += () =>
        {
            var message = current.Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant);
            if (ReferenceEquals(message, citedMessage) || ReferenceEquals(current, citedSession) is false) return;

            citedMessage = message;
            InvokeAsync(StateHasChanged);
        };

        await current.SubmitAsync("What does Interactive Server trade away?");
    }



    // Cascading parameters.
    private readonly BitAiCitationMarkerParams[] markerParams =
    [
        new() { Styles = new() { Root = "border-radius: 1rem; font-weight: bold" } }
    ];



    // RTL.
    private static readonly BitAiCitation rtlCitation = new("c1", new BitAiCitationSource("render-modes", "حالت‌های رندر بلیزر")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
    })
    {
        Number = 1,
    };



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (citedSession is not null) await citedSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
