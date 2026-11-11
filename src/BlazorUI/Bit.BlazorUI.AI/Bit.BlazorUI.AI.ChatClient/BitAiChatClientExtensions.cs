using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Bit.BlazorUI;

/// <summary>
/// Connects the package to <c>Microsoft.Extensions.AI</c>: streams the reply of an <see cref="IChatClient"/> as
/// <see cref="BitAiEvent"/>s, and turns a <see cref="BitAiRequest"/> into the <see cref="ChatMessage"/>s and the
/// <see cref="ChatOptions"/> a chat client takes.
/// </summary>
/// <remarks>
/// <para>
/// This is the mapping an app would otherwise write by hand on its server, and what <see cref="BitAiChatClientTransport"/>
/// runs in the browser (or a Blazor Server circuit) for an app that calls its chat client there. On a server it pairs
/// with an endpoint of Bit.BlazorUI.AI.Server - <c>MapBitAiSse</c>, <c>MapBitAiWebSocket</c> - or its <c>BitAiHub</c>:
/// </para>
/// <code>
/// app.MapBitAiSse("/api/ai/chat", (request, http, ct) =&gt;
///     http.RequestServices.GetRequiredService&lt;IChatClient&gt;().StreamBitAiEventsAsync(request, chatOptions, ct));
/// </code>
/// <para>
/// What a reply streams maps to events this way: text to <see cref="BitAiTextDeltaEvent"/>s, reasoning to
/// <see cref="BitAiReasoningDeltaEvent"/>s, a function call to a <see cref="BitAiToolCallStartedEvent"/> with its
/// arguments and a function result to a <see cref="BitAiToolResultEvent"/>, an image (by data or by address) to a
/// <see cref="BitAiImageEvent"/> and any other file to a <see cref="BitAiFileEvent"/>, the usage to a
/// <see cref="BitAiUsageUpdatedEvent"/> (summed over every model call of the reply, such as the rounds of a function
/// invoking client), a citation annotation to a <see cref="BitAiCitationEvent"/>, an error content to a non-terminal
/// <see cref="BitAiErrorEvent"/>, and the model id to a <see cref="BitAiMetadataEvent"/>. The reply opens with a
/// <see cref="BitAiMessageStartedEvent"/> and closes with a <see cref="BitAiMessageCompletedEvent"/> carrying its finish
/// reason.
/// </para>
/// <para>
/// A function the server's client runs itself (a <c>FunctionInvokingChatClient</c> over an <see cref="AIFunction"/>)
/// shows as a tool card with its result. A tool the browser runs (one of <see cref="BitAiRequest.Tools"/>) is declared to
/// the model without being invocable on the server, so its call ends the reply and the chat session runs it and asks
/// again with the result, which <see cref="ToChatMessagesAsync"/> hands back to the model.
/// </para>
/// </remarks>
public static class BitAiChatClientExtensions
{
    private static readonly JsonElement EmptyParametersSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement.Clone();
    private static readonly JsonElement EmptyArguments = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>
    /// Sends a request to a chat client and streams its reply as events.
    /// </summary>
    /// <param name="chatClient">The chat client.</param>
    /// <param name="request">The request, as a transport receives it or as <see cref="BitAiWireSerializer.ToRequest"/> rebuilds it on a server.</param>
    /// <param name="options">The chat options of the app, which win over what the request asks for (see <see cref="ToChatOptions"/>).</param>
    /// <param name="cancellationToken">Stops the generation.</param>
    /// <returns>The events of the reply.</returns>
    public static async IAsyncEnumerable<BitAiEvent> StreamBitAiEventsAsync(
        this IChatClient chatClient,
        BitAiRequest request,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(request);

        var messages = await ToChatMessagesAsync(request.Messages, cancellationToken).ConfigureAwait(false);
        var chatOptions = ToChatOptions(request, options);

        await foreach (var evt in chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken)
                           .ToBitAiEventsAsync(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return evt;
        }
    }

    /// <summary>
    /// Turns the streamed reply of a chat client into events.
    /// </summary>
    /// <param name="updates">The updates of one reply.</param>
    /// <param name="cancellationToken">Stops reading the updates.</param>
    /// <returns>The events of the reply, opened by a <see cref="BitAiMessageStartedEvent"/> and closed by a <see cref="BitAiMessageCompletedEvent"/>.</returns>
    public static async IAsyncEnumerable<BitAiEvent> ToBitAiEventsAsync(
        this IAsyncEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        yield return new BitAiMessageStartedEvent();

        string? modelId = null;
        ChatFinishReason? finishReason = null;
        UsageDetails? usage = null;
        var startedCalls = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (update.ModelId is { Length: > 0 } updateModel && updateModel != modelId)
            {
                modelId = updateModel;
                yield return new BitAiMetadataEvent(new BitAiGenerationMetadata { ModelId = modelId });
            }

            if (update.FinishReason is { } reason) finishReason = reason;

            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextContent { Text: { Length: > 0 } text } textContent:
                        yield return new BitAiTextDeltaEvent(text);
                        foreach (var citation in Citations(textContent)) yield return new BitAiCitationEvent(citation);
                        break;

                    case TextContent textContent:
                        foreach (var citation in Citations(textContent)) yield return new BitAiCitationEvent(citation);
                        break;

                    case TextReasoningContent reasoning when !string.IsNullOrEmpty(reasoning.Text) || reasoning.ProtectedData is not null:
                        yield return new BitAiReasoningDeltaEvent(reasoning.Text ?? string.Empty) { Signature = reasoning.ProtectedData };
                        break;

                    case FunctionCallContent call when !string.IsNullOrEmpty(call.CallId) && !string.IsNullOrEmpty(call.Name):
                        // A call is reported once: a function invoking client streams the call it ran again in its
                        // history, and the session must not see (and maybe run) it twice.
                        // The arguments are always sent: a call started without them waits for a completion that a
                        // chat client never streams, so a call with none (a tool without parameters, or arguments the
                        // client could not parse) goes out with an empty object and runs.
                        if (startedCalls.Add(call.CallId))
                        {
                            yield return new BitAiToolCallStartedEvent(call.CallId, call.Name) { Arguments = ToJson(call.Arguments) ?? EmptyArguments };
                        }
                        break;

                    case FunctionResultContent result when !string.IsNullOrEmpty(result.CallId):
                        yield return new BitAiToolResultEvent(result.CallId, ToOutcome(result));
                        break;

                    case DataContent data:
                        yield return IsImage(data.MediaType)
                            ? new BitAiImageEvent(data.Uri) { Alt = data.Name }
                            : new BitAiFileEvent(new BitAiAttachment(BitAiId.New("file"), data.Name ?? "file")
                            {
                                ContentType = data.MediaType,
                                Kind = BitAiAttachment.InferKind(data.MediaType, data.Name),
                                Data = data.Data,
                                Size = data.Data.Length,
                            });
                        break;

                    case UriContent uri:
                        yield return IsImage(uri.MediaType)
                            ? new BitAiImageEvent(Address(uri.Uri))
                            : new BitAiFileEvent(new BitAiAttachment(BitAiId.New("file"), FileName(uri.Uri))
                            {
                                ContentType = uri.MediaType,
                                Kind = BitAiAttachment.InferKind(uri.MediaType, uri.Uri.AbsolutePath),
                                Url = Address(uri.Uri),
                            });
                        break;

                    case UsageContent { Details: { } details }:
                        if (usage is null) usage = new UsageDetails();
                        usage.Add(details);
                        yield return new BitAiUsageUpdatedEvent(ToUsage(usage));
                        break;

                    case ErrorContent error:
                        yield return new BitAiErrorEvent(new BitAiError(error.Message ?? "The model reported an error.")
                        {
                            Code = error.ErrorCode,
                            Detail = error.Details,
                            Kind = BitAiErrorKind.Provider,
                        }, isTerminal: false);
                        break;
                }
            }
        }

        yield return new BitAiMessageCompletedEvent(ToFinishReason(finishReason));
    }

    /// <summary>
    /// Turns the transcript of a request into chat messages, reading the content of every attachment only reachable
    /// through its <see cref="BitAiAttachment.ContentFactory"/>.
    /// </summary>
    /// <param name="messages">The transcript, oldest first.</param>
    /// <param name="cancellationToken">Cancels reading the attachments.</param>
    /// <returns>The chat messages.</returns>
    /// <remarks>
    /// <para>
    /// A message keeps its role, and its parts become contents in order: text, Markdown and code as
    /// <see cref="TextContent"/>, reasoning as <see cref="TextReasoningContent"/> (its signature as the protected data a
    /// provider may need back), an image as <see cref="DataContent"/> or <see cref="UriContent"/>, any other textual part
    /// (structured output, a custom part) as its text. The message's attachments follow as <see cref="DataContent"/>
    /// (inline data, a data URL) or <see cref="UriContent"/> (an absolute address). An image or an attachment known only
    /// by a relative address or a file path is left out, since the provider cannot fetch it: give it its data
    /// (<see cref="BitAiAttachment.Data"/>, or a <see cref="BitAiAttachment.ContentFactory"/> read here) or an absolute
    /// address.
    /// </para>
    /// <para>
    /// A tool call becomes a <see cref="FunctionCallContent"/> of its message, and its result - on the call, or a tool
    /// result part - a <see cref="FunctionResultContent"/> in a tool message right after it, as the providers expect; what
    /// the message holds after a call with its result (the model's answer once a server-run function returned) goes into
    /// a message of the same role after that tool message, so every round reads in the order it happened. A
    /// call nothing answers (stopped, rejected, cancelled) gets a result saying so, since a provider refuses a transcript
    /// with an unanswered call.
    /// </para>
    /// </remarks>
    public static async ValueTask<List<ChatMessage>> ToChatMessagesAsync(
        IEnumerable<IBitAiMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var transcript = messages as IReadOnlyCollection<IBitAiMessage> ?? [.. messages];
        var answered = AnsweredCalls(transcript);

        var result = new List<ChatMessage>();
        foreach (var message in transcript)
        {
            var role = new ChatRole(message.Role.Value);
            var contents = new List<AIContent>();
            var results = new List<AIContent>();

            foreach (var part in message.Parts)
            {
                // A reply of a function invoking client is one message over several model calls: the calls a round
                // made, their results, then what the model said after reading them. What follows a call that has its
                // result goes after that result, in a message of its own, as the model produced it.
                if (results.Count > 0 && part is not (BitAiToolCallPart or BitAiToolResultPart))
                {
                    Flush(result, role, message.MessageId, contents, results);
                    contents = [];
                    results = [];
                }

                AddPart(part, contents, results, answered);
            }

            foreach (var attachment in message.Attachments)
            {
                if (await ToContentAsync(attachment, cancellationToken).ConfigureAwait(false) is { } content) contents.Add(content);
            }

            Flush(result, role, message.MessageId, contents, results);
        }

        return result;
    }

    // One round of a message: its contents, then a tool message with the results of the calls among them.
    private static void Flush(List<ChatMessage> messages, ChatRole role, string messageId, List<AIContent> contents, List<AIContent> results)
    {
        if (contents.Count > 0) messages.Add(new ChatMessage(role, contents) { MessageId = messageId });
        if (results.Count > 0) messages.Add(new ChatMessage(ChatRole.Tool, results));
    }

    /// <summary>
    /// The chat options to send a request with: the app's own, with what the request asks for filled in where the app's
    /// leave a setting unset.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="options">The app's options, cloned rather than changed. Their settings win.</param>
    /// <returns>The options.</returns>
    /// <remarks>
    /// <para>
    /// The request comes from the browser, so it never overrides the server: a model, a temperature, a token limit or
    /// instructions set in <paramref name="options"/> stay as they are, and the request's
    /// <see cref="BitAiRequestOptions"/> only fill the settings left unset (<see cref="BitAiRequestOptions.SystemPrompt"/>
    /// fills <see cref="ChatOptions.Instructions"/>). Pin every setting the client must not choose. The request's
    /// <see cref="BitAiRequestOptions.Extra"/> is never forwarded.
    /// </para>
    /// <para>
    /// A request a server reads off the wire (<see cref="BitAiWireSerializer.ToRequest"/>, which every endpoint of
    /// Bit.BlazorUI.AI.Server goes through) carries no system prompt, and no tool choice forcing a tool the browser did not
    /// declare: those are the server's alone to set. Only a request the app builds where its own code runs
    /// (<see cref="BitAiChatClientTransport"/> in a Blazor Server circuit or a Hybrid app) still has them.
    /// </para>
    /// <para>
    /// Each tool of <see cref="BitAiRequest.Tools"/> is declared to the model (<see cref="AIFunctionFactory.CreateDeclaration"/>),
    /// unless the app's options already have a tool of that name: a declaration is not invocable on the server, so a call
    /// to it is handed back to the browser, which runs it.
    /// </para>
    /// </remarks>
    public static ChatOptions ToChatOptions(BitAiRequest request, ChatOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = options?.Clone() ?? new ChatOptions();

        if (request.Options is { } asked)
        {
            result.ModelId ??= asked.ModelId;
            result.Temperature ??= (float?)asked.Temperature;
            result.TopP ??= (float?)asked.TopP;
            result.MaxOutputTokens ??= asked.MaxOutputTokens is { } max ? (int)Math.Clamp(max, 1, int.MaxValue) : null;
            result.StopSequences ??= asked.StopSequences is { Count: > 0 } stops ? [.. stops] : null;
            result.Seed ??= asked.Seed;
            result.Instructions ??= asked.SystemPrompt;
            result.ResponseFormat ??= asked.ResponseFormat is { } format ? ToResponseFormat(format) : null;
            result.ToolMode ??= asked.ToolChoice is { } choice ? ToToolMode(choice) : null;
        }

        if (request.Tools.Count > 0)
        {
            var tools = result.Tools is null ? new List<AITool>() : [.. result.Tools];
            foreach (var definition in request.Tools)
            {
                if (tools.Exists(t => string.Equals(t.Name, definition.Name, StringComparison.Ordinal))) continue;
                tools.Add(AIFunctionFactory.CreateDeclaration(definition.Name, definition.Description, definition.ParametersSchema ?? EmptyParametersSchema));
            }

            result.Tools = tools;
        }

        return result;
    }

    // The calls a result answers anywhere in the transcript: on the call itself, or a tool result part.
    private static HashSet<string> AnsweredCalls(IEnumerable<IBitAiMessage> messages)
    {
        var answered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var message in messages)
        {
            foreach (var part in message.Parts)
            {
                if (part is BitAiToolCallPart { Result: not null } call) answered.Add(call.ToolCallId);
                else if (part is BitAiToolResultPart toolResult) answered.Add(toolResult.ToolCallId);
            }
        }

        return answered;
    }

    private static void AddPart(IBitAiMessagePart part, List<AIContent> contents, List<AIContent> results, HashSet<string> answered)
    {
        switch (part)
        {
            case BitAiReasoningPart reasoning:
                if (!string.IsNullOrEmpty(reasoning.Text) || reasoning.Signature is not null)
                {
                    contents.Add(new TextReasoningContent(reasoning.Text) { ProtectedData = reasoning.Signature });
                }
                break;

            case BitAiImagePart image:
                if (ToContent(image.Url, "image/*") is { } imageContent) contents.Add(imageContent);
                break;

            case BitAiFilePart file:
                if (ToContent(file.Attachment) is { } fileContent) contents.Add(fileContent);
                break;

            case BitAiToolCallPart call:
                contents.Add(new FunctionCallContent(call.ToolCallId, call.ToolName, ToArguments(call.Arguments)));
                if (call.Result is { } outcome) results.Add(new FunctionResultContent(call.ToolCallId, ToResult(outcome)));
                else if (!answered.Contains(call.ToolCallId))
                {
                    // A call left without a result (stopped, rejected, cancelled while it waited for an approval) still
                    // needs one: the providers refuse a transcript with a tool call nothing answers, and every later
                    // turn of the conversation would fail.
                    results.Add(new FunctionResultContent(call.ToolCallId, Unanswered(call.Status)));
                }
                break;

            case BitAiToolResultPart toolResult:
                results.Add(new FunctionResultContent(toolResult.ToolCallId, ToResult(toolResult.Outcome)));
                break;

            case BitAiErrorPart or BitAiCitationPart or BitAiSourcesPart:
                break;

            case IBitAiTextualPart { Text: { Length: > 0 } text }:
                contents.Add(new TextContent(text));
                break;
        }
    }

    private static async ValueTask<AIContent?> ToContentAsync(BitAiAttachment attachment, CancellationToken cancellationToken)
    {
        if (ToContent(attachment) is { } content) return content;
        if (attachment.ContentFactory is null) return null;

        var bytes = await attachment.ReadAllBytesAsync(cancellationToken).ConfigureAwait(false);
        return new DataContent(bytes, MediaType(attachment)) { Name = attachment.Name };
    }

    private static AIContent? ToContent(BitAiAttachment attachment)
    {
        if (attachment.Data is { } data) return new DataContent(data, MediaType(attachment)) { Name = attachment.Name };
        if (attachment.Url is { } url && ToContent(url, MediaType(attachment)) is { } content)
        {
            if (content is DataContent dataContent) dataContent.Name = attachment.Name;
            return content;
        }

        return null;
    }

    // An address the provider can fetch itself: a data URL, or an absolute one. A relative address ('/uploads/a.png',
    // which .NET on Linux would take for file:///uploads/a.png, and on Windows for nothing) means nothing to the provider,
    // and neither does a file path, so both are left out on every platform; the app reads such a file into the
    // attachment's data instead.
    private static AIContent? ToContent(string url, string mediaType)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return new DataContent(url);
        if (url.StartsWith('/') || url.StartsWith('\\')) return null;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && !uri.IsFile && !uri.IsUnc ? new UriContent(uri, mediaType) : null;
    }

    private static string MediaType(BitAiAttachment attachment) =>
        attachment.ContentType is { Length: > 0 } contentType ? contentType : attachment.Kind switch
        {
            BitAiAttachmentKind.Image => "image/*",
            BitAiAttachmentKind.Audio => "audio/*",
            BitAiAttachmentKind.Video => "video/*",
            _ => "application/octet-stream",
        };

    private static bool IsImage(string? mediaType) => mediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) is true;

    // Uri.ToString() unescapes (%2F becomes /), which changes the address a signed URL was signed for; the escaped form
    // is the one the browser has to request.
    private static string Address(Uri uri) => uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.OriginalString;

    private static string FileName(Uri uri) =>
        uri.IsAbsoluteUri && Path.GetFileName(uri.AbsolutePath) is { Length: > 0 } name ? name : "file";

    private static IEnumerable<BitAiCitation> Citations(AIContent content)
    {
        if (content.Annotations is not { Count: > 0 } annotations) yield break;

        foreach (var annotation in annotations)
        {
            if (annotation is not CitationAnnotation citation) continue;

            var url = citation.Url is { } address ? Address(address) : null;
            var sourceId = url ?? citation.FileId ?? citation.Title ?? BitAiId.New("src");
            var source = new BitAiCitationSource(sourceId, citation.Title ?? url ?? citation.FileId ?? string.Empty)
            {
                Url = url,
                Kind = url is not null ? BitAiSourceKind.Web : citation.FileId is not null ? BitAiSourceKind.File : citation.ToolName is not null ? BitAiSourceKind.Tool : BitAiSourceKind.Unknown,
            };

            // The regions of an annotation count within the update that carries it, not within the text part the
            // deltas add up to, so the citation is listed with the reply rather than anchored to a span of it.
            yield return new BitAiCitation(BitAiId.New("cite"), source) { Quote = citation.Snippet };
        }
    }

    private static JsonElement? ToJson(IDictionary<string, object?>? arguments)
    {
        if (arguments is null) return null;
        return JsonSerializer.SerializeToElement(arguments, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object?>)));
    }

    private static Dictionary<string, object?>? ToArguments(JsonElement? arguments)
    {
        if (arguments is not { ValueKind: JsonValueKind.Object } json) return null;

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in json.EnumerateObject()) result[property.Name] = property.Value.Clone();
        return result;
    }

    private static string Unanswered(BitAiToolCallStatus status) => status switch
    {
        BitAiToolCallStatus.Rejected => "The user rejected this tool call; it was not run.",
        _ => "This tool call was cancelled before it produced a result.",
    };

    private static object? ToResult(BitAiToolOutcome outcome)
    {
        if (!outcome.IsSuccess) return outcome.Error?.Message ?? "The tool failed.";
        return outcome.Value is { ValueKind: not JsonValueKind.Undefined } value ? value : outcome.Text;
    }

    private static BitAiToolOutcome ToOutcome(FunctionResultContent result)
    {
        if (result.Exception is { } exception)
        {
            return BitAiToolOutcome.Failure(new BitAiError(exception.Message) { Kind = BitAiErrorKind.Tool, Exception = exception });
        }

        return result.Result switch
        {
            null => BitAiToolOutcome.FromText(string.Empty),
            JsonElement element => BitAiToolOutcome.Success(element),
            string text => BitAiToolOutcome.FromText(text),
            var value => ToOutcome(value),
        };
    }

    // A result of another type is written by the metadata Microsoft.Extensions.AI has for it (the app's own types reach
    // it through AIJsonUtilities.DefaultOptions); one nothing can write is shown as its text.
    private static BitAiToolOutcome ToOutcome(object value)
    {
        try
        {
            return BitAiToolOutcome.Success(JsonSerializer.SerializeToElement(value, AIJsonUtilities.DefaultOptions.GetTypeInfo(value.GetType())));
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
        {
            return BitAiToolOutcome.FromText(value.ToString() ?? string.Empty);
        }
    }

    private static BitAiTokenUsage ToUsage(UsageDetails usage) => new()
    {
        InputTokens = usage.InputTokenCount,
        OutputTokens = usage.OutputTokenCount,
        CachedInputTokens = usage.CachedInputTokenCount,
        ReasoningTokens = usage.ReasoningTokenCount,
    };

    private static BitAiFinishReason ToFinishReason(ChatFinishReason? reason)
    {
        if (reason is not { } value) return BitAiFinishReason.Stop;
        if (value == ChatFinishReason.Stop) return BitAiFinishReason.Stop;
        if (value == ChatFinishReason.Length) return BitAiFinishReason.Length;
        if (value == ChatFinishReason.ToolCalls) return BitAiFinishReason.ToolCalls;
        if (value == ChatFinishReason.ContentFilter) return BitAiFinishReason.ContentFilter;
        return BitAiFinishReason.Unknown;
    }

    private static ChatResponseFormat ToResponseFormat(BitAiResponseFormat format) => format.Kind switch
    {
        BitAiResponseFormatKind.JsonSchema when format.Schema is { } schema => ChatResponseFormat.ForJsonSchema(schema, format.Name),
        BitAiResponseFormatKind.Json or BitAiResponseFormatKind.JsonSchema => ChatResponseFormat.Json,
        _ => ChatResponseFormat.Text,
    };

    private static ChatToolMode ToToolMode(BitAiToolChoice choice) => choice.Mode switch
    {
        "none" => ChatToolMode.None,
        "required" => ChatToolMode.RequireAny,
        "specific" when choice.ToolName is { } name => ChatToolMode.RequireSpecific(name),
        _ => ChatToolMode.Auto,
    };
}
