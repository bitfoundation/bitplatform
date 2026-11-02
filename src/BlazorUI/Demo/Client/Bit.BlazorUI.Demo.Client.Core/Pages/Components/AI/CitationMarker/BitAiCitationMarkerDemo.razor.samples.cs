namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.CitationMarker;

public partial class BitAiCitationMarkerDemo
{
    private readonly string example1RazorCode = """
<div>
    Interactive Server keeps component state on the server and streams UI diffs over a SignalR
    circuit<BitAiCitationMarker Citation="renderModes" />. If that circuit drops, the state goes with it unless
    you have persisted it yourself<BitAiCitationMarker Citation="circuits" />.
</div>
""";
    private readonly string example1CsharpCode = """
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
""";

    private readonly string example2RazorCode = """
<div>
    Prerendering runs the component twice<BitAiCitationMarker Citation="prerendering" />, once on the server and
    once in the browser<BitAiCitationMarker Citation="prerenderingNoQuote" AriaLabel="Source 4, the prerendering guide" />.
</div>
""";
    private readonly string example2CsharpCode = """
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
""";

    private readonly string example3RazorCode = """
<div>
    Interactive Server keeps component state on the server<BitAiCitationMarker Citation="renderModes" OnActivate="c => activated = c" />
    and loses it with the circuit<BitAiCitationMarker Citation="circuits" OnActivate="c => activated = c" />,
    and prerendering runs it twice<BitAiCitationMarker Citation="prerendering" OnActivate="c => activated = c" Disabled />.
</div>

@if (activated is not null)
{
    <BitAiSourceCard Source="activated.Source" Number="activated.Number" />
    <blockquote>@activated.Quote</blockquote>
}
""";
    private readonly string example3CsharpCode = """
// renderModes, circuits and prerendering are the citations of the examples above.
private BitAiCitation? activated;
""";

    private readonly string example4RazorCode = """
<BitButton OnClick="StreamAnswer" Disabled="isStreaming">Stream the answer</BitButton>

@if (streamedText.Length > 0)
{
    <BitAiStreamingMarkdown Text="@streamedText"
                            IsStreaming="isStreaming"
                            Citations="markdownCitations"
                            OnCitationClick="c => markdownActivated = c" />

    <BitAiSourceList Citations="markdownCitations" />

    <div>Last activated: <b>@(markdownActivated?.Source.Title ?? "-")</b></div>
}
""";
    private readonly string example4CsharpCode = """"
// renderModes and circuits are the citations of the Basic example.
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
"""";

    private readonly string example5RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskCited">Ask</BitButton>

@if (citedMessage is not null)
{
    <BitAiChatMessage Message="citedMessage" Conversation="citedSession!.Conversation" />
}
""";
    private readonly string example5CsharpCode = """
// AiDemoContentTransports.Cited is a scripted transport of this site that streams anchored citations:
// https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Services/AI/AiDemoContentTransports.cs
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

public async ValueTask DisposeAsync()
{
    if (citedSession is not null) await citedSession.DisposeAsync();
}
""";

    private readonly string example6RazorCode = """
<BitParams Parameters="markerParams">
    <div>
        State lives on the server<BitAiCitationMarker Citation="renderModes" />, goes with the
        circuit<BitAiCitationMarker Citation="circuits" />, and is rendered
        twice<BitAiCitationMarker Citation="prerendering" Styles="@(new() { Root = "border-radius: 0" })" />.
    </div>
</BitParams>
""";
    private readonly string example6CsharpCode = """
// renderModes, circuits and prerendering are the citations of the examples above.
private readonly BitAiCitationMarkerParams[] markerParams =
[
    new() { Styles = new() { Root = "border-radius: 1rem; font-weight: bold" } }
];
""";

    private readonly string example7RazorCode = """
<style>
    .custom-class {
        color: white;
        background-color: #16a34a;
        box-shadow: 0 0 0.25rem #16a34a;
    }

    .custom-root {
        color: #7c3aed;
        background-color: transparent;
        outline: 1px solid #7c3aed;
    }
</style>

<div>
    A styled marker<BitAiCitationMarker Citation="renderModes" Style="font-weight: bold; text-decoration: underline" />,
    a classy one<BitAiCitationMarker Citation="circuits" Class="custom-class" />,
    one with Styles<BitAiCitationMarker Citation="renderModes" Styles="@(new() { Root = "border-radius: 50%; font-style: italic" })" />
    and one with Classes<BitAiCitationMarker Citation="circuits" Classes="@(new() { Root = "custom-root" })" />.
</div>

<div style="--bit-AiCitationMarker-background: #fef3c7;
            --bit-AiCitationMarker-color: #92400e;
            --bit-AiCitationMarker-hover-background: #d97706;
            --bit-AiCitationMarker-hover-color: #fff;
            --bit-AiCitationMarker-radius: 1rem">
    Restyled by the variables of their container<BitAiCitationMarker Citation="renderModes" /><BitAiCitationMarker Citation="circuits" />.
</div>
""";

    private readonly string example8RazorCode = """
<div dir="rtl">
    حالت تعاملی سرور، وضعیت کامپوننت را روی سرور نگه می‌دارد<BitAiCitationMarker Citation="rtlCitation" />
    و با قطع شدن اتصال آن را از دست می‌دهد<BitAiCitationMarker Dir="BitDir.Rtl" Citation="rtlCitation" />.
</div>
""";
    private readonly string example8CsharpCode = """
private static readonly BitAiCitation rtlCitation = new("c1", new BitAiCitationSource("render-modes", "حالت‌های رندر بلیزر")
{
    Kind = BitAiSourceKind.Web,
    Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
})
{
    Number = 1,
};
""";
}
