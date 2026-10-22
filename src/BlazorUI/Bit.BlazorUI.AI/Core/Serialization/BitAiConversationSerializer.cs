using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// Hooks for persisting application-defined parts and for customising the JSON produced.
/// </summary>
public sealed class BitAiConversationSerializerOptions
{
    private const string ReflectionFallbackUnreferencedCode = "Serializes the app's own types held in the loosely typed values of a conversation (its extensions and the metadata dictionaries) with the reflection-based System.Text.Json serializer, which needs members of those types that trimming can remove. Add the app's own JsonSerializerContext to the settings of CreateJsonOptions instead.";

    private const string ReflectionFallbackDynamicCode = "Serializes the app's own types held in the loosely typed values of a conversation (its extensions and the metadata dictionaries) with the reflection-based System.Text.Json serializer, which can need code generated at run time that native AOT cannot make. Add the app's own JsonSerializerContext to the settings of CreateJsonOptions instead.";

    /// <summary>The default options.</summary>
    public static BitAiConversationSerializerOptions Default { get; } = new();

    /// <summary>
    /// Converts an application-defined part to a snapshot. Return <see langword="null"/> to fall
    /// back to the built-in handling, which keeps any <see cref="IBitAiTextualPart"/> text and otherwise
    /// drops the part.
    /// </summary>
    public Func<BitAiMessagePart, BitAiPartSnapshot?>? SnapshotPart { get; init; }

    /// <summary>
    /// Rebuilds an application-defined part from its snapshot. Return <see langword="null"/> to fall
    /// back to the built-in handling, which produces a <see cref="BitAiCustomJsonPart"/>.
    /// </summary>
    public Func<BitAiPartSnapshot, BitAiMessagePart?>? RestorePart { get; init; }

    /// <summary>JSON settings used by <see cref="BitAiConversationSerializer.ToJson(BitAiConversation, BitAiConversationSerializerOptions?)"/> and <see cref="BitAiConversationSerializer.FromJson"/>.</summary>
    /// <remarks>
    /// <para>
    /// The serializer takes the metadata of every type from the <see cref="JsonSerializerOptions.TypeInfoResolver"/> of
    /// these settings and never reflects on its own, so persisting a conversation is safe in an app that is trimmed or
    /// compiled ahead of time. Settings with no resolver at all (built by hand) are given the package's.
    /// </para>
    /// <para>
    /// A loosely typed value - an entry of <see cref="BitAiConversation.Extensions"/>,
    /// <see cref="BitAiMessage.Extensions"/>, or of one of the <c>Metadata</c> dictionaries (of an artifact, an
    /// attachment, a citation source, a tool definition) or <see cref="BitAiGenerationMetadata.Extra"/> - is written by
    /// the type it holds, through the metadata of these settings. The package's covers strings, booleans, numbers,
    /// dates, identifiers, JSON values, and dictionaries, lists and arrays of them. A value of a type the settings have no
    /// metadata for fails the serialization with a <see cref="NotSupportedException"/>, wherever it is: that is what
    /// System.Text.Json does with any type its resolver does not know. For the app's own types, either add the app's
    /// <see cref="JsonSerializerContext"/> to the settings (see <see cref="CreateJsonOptions"/>), which is safe to trim,
    /// or use the settings of <see cref="CreateJsonOptionsWithReflectionFallback"/> in an app that is not trimmed.
    /// </para>
    /// </remarks>
    public JsonSerializerOptions Json { get; init; } = CreateJsonOptions();

    /// <summary>Creates the JSON settings the serializer uses by default: web naming, enums as strings, nulls omitted.</summary>
    /// <returns>The settings.</returns>
    /// <remarks>
    /// <para>
    /// The metadata comes from the package's source-generated context, and the package's enumerations are written as
    /// camelCase strings by a <see cref="JsonStringEnumConverter{TEnum}"/> each. A snapshot property the JSON leaves out
    /// keeps the value it starts with (<c>schemaVersion</c> 1, an empty list, an attachment's
    /// <see cref="BitAiAttachmentStatus.Ready"/> status, ...), as the reflection-based serializer left it.
    /// </para>
    /// <para>
    /// The settings are not locked until first used, so they can be extended for the app's own types before they are
    /// handed over: <c>options.TypeInfoResolverChain.Add(AppJsonContext.Default)</c> for the metadata, and a
    /// <see cref="JsonStringEnumConverter{TEnum}"/> in <see cref="JsonSerializerOptions.Converters"/> for an
    /// enumeration of the app that should be written as a string too.
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions CreateJsonOptions() => new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter<BitAiMessageStatus>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiFeedbackKind>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiFinishReason>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiErrorKind>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiAttachmentKind>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiAttachmentStatus>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiSourceKind>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiToolRisk>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiToolCallStatus>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiApprovalDecision>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiAgentRunStatus>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<BitAiAgentStepStatus>(JsonNamingPolicy.CamelCase),
        },
        WriteIndented = false,
        TypeInfoResolver = BitAiSnapshotJson.Resolver,
    };

    /// <summary>
    /// Creates the JSON settings the serializer used before it was made safe to trim: those of
    /// <see cref="CreateJsonOptions"/>, with every type they have no metadata for read by reflection and every
    /// enumeration written as a camelCase string.
    /// </summary>
    /// <returns>The settings.</returns>
    /// <remarks>
    /// <para>
    /// The opt-in for an app that keeps its own objects (and enumerations) in the extensions or the metadata of a
    /// conversation and is not trimmed:
    /// <c>new BitAiConversationSerializerOptions { Json = BitAiConversationSerializerOptions.CreateJsonOptionsWithReflectionFallback() }</c>.
    /// Those values are then written as they always were, and an extension value (or an artifact's metadata value) the
    /// settings cannot write at all - a delegate, a live object - is left out of the snapshot rather than failing it, as
    /// it always was. A value nested in the metadata of an attachment, a citation source, a tool definition or the
    /// generation metadata still fails the serialization then, as it always did.
    /// </para>
    /// <para>
    /// The package's own types keep taking their metadata from its source-generated context, which comes first and
    /// writes the same JSON; reflection only reaches what that does not cover. An app that is trimmed or compiled ahead
    /// of time adds its own <see cref="JsonSerializerContext"/> to the settings of <see cref="CreateJsonOptions"/>
    /// instead.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionFallbackUnreferencedCode)]
    [RequiresDynamicCode(ReflectionFallbackDynamicCode)]
    public static JsonSerializerOptions CreateJsonOptionsWithReflectionFallback()
    {
        var json = CreateJsonOptions();

        // Every other enumeration as a camelCase string too, as the one converter the serializer used to have wrote
        // them all; the package's own keep their converters, which come first and write the same.
        json.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        json.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

        return json;
    }
}

/// <summary>
/// Converts between live <see cref="BitAiConversation"/> objects and <see cref="BitAiConversationSnapshot"/>
/// data, and between snapshots and JSON.
/// </summary>
/// <remarks>
/// <para>
/// This is the missing piece between <see cref="IBitAiConversationStore"/> and the UI: a store persists
/// snapshots (or their JSON), and <see cref="ToConversation(BitAiConversationSnapshot, BitAiConversationSerializerOptions?)"/>
/// turns a loaded one back into something a <see cref="BitAiChatSession"/> can drive.
/// </para>
/// <para>
/// Attachments are persisted by reference (<see cref="BitAiAttachment.Url"/>) plus any inline
/// <see cref="BitAiAttachment.Data"/>; a <see cref="BitAiAttachment.ContentFactory"/> cannot be serialized
/// and is dropped. <see cref="BitAiError.Exception"/> is likewise dropped.
/// </para>
/// </remarks>
public static class BitAiConversationSerializer
{
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> WithPackageMetadata = new();

    /// <summary>Takes a snapshot of a conversation.</summary>
    /// <param name="conversation">The conversation.</param>
    /// <param name="options">Serializer hooks.</param>
    /// <returns>The snapshot.</returns>
    public static BitAiConversationSnapshot ToSnapshot(BitAiConversation conversation, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        options ??= BitAiConversationSerializerOptions.Default;

        return new BitAiConversationSnapshot
        {
            ConversationId = conversation.ConversationId,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = conversation.Messages.Select(m => ToSnapshot(m, options)).ToArray(),
            Artifacts = conversation.Artifacts.Values.Select(a => ToSnapshot(a, options.Json)).ToArray(),
            Runs = conversation.Runs.Values.Select(ToSnapshot).ToArray(),
            Extensions = ToJsonDictionary(conversation.Extensions, options.Json),
        };
    }

    /// <summary>
    /// Takes a snapshot of any <see cref="IBitAiConversation"/>, including implementations from other
    /// packages. Parts that are not <see cref="BitAiMessagePart"/> instances are kept as text when they
    /// implement <see cref="IBitAiTextualPart"/>.
    /// </summary>
    /// <param name="conversation">The conversation.</param>
    /// <param name="options">Serializer hooks.</param>
    /// <returns>The snapshot.</returns>
    public static BitAiConversationSnapshot ToSnapshot(IBitAiConversation conversation, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        if (conversation is BitAiConversation typed) return ToSnapshot(typed, options);
        options ??= BitAiConversationSerializerOptions.Default;

        return new BitAiConversationSnapshot
        {
            ConversationId = conversation.ConversationId,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = conversation.Messages.Select(m => m is BitAiMessage typedMessage
                ? ToSnapshot(typedMessage, options)
                : new BitAiMessageSnapshot
                {
                    MessageId = m.MessageId,
                    Role = m.Role,
                    CreatedAt = m.CreatedAt,
                    Status = m.Status,
                    Metadata = m.Metadata,
                    Parts = m.Parts.Select(p => p is BitAiMessagePart part
                        ? ToSnapshot(part, options)
                        : new BitAiPartSnapshot { PartId = p.PartId, Kind = p.Kind, Text = (p as IBitAiTextualPart)?.Text }).ToArray(),
                    Attachments = m.Attachments,
                }).ToArray(),
        };
    }

    /// <summary>Rebuilds a live conversation from a snapshot.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="options">Serializer hooks.</param>
    /// <returns>The conversation.</returns>
    public static BitAiConversation ToConversation(BitAiConversationSnapshot snapshot, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        options ??= BitAiConversationSerializerOptions.Default;

        var conversation = new BitAiConversation(snapshot.ConversationId);

        foreach (var artifactSnapshot in snapshot.Artifacts)
        {
            var artifact = new BitAiArtifact(artifactSnapshot.ArtifactId, artifactSnapshot.Kind, artifactSnapshot.Title)
            {
                Language = artifactSnapshot.Language,
                Url = artifactSnapshot.Url,
                Metadata = FromJsonDictionary(artifactSnapshot.Metadata),
            };
            artifact.RestoreState(
                artifactSnapshot.Content,
                artifactSnapshot.Version,
                artifactSnapshot.IsStreaming,
                artifactSnapshot.CreatedAt,
                artifactSnapshot.UpdatedAt,
                artifactSnapshot.Versions);
            conversation.AttachArtifact(artifact);
        }

        foreach (var runSnapshot in snapshot.Runs)
        {
            var run = new BitAiAgentRun(runSnapshot.RunId, runSnapshot.AgentName);
            run.RestoreState(
                runSnapshot.Status,
                runSnapshot.Goal,
                runSnapshot.PendingPrompt,
                runSnapshot.PendingSuggestions,
                runSnapshot.StartedAt,
                runSnapshot.CompletedAt,
                runSnapshot.Usage,
                runSnapshot.Error,
                runSnapshot.Steps.Select(s =>
                {
                    var step = new BitAiAgentStep(s.StepId, s.Title) { Detail = s.Detail };
                    step.RestoreState(s.Status, s.Summary, s.Error, s.StartedAt, s.CompletedAt, s.ToolCallIds);
                    return step;
                }));
            conversation.AttachRun(run);
        }

        foreach (var messageSnapshot in snapshot.Messages)
        {
            var message = new BitAiMessage(messageSnapshot.Role, messageSnapshot.MessageId)
            {
                CreatedAt = messageSnapshot.CreatedAt,
            };

            foreach (var partSnapshot in messageSnapshot.Parts)
            {
                var part = options.RestorePart?.Invoke(partSnapshot) ?? RestoreBuiltIn(partSnapshot, options);
                if (part is not null) message.AddPart(part);
            }

            foreach (var attachment in messageSnapshot.Attachments) message.AddAttachment(attachment);
            foreach (var citation in messageSnapshot.Citations) message.AddCitation(citation);
            if (messageSnapshot.Extensions is { } extensions)
            {
                foreach (var (key, value) in extensions) message.Extensions[key] = value;
            }

            // A message interrupted mid-stream is restored as cancelled: nothing will ever finish it.
            var status = messageSnapshot.Status is BitAiMessageStatus.Pending or BitAiMessageStatus.Streaming
                ? BitAiMessageStatus.Cancelled
                : messageSnapshot.Status;

            message.Restore(
                status,
                messageSnapshot.Metadata,
                messageSnapshot.Error,
                messageSnapshot.Feedback,
                messageSnapshot.FeedbackComment,
                messageSnapshot.RunId is { } runId ? conversation.FindRun(runId) : null);

            conversation.AddMessage(message);
        }

        if (snapshot.Extensions is { } conversationExtensions)
        {
            foreach (var (key, value) in conversationExtensions) conversation.Extensions[key] = value;
        }

        conversation.SetTitle(snapshot.Title);
        conversation.RestoreTimestamps(snapshot.CreatedAt, snapshot.UpdatedAt);
        return conversation;
    }

    /// <summary>
    /// Returns the conversation itself when it already is a <see cref="BitAiConversation"/>, and otherwise
    /// rebuilds one from it. This is what to call on the result of <see cref="IBitAiConversationStore.LoadAsync"/>.
    /// </summary>
    /// <param name="conversation">The loaded conversation.</param>
    /// <param name="options">Serializer hooks.</param>
    /// <returns>A live conversation.</returns>
    public static BitAiConversation ToConversation(IBitAiConversation conversation, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        return conversation as BitAiConversation ?? ToConversation(ToSnapshot(conversation, options), options);
    }

    /// <summary>Serializes a conversation to JSON.</summary>
    /// <param name="conversation">The conversation.</param>
    /// <param name="options">Serializer hooks and JSON settings.</param>
    /// <returns>The JSON.</returns>
    public static string ToJson(BitAiConversation conversation, BitAiConversationSerializerOptions? options = null)
    {
        options ??= BitAiConversationSerializerOptions.Default;
        return JsonSerializer.Serialize(ToSnapshot(conversation, options), TypeInfo<BitAiConversationSnapshot>(options.Json));
    }

    /// <summary>Serializes a snapshot to JSON.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="options">JSON settings.</param>
    /// <returns>The JSON.</returns>
    public static string ToJson(BitAiConversationSnapshot snapshot, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, TypeInfo<BitAiConversationSnapshot>((options ?? BitAiConversationSerializerOptions.Default).Json));
    }

    /// <summary>Deserializes a snapshot from JSON.</summary>
    /// <param name="json">The JSON produced by <see cref="ToJson(BitAiConversationSnapshot, BitAiConversationSerializerOptions?)"/>.</param>
    /// <param name="options">JSON settings.</param>
    /// <returns>The snapshot.</returns>
    /// <exception cref="JsonException">The JSON is not a conversation snapshot.</exception>
    public static BitAiConversationSnapshot SnapshotFromJson(string json, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize(json, TypeInfo<BitAiConversationSnapshot>((options ?? BitAiConversationSerializerOptions.Default).Json))
            ?? throw new JsonException("The JSON did not contain a conversation snapshot.");
    }

    /// <summary>Deserializes JSON straight into a live conversation.</summary>
    /// <param name="json">The JSON.</param>
    /// <param name="options">Serializer hooks and JSON settings.</param>
    /// <returns>The conversation.</returns>
    public static BitAiConversation FromJson(string json, BitAiConversationSerializerOptions? options = null) =>
        ToConversation(SnapshotFromJson(json, options), options);

    private static BitAiMessageSnapshot ToSnapshot(BitAiMessage message, BitAiConversationSerializerOptions options) => new()
    {
        MessageId = message.MessageId,
        Role = message.Role,
        CreatedAt = message.CreatedAt,
        Status = message.Status,
        Metadata = message.Metadata,
        Error = message.Error,
        Feedback = message.Feedback,
        FeedbackComment = message.FeedbackComment,
        RunId = message.Run?.RunId,
        Parts = message.Parts.Select(p => ToSnapshot(p, options)).ToArray(),
        Attachments = message.Attachments,
        Citations = message.Citations,
        Extensions = ToJsonDictionary(message.Extensions, options.Json),
    };

    private static BitAiPartSnapshot ToSnapshot(BitAiMessagePart part, BitAiConversationSerializerOptions options)
    {
        if (options.SnapshotPart?.Invoke(part) is { } custom) return custom;

        var snapshot = new BitAiPartSnapshot { PartId = part.PartId, Kind = part.Kind };
        return part switch
        {
            BitAiReasoningPart reasoning => snapshot with
            {
                Text = reasoning.Text,
                Signature = reasoning.Signature,
                StartedAt = reasoning.StartedAt,
                CompletedAt = reasoning.CompletedAt,
            },
            BitAiCodePart code => snapshot with { Text = code.Text, Language = code.Language, FileName = code.FileName },
            BitAiTextPart text => snapshot with { Text = text.Text },
            BitAiImagePart image => snapshot with
            {
                Url = image.Url,
                Alt = image.Alt,
                Caption = image.Caption,
                Width = image.Width,
                Height = image.Height,
            },
            BitAiFilePart file => snapshot with { Attachment = file.Attachment },
            BitAiCitationPart citation => snapshot with { Citation = citation.Citation },
            BitAiSourcesPart sources => snapshot with { CitationIds = sources.CitationIds },
            BitAiToolCallPart call => snapshot with
            {
                ToolCallId = call.ToolCallId,
                ToolName = call.ToolName,
                Definition = call.Definition,
                Arguments = call.Arguments,
                RawArguments = call.Arguments is null ? call.RawArguments : null,
                ToolStatus = call.Status,
                Approval = call.Approval,
                ApprovalReason = call.ApprovalReason,
                ToolStartedAt = call.StartedAt,
                CompletedAt = call.CompletedAt,
                Outcome = call.Result is { } result ? BitAiToolOutcomeSnapshot.From(result) : null,
            },
            BitAiToolResultPart toolResult => snapshot with
            {
                ToolCallId = toolResult.ToolCallId,
                ToolName = toolResult.ToolName,
                Outcome = BitAiToolOutcomeSnapshot.From(toolResult.Outcome),
            },
            BitAiArtifactPart artifact => snapshot with { ArtifactId = artifact.ArtifactId, ArtifactVersion = artifact.ArtifactVersion },
            BitAiStructuredDataPart structured => snapshot with { SchemaName = structured.SchemaName, Data = structured.Value, Text = structured.Value is null ? structured.RawJson : null },
            BitAiGenerativeUiPart generative => snapshot with { ComponentName = generative.ComponentName, Data = generative.Props },
            BitAiErrorPart error => snapshot with { Error = error.Error },
            BitAiCustomJsonPart customJson => snapshot with { Data = customJson.Data, Text = customJson.Text },
            IBitAiTextualPart textual => snapshot with { Text = textual.Text },
            _ => snapshot,
        };
    }

    private static BitAiMessagePart? RestoreBuiltIn(BitAiPartSnapshot snapshot, BitAiConversationSerializerOptions options)
    {
        switch (snapshot.Kind)
        {
            case BitAiMessagePartKinds.Text:
                return new BitAiTextPart(snapshot.Text ?? string.Empty, snapshot.PartId);

            case BitAiMessagePartKinds.Markdown:
                return new BitAiMarkdownPart(snapshot.Text ?? string.Empty, snapshot.PartId);

            case BitAiMessagePartKinds.Reasoning:
            {
                var part = new BitAiReasoningPart(snapshot.Text ?? string.Empty, snapshot.PartId) { Signature = snapshot.Signature };
                part.RestoreTimings(snapshot.StartedAt ?? part.StartedAt, snapshot.CompletedAt);
                return part;
            }

            case BitAiMessagePartKinds.Code:
                return new BitAiCodePart(snapshot.Text ?? string.Empty, snapshot.Language, snapshot.PartId) { FileName = snapshot.FileName };

            case BitAiMessagePartKinds.Image when snapshot.Url is { Length: > 0 } url:
                return new BitAiImagePart(url, snapshot.PartId)
                {
                    Alt = snapshot.Alt,
                    Caption = snapshot.Caption,
                    Width = snapshot.Width,
                    Height = snapshot.Height,
                };

            case BitAiMessagePartKinds.File when snapshot.Attachment is { } attachment:
                return new BitAiFilePart(attachment, snapshot.PartId);

            case BitAiMessagePartKinds.Citation when snapshot.Citation is { } citation:
                return new BitAiCitationPart(citation, snapshot.PartId);

            case BitAiMessagePartKinds.Sources:
                return new BitAiSourcesPart(snapshot.PartId) { CitationIds = snapshot.CitationIds };

            case BitAiMessagePartKinds.ToolCall when snapshot.ToolCallId is { } callId && snapshot.ToolName is { } toolName:
            {
                var part = new BitAiToolCallPart(callId, toolName, snapshot.PartId)
                {
                    Definition = snapshot.Definition,
                    ApprovalReason = snapshot.ApprovalReason,
                };

                // A call that never finished cannot finish now: it is restored as cancelled.
                var status = snapshot.ToolStatus ?? BitAiToolCallStatus.Pending;
                if (status is BitAiToolCallStatus.Streaming or BitAiToolCallStatus.Pending or BitAiToolCallStatus.AwaitingApproval or BitAiToolCallStatus.Running)
                {
                    status = BitAiToolCallStatus.Cancelled;
                }

                part.RestoreState(
                    snapshot.Arguments,
                    snapshot.RawArguments,
                    status,
                    snapshot.Approval ?? BitAiApprovalDecision.Undecided,
                    snapshot.Outcome?.ToOutcome(),
                    snapshot.ToolStartedAt,
                    snapshot.CompletedAt);
                return part;
            }

            case BitAiMessagePartKinds.ToolResult when snapshot.ToolCallId is { } callId && snapshot.Outcome is { } outcome:
                return new BitAiToolResultPart(callId, outcome.ToOutcome(), snapshot.PartId) { ToolName = snapshot.ToolName };

            case BitAiMessagePartKinds.Artifact when snapshot.ArtifactId is { } artifactId:
                return new BitAiArtifactPart(artifactId, snapshot.PartId) { ArtifactVersion = snapshot.ArtifactVersion };

            case BitAiMessagePartKinds.StructuredData:
            {
                if (snapshot.Data is { } value) return new BitAiStructuredDataPart(value, snapshot.SchemaName, snapshot.PartId);
                var part = new BitAiStructuredDataPart(snapshot.SchemaName, snapshot.PartId);
                part.Append(snapshot.Text);
                part.Complete();
                return part;
            }

            case BitAiMessagePartKinds.GenerativeUi when snapshot.ComponentName is { } component:
            {
                var part = new BitAiGenerativeUiPart(component, snapshot.PartId);
                if (snapshot.Data is { } props) part.SetProps(props);
                part.Complete();
                return part;
            }

            case BitAiMessagePartKinds.Error when snapshot.Error is { } error:
                return new BitAiErrorPart(error, snapshot.PartId);

            default:
            {
                // Unknown or application-defined kind: keep the payload so nothing is lost on load.
                var data = snapshot.Data ?? JsonSerializer.SerializeToElement(snapshot, TypeInfo<BitAiPartSnapshot>(options.Json));
                return new BitAiCustomJsonPart(snapshot.Kind, data, snapshot.PartId) { Text = snapshot.Text };
            }
        }
    }

    private static BitAiArtifactSnapshot ToSnapshot(BitAiArtifact artifact, JsonSerializerOptions json) => new()
    {
        ArtifactId = artifact.ArtifactId,
        Kind = artifact.Kind,
        Title = artifact.Title,
        Content = artifact.Content,
        Language = artifact.Language,
        Url = artifact.Url,
        Version = artifact.Version,
        IsStreaming = artifact.IsStreaming,
        CreatedAt = artifact.CreatedAt,
        UpdatedAt = artifact.UpdatedAt,
        Versions = artifact.Versions.ToArray(),
        Metadata = ToJsonDictionary(artifact.Metadata, json),
    };

    private static BitAiAgentRunSnapshot ToSnapshot(BitAiAgentRun run) => new()
    {
        RunId = run.RunId,
        AgentName = run.AgentName,
        Goal = run.Goal,
        Status = run.Status,
        PendingPrompt = run.PendingPrompt,
        PendingSuggestions = run.PendingSuggestions,
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        Usage = run.Usage,
        Error = run.Error,
        Steps = run.Steps.Select(s => new BitAiAgentStepSnapshot
        {
            StepId = s.StepId,
            Title = s.Title,
            Detail = s.Detail,
            Status = s.Status,
            Summary = s.Summary,
            Error = s.Error,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            ToolCallIds = s.ToolCallIds.ToArray(),
        }).ToArray(),
    };

    private static IReadOnlyDictionary<string, JsonElement>? ToJsonDictionary(
        IReadOnlyDictionary<string, object?>? values,
        JsonSerializerOptions json)
    {
        if (values is null || values.Count == 0) return null;

        var settings = Resolve(json);
        var anyValue = settings.GetTypeInfo(typeof(object));
        var skipUnwritable = FallsBackToReflection(settings);
        var result = new Dictionary<string, JsonElement>(values.Count, StringComparer.Ordinal);

        foreach (var (key, value) in values)
        {
            try
            {
                // Written as an object, so by the type it holds - which the settings must have metadata for.
                result[key] = value is JsonElement element ? element : JsonSerializer.SerializeToElement(value, anyValue);
            }
            catch (NotSupportedException) when (skipUnwritable)
            {
                // Settings that read the app's types by reflection leave a value they cannot write (a delegate, a live
                // object) out of the snapshot, as the serializer always did. Otherwise the exception stands: it is what
                // System.Text.Json reports for a type it has no metadata for, here as anywhere else in the snapshot.
            }
        }

        return result;
    }

    /// <summary>
    /// Whether the settings read what they have no generated metadata for by reflection (those of
    /// <see cref="BitAiConversationSerializerOptions.CreateJsonOptionsWithReflectionFallback"/>, or any the app put the
    /// reflection-based resolver in).
    /// </summary>
    private static bool FallsBackToReflection(JsonSerializerOptions settings)
    {
        foreach (var resolver in settings.TypeInfoResolverChain)
        {
            if (resolver is DefaultJsonTypeInfoResolver) return true;
        }

        return false;
    }

    private static IReadOnlyDictionary<string, object?>? FromJsonDictionary(IReadOnlyDictionary<string, JsonElement>? values)
    {
        if (values is null) return null;
        var result = new Dictionary<string, object?>(values.Count, StringComparer.Ordinal);
        foreach (var (key, value) in values) result[key] = value;
        return result;
    }

    /// <summary>
    /// The metadata of one of the package's types, from the settings the caller handed over.
    /// </summary>
    private static JsonTypeInfo<T> TypeInfo<T>(JsonSerializerOptions json) => (JsonTypeInfo<T>)Resolve(json).GetTypeInfo(typeof(T));

    /// <summary>
    /// The settings themselves when they name a resolver (the default ones name the package's source-generated
    /// context), and otherwise a copy of them that takes its metadata from that context: settings built by hand, with no
    /// resolver, still serialize the package's types the way they ask, as the reflection-based serializer filled one in.
    /// </summary>
    private static JsonSerializerOptions Resolve(JsonSerializerOptions json) =>
        json.TypeInfoResolver is not null
            ? json
            : WithPackageMetadata.GetValue(json, static settings => new JsonSerializerOptions(settings) { TypeInfoResolver = BitAiSnapshotJson.Resolver });
}
