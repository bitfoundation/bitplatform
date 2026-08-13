namespace Boilerplate.Client.Core.Components.Layout;

// The microphone half of the panel: speech in. Its counterpart is AppAiChatPanel.razor.SpeechSynthesis.cs, and the
// two are deliberately not independent - only one of them may be live at a time, or the engine's own voice ends up in
// the transcript. Every crossing between them goes through StopDictation / PauseReadAloud.
public partial class AppAiChatPanel
{
    [AutoInject] private SpeechRecognition speechRecognition = default!;
    [AutoInject] private ILogger<AppAiChatPanel> logger = default!;


    private bool isDictationSupported;
    private bool isListening;
    private bool dictationRequested;
    private string? dictationPrefix;

    /// <summary>What the sessions that have already ended heard.</summary>
    private string dictationTranscript = string.Empty;

    /// <summary>What the session running now has heard, which is one utterance - see <see cref="StartDictationSession"/>.</summary>
    private string dictationUtterance = string.Empty;

    private IAsyncDisposable? dictationSession;


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

        // An open microphone and a speaking answer talk over each other, and the engine's own voice is what the
        // recognizer would hear. Read aloud itself stays on: it is the next answer the user wants read, not this one.
        await PauseReadAloud();

        // Whatever is already typed is kept and dictation appends to it.
        dictationPrefix = string.IsNullOrWhiteSpace(userInput) ? null : $"{userInput.TrimEnd()} ";
        dictationTranscript = string.Empty;
        dictationUtterance = string.Empty;
        dictationRequested = true;

        await StartDictationSession();
    }

    /// <summary>
    /// Opens one recognition session, which is one utterance: the engine reports what it heard and stops, and
    /// <see cref="HandleDictationEnd"/> puts the next session in its place, so dictation is a chain of them rather
    /// than one long session. Chained because a continuous session is not kept alive by the browser either - it ends
    /// on a dropped connection to the speech service or a long enough silence - and because Android's recognizer
    /// reports the results of one over and over, which is what turns a single spoken "hello" into "hellohello".
    /// The transcript and the prefix belong to the dictation rather than to the session, so they are untouched and
    /// the box keeps everything heard so far.
    /// </summary>
    private async Task StartDictationSession()
    {
        isListening = true;

        IAsyncDisposable session;

        try
        {
            session = await speechRecognition.Start(
                new()
                {
                    Lang = CultureInfoManager.InvariantGlobalization is false ? CultureInfoManager.DefaultCulture.Name : null,
                    InterimResults = true // Streams the words into the box as they are heard, so the mic is visibly live.
                },
                onResult: result => _ = InvokeAsync(() => HandleDictationResult(result)),
                onError: error => _ = InvokeAsync(() => HandleDictationError(error)),
                onEnd: () => _ = InvokeAsync(HandleDictationEnd));
        }
        catch
        {
            // Nothing is listening, so the button must not say it is - and read aloud was silenced for a dictation
            // that never began, which would otherwise stay silent until the user pressed the microphone again.
            isListening = false;
            dictationRequested = false;
            readAloudPaused = false;
            throw;
        }

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
        // A session is one utterance, so each of its results is that utterance again - the engine revising what it
        // heard, and Android repeating what it has already reported. The last one replaces the one before it rather
        // than being added to it, which is what keeps a word said once from being written twice.
        dictationUtterance = result.Transcript.Trim();

        userInput = $"{dictationPrefix}{dictationTranscript}{dictationUtterance}";

        StateHasChanged();
    }

    /// <summary>
    /// Most of what the recognizer reports is not the end of anything: it keeps listening straight through a dropped
    /// connection to the speech service, a pause too long to be silence, and the abort its own Stop produces. Ending
    /// dictation on those cuts a working session short mid-sentence, so only an error that recognition cannot come
    /// back from ends it here - the rest is left to <see cref="HandleDictationEnd"/>, which runs when it really ends.
    /// </summary>
    private async Task HandleDictationError(string error)
    {
        // Every error is logged, including the ones dictation carries on through: which of them are survivable is a
        // judgement about the browser's recognizer, and the only way to revisit it is to have seen what really
        // arrives, in what order, and what state dictation was in at the time.
        logger.LogWarning("Dictation reported {DictationError}. Listening: {IsListening}, session: {HasDictationSession}, read aloud: {IsReadAloudEnabled}.",
                          error, isListening, dictationSession is not null, readAloudEnabled);

        // A permission or hardware refusal is not followed by an end callback, so the session is released here or not
        // at all - otherwise the microphone button stays lit for a recognizer that never started.
        if (error is "not-allowed" or "service-not-allowed" or "audio-capture" or "language-not-supported")
        {
            await StopDictation();

            SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelDictation)], error is "not-allowed" or "service-not-allowed"
                ? Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]
                : Localizer[nameof(AppStrings.AiChatPanelDictationStopped), error]);

            StateHasChanged();
        }
    }

    /// <summary>
    /// The session has ended. That is not the same as the user having finished, so what it heard is added to the
    /// dictation and, unless the user asked to stop, the next session takes its place.
    /// </summary>
    private async Task HandleDictationEnd()
    {
        await ReleaseDictationSession();

        if (dictationUtterance is not "")
        {
            // Settled, and the next session's utterance is a separate one rather than more of this one.
            dictationTranscript = $"{dictationTranscript}{dictationUtterance} ";
            dictationUtterance = string.Empty;
        }

        if (dictationRequested is false)
        {
            isListening = false;
            StateHasChanged();
            return;
        }

        await StartDictationSession();

        StateHasChanged();
    }

    /// <summary>Ends dictation because the user - or the panel on their behalf - asked for it.</summary>
    private async Task StopDictation()
    {
        isListening = false;
        dictationRequested = false;

        await ReleaseDictationSession();
    }

    /// <summary>
    /// Drops the handle to one recognition session without saying anything about whether dictation itself is over.
    /// The handle is what unregisters the callbacks from the scoped <c>SpeechRecognition</c> service, so a session
    /// left undisposed keeps this component alive and lands its late events on whichever session comes next.
    /// </summary>
    private async ValueTask ReleaseDictationSession()
    {
        if (dictationSession is null) return;

        var session = dictationSession;
        dictationSession = null;
        await session.DisposeAsync();
    }
}
