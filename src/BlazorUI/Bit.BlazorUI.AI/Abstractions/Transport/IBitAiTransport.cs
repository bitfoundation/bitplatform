namespace Bit.BlazorUI;

/// <summary>
/// Everything a transport needs to produce one response.
/// </summary>
public sealed class BitAiRequest
{
    /// <summary>Creates a request.</summary>
    /// <param name="messages">The conversation so far, oldest first.</param>
    public BitAiRequest(IReadOnlyList<IBitAiMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        Messages = messages;
    }

    /// <summary>The conversation so far, oldest first.</summary>
    public IReadOnlyList<IBitAiMessage> Messages { get; }

    /// <summary>Identifier of the conversation, for transports that keep server-side state.</summary>
    public string? ConversationId { get; init; }

    /// <summary>Generation settings.</summary>
    public BitAiRequestOptions? Options { get; init; }

    /// <summary>
    /// The tools the model may call. Supplied by the framework from the tool registry so a transport
    /// does not have to know how tools were configured.
    /// </summary>
    public IReadOnlyList<BitAiToolDefinition> Tools { get; init; } = [];

    /// <summary>Scoped services, for transports that resolve per-request dependencies.</summary>
    public IServiceProvider? Services { get; init; }

    /// <summary>Application-defined values threaded through from the caller.</summary>
    public IReadOnlyDictionary<string, object?>? Context { get; init; }
}

/// <summary>
/// The boundary between the UI framework and whatever actually produces AI responses.
/// </summary>
/// <remarks>
/// <para>
/// This is the one interface an application must implement to use the framework. It can wrap a
/// provider SDK, call an HTTP endpoint over server-sent events, run a local model, drive a
/// multi-agent orchestrator, or replay a fixture in a test. The UI cannot tell the difference,
/// which is the point: no component in this framework references a provider type.
/// </para>
/// <para>
/// Implementations should be safe to call concurrently, should honour the cancellation token
/// promptly so the stop button works, and should translate failures into
/// <see cref="BitAiErrorEvent"/> rather than throwing wherever the failure is expected.
/// </para>
/// </remarks>
/// <example>
/// A transport that streams a canned reply:
/// <code>
/// public sealed class EchoTransport : IBitAiTransport
/// {
///     public async IAsyncEnumerable&lt;BitAiEvent&gt; StreamAsync(
///         BitAiRequest request,
///         [EnumeratorCancellation] CancellationToken cancellationToken = default)
///     {
///         yield return new BitAiMessageStartedEvent();
///         foreach (var word in request.Messages[^1].GetText().Split(' '))
///         {
///             await Task.Delay(30, cancellationToken);
///             yield return new BitAiTextDeltaEvent(word + " ");
///         }
///         yield return new BitAiMessageCompletedEvent();
///     }
/// }
/// </code>
/// </example>
public interface IBitAiTransport
{
    /// <summary>
    /// Produces the events for one response.
    /// </summary>
    /// <param name="request">The conversation and settings.</param>
    /// <param name="cancellationToken">
    /// Cancelled when the user stops generation. Honour it promptly: the stop button is only as
    /// responsive as the transport.
    /// </param>
    /// <returns>The event stream. Enumeration begins when the framework starts consuming it.</returns>
    IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persistence for conversations.
/// </summary>
/// <remarks>
/// Optional. Applications that keep conversations in memory for the lifetime of a page never need
/// it; applications with a conversation sidebar implement it over their own storage.
/// </remarks>
public interface IBitAiConversationStore
{
    /// <summary>
    /// Lists conversation summaries, newest first.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The summaries.</returns>
    ValueTask<IReadOnlyList<BitAiConversationSummary>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads one conversation in full.
    /// </summary>
    /// <param name="conversationId">The conversation to load.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The conversation, or <see langword="null"/> when it does not exist.</returns>
    ValueTask<IBitAiConversation?> LoadAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces a conversation.
    /// </summary>
    /// <param name="conversation">The conversation to persist.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the write is durable.</returns>
    ValueTask SaveAsync(IBitAiConversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation to delete.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the deletion is durable.</returns>
    ValueTask DeleteAsync(string conversationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The lightweight projection of a conversation used to render a list without loading messages.
/// </summary>
/// <param name="ConversationId">The conversation identifier.</param>
/// <param name="Title">The display title.</param>
/// <param name="UpdatedAt">When the conversation last changed.</param>
/// <param name="Preview">A snippet of the last message.</param>
/// <param name="MessageCount">How many messages it holds.</param>
public sealed record BitAiConversationSummary(
    string ConversationId,
    string? Title,
    DateTimeOffset UpdatedAt,
    string? Preview = null,
    int MessageCount = 0);
