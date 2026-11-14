//+:cnd:noEmit
using System.Net;
using System.Runtime.CompilerServices;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Shared.Features.Attachments;
//#if (module == "Sales")
//#if (database == "PostgreSQL" || database == "SqlServer")
using Boilerplate.Shared.Features.Products;
//#endif
//#endif

namespace Boilerplate.Client.Core.Components.Layout.AiChat;

/// <summary>
/// The AI chat itself, on Bit.BlazorUI.AI's BitAiChat: the conversation, the composer with its images and dictation, read
/// aloud, the tools' cards and the voice call. Each answer streams from <c>AppHub</c> (a BitAiHub) over the app's own
/// SignalR connection, through Bit.BlazorUI.AI.SignalR's transport, and the conversation stays on the device
/// (AppAiChat.razor.History.cs).
/// <para>
/// AppAiChatPanel renders it only once the lazily loaded AI assemblies are. The router reads every type of this assembly
/// as the app starts, before then, so no type here may derive from or implement one of theirs (AiChatStrings lives in
/// Shared for that reason), and none of theirs appears in a public member here.
/// </para>
/// </summary>
public partial class AppAiChat : IAppAiChat
{
    /// <summary>The largest image the composer takes; the server refuses anything larger (See AttachmentController).</summary>
    private const long MaxImageSize = 10 * 1024 * 1024;

    /// <summary>The model is shown three pictures of the conversation at most (See AppChatbot.MaxImagesInHistory).</summary>
    private const int MaxImagesPerMessage = 3;

    /// <summary>The largest recording the speech endpoint accepts (See ChatbotController.MaxSpeechUploadSizeBytes).</summary>
    private const long MaxRecordingSize = 2 * 1024 * 1024;

    /// <summary>The voice reads slowly at 1; browsers keep its pitch.</summary>
    private const double ReadAloudPlaybackRate = 1.25;

    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }

    /// <summary>A prompt of the app's own the chat sends as it opens, once it has read its conversation back (See AppAiChatPanel.OpenAndSend).</summary>
    [Parameter] public string? InitialPrompt { get; set; }

    /// <summary>What the floating button shows of the chat while the panel is closed: a call going on, a question waiting.</summary>
    [Parameter] public EventCallback<AppAiChatStatus> OnStatusChanged { get; set; }

    /// <summary>
    /// Asks the user to sign in, with the title and message saying why, and answers whether they did. The panel closes
    /// meanwhile, so the modal isn't caught in its focus trap (See AppAiChatPanel.EnsureSignedIn).
    /// </summary>
    [Parameter, EditorRequired] public Func<string, string, Task<bool>> EnsureSignedIn { get; set; } = default!;

    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private AppHubConnection hubConnection = default!;
    [AutoInject] private ILogger<AppAiChat> logger = default!;

    private BitAiChat? chat;
    private BitAiReadAloud? tunedReadAloud;
    private BitAiChatSession? session;
    private BitAiUiOptions uiOptions = default!;
    private IBitComponentParams[] parameters = [];
    private readonly BitAiRendererRegistry renderers = new();
    private string[] suggestions = [];
    private BitAiSlashCommand[] commands = [];
    private AppAiChatStatus status = new(IsInVoiceCall: false, NeedsAnswer: false);

    private string GreetingText
        => Localizer[nameof(AppStrings.AiChatPanelInitialResponse), string.IsNullOrWhiteSpace(CurrentUser?.DisplayName) ? string.Empty : $" {CurrentUser.DisplayName}"];

    protected override async Task OnInitAsync()
    {
        var strings = new AiChatStrings(Localizer);

        uiOptions = BitAiUiOptions.Default with
        {
            Strings = strings,
            ShowTimestamps = true,
            // The zone the user picked, rather than the device's (See TimeZoneService)
            TimeZone = await TimeZoneService.GetCurrentTimeZone(),
            // What each answer cost and which model wrote it, for the developer tuning the prompt and the settings
            ShowUsage = AppEnvironment.IsDevelopment(),
            ShowModelName = AppEnvironment.IsDevelopment(),
            // A call that is a step of its answer's timeline is shown there instead (See AppChatbot.AnswerSteps)
            ToolCallDisplay = call => IsStep(call) ? BitAiToolCallDisplay.Hidden : AppAiChatTools.Display(call)
        };

        // Through BitParams (See AppAiChat.razor): BitAiChat renders the prompt input, the voice input, the approval card
        // and an answer's steps itself, and has no parameters of its own for these. Here rather than in AppBitParams, which
        // renders as the app starts: Bit.BlazorUI.AI loads only once the user reaches for the chat, so nothing at startup can
        // name its types.
        parameters =
        [
            // Accept only narrows the file dialog; the composer checks a drop and a paste against it too
            new BitAiPromptInputParams { Accept = ImageUploadContentTypes.Accept, MaxAttachments = MaxImagesPerMessage },
            new BitAiVoiceInputParams { MaxRecordingSize = MaxRecordingSize },
            new BitAiToolApprovalParams { DetailTemplate = ApprovalDetails },
            // A step's title already says what it called, in the user's language; the tags would repeat it by the tool's name
            new BitAiAgentRunViewParams { Classes = new() { Step = new() { ToolCalls = "step-tool-calls" } } },
            // A line the size of a tool call's rather than a box: an answer that reasons between its calls shows several
            new BitAiReasoningPartViewParams
            {
                Classes = new()
                {
                    Root = "reasoning",
                    Icon = "reasoning-icon",
                    Body = "reasoning-body",
                    Collapsible = new() { Accordion = new() { Header = "reasoning-toggle" } }
                }
            }
        ];

        commands =
        [
            new("clear", Localizer[nameof(AppStrings.Clear)], BitIconName.Delete) { Handler = WrapHandled(Clear) },
            new("regenerate", Localizer[nameof(AppStrings.AiChatRegenerate)], BitIconName.Refresh) { Handler = WrapHandled(Regenerate) }
        ];

        suggestions =
        [
            Localizer[nameof(AppStrings.AiChatPanelPrompt1)],
            Localizer[nameof(AppStrings.AiChatPanelPrompt2)],
            Localizer[nameof(AppStrings.AiChatPanelPrompt3)]
        ];

        RegisterRenderers();

        // Bit.BlazorUI.AI.SignalR's transport, over the app's own connection: each answer streams from AppHub (a BitAiHub),
        // with as much of the conversation as the server reads
        var transport = new AiChatHistoryWindow(CreateTransport(await hubConnection.GetHubConnection(CurrentCancellationToken)));

        session = new BitAiChatSession(transport, await RestoreHistory(), new()
        {
            Strings = strings,
            RequestOptions = new() { Extra = await RequestContext() },
            AutoTitle = false,
            // The server runs every tool: the panel only shows them, and asks for the approval ClearAppFiles waits on
            ExecuteTools = false,
            // A message sent while an answer is still coming stops that answer and is answered instead
            InterruptOnSubmit = true
        });

        foreach (var call in session.Conversation.Messages.SelectMany(message => message.PartsOf<BitAiToolCallPart>()))
        {
            Describe(call);
        }

        session.Processor.EventApplied += OnEventApplied;
        session.TurnCompleted += OnTurnCompleted;
        session.StateChanged += OnSessionStateChanged;

        if (string.IsNullOrWhiteSpace(InitialPrompt) is false)
        {
            // Not awaited, so the chat renders while the answer streams; what goes wrong is still handled, as an awaited one's would be
            _ = WrapHandled<string>(SendPrompt)(InitialPrompt);
        }

        await base.OnInitAsync();
    }

    protected override async Task OnParamsSetAsync()
    {
        // CurrentUser cascades, so this is where a sign in or a sign out reaches the chat (See AppAiChat.razor.History.cs).
        await SyncHistoryOwner();

        await base.OnParamsSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The chat makes a new reader whenever reading aloud comes back - after a voice call - and is itself new after a
        // change of hands (See SyncHistoryOwner), so each one is set up as it appears
        if (chat?.ReadAloud is { } readAloud && readAloud != tunedReadAloud)
        {
            readAloud.PlaybackRate = ReadAloudPlaybackRate;
            tunedReadAloud = readAloud;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await InitializeVoiceCall();

        await base.OnAfterFirstRenderAsync();
    }

    /// <summary>
    /// Sends <paramref name="prompt"/> as the user's next message: a suggestion of the app's own (See AppAiChatPanel's
    /// SEARCH_PRODUCTS), or a card's (a product to ask about). Into the call, during one.
    /// </summary>
    public async Task SendPrompt(string prompt)
    {
        if (session is null) return;

        if (isInVoiceCall)
        {
            await SendVoiceCallMessage(prompt);
            return;
        }

        await RefreshRequestContext();

        await session.SubmitAsync(prompt);
    }

    /// <summary>Starts over: stops the answer, the reading and the call, and forgets the conversation on this device too.</summary>
    public async Task Clear()
    {
        if (session is null) return;

        session.Stop();

        await EndVoiceCall();

        if (chat?.ReadAloud is { } readAloud)
        {
            await readAloud.StopAsync();
        }

        session.Conversation.Clear();

        await ForgetHistory(); // Clear means gone, not gone from the screen.

        RaiseStatusChanged();
    }

    /// <summary>The /regenerate command: the last answer again, as its own regenerate button would.</summary>
    private async Task Regenerate()
    {
        if (session is null || session.Conversation.Messages.Any(message => message.Role.IsAssistant) is false) return;

        await session.RegenerateAsync(cancellationToken: CurrentCancellationToken);
    }

    /// <summary>Whether a call is one of its answer's steps (See AppChatbot.AnswerSteps), whose step id is the call's own.</summary>
    private bool IsStep(BitAiToolCallPart call)
        => session?.Conversation.Runs.Values.Any(run => run.Steps.Any(step => step.StepId == call.ToolCallId)) is true;

    /// <summary>The panel closed: what's read aloud stops, while a voice call and an answer go on.</summary>
    public async Task OnPanelClosed()
    {
        if (chat?.ReadAloud is { } readAloud)
        {
            await readAloud.StopAsync();
        }
    }

    private async Task Submit(BitAiPromptSubmission submission)
    {
        try
        {
            if (isInVoiceCall)
            {
                if (await SendVoiceCallMessage(submission.Text) is false)
                {
                    submission.Reject();
                }

                return;
            }

            await RefreshRequestContext();

            // The images went up as they were picked (See UploadImage), so the message carries only their ids
            if (await session!.SubmitAsync(submission.Text, submission.Attachments) is null)
            {
                submission.Reject();
            }
        }
        catch
        {
            // What was typed and picked goes back in the box, so sending again is a retry
            submission.Reject();
            throw;
        }
    }

    /// <summary>The user may pick another time zone between two questions (See TimeZoneService).</summary>
    private async Task RefreshRequestContext()
    {
        session!.Options = session.Options with { RequestOptions = new() { Extra = await RequestContext() } };
    }

    /// <summary>What the server needs besides the conversation (See <see cref="AiChatProtocol"/>), sent with every question.</summary>
    private async Task<IReadOnlyDictionary<string, object?>> RequestContext() => new Dictionary<string, object?>
    {
        [AiChatProtocol.CultureId] = CultureInfo.CurrentCulture.LCID.ToString(CultureInfo.InvariantCulture),
        [AiChatProtocol.TimeZoneId] = (await TimeZoneService.GetCurrentTimeZone()).Id,
        [AiChatProtocol.DeviceInfo] = TelemetryContext.Platform
    };

    /// <summary>
    /// Uploads an image the moment it's picked, dropped or pasted, so sending the message waits for nothing: the message
    /// carries the id it was stored under (See <see cref="AiChatProtocol.AttachmentId"/>), and the conversation kept on
    /// the device holds its address rather than its bytes.
    /// </summary>
    private async Task<BitAiAttachment> UploadImage(BitAiAttachment image, IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (await EnsureSignedIn(Localizer[nameof(AppStrings.AiChatPanelImageSignInTitle)], Localizer[nameof(AppStrings.AiChatPanelImageSignInMessage)]) is false)
            return image with { Status = BitAiAttachmentStatus.Failed, Error = Localizer[nameof(AppStrings.AiChatPanelImageSignInMessage)] };

        try
        {
            await using var stream = await image.OpenReadAsync(cancellationToken);

            var content = new StreamContent(stream);
            content.Headers.ContentType = new(string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType);

            using MultipartFormDataContent form = new() { { content, "file", image.Name } };

            using var response = await httpClient.PostAsync("api/v1/Attachment/UploadAiChatImage", form, cancellationToken);

            var attachmentId = Guid.Parse((await response.Content.ReadAsStringAsync(cancellationToken)).Trim('"'));
            var url = new Uri(AbsoluteServerAddress, $"api/v1/Attachment/GetAttachment/{attachmentId}/{AttachmentKind.AiChatImage}").ToString();

            progress.Report(1);

            return image with
            {
                Url = url,
                ThumbnailUrl = url,
                Data = null,
                ContentFactory = null,
                Status = BitAiAttachmentStatus.Ready,
                Metadata = new Dictionary<string, object?> { [AiChatProtocol.AttachmentId] = attachmentId.ToString() }
            };
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            // The composer shows no exception's text: the server's own reason where it gave one (a known exception is
            // localized), else a word of the app's
            logger.LogWarning(exp, "An image of the AI chat could not be uploaded.");

            return image with { Status = BitAiAttachmentStatus.Failed, Error = exp is KnownException ? exp.Message : Localizer[nameof(AppStrings.AiChatPanelAttachmentFailed)] };
        }
    }

    /// <summary>
    /// What a recording of the composer's microphone said. Recording is free, so the microphone opens for anyone; this
    /// request is the part that needs an account.
    /// </summary>
    private async Task<string?> Transcribe(Stream recording, string contentType, CancellationToken cancellationToken)
    {
        if (await EnsureSignedInForSpeech() is false) return null;

        var audio = new StreamContent(recording);
        // MediaRecorder reports codecs beside the media type ("audio/webm; codecs=opus"), which not every server parses
        audio.Headers.ContentType = new(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Split(';')[0]);

        using MultipartFormDataContent form = new() { { audio, "file", "recording" } };

        using var response = await httpClient.PostAsync("api/v1/Chatbot/TranscribeSpeech", form, cancellationToken);

        var transcription = await response.Content.ReadFromJsonAsync(JsonSerializerOptions.GetTypeInfo<TranscribeSpeechResponseDto>(), cancellationToken);

        return transcription?.Text;
    }

    /// <summary>
    /// The audio of an answer, synthesized on the server - which only speaks words its assistant wrote, so it's handed the
    /// answer's signature (See <see cref="AiChatProtocol.Signature"/>). An unsigned message - the greeting, a voice call's
    /// transcript - has nothing to read.
    /// </summary>
    private async Task<BitAiSpeechAudio?> Synthesize(BitAiMessage reply, CancellationToken cancellationToken)
    {
        if (ExtraValue(reply, AiChatProtocol.Answer) is not { Length: > 0 } answer || ExtraValue(reply, AiChatProtocol.Signature) is not { } signature)
            return null;

        if (await EnsureSignedInForSpeech() is false) return null;

        using var response = await httpClient.PostAsJsonAsync("api/v1/Chatbot/SynthesizeSpeech",
                                                              new SynthesizeSpeechRequestDto { Text = answer, Signature = signature },
                                                              JsonSerializerOptions.GetTypeInfo<SynthesizeSpeechRequestDto>(),
                                                              cancellationToken);

        // Nothing but a code block or a picture leaves no words once the server has stripped the markdown
        if (response.StatusCode is HttpStatusCode.NoContent) return null;

        return BitAiSpeechAudio.FromBytes(await response.Content.ReadAsByteArrayAsync(cancellationToken),
                                          response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg");
    }

    /// <summary>
    /// What both halves of speech say when they need an account. To the user they are one thing - talking to the chat and
    /// being talked back to - so they ask for it in one set of words rather than two.
    /// </summary>
    private Task<bool> EnsureSignedInForSpeech()
        => EnsureSignedIn(Localizer[nameof(AppStrings.AiChatPanelSpeechSignInTitle)], Localizer[nameof(AppStrings.AiChatPanelSpeechSignInMessage)]);

    /// <summary>Starter code: replace the log with your own store of the answers users rate, next to the chatbot's metrics.</summary>
    private void RecordFeedback(BitAiMessageFeedback feedback)
    {
        logger.LogInformation("The answer {MessageId} was rated {Feedback}.", feedback.Message.MessageId, feedback.Feedback);
    }

    /// <summary>The tools whose calls the panel draws as cards of its own, from what the call carries (the cards are in AppAiChat.razor).</summary>
    private void RegisterRenderers()
    {
        renderers.RegisterTool(AiChatProtocol.Tools.RequestHumanFollowUp, HumanFollowUp);

        //#if (module == "Sales")
        //#if (database == "PostgreSQL" || database == "SqlServer")
        renderers.RegisterTool(AiChatProtocol.Tools.ShowProducts, Products);
        //#endif
        //#endif
    }

    // The cast stays here, out of line: the WebAssembly interpreter compiles OnInitAsync as it starts, before GetHubConnection has loaded SignalR
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IBitAiTransport CreateTransport(object hubConnection) => new BitAiSignalRTransport((HubConnection)hubConnection);

    private static string FollowUpSentKey(BitAiToolCallPart call) => $"followUpSent:{call.ToolCallId}";

    private static bool IsFollowUpSent(BitAiToolCallContext context)
        => context.Message.Extensions.GetValueOrDefault(FollowUpSentKey(context.Call)) is true or JsonElement { ValueKind: JsonValueKind.True };

    /// <summary>Kept with the conversation, so a form already sent isn't offered again when the app opens it next time.</summary>
    private async Task MarkFollowUpSent(BitAiToolCallContext context)
    {
        context.Message.Extensions[FollowUpSentKey(context.Call)] = true;
        await SaveHistory();
    }

    //#if (module == "Sales")
    //#if (database == "PostgreSQL" || database == "SqlServer")
    /// <summary>What a ShowProducts call showed (See ShownProductsDto), read once per call: the card re-renders as the answer streams.</summary>
    private readonly Dictionary<string, ShownProductsDto?> shownProducts = [];

    private ShownProductsDto? ShownProducts(BitAiToolCallPart call)
    {
        if (call.Result?.Value is not { ValueKind: JsonValueKind.Object } value) return null;

        if (shownProducts.TryGetValue(call.ToolCallId, out var shown)) return shown;

        try
        {
            shown = value.Deserialize(JsonSerializerOptions.GetTypeInfo<ShownProductsDto>());
        }
        catch (JsonException exp)
        {
            // Written by another version of the app: the call shows as nothing rather than a broken card
            logger.LogWarning(exp, "Couldn't read the products an AI chat answer showed.");
        }

        return shownProducts[call.ToolCallId] = shown;
    }
    //#endif
    //#endif

    private static string? Argument(BitAiToolCallPart call, string name)
        => call.Arguments is { ValueKind: JsonValueKind.Object } arguments && arguments.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() : null;

    /// <summary>A value the server wrote on an answer (See <see cref="AiChatProtocol"/>), as it streamed in or as it came back from the device.</summary>
    private static string? ExtraValue(BitAiMessage message, string key) => message.Metadata?.Extra?.GetValueOrDefault(key) switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
        _ => null
    };

    /// <summary>Gives a tool call the panel's title and icon for it (See <see cref="AppAiChatTools.Describe"/>), as it starts.</summary>
    private void OnEventApplied(BitAiEvent evt)
    {
        if (evt is not BitAiToolCallStartedEvent started || session is null) return;

        var call = (evt.MessageId is { } messageId ? session.Conversation.FindMessage(messageId) : session.Processor.Current)?.FindToolCall(started.ToolCallId);

        if (call is not null)
        {
            Describe(call);
        }
    }

    private void Describe(BitAiToolCallPart call)
    {
        call.Definition ??= AppAiChatTools.Describe(call.ToolName, Localizer);
    }

    private void OnTurnCompleted(BitAiMessage answer)
    {
        // Settled once the answer is: what comes back next time is what is on screen now, a stopped answer included.
        _ = InvokeAsync(SaveHistory);
    }

    private void OnSessionStateChanged() => _ = InvokeAsync(RaiseStatusChanged);

    /// <summary>Tells the panel when what the floating button shows has changed, and only then: this runs on every streamed event.</summary>
    private void RaiseStatusChanged()
    {
        var current = new AppAiChatStatus(IsInVoiceCall: isInVoiceCall,
                                          NeedsAnswer: session?.Conversation.LastMessage?.PartsOf<BitAiToolCallPart>().Any(call => call.Status is BitAiToolCallStatus.AwaitingApproval) is true);

        if (current == status) return;

        status = current;

        _ = OnStatusChanged.InvokeAsync(current);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (session is not null)
        {
            session.Processor.EventApplied -= OnEventApplied;
            session.TurnCompleted -= OnTurnCompleted;
            session.StateChanged -= OnSessionStateChanged;
        }

        await EndVoiceCall();

        voiceCallEventsSubscription?.Dispose();
        voiceCallDecisionSubscription?.Dispose();

        if (session is not null)
        {
            await session.DisposeAsync();
        }

        if (historyDb is not null)
        {
            await historyDb.DisposeAsync();
        }

        await base.DisposeAsync(disposing);
    }
}

/// <summary>What the floating button shows of the AI chat while its panel is closed (See AppAiChat.OnStatusChanged).</summary>
public sealed record AppAiChatStatus(bool IsInVoiceCall, bool NeedsAnswer);
