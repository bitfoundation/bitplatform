using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// A conversation store that keeps everything in process memory.
/// </summary>
/// <remarks>
/// Useful for demos, tests, and Blazor Server apps where a conversation only has to outlive a page
/// navigation. It is not durable: restart the process and the conversations are gone. Implement
/// <see cref="IBitAiConversationStore"/> over a database for anything else.
/// </remarks>
public sealed class BitAiInMemoryConversationStore : IBitAiConversationStore
{
    private readonly ConcurrentDictionary<string, IBitAiConversation> _conversations = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<BitAiConversationSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var summaries = _conversations.Values
            .OrderByDescending(c => c.UpdatedAt)
            .Select(Summarize)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<BitAiConversationSummary>>(summaries);
    }

    /// <inheritdoc />
    public ValueTask<IBitAiConversation?> LoadAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        _conversations.TryGetValue(conversationId, out var conversation);
        return ValueTask.FromResult(conversation);
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(IBitAiConversation conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        _conversations[conversation.ConversationId] = conversation;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DeleteAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversationId);
        _conversations.TryRemove(conversationId, out _);
        return ValueTask.CompletedTask;
    }

    private static BitAiConversationSummary Summarize(IBitAiConversation conversation)
    {
        if (conversation is BitAiConversation typed) return typed.ToSummary();

        var preview = conversation.Messages.Count > 0
            ? conversation.Messages[^1].GetText()
            : null;

        if (preview is { Length: > 140 }) preview = string.Concat(preview.AsSpan(0, 140), "...");

        return new BitAiConversationSummary(
            conversation.ConversationId,
            conversation.Title,
            conversation.UpdatedAt,
            preview,
            conversation.Messages.Count);
    }
}
