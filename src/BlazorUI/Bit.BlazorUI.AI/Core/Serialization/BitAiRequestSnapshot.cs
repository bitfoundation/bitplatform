namespace Bit.BlazorUI;

/// <summary>
/// A <see cref="BitAiRequest"/> as data: what a transport sends to a server that generates the reply, in the wire format
/// of <see cref="BitAiWireSerializer"/>.
/// </summary>
/// <remarks>
/// <para>
/// The messages are the same snapshots a persisted conversation holds (<see cref="BitAiMessageSnapshot"/>), so a server
/// reads the transcript - the text, the attachments, the tool calls and their results - exactly as a conversation store
/// would, and gets live <see cref="BitAiMessage"/> objects back from <see cref="BitAiWireSerializer.ToRequest"/>.
/// </para>
/// <para>
/// Everything in it comes from the browser, so a server treats it as untrusted input: the options and tool definitions
/// are what the client asks for, never what the server has to grant (see
/// <see cref="BitAiChatClientExtensions.ToChatOptions"/>, where the server's own options win).
/// </para>
/// </remarks>
public sealed record BitAiRequestSnapshot
{
    /// <summary>The conversation the request belongs to.</summary>
    public string? ConversationId { get; init; }

    /// <summary>The transcript the reply continues, oldest first.</summary>
    public required IReadOnlyList<BitAiMessageSnapshot> Messages { get; init; }

    /// <summary>The generation settings the client asks for.</summary>
    public BitAiRequestOptions? Options { get; init; }

    /// <summary>The tools the client can run, which the model may call.</summary>
    public IReadOnlyList<BitAiToolDefinition>? Tools { get; init; }

    /// <summary>
    /// <see cref="BitAiRequest.Context"/>, each value as the JSON it was written as.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? Context { get; init; }
}
