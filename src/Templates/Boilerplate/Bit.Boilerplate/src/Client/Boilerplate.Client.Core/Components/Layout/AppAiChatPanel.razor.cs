//+:cnd:noEmit
using System.Threading.Channels;
using System.Text.RegularExpressions;
using Boilerplate.Shared.Features.Chatbot;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppAiChatPanel
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [CascadingParameter] public AppThemeType? CurrentTheme { get; set; }

    [CascadingParameter] public UserDto? CurrentUser { get; set; }


    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private HubConnection hubConnection = default!;
    [AutoInject] private SpeechSynthesis speechSynthesis = default!;
    [AutoInject] private SpeechRecognition speechRecognition = default!;


    private bool isReadAloudSupported;
    private bool isDictationSupported;
    private AiChatMessage? speakingMessage;

    private bool isOpen;
    private bool isLoading;
    private string? userInput;
    private bool isSmallScreen;
    private bool isListening;
    private string? dictationPrefix;
    private string dictationTranscript = string.Empty;
    private IAsyncDisposable? dictationSession;
    private int responseCounter;
    private Channel<string>? channel;
    private AiChatMessage? lastAssistantMessage;
    private List<AiChatMessage> chatMessages = []; // TODO: Persist these values in client-side storage to retain them across app restarts.
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
        (isDictationSupported, isReadAloudSupported) = (await speechRecognition.IsSupported(), await speechSynthesis.IsSupported());
        StateHasChanged();
        hubConnection.Reconnected += HubConnection_Reconnected;

        await base.OnAfterFirstRenderAsync();
    }


    private async Task HubConnection_Reconnected(string? _)
    {
        if (channel is null) return;
        await RestartChannel();
    }

    private async Task SendPromptMessage(string message)
    {
        followUpSuggestions = [];
        userInput = message;
        StateHasChanged();
        await SendMessage();
    }

    private async Task SendMessage()
    {
        // Dictation rebuilds userInput from dictationPrefix plus the whole transcript on every result, so a recognizer
        // still running across a send would refill the emptied box with the text that was just sent. Stopping first
        // also lets a final transcript still in flight land before the message is taken.
        await StopDictation();

        if (channel is null)
        {
            _ = StartChannel();
        }

        isLoading = true;

        var input = userInput;
        userInput = string.Empty;

        chatMessages.Add(new() { Content = input, Role = AiChatMessageRole.User });
        lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        chatMessages.Add(lastAssistantMessage);

        StateHasChanged();

        await channel!.Writer.WriteAsync(input!, CurrentCancellationToken);
    }

    private async Task ClearChat()
    {
        SetDefaultValues();

        await RestartChannel();
    }

    private void SetDefaultValues()
    {
        isLoading = false;
        responseCounter = 0;
        followUpSuggestions = [];
        lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        chatMessages = [
            new()
            {
                Role = AiChatMessageRole.Assistant,
                Content = Localizer[nameof(AppStrings.AiChatPanelInitialResponse), string.IsNullOrEmpty(CurrentUser?.DisplayName) ? string.Empty : $" {CurrentUser.DisplayName}"],
            }
        ];
    }

    private async Task HandleOnDismissPanel()
    {
        await StopDictation();

        await StopReadAloud();

        await StopChannel();
    }


    private async Task CopyMessage(AiChatMessage message)
    {
        if (message.Content is not { Length: > 0 } content) return;

        await clipboard.WriteText(content);

        SnackBarService.Info(Localizer[nameof(AppStrings.Copied)]);
    }

    private async Task ToggleReadAloud(AiChatMessage message)
    {
        var wasReadingThisMessage = ReferenceEquals(speakingMessage, message);

        // The engine has a single queue, so whatever is being read stops first: a second press on the same message
        // is "stop", and a press on another one replaces it rather than overlapping two answers.
        await StopReadAloud();

        if (wasReadingThisMessage || message.Content is not { Length: > 0 } content) return;

        await speechSynthesis.Speak(new SpeechUtterance
        {
            Text = ToSpeakableText(content),
            VoiceName = GetVoiceName(),
            Lang = CultureInfoManager.InvariantGlobalization is false ? CultureInfoManager.DefaultCulture.Name : null
        });

        speakingMessage = message;

        _ = MonitorReadAloud();
    }

    // Sample for customizing the voice name based on the platform and language. The default voice is used if this returns null.
    private string? GetVoiceName()
    {
        if (CultureInfoManager.InvariantGlobalization is true || CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is not "en")
            return null;

        if (TelemetryContext.Platform?.Contains("Windows", StringComparison.OrdinalIgnoreCase) is true)
            return "Microsoft Ava Multilingual Online (Natural) - English (United States)";

        return null;
    }

    /// <summary>
    /// The synthesis API raises no completion event, so asking is the only way to stop the button claiming it is still
    /// reading after the answer has been read out. The poll exists only while something is being read, and ends itself.
    /// </summary>
    private async Task MonitorReadAloud()
    {
        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(400));

            while (speakingMessage is not null && await timer.WaitForNextTickAsync(CurrentCancellationToken))
            {
                if (await speechSynthesis.IsSpeaking() || await speechSynthesis.IsPending()) continue;

                speakingMessage = null;
                await InvokeAsync(StateHasChanged);
                return;
            }
        }
        catch (Exception exp) when (exp is OperationCanceledException or JSDisconnectedException)
        {
            // The panel closed or the circuit went away mid-poll; there is nothing left to update.
        }
    }

    private async Task StopReadAloud()
    {
        if (speakingMessage is null) return;

        speakingMessage = null;

        await speechSynthesis.Cancel();
    }

    /// <summary>
    /// Dictates into the message box. The transcript is written to <c>userInput</c> and goes out through the ordinary
    /// send path, so the user reads what was heard before it is sent - a misrecognition is corrected, not delivered.
    /// </summary>
    private async Task ToggleDictation()
    {
        if (isListening)
        {
            await StopDictation();
            return;
        }

        // Whatever is already typed is kept and dictation appends to it.
        dictationPrefix = string.IsNullOrWhiteSpace(userInput) ? null : $"{userInput.TrimEnd()} ";
        dictationTranscript = string.Empty;
        isListening = true;

        var session = await speechRecognition.Start(
            new()
            {
                Lang = CultureInfoManager.InvariantGlobalization is false ? CultureInfoManager.DefaultCulture.Name : null,
                Continuous = true,    // The user decides when a prompt is finished, not a pause in speech.
                InterimResults = true // Streams the words into the box as they are heard, so the mic is visibly live.
            },
            onResult: result => _ = InvokeAsync(() => HandleDictationResult(result)),
            onError: error => _ = InvokeAsync(() => HandleDictationError(error)),
            onEnd: () => _ = InvokeAsync(HandleDictationEnd));

        // Start is an interop round trip, so a stop pressed while it was in flight already ran: it set isListening
        // to false and found no session to dispose. Assigning unconditionally would leave the microphone live behind
        // a UI that says dictation is off, with the only handle able to close it overwritten by the next start.
        if (isListening is false)
        {
            await session.DisposeAsync();
            return;
        }

        dictationSession = session;
    }

    private void HandleDictationResult(SpeechRecognitionResult result)
    {
        if (result.IsFinal)
        {
            dictationTranscript += result.Transcript;
        }

        // A non-final transcript is a preview of the utterance in progress: shown, but not yet accumulated, or the
        // engine's own revisions would be appended on top of each other.
        userInput = $"{dictationPrefix}{dictationTranscript}{(result.IsFinal ? null : result.Transcript)}";

        StateHasChanged();
    }

    /// <summary>
    /// Recognition that ends on its own - an error, or the engine simply stopping - still leaves the handle alive on
    /// both sides, and the handle is what removes the callbacks from the scoped <c>SpeechRecognition</c> service. Going
    /// through <see cref="StopDictation"/> is what releases them; otherwise every dictation leaves its callbacks (and
    /// the component they capture) registered, and a late event from a finished session lands on the next one.
    /// </summary>
    private async Task HandleDictationError(string error)
    {
        await StopDictation();

        // 'aborted' is what an ordinary Stop produces and 'no-speech' just means silence - neither is worth a message.
        if (error is not ("aborted" or "no-speech"))
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelDictation)], error is "not-allowed"
                ? Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]
                : Localizer[nameof(AppStrings.AiChatPanelDictationStopped), error]);
        }

        StateHasChanged();
    }

    /// <inheritdoc cref="HandleDictationError"/>
    private async Task HandleDictationEnd()
    {
        await StopDictation();

        StateHasChanged();
    }

    private async Task StopDictation()
    {
        isListening = false;

        if (dictationSession is null) return;

        var session = dictationSession;
        dictationSession = null;
        await session.DisposeAsync();
    }

    private async Task HandleOnUserInputEnter(KeyboardEventArgs e)
    {
        if (e.ShiftKey) return;

        await SendMessage();
    }

    private async Task StartChannel()
    {
        channel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });

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

    protected override bool CoalesceRenders => true;

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


    [GeneratedRegex(@"(?:```|~~~)[\s\S]*?(?:```|~~~)")]
    private static partial Regex FencedCodeRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex MarkdownImageRegex();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"<[a-z][a-z0-9+.-]*://[^>]*>|(?:https?://|www\.)\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"^[ \t]{0,3}(?:>+[ \t]*|(?:#{1,6}|[-*+\u2022]|\d+[.)])[ \t]+)|[*~`]", RegexOptions.Multiline)]
    private static partial Regex MarkdownMarkerRegex();

    /// <summary>Horizontal rules and the dashes under a table header, which are a line of punctuation and nothing else.</summary>
    [GeneratedRegex(@"^[-=:| \t]{3,}$", RegexOptions.Multiline)]
    private static partial Regex MarkdownRuleRegex();

    [GeneratedRegex(@"(?:[ \t]*\|[ \t]*)+")]
    private static partial Regex TableCellSeparatorRegex();

    /// <summary>
    /// The dingbat and arrow blocks (U+2600-27BF, U+2B00-2BFF and friends) plus U+1F000-1FBFF as surrogate pairs,
    /// since .NET matches one utf-16 unit at a time and \p{So} would never see an astral character. FE0F, 200D and
    /// 20E3 are the joiners emoji sequences are built from, and are left behind when their pictograph goes.
    /// </summary>
    [GeneratedRegex(@"[\u2190-\u21FF\u231A-\u231B\u23E9-\u23FA\u24C2\u25AA-\u25FE\u2600-\u27BF\u2B00-\u2BFF\uFE0F\u200D\u20E3]|[\uD83C-\uD83E][\uDC00-\uDFFF]")]
    private static partial Regex EmojiRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex ExtraSpaceRegex();

    /// <summary>
    /// Answers are markdown and a synthesizer reads the syntax out loud - "star star bit platform star star" - so the
    /// markers go and links are reduced to their label. What carries nothing once heard is dropped rather than spelled
    /// out: code blocks, image markup, urls, and emoji - a five star rating is five spoken words in front of the number
    /// that already says it, and a check mark becomes "check mark button" in front of every line it decorates.
    /// </summary>
    private static string ToSpeakableText(string markdown)
    {
        var text = FencedCodeRegex().Replace(markdown.ReplaceLineEndings("\n"), " ");
        text = MarkdownImageRegex().Replace(text, string.Empty);
        text = MarkdownLinkRegex().Replace(text, "$1");
        text = UrlRegex().Replace(text, string.Empty);
        text = EmojiRegex().Replace(text, string.Empty);
        // An underscore is emphasis around a word and the joint inside an identifier both, and a space serves either:
        // dropping it outright would hand the synthesizer "snakecasename" as one unpronounceable word.
        text = text.Replace('_', ' ');
        text = MarkdownMarkerRegex().Replace(text, string.Empty);
        text = MarkdownRuleRegex().Replace(text, string.Empty);
        text = TableCellSeparatorRegex().Replace(text, ", ");
        text = ExtraSpaceRegex().Replace(text, " ");

        // A product arrives titled, pictured and linked with the same words, so its name would be read three times over
        // - the picture is gone by now and the two that are left are neighbouring lines. Lines emptied by the markup
        // that was all they held go too, so the reading is not paced by pauses around nothing.
        List<string> lines = [];
        foreach (var line in text.Split('\n'))
        {
            var spoken = line.Trim(' ', '\t', ',');

            if (spoken.Length is 0) continue;

            if (lines.Count > 0 && string.Equals(lines[^1], spoken, StringComparison.OrdinalIgnoreCase)) continue;

            lines.Add(spoken);
        }

        return string.Join('\n', lines);
    }
}
