//+:cnd:noEmit
using System.Threading.Channels;
using Microsoft.Agents.AI;
using FluentStorage.Storage;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Api.Features.Attachments;
using Microsoft.AspNetCore.Authentication.BearerToken;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

/// <summary>
/// Service responsible for managing chatbot conversations, maintaining chat history,
/// and handling AI interactions including getting user feedbacks, describing app's features and pages etc.
/// This service is exposed over SignalR's AppHub.Chat.cs, so it can accept stream of user messages and return stream of AI responses using AiChatPanel.razor
/// Only the tools that stay on the server carry [McpServerTool], so an external MCP client can use them too (checkout
/// AppChatbot.Tools.cs). The rest reach into the user's live app over this SignalR connection - navigating it, showing
/// a sign-in modal, clearing its files - which is the agent's to do and nobody else's, so they are AIFunctions only
/// and signalRConnectionId is always the connection StartChat was given.
/// 
/// Microsoft.Agents.AI:
/// Workflows are not implemented in this project, but with AIAgent, achieving them is now easier compared to using IChatClient directly.
/// For example, it would be better to have separate Agents: one for product search, one for support, and one for app guidance.
/// A coordinator Agent could determine which specialized Agent to delegate the task to based on the user's message,
/// rather than having a single Agent with a very long System Prompt and many Tools.
/// </summary>
public partial class AppChatbot
{
    private AIAgent? supportAgent = default!;

    [AutoInject] private IStore blobStorage = default!;
    [AutoInject] private IHostEnvironment hostEnvironment = default!;
    [AutoInject] private TimeProvider timeProvider = default!;
    [AutoInject] private ChatbotAnswerSigner answerSigner = default!;
    [AutoInject] private ServerApiSettings appSettings = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private ApiServerExceptionHandler exceptionHandler = default!;
    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    private string? variablesDefault;
    private string? signalRConnectionId;
    private List<ChatMessage> chatMessages = [];

    /// <summary>
    /// This is a heart of streaming AI responses back to the client.
    /// </summary>
    private readonly Channel<string> responseChannel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });

    /// <summary>
    /// Starts the chat session with history and system prompt
    /// </summary>
    public async Task StartChat(
        StartChatRequest request,
        string? signalRConnectionId,
        CancellationToken cancellationToken)
    {
        chatMessages = [];

        var history = request.ChatMessagesHistory
            .Where(c => c.Successful && (string.IsNullOrWhiteSpace(c.Content) is false || c.AttachmentId is not null))
            .Where(WrittenByThisAssistantOrByTheUser)
            .TakeLast(MaxMessagesInHistory)
            .ToArray();

        foreach (var message in history)
        {
            chatMessages.Add(await ToChatMessage(message, cancellationToken));
        }

        TrimChatHistory();

        CultureInfo? culture = null;
        if (request.CultureId is not null && CultureInfoManager.InvariantGlobalization is false)
        {
            culture = CultureInfo.GetCultureInfo(request.CultureId.Value);
        }

        // The following variables won't change unless SignalR connection restarts and StartChat gets called again, so setting variables once here is sufficient.
        // For example, the user's culture won't change unless they restart the app.
        variablesDefault = @$"
{{{{UserCulture}}}}: ""{culture?.NativeName ?? "English"}""
{{{{DeviceInfo}}}}: ""{request.DeviceInfo ?? "Generic Device"}""
{{{{UserTimeZoneId}}}}: ""{request.TimeZoneId ?? "Unknown"}""
";

        this.signalRConnectionId = signalRConnectionId;
    }

    /// <summary>
    /// A resent assistant turn is believed only when it carries the signature this app wrote it with - anything else,
    /// the panel's own local greeting included, is the caller putting words in the assistant's mouth.
    /// </summary>
    private bool WrittenByThisAssistantOrByTheUser(AiChatMessage message)
    {
        return message.Role is not AiChatMessageRole.Assistant || answerSigner.Verify(message.Content, message.Signature);
    }

    /// <summary>
    /// Get the response channel for streaming AI responses
    /// </summary>
    public ChannelReader<string> GetStreamingChannel() => responseChannel.Reader;

    /// <summary>
    /// Stops streaming
    /// </summary>
    public void Stop() => responseChannel.Writer.TryComplete();

    /// <summary>
    /// Process an incoming message and stream the AI response
    /// </summary>
    public async Task ProcessNewMessage(
        AiChatMessage incomingMessage,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        // Everything sent for this turn is one json document (See AssistantTurn): the opening is written here, the
        // reader follows the model's reply through the middle, and CloseTurn splices the closing on.
        var reply = new PartialJsonReader<AssistantReply>(AppJsonContext.Default.AssistantReply);
        var opened = false;

        try
        {
            if (string.IsNullOrWhiteSpace(variablesDefault))
                throw new InvalidOperationException($"Chat session must be started before processing messages. Call {nameof(StartChat)} method first.");

            supportAgent ??= serviceProvider.GetRequiredKeyedService<AIAgent>("SupportAgent");

            // ChatRole.User rather than the role on the payload: whatever it claims, everything arriving on this
            // stream is the user speaking.
            chatMessages.Add(await ToChatMessage(ChatRole.User, incomingMessage.Content, incomingMessage.AttachmentId, cancellationToken));

            TrimChatHistory();

            var chatOptions = CreateChatOptions();

            // The following variables might change without SignalR connection restarts, so these should set here every time a new message is about to be processed.
            // For example, user can sign-in/sign-out during chat without restarting the app or SignalR connection.
            // User can change these values using prompt injection, so it's important not to rely on these variables for critical logic or security decisions,
            // but rather use them for providing better context to the model to generate more relevant responses.
            // You can either check if user is authenticated or not at a time tools are being called, or add dedicated tool so the LLM model would call it to figure out the user's authentication state instead of relying on variables.
            var variablesPrompt = @$"
### Variables:
{variablesDefault}
{{{{IsAuthenticated}}}}: ""{user.IsAuthenticated()}"",
{{{{UserEmail}}}}: ""{(user.IsAuthenticated() ? user!.GetEmail()?.ToString() : "null")}"",
{{{{WebAppUrl}}}}: ""{(httpContextAccessor.HttpContext!.Request.GetWebAppUrl())}"",
";

            await foreach (var response in supportAgent.RunStreamingAsync([
                new (ChatRole.System, variablesPrompt),
                .. chatMessages,
                ], options: new ChatClientAgentRunOptions(chatOptions), cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await CloseTurn(reply, opened, successful: false);
                    return;
                }

                if (response.Text is not { Length: > 0 } chunk) continue;

                reply.Append(chunk);

                if (opened is false)
                {
                    await SendStringToClient(OpenTurn(), cancellationToken);
                    opened = true;
                }

                // Only json goes through as it comes. A provider that didn't hold the model to the schema is writing
                // prose where a document belongs, which CloseTurn puts right instead.
                if (reply.IsDocument)
                {
                    await responseChannel.Writer.WriteAsync(chunk, cancellationToken);
                }
            }

            if (AnswerOf(reply) is { Length: > 0 } answer)
            {
                chatMessages.Add(new(ChatRole.Assistant, answer));
            }

            await CloseTurn(reply, opened, successful: true);
        }
        catch (Exception exp) when (exp is OperationCanceledException or ChannelClosedException)
        {
            await CloseTurn(reply, opened, successful: false);
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp, new() { { "SignalRConnectionId", signalRConnectionId } });
            await CloseTurn(reply, opened, successful: false);
        }
    }

    /// <summary>Everything of the turn's document that comes before the model's reply.</summary>
    private string OpenTurn() => $$"""{"sentAt":"{{timeProvider.GetUtcNow():O}}","reply":""";

    /// <summary>
    /// Finishes the turn's document: whatever a reply cut off mid word still owes, then the fields only the server
    /// can fill in, over the answer the user was actually shown.
    /// <para>
    /// Never sent with the message's own token: the client ends a turn when the document closes, and on an unbounded
    /// channel WriteAsync drops what it is given once that token is cancelled - which is the case here.
    /// </para>
    /// </summary>
    private Task CloseTurn(PartialJsonReader<AssistantReply> reply, bool opened, bool successful)
    {
        var rest = opened is false
            // Nothing was written at all, so the opening still has to go out with it.
            ? $"{OpenTurn()}null"
            : reply.IsDocument
                // What a reply cut off mid word still owes. Empty for one that ended on its own.
                ? reply.Completion()
                // Prose that was held back above, put where a reply belongs.
                : JsonSerializer.Serialize(new AssistantReply { Answer = reply.Json }, AppJsonContext.Default.AssistantReply);

        var signature = successful && AnswerOf(reply) is { Length: > 0 } answer
            ? $"\"{JsonEncodedText.Encode(answerSigner.Sign(answer))}\""
            : "null";

        return SendStringToClient($$"""{{rest}},"signature":{{signature}},"successful":{{(successful ? "true" : "false")}}}""",
                                  CancellationToken.None);
    }

    /// <summary>
    /// The text the user was shown - what gets signed and what is replayed to the model as its own previous turn -
    /// read with the same reader the panel uses (See <c>AppAiChatPanel.RunChannel</c>).
    /// </summary>
    private static string? AnswerOf(PartialJsonReader<AssistantReply> reply)
        => reply.IsDocument ? reply.Append(null)?.Answer : reply.Json;

    /// <summary>
    /// A message of the resent history, as the role it claims. Only messages that got past
    /// <see cref="WrittenByThisAssistantOrByTheUser"/> reach here, so an assistant turn is one this app signed.
    /// </summary>
    private Task<ChatMessage> ToChatMessage(AiChatMessage message, CancellationToken cancellationToken)
        => ToChatMessage(message.Role is AiChatMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User,
                         message.Content,
                         message.AttachmentId,
                         cancellationToken);

    /// <summary>
    /// The text of a message, plus the image the user attached to it, if any.
    /// </summary>
    private async Task<ChatMessage> ToChatMessage(ChatRole role, string? content, Guid? attachmentId, CancellationToken cancellationToken)
    {
        List<AIContent> contents = [];

        if (string.IsNullOrWhiteSpace(content) is false)
        {
            contents.Add(new TextContent(content));
        }

        if (await ReadAttachedImage(attachmentId, cancellationToken) is AIContent image)
        {
            contents.Add(image);
        }

        return new ChatMessage(role, contents);
    }

    /// <summary>What <c>AttachmentController</c> stores and serves an AI chat image as.</summary>
    private const string AiChatImageMediaType = "image/webp";

    /// <summary>
    /// The chat image the client named, or null when it named none. The kind is fixed here rather than taken from
    /// the client, so an id is only ever looked for among this app's chat images - a payload cannot point this at a
    /// profile picture.
    /// <para>
    /// The picture is handed over as a url for the provider to fetch, which costs nothing to produce and does not
    /// re-upload the same bytes on every turn of the conversation. That only works where the provider can reach this
    /// backend, and a machine serving localhost cannot be reached from the internet - so development sends the bytes
    /// themselves instead. A deployment that is not reachable from the internet either (a private network, say) has
    /// to do the same.
    /// </para>
    /// </summary>
    private async Task<AIContent?> ReadAttachedImage(Guid? attachmentId, CancellationToken cancellationToken)
    {
        if (attachmentId is null)
            return null;

        if (hostEnvironment.IsDevelopment() is false)
        {
            return new UriContent(new Uri(httpContextAccessor.HttpContext!.Request.GetBaseUrl(), $"api/v1/Attachment/GetAttachment/{attachmentId}/{AttachmentKind.AiChatImage}"), AiChatImageMediaType);
        }

        var path = AttachmentController.GetFilePath(appSettings, attachmentId.Value, AttachmentKind.AiChatImage);

        return new DataContent(await blobStorage.GetBytes(path, cancellationToken), AiChatImageMediaType);
    }

    /// <summary>
    /// How many of the newest pictures the model is shown. An image costs orders of magnitude more tokens than the
    /// sentence around it, so a handful of them reach the context window long before the message count below does.
    /// The panel still shows every picture the user attached; only what is replayed to the model is capped.
    /// </summary>
    private const int MaxImagesInHistory = 3;

    /// <inheritdoc cref="StartChatRequest.MaxChatMessagesHistory"/>
    private const int MaxMessagesInHistory = StartChatRequest.MaxChatMessagesHistory;

    /// <summary>
    /// The conversation is resent in full on every message, so an unbounded history grows the prompt (and its
    /// cost) without limit until the provider rejects it for exceeding the context window.
    /// </summary>
    private void TrimChatHistory()
    {
        if (chatMessages.Count > MaxMessagesInHistory)
        {
            chatMessages.RemoveRange(0, chatMessages.Count - MaxMessagesInHistory);
        }

        var pictures = 0;

        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            var contents = chatMessages[i].Contents;

            for (var j = contents.Count - 1; j >= 0; j--)
            {
                // Both shapes a picture arrives in - see ReadAttachedImage - or the cap would quietly stop applying
                // in every environment that hands the model a url.
                if (contents[j] is not (DataContent or UriContent)) continue;

                if (++pictures <= MaxImagesInHistory) continue;

                contents.RemoveAt(j);
            }

            // What was said about a picture stays in the conversation once the picture itself is gone - but a message
            // that was nothing else is left with no content at all, which not every provider accepts.
            if (contents.Count == 0)
            {
                contents.Add(new TextContent("(an image the user attached earlier)"));
            }
        }
    }

    /// <summary>
    /// Create chat options with AI tools
    /// </summary>
    public List<AIFunction> GetAIFunctions()
    {
        var aiFunctions = new List<AIFunction>
        {
            AIFunctionFactory.Create(GetCurrentDateTime),
            AIFunctionFactory.Create(SaveUserEmailAndConversationHistory),
            AIFunctionFactory.Create(GetAppPages),
            AIFunctionFactory.Create(NavigateToPage),
            AIFunctionFactory.Create(ShowSignInModal),
            AIFunctionFactory.Create(SetApplicationCulture),
            AIFunctionFactory.Create(SetApplicationTheme),
            AIFunctionFactory.Create(CheckLastError),
            AIFunctionFactory.Create(ClearAppFiles),
            //#if (module == "Sales")
            //#if (database == "PostgreSQL" || database == "SqlServer")
            AIFunctionFactory.Create(GetProductRecommendations)
            //#endif
            //#endif
        };

        return aiFunctions;
    }

    /// <summary>
    /// Makes the model write one json document (See <see cref="AssistantReply"/>) instead of prose, so the answer and
    /// its follow-up suggestions arrive together rather than in two round trips. A provider that doesn't enforce the
    /// schema streams whatever it likes, and both ends read that as the answer itself - minus the suggestions.
    /// </summary>
    private static readonly ChatResponseFormat AnswerFormat = ChatResponseFormat.ForJsonSchema(
        AIJsonUtilities.CreateJsonSchema(typeof(AssistantReply), serializerOptions: AppJsonContext.Default.Options, inferenceOptions: new()
        {
            // What OpenAI's strict json schema mode requires of a schema, and what the rest ignore.
            TransformOptions = new() { DisallowAdditionalProperties = true, RequireAllProperties = true }
        }),
        schemaName: "assistant_answer",
        schemaDescription: "The assistant's reply to the user, and what the user might want to ask next.");

    /// <summary>
    /// Create chat options with AI tools
    /// </summary>
    private ChatOptions CreateChatOptions()
    {
        var chatOptions = new ChatOptions { };
        configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);
        chatOptions.ResponseFormat = AnswerFormat;
        return chatOptions;
    }

    private async Task SendStringToClient(string message, CancellationToken ct)
    {
        try
        {
            await responseChannel.Writer.WriteAsync(message, ct);
        }
        catch (ChannelClosedException) { } // Normal when client has disconnected / stream ended.
        catch (OperationCanceledException) { }
    }
}
