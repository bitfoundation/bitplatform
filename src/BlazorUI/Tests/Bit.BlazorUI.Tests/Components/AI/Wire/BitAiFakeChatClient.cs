using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

/// <summary>
/// A chat client that streams a scripted reply per call and records what it was asked.
/// </summary>
internal sealed class BitAiFakeChatClient : IChatClient
{
    private readonly Func<int, IReadOnlyList<ChatMessage>, IEnumerable<ChatResponseUpdate>> _script;

    public BitAiFakeChatClient(params ChatResponseUpdate[] updates) => _script = (_, _) => updates;

    public BitAiFakeChatClient(Func<int, IReadOnlyList<ChatMessage>, IEnumerable<ChatResponseUpdate>> script) => _script = script;

    public List<IReadOnlyList<ChatMessage>> Messages { get; } = [];

    public List<ChatOptions?> Options { get; } = [];

    public Exception? Throw { get; set; }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        Messages.Add(list);
        Options.Add(options);

        foreach (var update in _script(Messages.Count - 1, list))
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }

        if (Throw is not null) throw Throw;
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }

    public static ChatResponseUpdate Text(string text) => new(ChatRole.Assistant, text);

    public static ChatResponseUpdate Of(params AIContent[] contents) => new(ChatRole.Assistant, contents);
}
