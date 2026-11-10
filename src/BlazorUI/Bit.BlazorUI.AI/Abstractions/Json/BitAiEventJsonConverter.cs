using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// Reads and writes a <see cref="BitAiEvent"/> in the package's wire format: one JSON object per event, told apart by its
/// <c>type</c> property, which holds the event's <see cref="BitAiEvent.Type"/>.
/// </summary>
/// <remarks>
/// <para>
/// The wire format is a versioned contract (<see cref="BitAiWireSerializer.Version"/>) between a server streaming events
/// and the browser rendering them, so it is written out member by member here rather than left to whatever a contract
/// resolver infers from the records: every built-in event has a fixed shape, independent of the naming policy of the
/// settings it is handed, and is read back through its own constructor, which checks what it always checks.
/// </para>
/// <para>
/// Every event carries <c>type</c>, <c>timestamp</c> and, when set, <c>messageId</c> (and <c>partId</c> for the events
/// scoped to a part). The other properties are the event's own, camelCase, with <see langword="null"/> values left out
/// and the package's enumerations written as camelCase strings.
/// </para>
/// <para>
/// A reader ignores what it does not know, so a producer can add to the format without breaking an older consumer: an
/// unknown property is skipped, and an event of an unknown <c>type</c> reads as <see langword="null"/>, which
/// <see cref="BitAiWireSerializer"/> and the transports built on it drop. Writing an event type the format does not
/// define fails with a <see cref="NotSupportedException"/>; carry an application's own data in a
/// <see cref="BitAiCustomEvent"/> instead.
/// </para>
/// <para>
/// The nested records (an error, a citation, an attachment, a tool definition, the usage, the generation metadata) are
/// written through the settings of <see cref="BitAiConversationSerializerOptions.CreateJsonOptions"/>, whose metadata is
/// generated at compile time, so the format is safe to trim and to compile ahead of time. Settings that do not come from
/// there (a SignalR hub's, an MVC controller's) are replaced by the package's own for everything inside an event. A
/// loosely typed value - a metadata entry, <see cref="BitAiCustomEvent.Data"/> - is written by the type it holds and
/// read back as a <see cref="JsonElement"/>; an application's own type in one needs the application's
/// <see cref="JsonSerializerContext"/> added to the settings (<c>options.Json.TypeInfoResolverChain.Add(...)</c>).
/// </para>
/// </remarks>
public sealed class BitAiEventJsonConverter : JsonConverter<BitAiEvent>
{
    /// <inheritdoc />
    public override BitAiEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null) return null;
        if (reader.TokenType is not JsonTokenType.StartObject) throw new JsonException("A BitAiEvent is a JSON object.");

        return ReadEvent(JsonElement.ParseValue(ref reader), Wire(options));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BitAiEvent value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        WriteEvent(writer, value, Wire(options));
    }

    /// <summary>
    /// The settings the nested records are written through: the ones handed over when they carry the package's
    /// metadata (those of <see cref="BitAiConversationSerializerOptions.CreateJsonOptions"/>, perhaps with the app's
    /// resolvers added), and the package's own otherwise, so the format does not change with the caller's naming policy.
    /// </summary>
    internal static JsonSerializerOptions Wire(JsonSerializerOptions options) =>
        options.TypeInfoResolverChain.Contains(BitAiSnapshotJson.Resolver) ? options : BitAiConversationSerializerOptions.Default.Json;

    private static BitAiEvent? ReadEvent(JsonElement json, JsonSerializerOptions options)
    {
        var type = String(json, "type") ?? throw new JsonException("A BitAiEvent has no 'type'.");
        // An optional value that does not read as its type (a timestamp in another format) is left out, as an unknown
        // property is, rather than failing the whole event.
        var timestamp = json.TryGetProperty("timestamp", out var at) && at.ValueKind is JsonValueKind.String && at.TryGetDateTimeOffset(out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;
        var messageId = String(json, "messageId");
        var partId = String(json, "partId");

        try
        {
            BitAiEvent? evt = type switch
            {
                "message.started" => new BitAiMessageStartedEvent(String(json, "role") is { Length: > 0 } role ? new BitAiRole(role) : (BitAiRole?)null),
                "message.completed" => new BitAiMessageCompletedEvent(Value(json, "finishReason", options, BitAiFinishReason.Stop)),
                "part.started" => new BitAiPartStartedEvent(Required(json, "kind")) { PartId = partId, Attributes = Object<IReadOnlyDictionary<string, object?>>(json, "attributes", options) },
                "part.completed" => new BitAiPartCompletedEvent { PartId = partId },
                "text.delta" => new BitAiTextDeltaEvent(Required(json, "delta")) { PartId = partId },
                "reasoning.delta" => new BitAiReasoningDeltaEvent(Required(json, "delta")) { PartId = partId, Signature = String(json, "signature") },
                "status" => new BitAiStatusEvent(String(json, "status")),
                "usage.updated" => new BitAiUsageUpdatedEvent(Value(json, "usage", options, default(BitAiTokenUsage))),
                "metadata" => new BitAiMetadataEvent(RequiredObject<BitAiGenerationMetadata>(json, "metadata", options)),
                "error" => new BitAiErrorEvent(RequiredObject<BitAiError>(json, "error", options), Boolean(json, "isTerminal") ?? true),
                "custom" => new BitAiCustomEvent(Required(json, "name"), Element(json, "data")),
                "citation" => new BitAiCitationEvent(RequiredObject<BitAiCitation>(json, "citation", options)),
                "follow_ups" => new BitAiFollowUpsEvent(Object<IReadOnlyList<string>>(json, "followUps", options) ?? []),
                "image" => new BitAiImageEvent(Required(json, "url")) { PartId = partId, Alt = String(json, "alt"), Width = Int32(json, "width"), Height = Int32(json, "height") },
                "file" => new BitAiFileEvent(RequiredObject<BitAiAttachment>(json, "attachment", options)) { PartId = partId },
                "artifact.created" => new BitAiArtifactCreatedEvent(Required(json, "artifactId"), Required(json, "kind"), String(json, "title") ?? string.Empty)
                {
                    Language = String(json, "language"),
                    Content = String(json, "content"),
                    Url = String(json, "url"),
                    Metadata = Object<IReadOnlyDictionary<string, object?>>(json, "metadata", options),
                },
                "artifact.delta" => new BitAiArtifactDeltaEvent(Required(json, "artifactId"), Required(json, "delta")),
                "artifact.completed" => new BitAiArtifactCompletedEvent(Required(json, "artifactId")) { Summary = String(json, "summary") },
                "agent.run.started" => new BitAiAgentRunStartedEvent(Required(json, "runId")) { AgentName = String(json, "agentName"), Goal = String(json, "goal") },
                "agent.plan" => new BitAiAgentPlanEvent(Required(json, "runId"), ReadPlan(json)),
                "agent.step.started" => new BitAiAgentStepStartedEvent(Required(json, "runId"), Required(json, "stepId"), String(json, "title")),
                "agent.step.completed" => new BitAiAgentStepCompletedEvent(Required(json, "runId"), Required(json, "stepId"), Value(json, "status", options, BitAiAgentStepStatus.Completed))
                {
                    Summary = String(json, "summary"),
                    Error = Object<BitAiError>(json, "error", options),
                },
                "agent.run.completed" => new BitAiAgentRunCompletedEvent(Required(json, "runId"), Value(json, "status", options, BitAiAgentRunStatus.Completed)),
                "agent.input_requested" => new BitAiAgentInputRequestedEvent(Required(json, "runId"), Required(json, "prompt")) { Suggestions = Object<IReadOnlyList<string>>(json, "suggestions", options) },
                "structured.delta" => new BitAiStructuredDataDeltaEvent(Required(json, "jsonDelta")) { PartId = partId, SchemaName = String(json, "schemaName") },
                "structured" => new BitAiStructuredDataEvent(RequiredElement(json, "value")) { PartId = partId, SchemaName = String(json, "schemaName") },
                "generative_ui" => new BitAiGenerativeUiEvent(Required(json, "componentName"), RequiredElement(json, "props")) { PartId = partId, IsPartial = Boolean(json, "isPartial") ?? false },
                "generative_ui.delta" => new BitAiGenerativeUiDeltaEvent(Required(json, "componentName"), Required(json, "propsDelta")) { PartId = partId },
                "tool_call.started" => new BitAiToolCallStartedEvent(Required(json, "toolCallId"), Required(json, "toolName"))
                {
                    Arguments = Element(json, "arguments"),
                    Definition = Object<BitAiToolDefinition>(json, "definition", options),
                },
                "tool_call.delta" => new BitAiToolCallDeltaEvent(Required(json, "toolCallId"), Required(json, "argumentsDelta")),
                "tool_call.completed" => new BitAiToolCallCompletedEvent(Required(json, "toolCallId"), Element(json, "arguments")),
                "tool_call.progress" => new BitAiToolProgressEvent(Required(json, "toolCallId"), ReadProgress(json)),
                "tool_call.result" => new BitAiToolResultEvent(Required(json, "toolCallId"), ReadOutcome(json, options)),
                "tool_call.approval_requested" => new BitAiToolApprovalRequestedEvent(Required(json, "toolCallId"))
                {
                    Reason = String(json, "reason"),
                    AllowApproveAlways = Boolean(json, "allowApproveAlways") ?? true,
                },
                _ => null,
            };

            return evt is null ? null : evt with { Timestamp = timestamp, MessageId = messageId };
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException)
        {
            // What the event's constructor refuses (an empty id, a missing text), or a value a JsonElement cannot read as
            // the type asked for, is malformed JSON to a reader.
            throw new JsonException($"The '{type}' event is not valid: {exception.Message}", exception);
        }
    }

    private static void WriteEvent(Utf8JsonWriter writer, BitAiEvent evt, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", evt.Type);
        writer.WriteString("timestamp", evt.Timestamp);
        if (evt.MessageId is { } messageId) writer.WriteString("messageId", messageId);
        if (evt is BitAiPartScopedEvent { PartId: { } partId }) writer.WriteString("partId", partId);

        switch (evt)
        {
            case BitAiMessageStartedEvent e:
                writer.WriteString("role", e.Role.Value);
                break;
            case BitAiMessageCompletedEvent e:
                WriteValue(writer, "finishReason", e.FinishReason, options);
                break;
            case BitAiPartStartedEvent e:
                writer.WriteString("kind", e.Kind);
                WriteValue(writer, "attributes", e.Attributes, options);
                break;
            case BitAiPartCompletedEvent:
                break;
            case BitAiTextDeltaEvent e:
                writer.WriteString("delta", e.Delta);
                break;
            case BitAiReasoningDeltaEvent e:
                writer.WriteString("delta", e.Delta);
                WriteString(writer, "signature", e.Signature);
                break;
            case BitAiStatusEvent e:
                WriteString(writer, "status", e.Status);
                break;
            case BitAiUsageUpdatedEvent e:
                WriteValue(writer, "usage", e.Usage, options);
                break;
            case BitAiMetadataEvent e:
                WriteValue(writer, "metadata", e.Metadata, options);
                break;
            case BitAiErrorEvent e:
                WriteValue(writer, "error", e.Error, options);
                writer.WriteBoolean("isTerminal", e.IsTerminal);
                break;
            case BitAiCustomEvent e:
                writer.WriteString("name", e.Name);
                WriteValue(writer, "data", e.Data, options);
                break;
            case BitAiCitationEvent e:
                WriteValue(writer, "citation", e.Citation, options);
                break;
            case BitAiFollowUpsEvent e:
                WriteValue(writer, "followUps", e.FollowUps, options);
                break;
            case BitAiImageEvent e:
                writer.WriteString("url", e.Url);
                WriteString(writer, "alt", e.Alt);
                if (e.Width is { } width) writer.WriteNumber("width", width);
                if (e.Height is { } height) writer.WriteNumber("height", height);
                break;
            case BitAiFileEvent e:
                WriteValue(writer, "attachment", e.Attachment, options);
                break;
            case BitAiArtifactCreatedEvent e:
                writer.WriteString("artifactId", e.ArtifactId);
                writer.WriteString("kind", e.Kind.Value);
                writer.WriteString("title", e.Title);
                WriteString(writer, "language", e.Language);
                WriteString(writer, "content", e.Content);
                WriteString(writer, "url", e.Url);
                WriteValue(writer, "metadata", e.Metadata, options);
                break;
            case BitAiArtifactDeltaEvent e:
                writer.WriteString("artifactId", e.ArtifactId);
                writer.WriteString("delta", e.Delta);
                break;
            case BitAiArtifactCompletedEvent e:
                writer.WriteString("artifactId", e.ArtifactId);
                WriteString(writer, "summary", e.Summary);
                break;
            case BitAiAgentRunStartedEvent e:
                writer.WriteString("runId", e.RunId);
                WriteString(writer, "agentName", e.AgentName);
                WriteString(writer, "goal", e.Goal);
                break;
            case BitAiAgentPlanEvent e:
                writer.WriteString("runId", e.RunId);
                writer.WriteStartArray("steps");
                foreach (var step in e.Steps)
                {
                    writer.WriteStartObject();
                    writer.WriteString("stepId", step.StepId);
                    writer.WriteString("title", step.Title);
                    WriteString(writer, "detail", step.Detail);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
            case BitAiAgentStepStartedEvent e:
                writer.WriteString("runId", e.RunId);
                writer.WriteString("stepId", e.StepId);
                WriteString(writer, "title", e.Title);
                break;
            case BitAiAgentStepCompletedEvent e:
                writer.WriteString("runId", e.RunId);
                writer.WriteString("stepId", e.StepId);
                WriteValue(writer, "status", e.Status, options);
                WriteString(writer, "summary", e.Summary);
                WriteValue(writer, "error", e.Error, options);
                break;
            case BitAiAgentRunCompletedEvent e:
                writer.WriteString("runId", e.RunId);
                WriteValue(writer, "status", e.Status, options);
                break;
            case BitAiAgentInputRequestedEvent e:
                writer.WriteString("runId", e.RunId);
                writer.WriteString("prompt", e.Prompt);
                WriteValue(writer, "suggestions", e.Suggestions, options);
                break;
            case BitAiStructuredDataDeltaEvent e:
                writer.WriteString("jsonDelta", e.JsonDelta);
                WriteString(writer, "schemaName", e.SchemaName);
                break;
            case BitAiStructuredDataEvent e:
                WriteElement(writer, "value", e.Value);
                WriteString(writer, "schemaName", e.SchemaName);
                break;
            case BitAiGenerativeUiEvent e:
                writer.WriteString("componentName", e.ComponentName);
                WriteElement(writer, "props", e.Props);
                writer.WriteBoolean("isPartial", e.IsPartial);
                break;
            case BitAiGenerativeUiDeltaEvent e:
                writer.WriteString("componentName", e.ComponentName);
                writer.WriteString("propsDelta", e.PropsDelta);
                break;
            case BitAiToolCallStartedEvent e:
                writer.WriteString("toolCallId", e.ToolCallId);
                writer.WriteString("toolName", e.ToolName);
                if (e.Arguments is { } startArguments) WriteElement(writer, "arguments", startArguments);
                WriteValue(writer, "definition", e.Definition, options);
                break;
            case BitAiToolCallDeltaEvent e:
                writer.WriteString("toolCallId", e.ToolCallId);
                writer.WriteString("argumentsDelta", e.ArgumentsDelta);
                break;
            case BitAiToolCallCompletedEvent e:
                writer.WriteString("toolCallId", e.ToolCallId);
                if (e.Arguments is { } arguments) WriteElement(writer, "arguments", arguments);
                break;
            case BitAiToolProgressEvent e:
                writer.WriteString("toolCallId", e.ToolCallId);
                writer.WriteStartObject("progress");
                if (e.Progress.Fraction is { } fraction) writer.WriteNumber("fraction", fraction);
                WriteString(writer, "message", e.Progress.Message);
                writer.WriteEndObject();
                break;
            case BitAiToolResultEvent e:
                writer.WriteString("toolCallId", e.ToolCallId);
                WriteOutcome(writer, e.Outcome, options);
                break;
            case BitAiToolApprovalRequestedEvent e:
                writer.WriteString("toolCallId", e.ToolCallId);
                WriteString(writer, "reason", e.Reason);
                writer.WriteBoolean("allowApproveAlways", e.AllowApproveAlways);
                break;
            default:
                throw new NotSupportedException(
                    $"'{evt.GetType().FullName}' ('{evt.Type}') is not an event of the BitAiEvent wire format. Send an application's own data as a {nameof(BitAiCustomEvent)}.");
        }

        writer.WriteEndObject();
    }

    private static IReadOnlyList<BitAiAgentPlanStep> ReadPlan(JsonElement json)
    {
        if (!json.TryGetProperty("steps", out var steps) || steps.ValueKind is not JsonValueKind.Array) return [];

        var result = new List<BitAiAgentPlanStep>(steps.GetArrayLength());
        foreach (var step in steps.EnumerateArray())
        {
            result.Add(new BitAiAgentPlanStep(Required(step, "stepId"), String(step, "title") ?? string.Empty, String(step, "detail")));
        }

        return result;
    }

    private static BitAiToolProgress ReadProgress(JsonElement json)
    {
        if (!json.TryGetProperty("progress", out var progress) || progress.ValueKind is not JsonValueKind.Object) return default;

        double? fraction = progress.TryGetProperty("fraction", out var value) && value.ValueKind is JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number) ? number : null;
        return new BitAiToolProgress(fraction, String(progress, "message"));
    }

    // An outcome has no public constructor (its factories decide what a success or a failure holds), so it is written
    // and read here, the way BitAiToolOutcomeSnapshot restores one.
    private static void WriteOutcome(Utf8JsonWriter writer, BitAiToolOutcome outcome, JsonSerializerOptions options)
    {
        writer.WriteStartObject("outcome");
        writer.WriteBoolean("isSuccess", outcome.IsSuccess);
        if (outcome.Value is { } value) WriteElement(writer, "value", value);
        WriteString(writer, "text", outcome.Text);
        WriteValue(writer, "error", outcome.Error, options);
        WriteValue(writer, "citations", outcome.Citations, options);
        WriteValue(writer, "duration", outcome.Duration, options);
        WriteValue(writer, "metadata", outcome.Metadata, options);
        writer.WriteEndObject();
    }

    private static BitAiToolOutcome ReadOutcome(JsonElement json, JsonSerializerOptions options)
    {
        if (!json.TryGetProperty("outcome", out var outcome) || outcome.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("The 'tool_call.result' event has no 'outcome'.");
        }

        var text = String(outcome, "text");
        var result = Boolean(outcome, "isSuccess") is true
            ? Element(outcome, "value") is { } value ? BitAiToolOutcome.Success(value, text) : BitAiToolOutcome.FromText(text ?? string.Empty)
            : BitAiToolOutcome.Failure(Object<BitAiError>(outcome, "error", options) ?? new BitAiError("The tool failed.") { Kind = BitAiErrorKind.Tool });

        return result with
        {
            Text = text ?? result.Text,
            Citations = Object<IReadOnlyList<BitAiCitation>>(outcome, "citations", options),
            Duration = Object<TimeSpan?>(outcome, "duration", options),
            Metadata = Object<IReadOnlyDictionary<string, object?>>(outcome, "metadata", options),
        };
    }

    private static string? String(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) ? value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => throw new JsonException($"'{name}' is not a string."),
        } : null;

    private static string Required(JsonElement json, string name) =>
        String(json, name) ?? throw new JsonException($"'{name}' is missing.");

    private static bool? Boolean(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    // Optional, so a number that is no Int32 (1.5, 3e10) is left out like a missing one.
    private static int? Int32(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;

    private static JsonElement? Element(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind is not JsonValueKind.Null ? value : null;

    // A JSON null is a value here (the structured output may be null), so only a missing property fails.
    private static JsonElement RequiredElement(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) ? value : throw new JsonException($"'{name}' is missing.");

    private static T? Object<T>(JsonElement json, string name, JsonSerializerOptions options) =>
        json.TryGetProperty(name, out var value) && value.ValueKind is not JsonValueKind.Null
            ? value.Deserialize(TypeInfo<T>(options))
            : default;

    private static T RequiredObject<T>(JsonElement json, string name, JsonSerializerOptions options) where T : class =>
        Object<T>(json, name, options) ?? throw new JsonException($"'{name}' is missing.");

    private static T Value<T>(JsonElement json, string name, JsonSerializerOptions options, T fallback) where T : struct =>
        json.TryGetProperty(name, out var value) && value.ValueKind is not JsonValueKind.Null
            ? value.Deserialize(TypeInfo<T>(options))
            : fallback;

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null) writer.WriteString(name, value);
    }

    // An element that holds nothing (a default JsonElement) is written as null, which reads back as a JSON null.
    private static void WriteElement(Utf8JsonWriter writer, string name, JsonElement value)
    {
        writer.WritePropertyName(name);
        if (value.ValueKind is JsonValueKind.Undefined) writer.WriteNullValue();
        else value.WriteTo(writer);
    }

    private static void WriteValue<T>(Utf8JsonWriter writer, string name, T value, JsonSerializerOptions options)
    {
        if (value is null) return;
        writer.WritePropertyName(name);
        JsonSerializer.Serialize(writer, value, TypeInfo<T>(options));
    }

    private static JsonTypeInfo<T> TypeInfo<T>(JsonSerializerOptions options) => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
