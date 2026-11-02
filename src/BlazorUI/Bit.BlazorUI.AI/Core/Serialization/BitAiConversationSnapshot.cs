using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// A persistable, provider-independent picture of a whole conversation: messages, parts,
/// attachments, citations, artifacts with their version history, and agent runs.
/// </summary>
/// <remarks>
/// <para>
/// Plain data, no behaviour. This is what an <see cref="IBitAiConversationStore"/> implementation
/// writes to a database or a file, and what <see cref="BitAiConversationSerializer"/> turns back into a
/// live <see cref="BitAiConversation"/>. Every field maps onto a stable JSON shape so a conversation
/// saved by one version of the library can be read by the next.
/// </para>
/// <para>
/// Unknown part kinds survive the round trip: a part the library does not recognise is kept as a
/// <see cref="BitAiCustomJsonPart"/> carrying its raw payload, so an application-defined part is never
/// silently dropped on load.
/// </para>
/// </remarks>
public sealed record BitAiConversationSnapshot
{
    /// <summary>Schema version of the snapshot format.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Stable identifier.</summary>
    public required string ConversationId { get; init; }

    /// <summary>The title shown in a conversation list.</summary>
    public string? Title { get; init; }

    /// <summary>When the conversation was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the conversation last changed.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>The messages, in order.</summary>
    public IReadOnlyList<BitAiMessageSnapshot> Messages { get; init; } = [];

    /// <summary>The artifacts owned by the conversation.</summary>
    public IReadOnlyList<BitAiArtifactSnapshot> Artifacts { get; init; } = [];

    /// <summary>The agent runs owned by the conversation.</summary>
    public IReadOnlyList<BitAiAgentRunSnapshot> Runs { get; init; } = [];

    /// <summary>Application-defined values from <see cref="BitAiConversation.Extensions"/>.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Extensions { get; init; }
}

/// <summary>One message in a <see cref="BitAiConversationSnapshot"/>.</summary>
public sealed record BitAiMessageSnapshot
{
    /// <summary>Stable identifier, unique within its conversation.</summary>
    public required string MessageId { get; init; }

    /// <summary>Who authored the message.</summary>
    public BitAiRole Role { get; init; } = BitAiRole.User;

    /// <summary>When the message was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Where the message is in its lifecycle.</summary>
    public BitAiMessageStatus Status { get; init; } = BitAiMessageStatus.Complete;

    /// <summary>How the message was generated.</summary>
    public BitAiGenerationMetadata? Metadata { get; init; }

    /// <summary>The failure that ended the message, when it failed.</summary>
    public BitAiError? Error { get; init; }

    /// <summary>User feedback on the message.</summary>
    public BitAiFeedbackKind Feedback { get; init; }

    /// <summary>Free-text feedback.</summary>
    public string? FeedbackComment { get; init; }

    /// <summary>Identifier of the agent run that produced the message, if any.</summary>
    public string? RunId { get; init; }

    /// <summary>The ordered parts.</summary>
    public IReadOnlyList<BitAiPartSnapshot> Parts { get; init; } = [];

    /// <summary>Files carried by the message.</summary>
    public IReadOnlyList<BitAiAttachment> Attachments { get; init; } = [];

    /// <summary>Citations attached to the message.</summary>
    public IReadOnlyList<BitAiCitation> Citations { get; init; } = [];

    /// <summary>Application-defined values from <see cref="BitAiMessage.Extensions"/>.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Extensions { get; init; }
}

/// <summary>
/// One part in a <see cref="BitAiMessageSnapshot"/>. A single flat shape covers every built-in kind;
/// only the fields relevant to <see cref="Kind"/> are populated and serialized.
/// </summary>
public sealed record BitAiPartSnapshot
{
    /// <summary>Stable identifier, unique within its message.</summary>
    public required string PartId { get; init; }

    /// <summary>The part kind discriminator, as <see cref="IBitAiMessagePart.Kind"/>.</summary>
    public required string Kind { get; init; }

    /// <summary>Text content for text-like parts (text, markdown, reasoning, code).</summary>
    public string? Text { get; init; }

    /// <summary>Programming language for code parts.</summary>
    public string? Language { get; init; }

    /// <summary>File name for code parts.</summary>
    public string? FileName { get; init; }

    /// <summary>Provider signature for reasoning parts.</summary>
    public string? Signature { get; init; }

    /// <summary>When a reasoning part started.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>When a reasoning part or tool call completed.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Image URL for image parts.</summary>
    public string? Url { get; init; }

    /// <summary>Alternative text for image parts.</summary>
    public string? Alt { get; init; }

    /// <summary>Caption for image parts.</summary>
    public string? Caption { get; init; }

    /// <summary>Width for image parts.</summary>
    public int? Width { get; init; }

    /// <summary>Height for image parts.</summary>
    public int? Height { get; init; }

    /// <summary>The attachment for file parts.</summary>
    public BitAiAttachment? Attachment { get; init; }

    /// <summary>The citation for citation parts.</summary>
    public BitAiCitation? Citation { get; init; }

    /// <summary>Citation identifiers for sources parts.</summary>
    public IReadOnlyList<string>? CitationIds { get; init; }

    /// <summary>Tool call identifier for tool-call and tool-result parts.</summary>
    public string? ToolCallId { get; init; }

    /// <summary>Tool name for tool-call and tool-result parts.</summary>
    public string? ToolName { get; init; }

    /// <summary>The tool definition, when known.</summary>
    public BitAiToolDefinition? Definition { get; init; }

    /// <summary>Parsed arguments for tool-call parts.</summary>
    public JsonElement? Arguments { get; init; }

    /// <summary>Raw argument text for tool-call parts, when it differs from <see cref="Arguments"/>.</summary>
    public string? RawArguments { get; init; }

    /// <summary>Lifecycle status for tool-call parts.</summary>
    public BitAiToolCallStatus? ToolStatus { get; init; }

    /// <summary>Approval decision for tool-call parts.</summary>
    public BitAiApprovalDecision? Approval { get; init; }

    /// <summary>Why approval was requested, for tool-call parts.</summary>
    public string? ApprovalReason { get; init; }

    /// <summary>When a tool call started executing.</summary>
    public DateTimeOffset? ToolStartedAt { get; init; }

    /// <summary>The tool outcome for tool-call and tool-result parts.</summary>
    public BitAiToolOutcomeSnapshot? Outcome { get; init; }

    /// <summary>Artifact identifier for artifact parts.</summary>
    public string? ArtifactId { get; init; }

    /// <summary>Artifact version for artifact parts.</summary>
    public int? ArtifactVersion { get; init; }

    /// <summary>Schema name for structured-data parts.</summary>
    public string? SchemaName { get; init; }

    /// <summary>Component name for generative UI parts.</summary>
    public string? ComponentName { get; init; }

    /// <summary>
    /// Structured payload: the value of a structured-data part, the props of a generative UI part,
    /// or the data of a custom part.
    /// </summary>
    public JsonElement? Data { get; init; }

    /// <summary>The error for error parts.</summary>
    public BitAiError? Error { get; init; }
}

/// <summary>The persistable form of a <see cref="BitAiToolOutcome"/>.</summary>
public sealed record BitAiToolOutcomeSnapshot
{
    /// <summary>Whether the call succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>The structured result.</summary>
    public JsonElement? Value { get; init; }

    /// <summary>The human-readable rendering.</summary>
    public string? Text { get; init; }

    /// <summary>The failure, when unsuccessful.</summary>
    public BitAiError? Error { get; init; }

    /// <summary>Sources the tool consulted.</summary>
    public IReadOnlyList<BitAiCitation>? Citations { get; init; }

    /// <summary>How long the call took.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Converts an outcome to its snapshot.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The snapshot.</returns>
    public static BitAiToolOutcomeSnapshot From(BitAiToolOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return new BitAiToolOutcomeSnapshot
        {
            IsSuccess = outcome.IsSuccess,
            Value = outcome.Value,
            Text = outcome.Text,
            Error = outcome.Error,
            Citations = outcome.Citations,
            Duration = outcome.Duration,
        };
    }

    /// <summary>Rebuilds the outcome.</summary>
    /// <returns>The outcome.</returns>
    public BitAiToolOutcome ToOutcome()
    {
        var outcome = IsSuccess
            ? Value is { } value ? BitAiToolOutcome.Success(value, Text) : BitAiToolOutcome.FromText(Text ?? string.Empty)
            : BitAiToolOutcome.Failure(Error ?? new BitAiError("The tool failed.") { Kind = BitAiErrorKind.Tool });

        return outcome with { Text = Text ?? outcome.Text, Citations = Citations, Duration = Duration };
    }
}

/// <summary>One artifact in a <see cref="BitAiConversationSnapshot"/>.</summary>
public sealed record BitAiArtifactSnapshot
{
    /// <summary>Stable identifier.</summary>
    public required string ArtifactId { get; init; }

    /// <summary>The artifact kind.</summary>
    public BitAiArtifactKind Kind { get; init; } = BitAiArtifactKind.Document;

    /// <summary>Display title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The current content.</summary>
    public string? Content { get; init; }

    /// <summary>Language or format hint.</summary>
    public string? Language { get; init; }

    /// <summary>Addressable URL for binary artifacts.</summary>
    public string? Url { get; init; }

    /// <summary>Current version number.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Whether content was still streaming when the snapshot was taken.</summary>
    public bool IsStreaming { get; init; }

    /// <summary>When the artifact was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the current version was produced.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Version history.</summary>
    public IReadOnlyList<BitAiArtifactVersion> Versions { get; init; } = [];

    /// <summary>Application-defined values.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Metadata { get; init; }
}

/// <summary>One agent run in a <see cref="BitAiConversationSnapshot"/>.</summary>
public sealed record BitAiAgentRunSnapshot
{
    /// <summary>Stable identifier.</summary>
    public required string RunId { get; init; }

    /// <summary>The agent name.</summary>
    public string? AgentName { get; init; }

    /// <summary>What the run set out to do.</summary>
    public string? Goal { get; init; }

    /// <summary>Lifecycle status.</summary>
    public BitAiAgentRunStatus Status { get; init; }

    /// <summary>Pending question for the user, when waiting for input.</summary>
    public string? PendingPrompt { get; init; }

    /// <summary>Suggested answers for the pending question.</summary>
    public IReadOnlyList<string>? PendingSuggestions { get; init; }

    /// <summary>When the run started.</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>When the run ended.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Token usage accumulated across the run.</summary>
    public BitAiTokenUsage Usage { get; init; }

    /// <summary>The failure that ended the run, when it failed.</summary>
    public BitAiError? Error { get; init; }

    /// <summary>The steps, in order.</summary>
    public IReadOnlyList<BitAiAgentStepSnapshot> Steps { get; init; } = [];
}

/// <summary>One step in an <see cref="BitAiAgentRunSnapshot"/>.</summary>
public sealed record BitAiAgentStepSnapshot
{
    /// <summary>Stable identifier.</summary>
    public required string StepId { get; init; }

    /// <summary>Display title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Longer description.</summary>
    public string? Detail { get; init; }

    /// <summary>Lifecycle status.</summary>
    public BitAiAgentStepStatus Status { get; init; }

    /// <summary>What the step produced.</summary>
    public string? Summary { get; init; }

    /// <summary>The failure, when the step failed.</summary>
    public BitAiError? Error { get; init; }

    /// <summary>When the step started.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>When the step ended.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Tool calls made during the step.</summary>
    public IReadOnlyList<string> ToolCallIds { get; init; } = [];
}
