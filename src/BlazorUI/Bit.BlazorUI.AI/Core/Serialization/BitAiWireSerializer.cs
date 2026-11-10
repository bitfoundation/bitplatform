using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// The wire format between a server that generates replies and the browser that renders them: the
/// <see cref="BitAiEvent"/> stream going one way, and the <see cref="BitAiRequest"/> and the
/// <see cref="BitAiApprovalResponse"/> going the other.
/// </summary>
/// <remarks>
/// <para>
/// An event is one JSON object told apart by its <c>type</c> (<see cref="BitAiEventJsonConverter"/> documents the
/// shape); a request is a <see cref="BitAiRequestSnapshot"/>; an approval is the record itself. All of it is written
/// through the settings of <see cref="BitAiConversationSerializerOptions.Json"/> - web naming, the package's
/// enumerations as camelCase strings, nulls left out - whose metadata is generated at compile time, so it is safe to trim
/// and to compile ahead of time on both ends.
/// </para>
/// <para>
/// The format is versioned (<see cref="Version"/>) and changes only by addition: a reader skips a property it does not
/// know, and an event of a <c>type</c> it does not know reads as <see langword="null"/>, which every method here and the
/// transports of the package drop. So a server can send a newer event to an older client, which goes on without it.
/// </para>
/// <para>
/// The SSE and SignalR transports (<see cref="BitAiSseTransport"/>, <see cref="BitAiSignalRTransport"/>) and the
/// server helpers that pair with them use this, and an application with a transport of its own can too.
/// </para>
/// </remarks>
public static class BitAiWireSerializer
{
    /// <summary>The version of the wire format this package writes and reads.</summary>
    public const int Version = 1;

    /// <summary>Serializes one event.</summary>
    /// <param name="evt">The event.</param>
    /// <param name="options">The JSON settings, for an application's own types in a loosely typed value.</param>
    /// <returns>The JSON.</returns>
    public static string Serialize(BitAiEvent evt, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(evt);
        return JsonSerializer.Serialize(evt, TypeInfo<BitAiEvent>(options));
    }

    /// <summary>Serializes one event to a JSON element.</summary>
    /// <param name="evt">The event.</param>
    /// <param name="options">The JSON settings, for an application's own types in a loosely typed value.</param>
    /// <returns>The JSON.</returns>
    public static JsonElement SerializeToElement(BitAiEvent evt, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(evt);
        return JsonSerializer.SerializeToElement(evt, TypeInfo<BitAiEvent>(options));
    }

    /// <summary>Reads one event.</summary>
    /// <param name="json">The JSON of one event.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The event, or <see langword="null"/> for an event of a <c>type</c> this version does not know.</returns>
    /// <exception cref="JsonException">The JSON is not an event of the format.</exception>
    public static BitAiEvent? Deserialize(string json, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize(json, TypeInfo<BitAiEvent>(options));
    }

    /// <summary>Reads one event.</summary>
    /// <param name="json">The JSON of one event.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The event, or <see langword="null"/> for an event of a <c>type</c> this version does not know.</returns>
    /// <exception cref="JsonException">The JSON is not an event of the format.</exception>
    public static BitAiEvent? Deserialize(JsonElement json, BitAiConversationSerializerOptions? options = null) =>
        json.Deserialize(TypeInfo<BitAiEvent>(options));

    /// <summary>
    /// Takes a snapshot of a request to send, reading into it the content of every attachment only reachable through its
    /// <see cref="BitAiAttachment.ContentFactory"/> - a file picked in the browser - which the snapshot cannot carry
    /// otherwise.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="options">Serializer hooks, for custom parts, and the JSON settings.</param>
    /// <param name="cancellationToken">Cancels reading the attachments.</param>
    /// <returns>The snapshot.</returns>
    /// <remarks>
    /// An attachment with a <see cref="BitAiAttachment.Url"/> or inline <see cref="BitAiAttachment.Data"/> is sent as it
    /// is. Large files are better uploaded first and sent by address (see <see cref="BitAiPromptInput"/>'s upload hook).
    /// </remarks>
    public static async ValueTask<BitAiRequestSnapshot> ToSnapshotAsync(
        BitAiRequest request,
        BitAiConversationSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = ToSnapshot(request, options);
        BitAiMessageSnapshot[]? messages = null;

        for (var i = 0; i < snapshot.Messages.Count; i++)
        {
            var message = snapshot.Messages[i];
            BitAiAttachment[]? attachments = null;

            for (var j = 0; j < message.Attachments.Count; j++)
            {
                var attachment = message.Attachments[j];
                if (attachment is not { Data: null, Url: null, ContentFactory: not null }) continue;

                await using var stream = await attachment.OpenReadAsync(cancellationToken).ConfigureAwait(false);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

                attachments ??= [.. message.Attachments];
                attachments[j] = attachment with { Data = buffer.ToArray(), Size = attachment.Size ?? buffer.Length };
            }

            if (attachments is null) continue;
            messages ??= [.. snapshot.Messages];
            messages[i] = message with { Attachments = attachments };
        }

        return messages is null ? snapshot : snapshot with { Messages = messages };
    }

    /// <summary>
    /// Takes a snapshot of a request as it is, attachments included by reference only (see
    /// <see cref="ToSnapshotAsync"/>).
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="options">Serializer hooks, for custom parts, and the JSON settings.</param>
    /// <returns>The snapshot.</returns>
    public static BitAiRequestSnapshot ToSnapshot(BitAiRequest request, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        options ??= BitAiConversationSerializerOptions.Default;

        return new BitAiRequestSnapshot
        {
            ConversationId = request.ConversationId,
            Messages = request.Messages.Select(m => BitAiConversationSerializer.ToMessageSnapshot(m, options)).ToArray(),
            Options = request.Options,
            Tools = request.Tools is { Count: > 0 } tools ? tools : null,
            Context = BitAiConversationSerializer.ToJsonDictionary(request.Context, options.Json),
        };
    }

    /// <summary>Rebuilds a request from its snapshot, with live messages a server can read.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="options">Serializer hooks, for custom parts.</param>
    /// <param name="services">The services of the scope the request is handled in, put on <see cref="BitAiRequest.Services"/>.</param>
    /// <returns>The request.</returns>
    /// <remarks>
    /// A message that was still streaming when the snapshot was taken comes back cancelled, and a tool call that had not
    /// finished comes back cancelled too, as they do from a persisted conversation.
    /// </remarks>
    public static BitAiRequest ToRequest(
        BitAiRequestSnapshot snapshot,
        BitAiConversationSerializerOptions? options = null,
        IServiceProvider? services = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        options ??= BitAiConversationSerializerOptions.Default;

        var messages = (snapshot.Messages ?? []).Select(m => BitAiConversationSerializer.ToMessage(m, options, conversation: null)).ToArray();
        return new BitAiRequest(messages)
        {
            ConversationId = snapshot.ConversationId,
            Options = snapshot.Options,
            Tools = snapshot.Tools ?? [],
            Services = services,
            Context = BitAiConversationSerializer.FromJsonDictionary(snapshot.Context),
        };
    }

    /// <summary>Serializes a request snapshot.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The JSON.</returns>
    public static string SerializeRequest(BitAiRequestSnapshot snapshot, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, TypeInfo<BitAiRequestSnapshot>(options));
    }

    /// <summary>Serializes a request snapshot to a JSON element.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The JSON.</returns>
    public static JsonElement SerializeRequestToElement(BitAiRequestSnapshot snapshot, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.SerializeToElement(snapshot, TypeInfo<BitAiRequestSnapshot>(options));
    }

    /// <summary>Reads a request snapshot.</summary>
    /// <param name="json">The JSON.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The snapshot.</returns>
    /// <exception cref="JsonException">The JSON is not a request.</exception>
    public static BitAiRequestSnapshot DeserializeRequest(string json, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize(json, TypeInfo<BitAiRequestSnapshot>(options)) ?? throw NotA("request");
    }

    /// <summary>Reads a request snapshot.</summary>
    /// <param name="json">The JSON.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The snapshot.</returns>
    /// <exception cref="JsonException">The JSON is not a request.</exception>
    public static BitAiRequestSnapshot DeserializeRequest(JsonElement json, BitAiConversationSerializerOptions? options = null) =>
        json.Deserialize(TypeInfo<BitAiRequestSnapshot>(options)) ?? throw NotA("request");

    /// <summary>Reads a request snapshot from a stream of UTF-8 JSON, such as the body of an HTTP request.</summary>
    /// <param name="utf8Json">The JSON.</param>
    /// <param name="options">The JSON settings.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The snapshot.</returns>
    /// <exception cref="JsonException">The JSON is not a request.</exception>
    public static async ValueTask<BitAiRequestSnapshot> DeserializeRequestAsync(
        Stream utf8Json,
        BitAiConversationSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        return await JsonSerializer.DeserializeAsync(utf8Json, TypeInfo<BitAiRequestSnapshot>(options), cancellationToken).ConfigureAwait(false)
            ?? throw NotA("request");
    }

    /// <summary>Serializes a decision on a tool call.</summary>
    /// <param name="response">The decision.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The JSON.</returns>
    public static string SerializeApproval(BitAiApprovalResponse response, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        return JsonSerializer.Serialize(response, TypeInfo<BitAiApprovalResponse>(options));
    }

    /// <summary>Serializes a decision on a tool call to a JSON element.</summary>
    /// <param name="response">The decision.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The JSON.</returns>
    public static JsonElement SerializeApprovalToElement(BitAiApprovalResponse response, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        return JsonSerializer.SerializeToElement(response, TypeInfo<BitAiApprovalResponse>(options));
    }

    /// <summary>Reads a decision on a tool call.</summary>
    /// <param name="json">The JSON.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="JsonException">The JSON is not a decision.</exception>
    public static BitAiApprovalResponse DeserializeApproval(string json, BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Checked(JsonSerializer.Deserialize(json, TypeInfo<BitAiApprovalResponse>(options)));
    }

    /// <summary>Reads a decision on a tool call.</summary>
    /// <param name="json">The JSON.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="JsonException">The JSON is not a decision.</exception>
    public static BitAiApprovalResponse DeserializeApproval(JsonElement json, BitAiConversationSerializerOptions? options = null) =>
        Checked(json.Deserialize(TypeInfo<BitAiApprovalResponse>(options)));

    /// <summary>Reads a decision on a tool call from a stream of UTF-8 JSON, such as the body of an HTTP request.</summary>
    /// <param name="utf8Json">The JSON.</param>
    /// <param name="options">The JSON settings.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="JsonException">The JSON is not a decision.</exception>
    public static async ValueTask<BitAiApprovalResponse> DeserializeApprovalAsync(
        Stream utf8Json,
        BitAiConversationSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Json);
        return Checked(await JsonSerializer.DeserializeAsync(utf8Json, TypeInfo<BitAiApprovalResponse>(options), cancellationToken).ConfigureAwait(false));
    }

    /// <summary>The metadata of one of the wire types, from the settings of <paramref name="options"/>.</summary>
    internal static JsonTypeInfo<T> TypeInfo<T>(BitAiConversationSerializerOptions? options) =>
        BitAiConversationSerializer.TypeInfo<T>((options ?? BitAiConversationSerializerOptions.Default).Json);

    // The record's positional parameters are not checked by the serializer the way its constructor would be called.
    private static BitAiApprovalResponse Checked(BitAiApprovalResponse? response) =>
        response is { ToolCallId.Length: > 0, ToolName: not null } ? response : throw NotA("tool call decision");

    private static JsonException NotA(string what) => new($"The JSON is not a BitAi {what}.");
}
