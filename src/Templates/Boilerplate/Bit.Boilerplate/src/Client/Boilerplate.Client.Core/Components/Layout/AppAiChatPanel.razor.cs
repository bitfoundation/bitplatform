//+:cnd:noEmit
using System.Threading.Channels;
using Boilerplate.Shared.Features.Chatbot;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Components.Layout;

// Speech in, speech out and the image a message can carry live in AppAiChatPanel.razor.Dictation.cs,
// AppAiChatPanel.razor.ReadAloud.cs and AppAiChatPanel.razor.Attachment.cs. What is left here is the conversation itself.
public partial class AppAiChatPanel
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [CascadingParameter] public AppThemeType? CurrentTheme { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }


    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private HubConnection hubConnection = default!;
    [AutoInject] private SignInModalService signInModalService = default!;


    private bool isOpen;
    private bool isLoading;
    private string? userInput;

    /// <summary>
    /// The message is on its way out - an image goes up before the message that carries it does, and the send button
    /// is the only thing on screen that can say so.
    /// </summary>
    private bool isSending;

    private bool isSmallScreen;

    /// <summary>
    /// The panel is widened to the whole viewport, for a long answer or a wide code block. It outlives a close and
    /// a reopen, the way a maximized window does.
    /// </summary>
    private bool isMaximized;

    private Channel<AiChatMessage>? channel;

    /// <summary>
    /// Ends the hub call <see cref="channel"/> was opened with. The server streams back until this says otherwise, so
    /// a conversation the panel has walked away from has to be cancelled rather than merely forgotten.
    /// </summary>
    private CancellationTokenSource? channelCancellation;
    private AiChatMessage? lastAssistantMessage;

    /// <summary>
    /// The bubbles waiting to be filled in, oldest first: the stream carries no message identity, so a frame belongs
    /// to whichever question has waited longest, and a turn ends when its document does.
    /// </summary>
    private readonly Queue<AiChatMessage> unansweredMessages = new();

    /// <summary>A turn arrives bit by bit, as one json document (See <see cref="AssistantTurn"/>). This reassembles it.</summary>
    private PartialJsonReader<AssistantTurn>? turnReader;

    /// <summary>
    /// The line the panel opens on - the one assistant message the assistant did not write, so it carries no
    /// signature: read aloud is not offered on it and the server drops it from a resent history.
    /// </summary>
    private AiChatMessage? greetingMessage;
    private List<AiChatMessage> chatMessages = [];
    private List<string> followUpSuggestions = [];

    /// <summary>More than this pushes the message box off the screen on a phone.</summary>
    private const int MaxFollowUpSuggestions = 3;

    //#if(module == "Sales")
    private Action unsubSearchProducts = default!;
    //#endif
    //#if(ads == true)
    private Action unsubAdHaveTrouble = default!;
    //#endif


    protected override Task OnInitAsync()
    {
        //#if(module == "Sales")
        unsubSearchProducts = PubSubService.Subscribe(ClientAppMessages.SEARCH_PRODUCTS, async (value) =>
        {
            if (isOpen) return;

            isOpen = true;

            StateHasChanged();

            if (chatMessages.Count > 1) return;

            var message = (string?)value;

            if (string.IsNullOrWhiteSpace(message))
            {
                message = Localizer[nameof(AppStrings.AiChatPanelPrompt3)];
            }

            await SendPromptMessage(message);
        });
        //#endif

        //#if(ads == true)
        unsubAdHaveTrouble = PubSubService.Subscribe(ClientAppMessages.AD_HAVE_TROUBLE, async _ =>
        {
            if (isOpen) return;

            isOpen = true;

            StateHasChanged();

            var message = Localizer[nameof(AppStrings.UpgradeAdHaveTroublePrompt)];

            await SendPromptMessage(message);
        });
        //#endif

        return base.OnInitAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        SetDefaultValues();
        // Recording is the one half of speech that is still a browser capability: read aloud only needs an audio
        // element to play what the backend synthesised, and every engine has one of those.
        //
        // Both are asked about because they are separate globals that go missing separately: only
        // navigator.mediaDevices needs a secure context, so over plain http MediaRecorder is there and it is not.
        isDictationSupported = await mediaRecorder.IsSupported() && await mediaDevices.IsSupported();
        StateHasChanged();
        hubConnection.Reconnected += HubConnection_Reconnected;

        await RestoreHistory();

        await base.OnAfterFirstRenderAsync();
    }

    protected override async Task OnParamsSetAsync()
    {
        // CurrentUser cascades, so this is where a sign in or a sign out reaches the panel.
        await SyncHistoryOwner();

        await base.OnParamsSetAsync();
    }


    private Task HubConnection_Reconnected(string? _)
    {
        if (channel is not null)
        {
            RestartChannel();
        }

        return Task.CompletedTask;
    }

    private async Task SendPromptMessage(string prompt)
    {
        // The prompt replaces whatever is in the box, so a recording still open would write over it a moment later.
        // Its audio is dropped rather than transcribed: the user picked a suggestion instead of saying something.
        await StopDictation();

        followUpSuggestions = [];
        userInput = prompt;
        StateHasChanged();
        await SendMessage();
    }

    private async Task SendMessage()
    {
        // e.g. the user presses enter twice, or taps a suggestion while an image is still uploading: both arrive here
        // without going through the send button that the loading state is holding shut.
        if (isSending) return;

        // The restore appends what it read to chatMessages, so a message sent while it is still running ends up above
        // the conversation it belongs to - and, having already been stored, can come back a second time beneath it.
        if (isRestoringHistory) return;

        // Rendered before anything is awaited, so the button is already saying so by the time the upload starts.
        isSending = true;
        StateHasChanged();

        try
        {
            // A recording that is still open is part of the message the user means to send, so it is stopped and
            // transcribed here - what it heard lands in the box before the box is read below.
            await StopDictation(transcribe: true);

            if (string.IsNullOrWhiteSpace(userInput) && pendingAttachment is null) return;

            // The image goes up first: the message carries the path it was stored under, and there is none until the
            // upload answers with one. A failure sends nothing and leaves both in place, so send again is a retry.
            if (await UploadPendingAttachment() is false) return;

            if (channel is null)
            {
                StartChannel();
            }

            isLoading = true;

            followUpSuggestions = [];

            // The one the panel renders is the one it sends.
            var message = new AiChatMessage
            {
                Role = AiChatMessageRole.User,
                Content = userInput,
                AttachmentId = pendingAttachmentId,
                SentAt = TimeProvider.GetUtcNow()
            };

            userInput = string.Empty;

            chatMessages.Add(message);

            await RememberMessage(message);

            if (pendingAttachment is not null)
            {
                pendingAttachment = null;
                pendingAttachmentId = null;
                await attachmentUploadRef.Reset(); // So the same file can be picked again for the next message.
            }

            lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
            chatMessages.Add(lastAssistantMessage);
            unansweredMessages.Enqueue(lastAssistantMessage);

            if (readAloudEnabled)
            {
                // The answer to this prompt is what read aloud follows from here on. What is left of the previous answer
                // plays on until this one is ready to take over, rather than the user being dropped into silence for as
                // long as the model takes to reply.
                FollowReadAloud(lastAssistantMessage);
            }

            StateHasChanged();

            await channel!.Writer.WriteAsync(message, CurrentCancellationToken);
        }
        finally
        {
            isSending = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// The conversation is open to anyone, but dictation, read aloud and attaching an image each reach an endpoint
    /// that requires a signed-in user. The modal turns a request that would come back 401 into one the user can
    /// complete without leaving the conversation, and the snack bar says why it appeared - landing on top of a
    /// conversation, the modal explains nothing on its own.
    /// </summary>
    private async Task<bool> EnsureSignedIn(string title, string message)
    {
        if ((await AuthenticationStateTask).User.IsAuthenticated()) return true;

        SnackBarService.Info(title, message);

        var wasOpen = isOpen;

        isOpen = false; // Focus on the modal, not the conversation, so the panel is closed to avoid a focus trap.
        StateHasChanged();

        var result = await signInModalService.SignIn();

        isOpen = wasOpen;
        StateHasChanged();

        return result;
    }

    /// <summary>
    /// What both halves of speech say when they need an account. To the user they are one thing - talking to the chat
    /// and being talked back to - so they ask for it in one set of words rather than two.
    /// </summary>
    private Task<bool> EnsureSignedInForSpeech()
    {
        return EnsureSignedIn(Localizer[nameof(AppStrings.AiChatPanelSpeechSignInTitle)],
                              Localizer[nameof(AppStrings.AiChatPanelSpeechSignInMessage)]);
    }

    private async Task ClearChat()
    {
        // Clearing mid restore would empty the store and then let the restore put what it had already read back on
        // screen, under the name it was just cleared from (See SyncHistoryOwner, which sits out a restore too).
        if (isRestoringHistory) return;

        // The answer read aloud was following is one of the messages being thrown away.
        await StopReadAloud();

        SetDefaultValues();

        await ForgetHistory(); // Clear means gone, not gone from the screen.

        RestartChannel();
    }

    private void SetDefaultValues()
    {
        isLoading = false;
        followUpSuggestions = [];
        turnReader = null;
        unansweredMessages.Clear();
        lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        greetingMessage = new()
        {
            Role = AiChatMessageRole.Assistant,
            Content = Localizer[nameof(AppStrings.AiChatPanelInitialResponse), string.IsNullOrWhiteSpace(CurrentUser?.DisplayName) ? string.Empty : $" {CurrentUser.DisplayName}"],
            SentAt = TimeProvider.GetUtcNow()
        };
        chatMessages = [greetingMessage];
    }

    private async Task HandleOnDismissPanel()
    {
        await StopDictation();

        await StopReadAloud();

        StopChannel();
    }


    /// <summary>
    /// When a message was written, in the zone the user picked (See <c>TimeZoneService</c>). Empty while an answer is
    /// still on its way.
    /// </summary>
    private string SentAtLabel(AiChatMessage message)
    {
        if (message.SentAt == default) return string.Empty;

        var sentAt = TimeZoneService.ToLocalTime(message.SentAt);

        return $"{sentAt:t} · {sentAt:d}";
    }

    private async Task CopyMessage(AiChatMessage message)
    {
        if (message.Content is not { Length: > 0 } content) return;

        await clipboard.WriteText(content);

        SnackBarService.Info(Localizer[nameof(AppStrings.Copied)]);
    }

    private async Task HandleOnUserInputEnter(KeyboardEventArgs e)
    {
        if (e.ShiftKey) return;

        await SendMessage();
    }

    private void StartChannel()
    {
        var newChannel = Channel.CreateUnbounded<AiChatMessage>(new() { SingleReader = true, SingleWriter = true });

        // This conversation's own, so abandoning it ends the hub call it opened rather than leaving one behind for
        // every Clear (See StopChannel). Linked, so leaving the page still ends them all.
        var newCancellation = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);

        channel = newChannel;
        channelCancellation = newCancellation;

        // Not awaited: RunChannel lives as long as the conversation does.
        _ = RunChannel(newChannel, newCancellation.Token);
    }

    /// <summary>
    /// Streams the user's input messages to the server and processes the streamed responses.
    /// It keeps the chat ongoing until this conversation's own token is cancelled.
    /// </summary>
    private async Task RunChannel(Channel<AiChatMessage> ownChannel, CancellationToken ownCancellationToken)
    {
        try
        {
            var timeZoneId = (await TimeZoneService.GetCurrentTimeZone()).Id;

            await foreach (var response in hubConnection.StreamAsync<string>(SharedAppMessages.StartChat,
                                                                             new StartChatRequest()
                                                                             {
                                                                                 CultureId = CultureInfo.CurrentCulture.LCID,
                                                                                 TimeZoneId = timeZoneId,
                                                                                 DeviceInfo = TelemetryContext.Platform,
                                                                                 ChatMessagesHistory = chatMessages
                                                                             },
                                                                             ownChannel.Reader.ReadAllAsync(ownCancellationToken),
                                                                             cancellationToken: ownCancellationToken))
            {
                // Frames belonging to a conversation the panel has already replaced (Clear, or a reconnect) are dropped.
                if (ReferenceEquals(channel, ownChannel) is false) continue;

                await ReadTurnSoFar(response);

                StateHasChanged();
            }
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp, ExceptionDisplayKind.NonInterrupting);
        }
        finally
        {
            // A stream that ends with no error at all is how the server reports one (AppHub.StartChat yields nothing),
            // so the panel is released here rather than waiting for a marker that is not coming.
            //
            // Through StopChannel rather than by hand: a turn that never closed leaves its bubble queued and the
            // reader mid document, and SendMessage starts the next channel without draining either - so the next
            // answer would stream into this one's bubble, onto the end of an abandoned document.
            if (ReferenceEquals(channel, ownChannel) && ownCancellationToken.IsCancellationRequested is false)
            {
                StopChannel();
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Adds the next piece of the turn's document and renders what it says so far - one property of that document,
    /// so nothing the stream carries around the answer can be read as part of it.
    /// </summary>
    private async Task ReadTurnSoFar(string chunk)
    {
        // A frame with no bubble waiting for it belongs to a turn the panel has already given up on.
        if (unansweredMessages.TryPeek(out var answer) is false) return;

        turnReader ??= new(JsonSerializerOptions.GetTypeInfo<AssistantTurn>());

        var turn = turnReader.Append(chunk);

        // The first thing the server writes, so even a cancelled answer says when it was asked for.
        if (turn is not null && turn.SentAt != default)
        {
            answer.SentAt = turn.SentAt;
        }

        if (turnReader.IsComplete is false)
        {
            answer.Content = turn?.Reply?.Answer ?? answer.Content;
            return;
        }

        // From here the turn is over, whichever way it went, so the next frame starts the next one.
        unansweredMessages.Dequeue();
        turnReader = null;

        if (turn?.Successful is not true)
        {
            // What streamed is kept and tagged, but not re-read: a cut-off turn has its closing spliced onto half a
            // reply.
            answer.Successful = false;

            isLoading = unansweredMessages.Count > 0;

            await RememberMessage(answer); // Tagged as it is, so what comes back next time is what is on screen now.
            return;
        }

        answer.Content = turn.Reply?.Answer ?? answer.Content;
        answer.Signature = turn.Signature; // So the panel can prove to the server that the server wrote this.

        // Written once the turn is over rather than as it streams: an answer only settles when its document closes.
        await RememberMessage(answer);

        // Part of the turn's own document, so only the newest answer's are offered.
        if (unansweredMessages.Count is 0)
        {
            followUpSuggestions = [.. (turn.Reply?.FollowUpSuggestions ?? [])
                .Where(suggestion => string.IsNullOrWhiteSpace(suggestion) is false)
                .Select(suggestion => suggestion.Trim())
                .Take(MaxFollowUpSuggestions)];

            isLoading = false;
        }

        await ReadAloudCompletedAnswer(); // The answer is whole, so there is something worth reading out.
    }

    private void StopChannel()
    {
        if (channel is null) return;

        channel.Writer.Complete();
        channel = null;

        // The hub call goes with it: nothing more is coming back on it, and left running it would hold its
        // invocation - and the scoped chatbot behind it - for as long as the connection lives.
        channelCancellation?.Cancel();
        channelCancellation?.Dispose();
        channelCancellation = null;

        // Keeps a half-written answer out of the history replayed to the model, which would otherwise read its own
        // unfinished sentence as something it completed (see AiChatMessage.Successful).
        foreach (var unanswered in unansweredMessages)
        {
            unanswered.Successful = false;
        }

        unansweredMessages.Clear();
        turnReader = null;
        isLoading = false;
    }

    private void RestartChannel()
    {
        StopChannel();

        StartChannel();
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        //#if(module == "Sales")
        unsubSearchProducts();
        //#endif

        //#if(ads == true)
        unsubAdHaveTrouble();
        //#endif

        hubConnection.Reconnected -= HubConnection_Reconnected;

        await StopDictation();

        await StopReadAloud();

        StopChannel();

        if (historyDb is not null)
        {
            await historyDb.DisposeAsync();
        }

        await base.DisposeAsync(disposing);
    }
}
