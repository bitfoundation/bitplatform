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

    private int responseCounter;
    private Channel<AiChatMessageRequest>? channel;
    private AiChatMessageResponse? lastAssistantMessage;

    /// <summary>
    /// The line the panel opens on - the one assistant message the assistant did not write, which is why read aloud
    /// is not offered on it: the backend only speaks answers it has a record of writing.
    /// </summary>
    private AiChatMessageResponse? greetingMessage;
    private List<AiChatMessageResponse> chatMessages = []; // TODO: Persist these values in client-side storage to retain them across app restarts.
    private List<string> followUpSuggestions = [];
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

        await base.OnAfterFirstRenderAsync();
    }


    private async Task HubConnection_Reconnected(string? _)
    {
        if (channel is null) return;
        await RestartChannel();
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
        // The enter key and the suggestions arrive here without going through the send button the loading state is
        // holding shut, so a second send can otherwise start on top of one still uploading.
        if (isSending) return;

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
                _ = StartChannel();
            }

            isLoading = true;

            var message = new AiChatMessageRequest
            {
                Content = userInput,
                AttachmentId = pendingAttachmentId
            };

            userInput = string.Empty;

            chatMessages.Add(new()
            {
                Role = AiChatMessageRole.User,
                Content = message.Content,
                AttachmentId = message.AttachmentId
            });

            if (pendingAttachment is not null)
            {
                pendingAttachment = null;
                pendingAttachmentId = null;
                await attachmentUploadRef.Reset(); // So the same file can be picked again for the next message.
            }

            lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
            chatMessages.Add(lastAssistantMessage);

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
        // The answer read aloud was following is one of the messages being thrown away.
        await StopReadAloud();

        SetDefaultValues();

        await RestartChannel();
    }

    private void SetDefaultValues()
    {
        isLoading = false;
        responseCounter = 0;
        followUpSuggestions = [];
        lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        greetingMessage = new()
        {
            Role = AiChatMessageRole.Assistant,
            Content = Localizer[nameof(AppStrings.AiChatPanelInitialResponse), string.IsNullOrEmpty(CurrentUser?.DisplayName) ? string.Empty : $" {CurrentUser.DisplayName}"],
        };
        chatMessages = [greetingMessage];
    }

    private async Task HandleOnDismissPanel()
    {
        await StopDictation();

        await StopReadAloud();

        await StopChannel();
    }


    private async Task CopyMessage(AiChatMessageResponse message)
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

    private async Task StartChannel()
    {
        channel = Channel.CreateUnbounded<AiChatMessageRequest>(new() { SingleReader = true, SingleWriter = true });

        // The following code streams user's input messages to the server and processes the streamed responses.
        // It keeps the chat ongoing until CurrentCancellationToken is cancelled.
        await foreach (var response in hubConnection.StreamAsync<string>(SharedAppMessages.StartChat,
                                                                         new StartChatRequest()
                                                                         {
                                                                             CultureId = CultureInfo.CurrentCulture.LCID,
                                                                             TimeZoneId = TimeZoneInfo.Local.Id,
                                                                             DeviceInfo = TelemetryContext.Platform,
                                                                             ChatMessagesHistory = chatMessages
                                                                         },
                                                                         channel.Reader.ReadAllAsync(CurrentCancellationToken),
                                                                         cancellationToken: CurrentCancellationToken))
        {
            int expectedResponsesCount = chatMessages.Count(c => c.Role is AiChatMessageRole.User);

            if (response.Contains(nameof(AiChatFollowUpList.FollowUpSuggestions)))
            {
                followUpSuggestions = JsonSerializer.Deserialize<AiChatFollowUpList>(response)?.FollowUpSuggestions ?? [];
            }
            else
            {
                if (response is SharedAppMessages.MESSAGE_PROCESS_SUCCESS)
                {
                    responseCounter++;
                    isLoading = false;
                    await ReadAloudCompletedAnswer(); // The answer is whole, so there is something worth reading out.
                }
                else if (response is SharedAppMessages.MESSAGE_PROCESS_ERROR)
                {
                    responseCounter++;
                    if (responseCounter == expectedResponsesCount)
                    {
                        isLoading = false; // Hide loading only if this is an error for the last user's message.
                    }
                    chatMessages[responseCounter * 2].Successful = false;
                }
                else
                {
                    if ((responseCounter + 1) == expectedResponsesCount)
                    {
                        lastAssistantMessage!.Content += response;
                    }
                }
            }

            StateHasChanged();
        }
    }

    private async Task StopChannel()
    {
        if (channel is null) return;

        channel.Writer.Complete();
        channel = null;
    }

    private async Task RestartChannel()
    {
        await StopChannel();
        await StartChannel();
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

        await StopChannel();

        await base.DisposeAsync(disposing);
    }
}
