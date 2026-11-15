using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Bit.BlazorUI.Demo.Server.Services.AI;

/// <summary>
/// The model behind the "Streaming from a server" AI scenario (/ai/scenarios/server): an <see cref="IChatClient"/> that
/// streams a scripted reply the way a provider's does, so the scenario runs the real server pipeline - a
/// <see cref="FunctionInvokingChatClient"/>, the package's IChatClient adapter and its SSE endpoint - with no API key and
/// nothing a visitor can spend.
/// </summary>
/// <remarks>
/// The first round asks for the server's clock through a function call, which the function invoking client runs; the
/// second, which has the result, answers with it. An app registers its provider's chat client in its place
/// (<c>services.AddChatClient(...)</c>), and nothing else changes.
/// </remarks>
public sealed class DemoServerChatClient : IChatClient
{
    public const string ClockFunctionName = "get_server_time";

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = messages.ToList();
        var prompt = history.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        var clock = history.SelectMany(m => m.Contents).OfType<FunctionResultContent>().LastOrDefault(r => r.CallId.StartsWith("clock-", StringComparison.Ordinal));

        if (clock is null || history.Last().Role != ChatRole.Tool)
        {
            yield return Update(new TextReasoningContent("The visitor wants an answer from the server. I'll read the server's clock first."));

            foreach (var chunk in Chunks("Let me ask the server what time it is."))
            {
                await Task.Delay(25, cancellationToken);
                yield return Update(new TextContent(chunk));
            }

            yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent($"clock-{Guid.NewGuid():N}", ClockFunctionName, new Dictionary<string, object?>())])
            {
                ModelId = "demo-server-model",
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        var reply = $" The server's clock reads {clock.Result}. You said: \"{prompt}\" - every word of this reply was streamed by an IChatClient on the server and mapped to BitAiEvents by Bit.BlazorUI.AI, with no mapping written by hand.";
        foreach (var chunk in Chunks(reply))
        {
            await Task.Delay(25, cancellationToken);
            yield return Update(new TextContent(chunk));
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails
        {
            InputTokenCount = history.Sum(m => m.Text.Length) / 4,
            OutputTokenCount = reply.Length / 4,
        })])
        {
            ModelId = "demo-server-model",
            FinishReason = ChatFinishReason.Stop,
        };
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }

    private static ChatResponseUpdate Update(AIContent content) => new(ChatRole.Assistant, [content]) { ModelId = "demo-server-model" };

    private static IEnumerable<string> Chunks(string text, int size = 6)
    {
        for (var i = 0; i < text.Length; i += size) yield return text.Substring(i, Math.Min(size, text.Length - i));
    }
}
