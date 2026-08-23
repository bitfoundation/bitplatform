using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// A scripted <see cref="IChatClient"/> that stands in for the language model.
/// <para>
/// This is not a mock of one of the template's own interfaces: <see cref="IChatClient"/> is the
/// <c>Microsoft.Extensions.AI</c> abstraction every agent is built on (See <c>AddAppAIAgents</c>), so replacing the
/// registration is the one seam that removes the model - and only the model. <c>AsAIAgent</c> still wraps whatever
/// client it is given in a <c>FunctionInvokingChatClient</c>, so the agent under test keeps its real seeded system
/// prompt, its real tool set and its real streaming pipeline.
/// </para>
/// <para>
/// Two things make the tests deterministic. <see cref="ReceivedConversations"/> records the exact message list handed
/// to the model on every call - which is the only place the chatbot's private history is observable - and
/// <see cref="PauseAfterFirstChunk"/> stops a stream half way, so a test can interrupt a message at a known point
/// rather than racing the wall clock.
/// </para>
/// </summary>
public sealed class TestChatClient : IChatClient
{
    private int streamingCallCount;
    private readonly List<ChatMessage[]> receivedConversations = [];

    /// <summary>
    /// Every conversation the chatbot has sent to the model so far, oldest first. Each entry is the complete message
    /// list of one call, starting with the <c>### Variables:</c> system message the chatbot prepends.
    /// </summary>
    public IReadOnlyList<ChatMessage[]> ReceivedConversations
    {
        get { lock (receivedConversations) return [.. receivedConversations]; }
    }

    /// <summary>
    /// The chunks the streamed answer is delivered in, per streaming call (0 is the first one). Splitting an answer
    /// across several chunks is what lets a test assert the chatbot reassembles it.
    /// </summary>
    public Func<int, string[]> StreamingChunks { get; set; } = _ => ["Hi, how can I help?"];

    /// <summary>
    /// Takes over from <see cref="StreamingChunks"/> when a test needs to emit something other than text - in
    /// practice a <c>FunctionCallContent</c>, which is how a model asks for a tool.
    /// <para>
    /// Emitting one is not a shortcut around the tool: <c>AsAIAgent</c> puts a <c>FunctionInvokingChatClient</c> in
    /// front of this client, so the call is really dispatched to the real <c>AppChatbot</c> method with the real
    /// arguments, its result is appended to the conversation, and this client is called a second time to produce the
    /// answer the user finally reads.
    /// </para>
    /// <para>
    /// It is handed the conversation as well as the call index, so a script can branch on what it is looking at -
    /// "the tool result is already here, so answer" - rather than on a counter, which is both closer to how a model
    /// behaves and immune to a retried message shifting the counter.
    /// </para>
    /// </summary>
    public Func<int, ChatMessage[], ChatResponseUpdate[]>? StreamingUpdates { get; set; }

    /// <summary>
    /// When set, the stream stops after its first chunk and waits here. Tests never complete this task - they cancel
    /// the message's own <see cref="CancellationToken"/> instead, which is exactly what the hub does when a new user
    /// message arrives while the previous one is still streaming.
    /// </summary>
    public TaskCompletionSource? PauseAfterFirstChunk { get; set; }

    /// <summary>
    /// The answer to a non-streamed call. The follow-up suggestions agent asks for a strict json object, so the
    /// default is a valid (empty) <c>AiChatFollowUpList</c>.
    /// </summary>
    public string NonStreamingResponse { get; set; } = /*lang=json,strict*/ """{"FollowUpSuggestions":[]}""";

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversation = Capture(messages);

        var callIndex = Interlocked.Increment(ref streamingCallCount) - 1;

        var updates = StreamingUpdates?.Invoke(callIndex, conversation)
                      ?? [.. StreamingChunks(callIndex).Select(chunk => new ChatResponseUpdate(ChatRole.Assistant, chunk))];

        var isFirstChunk = true;

        foreach (var update in updates)
        {
            if (isFirstChunk is false && PauseAfterFirstChunk is not null)
            {
                // Throws OperationCanceledException the moment the test cancels the message, mid-answer.
                await PauseAfterFirstChunk.Task.WaitAsync(cancellationToken);
            }

            isFirstChunk = false;

            yield return update;
        }
    }

    /// <summary>
    /// When set, a non-streamed call waits here instead of answering. In practice that is the follow-up suggestions
    /// agent, which runs AFTER the answer's terminal marker has already gone out - so this holds a message in the
    /// one state a test could not otherwise reach: answered, but not finished. Tests never complete it; they send the
    /// next message, which is what cancels it, exactly as a user typing again does.
    /// </summary>
    public TaskCompletionSource? PauseNonStreamingResponse { get; set; }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Capture(messages);

        if (PauseNonStreamingResponse is not null)
        {
            // Throws OperationCanceledException the moment the hub cancels this message.
            await PauseNonStreamingResponse.Task.WaitAsync(cancellationToken);
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, NonStreamingResponse));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose() { }

    private ChatMessage[] Capture(IEnumerable<ChatMessage> messages)
    {
        // Materialized immediately: the chatbot hands over a spread of its own mutable list, so holding the
        // enumerable would let a later turn rewrite what an earlier call is supposed to have seen.
        ChatMessage[] conversation = [.. messages];

        lock (receivedConversations)
        {
            receivedConversations.Add(conversation);
        }

        return conversation;
    }
}
