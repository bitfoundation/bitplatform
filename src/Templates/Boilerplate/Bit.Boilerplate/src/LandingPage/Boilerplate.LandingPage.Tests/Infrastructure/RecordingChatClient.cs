using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Boilerplate.LandingPage.Tests.Infrastructure;

public sealed class RecordingChatClient : IChatClient
{
    public ChatOptions? Options { get; private set; }

    public ChatMessage[] Messages { get; private set; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Options = options;
        Messages = [.. messages];

        await Task.Yield();
        yield return new(ChatRole.Assistant, "It costs ");
        yield return new(ChatRole.Assistant, "nothing.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
