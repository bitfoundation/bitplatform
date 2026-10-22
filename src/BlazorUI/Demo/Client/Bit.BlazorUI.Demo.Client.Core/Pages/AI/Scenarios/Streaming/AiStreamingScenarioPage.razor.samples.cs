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
""";

    private readonly string example1CsharpCode = """
// A session of its own, over the transport this scenario streams from.
private readonly BitAiChatSession session = new(AiDemoTransports.Markdown());

private readonly string[] suggestions = ["Compare the render modes"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """"
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

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoTransports.cs", example1TransportCode),
    ];
}
