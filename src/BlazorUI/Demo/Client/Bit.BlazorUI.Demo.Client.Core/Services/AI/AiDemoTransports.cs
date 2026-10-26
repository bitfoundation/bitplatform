namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The scripted responses behind the basic AI scenarios: an echo, a long Markdown answer, and visible reasoning.
/// </summary>
public static class AiDemoTransports
{
    private const string ModelId = "demo-model-1";

    /// <summary>Echoes the prompt, the smallest useful transport.</summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Echo() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        var said = AiScript.LastUserMessage(request);
        var text = said?.GetText() ?? string.Empty;
        var files = said?.Attachments.Count ?? 0;

        // A message can carry files and no text at all, and an echo of nothing would read as no answer.
        var reply = text.Length > 0 ? $"You said: {text}"
                  : files == 1 ? "You sent 1 file."
                  : $"You sent {files} files.";

        await yielder(new BitAiMessageStartedEvent());
        await foreach (var evt in AiScript.Write(reply, 3, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, reply));
        await yielder(new BitAiMessageCompletedEvent());
    }));

    /// <summary>A full Markdown answer: headings, lists, a table, and code.</summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Markdown() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());
        await yielder(new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = ModelId, ContextWindow = 128_000 }));

        const string answer = """
            Blazor renders in two places, and the difference matters.

            ## Where the code runs

            | Mode | Runs on | First paint | Best for |
            | --- | --- | :---: | --- |
            | Server | The server, over a circuit | Fast | Data-heavy internal apps |
            | WebAssembly | The browser | Slower | Offline and low-latency work |

            ### What that means for streaming

            Every update on **Server** is a diff sent down a network connection, so the cost of
            rendering is measured in *messages*, not milliseconds. That is why the AI components
            coalesce renders instead of drawing every token:

            ```csharp
            var scheduler = new BitAiRenderScheduler(
                () => InvokeAsync(StateHasChanged),
                TimeSpan.FromMilliseconds(50));

            foreach (var token in tokens)
            {
                part.Append(token);
                scheduler.Request();
            }
            ```

            - On WebAssembly this saves CPU.
            - On Server it saves bandwidth, which matters more.
            - Either way the user sees the same thing.

            > Rendering more often than the eye can follow is not smoother. It is just louder.
            """;

        await foreach (var evt in AiScript.Write(answer, 5, 10, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, answer));
        await yielder(new BitAiMessageCompletedEvent());
    }));

    /// <summary>Visible reasoning before the answer.</summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Reasoning() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());

        const string thinking =
            "The user is asking about the cost of a round trip. I should separate the fixed cost of " +
            "the connection from the per-token cost, and give a number rather than an adjective.";

        // A word every 70 ms: about two seconds of thinking, long enough to watch the panel stream open before it
        // collapses into "Thought for ...".
        await foreach (var evt in AiScript.Think(thinking, 70, token)) await yielder(evt);
        await AiScript.Pause(250, token);

        const string answer = """
            Two costs are worth separating.

            **Connection setup** is roughly constant: one TLS handshake, one request, and whatever
            the provider spends before the first token. Call it 200 to 600 milliseconds.

            **Generation** scales with output length at the model throughput, so a 600 token answer
            at 80 tokens a second is about 7.5 seconds regardless of how fast your network is.

            Optimising the first without measuring the second is the usual mistake.
            """;

        await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, answer));
        await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.Stop));
    }));
}
