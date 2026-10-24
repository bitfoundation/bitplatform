namespace Bit.BlazorUI;

/// <summary>
/// Settings for a <see cref="BitAiChatSession"/>.
/// </summary>
/// <remarks>
/// Every setting has a defensible default, so a session created with none of them behaves the way a
/// modern chat product does: tools execute automatically, destructive ones ask first, and a runaway
/// tool loop is bounded.
/// </remarks>
public sealed record BitAiChatSessionOptions
{
    private readonly BitAiTextPacingOptions? _pacing = BitAiTextPacingOptions.Default;
    private readonly TimeSpan _toolResultHoldTime = BitAiConversationStreamProcessor.DefaultToolResultHoldTime;

    /// <summary>Generation settings passed to the transport on every request.</summary>
    public BitAiRequestOptions? RequestOptions { get; init; }

    /// <summary>
    /// Whether the session executes tool calls itself. Turn this off when the transport runs tools
    /// on the server and reports them purely as events.
    /// </summary>
    public bool ExecuteTools { get; init; } = true;

    /// <summary>
    /// How many times the session will feed tool results back and ask the model again.
    /// </summary>
    /// <remarks>
    /// The bound exists because a model that keeps calling tools will otherwise spend money
    /// indefinitely. Ten is generous for a chat turn; agent frameworks that legitimately need more
    /// should raise it deliberately.
    /// </remarks>
    public int MaxToolRoundtrips { get; init; } = 10;

    /// <summary>
    /// How long a tool result the transport reports before the call it answers is held for that call to start. The
    /// default is ten seconds.
    /// </summary>
    /// <remarks>
    /// A call that starts in time is given the result and is not run again. When none has started by then, the result
    /// is dropped: an error part carrying a <see cref="BitAiOrphanedToolResultException"/> is added to the message it
    /// arrived for, and the session logs it. Read at the start of every turn
    /// (<see cref="BitAiConversationStreamProcessor.ToolResultHoldTime"/>).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public TimeSpan ToolResultHoldTime
    {
        get => _toolResultHoldTime;
        init
        {
            // Validated here so a misconfiguration fails at startup, not on the first reply.
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _toolResultHoldTime = value;
        }
    }

    /// <summary>
    /// Evens out bursty text output. Set to <see langword="null"/> to render deltas exactly as the
    /// transport produces them.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The pacing settings contradict each other.</exception>
    public BitAiTextPacingOptions? Pacing
    {
        get => _pacing;
        init
        {
            // Validated here so a misconfiguration fails at startup, not on the first reply.
            value?.Validate();
            _pacing = value;
        }
    }

    /// <summary>
    /// How long to wait for the whole turn, including tool round trips. Null means no limit beyond
    /// what the transport enforces.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Derives a conversation title from the first user message when the conversation has none.
    /// </summary>
    public bool AutoTitle { get; init; } = true;

    /// <summary>
    /// Whether a failed turn leaves a retryable error on the assistant message rather than throwing.
    /// </summary>
    /// <remarks>
    /// Either way the assistant message ends up failed, with the error on it, and the generation is closed; this only
    /// decides whether the exception reaches the caller of the turn as well.
    /// </remarks>
    public bool CatchTransportExceptions { get; init; } = true;

    /// <summary>
    /// The text of the errors the session writes itself: a stopped or timed-out turn, a declined tool call, the tool loop
    /// limit. <see langword="null"/> uses the strings of the registered UI options (<see cref="BitAiUiOptions.Strings"/>)
    /// where the session is created by the factory or a <see cref="BitAiChat"/>, else <see cref="BitAiStrings.Default"/>.
    /// </summary>
    public BitAiStrings? Strings { get; init; }
}
