namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Citations;

public partial class AiCitationsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask about render modes"
               Suggestions="suggestions"
               OnCitationClick="OnCitationClicked" />
</div>

@if (selected is { } citation)
{
    <BitAiSourceCard Source="citation.Source" Number="citation.Number" />
    @if (citation.Quote is { Length: > 0 } quote)
    {
        <p><i>“@quote”</i></p>
    }
}
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoContentTransports.Cited());
private BitAiCitation? selected;

private readonly string[] suggestions = ["What happens to state when a circuit drops?"];

private void OnCitationClicked(BitAiCitation citation) => selected = citation;

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """
public static IBitAiTransport Cited() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    var source = new BitAiCitationSource("s1", "Blazor render modes")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
        SiteName = "Microsoft Learn",
    };

    await yielder(new BitAiMessageStartedEvent());
    await yielder(new BitAiStatusEvent("Searching the documentation"));
    await AiScript.Pause(700, token);
    await yielder(new BitAiStatusEvent(null));

    const string first =
        "Interactive Server keeps component state on the server and streams UI diffs over a SignalR circuit.";

    await foreach (var evt in AiScript.Write(first, 4, 12, token)) await yielder(evt);

    // Anchored to the sentence it supports, by character offsets into the text.
    await yielder(new BitAiCitationEvent(new BitAiCitation("c1", source)
    {
        StartIndex = 0,
        EndIndex = first.Length,
        Quote = "Interactive Server renders the component on the server and sends UI updates over a circuit.",
    }));

    // ... the next sentence, cited to the second source the same way.
    await yielder(new BitAiMessageCompletedEvent());
}));
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoContentTransports.cs", example1TransportCode),
    ];
}
