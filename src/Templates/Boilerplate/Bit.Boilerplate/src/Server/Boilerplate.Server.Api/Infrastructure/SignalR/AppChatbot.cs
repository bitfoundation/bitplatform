//+:cnd:noEmit
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Agents.AI;
using FluentStorage.Storage;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Api.Features.Attachments;
using Microsoft.AspNetCore.Authentication.BearerToken;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

/// <summary>
/// Answers the AI chat panel, one answer at a time: <see cref="Reply"/> reads the conversation the panel sends with each
/// question and streams the answer back as Bit.BlazorUI.AI events, through <see cref="AppHub"/> (a BitAiHub). Nothing of
/// the conversation is kept here between answers: the panel keeps it and sends it each time, and what it says the
/// assistant said counts only where the assistant's signature checks out (See <see cref="AsProvablySaid"/>).
/// <para>
/// Only the tools that stay on the server carry [McpServerTool], so an external MCP client can use them too (checkout
/// AppChatbot.Tools.cs). The rest reach into the user's live app over this SignalR connection - navigating it, showing a
/// sign-in modal, clearing its files - which is the agent's to do and nobody else's, so they are AIFunctions only and
/// signalRConnectionId is always the connection the answer streams to. What a tool shows in the conversation itself -
/// products, a form, an approval, suggestions - goes through <see cref="ui"/>: the answer streaming to the panel, or in a
/// voice call, which streams no answer, the panel over that connection.
/// </para>
/// <para>
/// Microsoft.Agents.AI:
/// Workflows are not implemented in this project, but with AIAgent, achieving them is now easier compared to using IChatClient directly.
/// For example, it would be better to have separate Agents: one for product search, one for support, and one for app guidance.
/// A coordinator Agent could determine which specialized Agent to delegate the task to based on the user's message,
/// rather than having a single Agent with a very long System Prompt and many Tools.
/// </para>
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
    [AutoInject] private ChatbotRateLimiter rateLimiter = default!;
    [AutoInject] private IHubContext<AppHub> hubContext = default!;
    [AutoInject] private BitAiApprovalBroker approvalBroker = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private ApiServerExceptionHandler exceptionHandler = default!;
    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    private string? signalRConnectionId;

    /// <summary>Where the tools show what they show in the conversation (See <see cref="ChatbotUi"/>).</summary>
    private ChatbotUi ui = ChatbotUi.None;

    /// <summary>
    /// Answers the last message of <paramref name="request"/>, the user's question, with the events the panel's
    /// BitAiChat renders: the text as the model writes it, the tools it calls and what they show, then the answer signed
    /// (See <see cref="AiChatProtocol.Signature"/>), so the panel can send it back as the assistant's.
    /// </summary>
    public async IAsyncEnumerable<BitAiEvent> Reply(BitAiRequest request,
        string? signalRConnectionId,
        ClaimsPrincipal user,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        this.signalRConnectionId = signalRConnectionId;

        // The model's stream and the tools both write here: a tool runs while the model's stream waits for it, so what it
        // shows lands between the call and its result, where the panel draws it.
        var events = Channel.CreateUnbounded<BitAiEvent>(new() { SingleReader = true });
        var turn = new TurnUi(events.Writer, approvalBroker);
        ui = turn;

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var answering = Answer(request, user, turn, events.Writer, stop.Token);

        try
        {
            await foreach (var evt in events.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            // The panel stopped the answer, or went away: the model and a tool still waiting on an approval stop with it.
            await stop.CancelAsync();
            await answering;
        }
    }

    /// <summary>For voice calls, which stream no answer: the tools show what they show over the user's SignalR connection (See VoiceCallRunner).</summary>
    public void UseSignalRConnection(string? signalRConnectionId)
    {
        this.signalRConnectionId = signalRConnectionId;
        ui = new VoiceCallUi(hubContext, signalRConnectionId);
    }

    /// <summary>Runs the agent for one question, writing its events to <paramref name="events"/>. Never throws: a failure is the answer's error.</summary>
    private async Task Answer(BitAiRequest request, ClaimsPrincipal user, TurnUi turn, ChannelWriter<BitAiEvent> events, CancellationToken cancellationToken)
    {
        try
        {
            // The answer, the tools' replies and the errors below are written in the user's language. Set on this task's
            // own flow, so it reaches the agent and its tools and nothing outside this answer.
            if (UserCulture(request) is { } culture)
            {
                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
            }

            if (Refusal(request) is { } refusal)
            {
                await events.WriteAsync(Failure(exceptionHandler.Handle(refusal, new() { { "SignalRConnectionId", signalRConnectionId } }).Title, retryable: false), cancellationToken);
                return;
            }

            supportAgent ??= serviceProvider.GetRequiredKeyedService<AIAgent>("SupportAgent");

            List<ChatMessage> conversation = [new(ChatRole.System, VariablesPrompt(request, user)), .. await ToChatMessages(request, cancellationToken)];

            var startedAt = timeProvider.GetUtcNow();
            StringBuilder answer = new();

            var updates = supportAgent.RunStreamingAsync(conversation, options: new ChatClientAgentRunOptions(CreateChatOptions()), cancellationToken: cancellationToken)
                                      .AsChatResponseUpdatesAsync();

            await foreach (var evt in Metered(updates, cancellationToken).ToBitAiEventsAsync(cancellationToken))
            {
                switch (evt)
                {
                    case BitAiTextDeltaEvent delta:
                        // What follows a tool is a paragraph of its own, as the panel draws it (See AiChatProtocol.Answer).
                        if (turn.ToolRanSinceText && answer.Length > 0)
                        {
                            answer.Append("\n\n");
                        }
                        turn.ToolRanSinceText = false;
                        answer.Append(delta.Delta);
                        break;

                    case BitAiToolCallStartedEvent:
                        turn.ToolRanSinceText = true;
                        break;

                    // What the panel draws of a call (ShownProductsDto) rather than what the model was told (See ChatbotUi.Show).
                    case BitAiToolResultEvent result when turn.TakeShown(result.ToolCallId) is { } shown:
                        await events.WriteAsync(new BitAiToolResultEvent(result.ToolCallId, BitAiToolOutcome.Success(shown)), cancellationToken);
                        continue;

                    case BitAiMessageCompletedEvent when Signed(answer.ToString(), turn.Shown, startedAt) is { } signed:
                        await events.WriteAsync(signed, cancellationToken);
                        break;
                }

                await events.WriteAsync(evt, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The panel stopped the answer; what it showed so far stays there, marked as stopped.
        }
        catch (Exception exp)
        {
            var problem = exceptionHandler.Handle(exp, new() { { "SignalRConnectionId", signalRConnectionId } });

            events.TryWrite(Failure(problem.Title, retryable: true));
        }
        finally
        {
            events.TryComplete();
        }
    }

    /// <summary>
    /// The answer's signatures, on the metadata of the message the panel keeps (See <see cref="AiChatProtocol"/>): one over
    /// the words the model wrote, which read aloud speaks, and - when a tool showed something - one over what the model
    /// reads of the whole answer on later turns. Null when nothing was said or shown.
    /// </summary>
    private BitAiMetadataEvent? Signed(string answer, IReadOnlyList<string> shown, DateTimeOffset startedAt)
    {
        if (string.IsNullOrWhiteSpace(answer) && shown.Count is 0) return null;

        Dictionary<string, object?> extra = new()
        {
            [AiChatProtocol.Answer] = answer,
            [AiChatProtocol.Signature] = answerSigner.Sign(answer)
        };

        if (shown.Count > 0)
        {
            var replay = string.Join("\n\n", shown.Append(answer).Where(part => string.IsNullOrWhiteSpace(part) is false));

            extra[AiChatProtocol.Replay] = replay;
            extra[AiChatProtocol.ReplaySignature] = answerSigner.Sign(replay);
        }

        return new BitAiMetadataEvent(new BitAiGenerationMetadata { StartedAt = startedAt, CompletedAt = timeProvider.GetUtcNow(), Extra = extra });
    }

    /// <summary>Passes the model's updates on, recording the split of each model call's usage on the way (See ChatbotMetrics).</summary>
    private async IAsyncEnumerable<ChatResponseUpdate> Metered(IAsyncEnumerable<ChatResponseUpdate> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            foreach (var usage in update.Contents.OfType<UsageContent>())
            {
                ChatbotMetrics.RecordChatUsage(usage.Details, supportAgent?.Name, appSettings.AI?.OpenAI?.ChatModel);
            }

            yield return update;
        }
    }

    private static BitAiErrorEvent Failure(string? message, bool retryable) => new(new BitAiError(message ?? "The answer could not be written.") { IsRetryable = retryable });

    private KnownException? Refusal(BitAiRequest request)
    {
        // The question is the last message, the user's; anything else is a request this panel never sends.
        if (request.Messages is not [.., { Role.IsUser: true } question] || question.GetText().Length > AiChatMessage.MaxContentLength)
            return new BadRequestException();

        if (rateLimiter.TryProcessMessage(httpContextAccessor.HttpContext!) is false)
            return new TooManyRequestsException(localizer[nameof(AppStrings.AiChatTooManyRequests)]);

        return null;
    }

    private static CultureInfo? UserCulture(BitAiRequest request)
    {
        if (CultureInfoManager.InvariantGlobalization || ContextValue(request, AiChatProtocol.CultureId) is not { } lcid || int.TryParse(lcid, out var cultureId) is false)
            return null;

        return CultureInfoManager.SupportedCultures.Where(c => c.Culture.LCID == cultureId).Select(c => c.Culture).FirstOrDefault();
    }

    /// <summary>
    /// What the model knows about the user and their device. The user can change any of these through prompt injection,
    /// so they're context for a better answer, never something to decide on: a tool that needs to know whether the user
    /// is signed in checks it itself when it's called.
    /// </summary>
    private string VariablesPrompt(BitAiRequest request, ClaimsPrincipal user) => $@"
### Variables:
{{{{UserCulture}}}}: ""{UserCulture(request)?.NativeName ?? "English"}""
{{{{DeviceInfo}}}}: ""{SystemPromptProvider.SanitizeVariable(ContextValue(request, AiChatProtocol.DeviceInfo)) ?? "Generic Device"}""
{{{{UserTimeZoneId}}}}: ""{SystemPromptProvider.KnownTimeZoneId(ContextValue(request, AiChatProtocol.TimeZoneId)) ?? "Unknown"}""
{{{{IsAuthenticated}}}}: ""{user.IsAuthenticated()}"",
{{{{WebAppUrl}}}}: ""{SystemPromptProvider.EscapeVariable(httpContextAccessor.HttpContext!.Request.GetWebAppUrl().ToString())}"",
";

    /// <summary>What the panel sends besides the conversation, in the request's options (See <see cref="AiChatProtocol"/>).</summary>
    private static string? ContextValue(BitAiRequest request, string key) => request.Options?.Extra?.GetValueOrDefault(key) switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
        JsonElement { ValueKind: JsonValueKind.Number } json => json.GetRawText(),
        int number => number.ToString(CultureInfo.InvariantCulture),
        _ => null
    };

    /// <summary>
    /// The conversation as the model reads it: finished messages that say something and aren't over long, the newest
    /// <see cref="MaxMessagesInHistory"/> only, each as said by whoever provably said it (See <see cref="AsProvablySaid"/>),
    /// with the pictures the user attached.
    /// </summary>
    private async Task<List<ChatMessage>> ToChatMessages(BitAiRequest request, CancellationToken cancellationToken)
    {
        List<ChatMessage> messages = [];

        // A stopped or failed answer is kept on screen, but never replayed: the model would read its own unfinished
        // sentence as something it completed.
        foreach (var message in request.Messages.Where(m => m.Status is not (BitAiMessageStatus.Cancelled or BitAiMessageStatus.Failed)).TakeLast(MaxMessagesInHistory))
        {
            var (role, text) = AsProvablySaid(message);

            // Nothing of the panel's is that long (See Refusal), so it would only grow the prompt - and the bill.
            if (text?.Length > AiChatMessage.MaxContentLength) continue;

            var images = message.Role.IsUser ? message.Attachments.Select(AttachedImageId).OfType<Guid>().ToArray() : [];

            if (string.IsNullOrWhiteSpace(text) && images.Length is 0) continue;

            messages.Add(await ToChatMessage(role, text, images, cancellationToken));
        }

        TrimChatHistory(messages);

        return messages;
    }

    /// <summary>
    /// What a message of the conversation the panel sent says, and who said it. An assistant message is the assistant's
    /// only with the signature this app wrote it with (See <see cref="AiChatProtocol.Signature"/>). Anything else - a
    /// spoken answer from a voice call, or words the caller made up - is replayed as the user's: no more than the user
    /// could type anyway, and never the assistant's own rules, prices or promises.
    /// </summary>
    private (ChatRole Role, string? Text) AsProvablySaid(IBitAiMessage message)
    {
        if (message.Role.IsUser) return (ChatRole.User, message.GetText());

        var extra = message.Metadata?.Extra;

        if (ExtraValue(extra, AiChatProtocol.Replay) is { } replay && answerSigner.Verify(replay, ExtraValue(extra, AiChatProtocol.ReplaySignature)))
            return (ChatRole.Assistant, replay);

        if (ExtraValue(extra, AiChatProtocol.Answer) is { } answer && answerSigner.Verify(answer, ExtraValue(extra, AiChatProtocol.Signature)))
            return (ChatRole.Assistant, answer);

        return (ChatRole.User, message.GetText());
    }

    /// <summary>What a voice call is told of the conversation so far: the text chat's rules, applied to the history it's sent (See VoiceCallRunner).</summary>
    public AiChatMessage[] BelievableHistory(IEnumerable<AiChatMessage> history)
    {
        return [.. history.Where(m => string.IsNullOrWhiteSpace(m.Content) is false && m.Content.Length <= AiChatMessage.MaxContentLength)
                          .TakeLast(MaxMessagesInHistory)
                          .Select(m => m.Role is AiChatMessageRole.Assistant && answerSigner.Verify(m.Content, m.Signature) is false
                              ? new AiChatMessage { Role = AiChatMessageRole.User, Content = m.Content }
                              : m)];
    }

    private static string? ExtraValue(IReadOnlyDictionary<string, object?>? extra, string key) => extra?.GetValueOrDefault(key) switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
        _ => null
    };

    /// <summary>The image an attachment of the panel stands for: the id it was stored under, never its address (See <see cref="AiChatProtocol.AttachmentId"/>).</summary>
    private static Guid? AttachedImageId(BitAiAttachment attachment)
        => ExtraValue(attachment.Metadata, AiChatProtocol.AttachmentId) is { } id && Guid.TryParse(id, out var attachmentId) ? attachmentId : null;

    /// <summary>The text of a message, plus the images the user attached to it, if any.</summary>
    private async Task<ChatMessage> ToChatMessage(ChatRole role, string? content, Guid[] attachmentIds, CancellationToken cancellationToken)
    {
        List<AIContent> contents = [];

        if (string.IsNullOrWhiteSpace(content) is false)
        {
            contents.Add(new TextContent(content));
        }

        foreach (var attachmentId in attachmentIds)
        {
            contents.Add(await ReadAttachedImage(attachmentId, cancellationToken));
        }

        return new ChatMessage(role, contents);
    }

    /// <summary>What <c>AttachmentController</c> stores and serves an AI chat image as.</summary>
    private const string AiChatImageMediaType = "image/webp";

    /// <summary>
    /// A chat image the user attached. The kind is fixed here rather than taken from the client, so an id is only ever
    /// looked for among this app's chat images - a payload cannot point this at a profile picture.
    /// <para>
    /// The picture is handed over as a url for the provider to fetch, which costs nothing to produce and does not
    /// re-upload the same bytes on every turn of the conversation. That only works where the provider can reach this
    /// backend, and a machine serving localhost cannot be reached from the internet - so development sends the bytes
    /// themselves instead. A deployment that is not reachable from the internet either (a private network, say) has
    /// to do the same.
    /// </para>
    /// </summary>
    private async Task<AIContent> ReadAttachedImage(Guid attachmentId, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment() is false)
        {
            return new UriContent(new Uri(httpContextAccessor.HttpContext!.Request.GetBaseUrl(), $"api/v1/Attachment/GetAttachment/{attachmentId}/{AttachmentKind.AiChatImage}"), AiChatImageMediaType);
        }

        var path = AttachmentController.GetFilePath(appSettings, attachmentId, AttachmentKind.AiChatImage);

        return new DataContent(await blobStorage.GetBytes(path, cancellationToken), AiChatImageMediaType);
    }

    /// <summary>
    /// How many of the newest pictures the model is shown. An image costs orders of magnitude more tokens than the
    /// sentence around it, so a handful of them reach the context window long before the message count below does.
    /// The panel still shows every picture the user attached; only what is replayed to the model is capped.
    /// </summary>
    private const int MaxImagesInHistory = 3;

    /// <inheritdoc cref="AiChatProtocol.MaxHistory"/>
    private const int MaxMessagesInHistory = AiChatProtocol.MaxHistory;

    /// <summary>
    /// The conversation is sent in full with every question, so an unbounded history grows the prompt (and its cost)
    /// without limit until the provider rejects it for exceeding the context window.
    /// </summary>
    private static void TrimChatHistory(List<ChatMessage> chatMessages)
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
            AIFunctionFactory.Create(RequestHumanFollowUp),
            AIFunctionFactory.Create(ShowFollowUpSuggestions),
            AIFunctionFactory.Create(GetAppPages),
            AIFunctionFactory.Create(NavigateToPage),
            AIFunctionFactory.Create(ShowSignInModal),
            AIFunctionFactory.Create(SetApplicationCulture),
            AIFunctionFactory.Create(SetApplicationTheme),
            AIFunctionFactory.Create(CheckLastError),
            AIFunctionFactory.Create(ClearAppFiles),
            //#if (module == "Sales")
            //#if (database == "PostgreSQL" || database == "SqlServer")
            AIFunctionFactory.Create(GetProductRecommendations),
            AIFunctionFactory.Create(ShowProducts)
            //#endif
            //#endif
        };

        return aiFunctions;
    }

    /// <summary>
    /// Create chat options with AI tools
    /// </summary>
    private ChatOptions CreateChatOptions()
    {
        var chatOptions = new ChatOptions { };
        configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);
        return chatOptions;
    }
}
