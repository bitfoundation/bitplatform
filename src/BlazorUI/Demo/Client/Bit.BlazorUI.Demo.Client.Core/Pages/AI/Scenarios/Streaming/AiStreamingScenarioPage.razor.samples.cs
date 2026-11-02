namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Streaming;

public partial class AiStreamingScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Watch it arrive"
               EmptyBody="Send anything. The reply is a full Markdown document written progressively."
               Suggestions="suggestions" />
</div>

<aside aria-label="Events">
    <h4>Events</h4>
    <AiEventLog Session="session" />
</aside>
""";

    private readonly string example1CsharpCode = """
// A session of its own, over the transport this scenario streams from.
private readonly BitAiChatSession session = new(AiDemoTransports.Markdown());

private readonly string[] suggestions = ["Compare the render modes"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """"
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport Markdown() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    await yielder(new BitAiMessageStartedEvent());
    await yielder(new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = "demo-model-1", ContextWindow = 128_000 }));

    const string answer = """
        Blazor renders in two places, and the difference matters.

        ## Where the code runs

        | Mode | Runs on | First paint | Best for |
        | --- | --- | :---: | --- |
        | Server | The server, over a circuit | Fast | Data-heavy internal apps |
        | WebAssembly | The browser | Slower | Offline and low-latency work |

        ...
        """;

    // A few characters every 10 ms, the way a model streams.
    await foreach (var evt in AiScript.Write(answer, chunk: 5, delayMs: 10, token)) await yielder(evt);
    await yielder(AiScript.Usage(request, answer));
    await yielder(new BitAiMessageCompletedEvent());
}));
"""";

    private const string example1EventLogCode = """
@implements IAsyncDisposable

@* Every event the session applies, in order: a run of the same event is one line with a count. *@
@if (lines.Length == 0)
{
    <p>Nothing yet. Send a message.</p>
}
else
{
    <ul>
        @foreach (var line in lines)
        {
            <li><code>@line.Type</code>@(line.Count > 1 ? $" × {line.Count}" : null)</li>
        }
    </ul>
}

@code {
    private readonly List<Line> log = [];
    private Line[] lines = [];
    private BitAiRenderScheduler? renders;
    private BitAiChatSession? subscribed;

    [Parameter, EditorRequired] public BitAiChatSession Session { get; set; } = default!;

    // EventApplied is raised on the thread that applied the event, for every token: the log is drawn through a
    // scheduler, at most once every 100 ms, and only this component renders for it.
    protected override void OnInitialized() =>
        renders = new BitAiRenderScheduler(() => InvokeAsync(Refresh), TimeSpan.FromMilliseconds(100));

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(subscribed, Session)) return;

        if (subscribed is not null) subscribed.Processor.EventApplied -= OnEventApplied;
        subscribed = Session;
        subscribed.Processor.EventApplied += OnEventApplied;
    }

    private void OnEventApplied(BitAiEvent evt)
    {
        lock (log)
        {
            // Every reply starts a log of its own.
            if (evt is BitAiMessageStartedEvent) log.Clear();

            if (log.Count > 0 && log[^1].Type == evt.Type) log[^1] = log[^1] with { Count = log[^1].Count + 1 };
            else log.Add(new Line(evt.Type, 1));
        }

        renders?.Request();
    }

    private void Refresh()
    {
        lock (log) lines = [.. log];

        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (subscribed is not null) subscribed.Processor.EventApplied -= OnEventApplied;
        if (renders is not null) await renders.DisposeAsync();
    }

    private sealed record Line(string Type, int Count);
}
""";

    private const string example1PacingCode = """
// Every session paces text by default (BitAiTextPacingOptions.Default): a chunk every 16 ms, sized to drain what is
// buffered over 150 ms, between 1 and 48 characters. One session can be given other settings...
var session = new BitAiChatSession(transport, options: new BitAiChatSessionOptions
{
    Pacing = BitAiTextPacingOptions.Default with { TargetDrain = TimeSpan.FromMilliseconds(300), MaxCharsPerTick = 24 },
});

// ...or none, to render the deltas exactly as the transport produces them.
var unpaced = new BitAiChatSession(transport, options: new BitAiChatSessionOptions { Pacing = null });

// Every session the factory creates, in Program.cs.
builder.Services.AddBitBlazorUIAIServices(ai => ai.ConfigureSessions(options => options with
{
    Pacing = BitAiTextPacingOptions.Default with { TargetDrain = TimeSpan.FromMilliseconds(300) },
}));

// A stream consumed outside a session - relayed by a server endpoint, say - is paced with the same operator.
await foreach (var evt in transport.StreamAsync(request, cancellationToken).Paced(cancellationToken: cancellationToken))
{
    // Hand evt on.
}
""";

    private const string example1ReducerCode = """
// The reducer a session applies every event with, on its own: recorded events replayed into a conversation, to test
// a renderer or to show a reply a server pushed.
var conversation = new BitAiConversation();
var processor = new BitAiConversationStreamProcessor(conversation);

processor.BeginGeneration();
foreach (var evt in recordedEvents) processor.Apply(evt);
processor.EndGeneration();

// What the framework has no event for, a transport sends as a custom event, its name namespaced...
yield return new BitAiCustomEvent("acme:quota", 42);

// ...which the reducer applies nothing for, but raises through EventApplied like every other event.
session.Processor.EventApplied += evt =>
{
    if (evt is BitAiCustomEvent { Name: "acme:quota", Data: int remaining })
    {
        quotaLeft = remaining;
        _ = InvokeAsync(StateHasChanged);
    }
};
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoTransports.cs", example1TransportCode),
        new("AiEventLog.razor", example1EventLogCode),
        new("Text pacing", example1PacingCode, "csharp"),
        new("The reducer", example1ReducerCode, "csharp"),
    ];
}
