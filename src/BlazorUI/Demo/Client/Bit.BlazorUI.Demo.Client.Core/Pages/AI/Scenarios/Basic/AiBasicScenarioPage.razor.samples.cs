namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Basic;

public partial class AiBasicScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask me anything"
               EmptyBody="This demo echoes what you send, one character at a time."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private BitAiChatSession? session;

private readonly string[] suggestions =
[
    "What can these components do?",
    "Show me streaming",
    "Explain render modes",
];

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

// A session over the transport registered with AddBitBlazorUIAIServices.
protected override void OnInitialized() => session = SessionFactory.Create();

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());
});
""";

    private const string example1TransportCode = """
// The smallest useful transport: it streams the prompt back. A real one wraps a provider's SDK and
// turns its stream into the same events.
public static IBitAiTransport Echo() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    var reply = $"You said: {AiScript.LastUserText(request)}";

    await yielder(new BitAiMessageStartedEvent());
    await foreach (var evt in AiScript.Write(reply, 3, 12, token)) await yielder(evt);
    await yielder(AiScript.Usage(request, reply));
    await yielder(new BitAiMessageCompletedEvent());
}));

public sealed class AiScriptedTransport(Func<BitAiRequest, CancellationToken, IAsyncEnumerable<BitAiEvent>> script)
    : IBitAiTransport
{
    public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default) =>
        script(request, cancellationToken);
}
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoTransports.cs", example1TransportCode),
    ];
}
