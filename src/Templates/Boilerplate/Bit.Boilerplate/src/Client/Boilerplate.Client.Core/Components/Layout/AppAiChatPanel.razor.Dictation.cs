using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout;

// The microphone half of the panel: speech in. Its counterpart is AppAiChatPanel.razor.ReadAloud.cs, and the two are
// deliberately not independent - only one of them may be live at a time, or the answer being read ends up in the
// recording. Every crossing between them goes through StopDictation / PauseReadAloud.
public partial class AppAiChatPanel
{
    /// <summary>
    /// Long enough for a sentence nobody would want to lose, short enough that the recording stays well inside
    /// everything it crosses at any bitrate a browser picks: the transcription endpoint's request size limit, and on
    /// Blazor Server the circuit too (<c>HubOptions:MaximumReceiveMessageSize</c> is derived from this). A take that
    /// reaches it is transcribed rather than thrown away.
    /// </summary>
    private static readonly TimeSpan MaxDictationDuration = TimeSpan.FromMinutes(1);


    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private WebAudio webAudio = default!;
    [AutoInject] private MediaDevices mediaDevices = default!;
    [AutoInject] private MediaRecorder mediaRecorder = default!;
    [AutoInject] private IPermissionService permissionService = default!;
    [AutoInject] private ILogger<AppAiChatPanel> logger = default!;


    private bool isDictationSupported;

    /// <summary>The microphone is open and audio is being captured.</summary>
    private bool isListening;

    /// <summary>The take has been handed to the backend and the panel is waiting to hear what it said.</summary>
    private bool isTranscribing;

    /// <summary>How much of <see cref="MaxDictationDuration"/> the open take has used, as the ring around the button.</summary>
    private double dictationPercent;

    /// <summary>Whatever was already typed when dictation started; what is heard is appended to it, not written over it.</summary>
    private string? dictationPrefix;

    private CancellationTokenSource? dictationTimeoutCts;
    private MediaStreamHandle? microphoneStream;
    private MediaRecordingHandle? dictationRecording;


    /// <summary>
    /// Dictates into the message box. What was heard is written to <c>userInput</c> and goes out through the ordinary
    /// send path, so the user reads it before it is sent - a misrecognition is corrected, not delivered.
    /// </summary>
    private async Task ToggleDictation()
    {
        if (isListening)
        {
            await StopDictation(transcribe: true);
            return;
        }

        // The previous take is still being transcribed and its text is about to land in the box; starting another one
        // now would race it for the box and for dictationPrefix.
        if (isTranscribing) return;

        if (await permissionService.RequestMicrophonePermission() is false)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelDictation)],
                                  Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]);
            return;
        }

        // An open microphone and an answer being read talk over each other, and the recording would carry both. Read
        // aloud itself stays on: it is the next answer the user wants read, not this one.
        await PauseReadAloud();

        // Audio only plays once the user has interacted with the page, and pressing this button is that interaction -
        // so the audio context is woken here rather than when there is finally an answer to read out, by which time
        // the gesture is long gone and mobile Safari refuses.
        await webAudio.Resume();

        await StartDictation();
    }

    private async Task StartDictation()
    {
        try
        {
            // getUserMedia raises the browser's own permission prompt, and answers with null rather than throwing
            // when the user refuses it or there is no microphone to open.
            microphoneStream = await mediaDevices.GetUserMedia(audio: true, video: false);

            if (microphoneStream is null)
            {
                readAloudPaused = false;

                SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelDictation)],
                                      Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]);
                return;
            }

            // Which containers exist differs per engine - chromium and firefox record webm, safari records mp4 - so
            // the best supported one is probed rather than named. The backend reads the format out of the bytes, so
            // whichever wins is fine, and null simply leaves the browser to its own default.
            var mimeType = (await mediaRecorder.GetSupportedTypes(MediaRecorder.CommonAudioTypes)).FirstOrDefault();

            dictationRecording = await mediaRecorder.Start(microphoneStream,
                mimeType is null ? null : new MediaRecorderOptions { MimeType = mimeType },
                onError: message => _ = InvokeAsync(() => HandleDictationError(message)));

            if (dictationRecording is null)
                throw new InvalidOperationException("The browser refused to record the microphone.");

            isListening = true;

            // Nothing else ever ends an abandoned take: the panel can sit open with the microphone live for as long
            // as the tab is, and every second of that is audio the backend would be asked to transcribe.
            dictationTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
            _ = StopDictationAfterMaxDuration(dictationTimeoutCts.Token);

            dictationPrefix = string.IsNullOrWhiteSpace(userInput) ? null : $"{userInput.TrimEnd()} ";
        }
        catch
        {
            // Nothing is listening, so the button must not say it is - and read aloud was silenced for a dictation
            // that never began, which would otherwise stay silent until the user pressed the microphone again.
            await ReleaseMicrophone();
            readAloudPaused = false;
            throw;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Ends the take once it reaches <see cref="MaxDictationDuration"/>, and on the way there fills the ring around
    /// the microphone button, so the deadline is something the user can see coming rather than be surprised by.
    /// </summary>
    private async Task StopDictationAfterMaxDuration(CancellationToken cancellationToken)
    {
        var startedAt = TimeProvider.GetUtcNow();

        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(500), TimeProvider);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var elapsed = TimeProvider.GetUtcNow() - startedAt;

                if (elapsed >= MaxDictationDuration) break;

                await InvokeAsync(() =>
                {
                    dictationPercent = elapsed / MaxDictationDuration * 100;
                    StateHasChanged();
                });
            }
        }
        catch (OperationCanceledException)
        {
            return; // The user stopped first, which is the ordinary case.
        }

        await InvokeAsync(async () =>
        {
            if (isListening is false) return;

            await StopDictation(transcribe: true);

            StateHasChanged();
        });
    }

    /// <summary>
    /// Ends dictation because the user - or the panel on their behalf - asked for it. With
    /// <paramref name="transcribe"/> the take is sent to the backend and what it heard lands in the message box;
    /// without it the recording is dropped, which is what closing the panel or navigating away should do.
    /// </summary>
    private async Task StopDictation(bool transcribe = false)
    {
        if (isListening is false) return;

        isListening = false;

        var recording = dictationRecording;
        dictationRecording = null;

        RecordedMedia? recorded = null;

        try
        {
            // Stopping is what produces the audio; disposing without stopping throws the take away, which is exactly
            // what the transcribe:false path wants.
            if (recording is not null && transcribe)
            {
                recorded = await recording.Stop();
            }
        }
        finally
        {
            if (recording is not null && recorded is null)
            {
                await recording.DisposeAsync();
            }

            // The microphone is a separate lifetime from the recording: stopping one does not close the other, and a
            // stream left open keeps the browser's recording indicator lit over a panel that stopped listening.
            await ReleaseMicrophone();
        }

        if (recorded?.Data is not { Length: > 0 } audio) return;

        await Transcribe(audio, recorded.MimeType);
    }

    /// <summary>Asks the backend what the recording said, and puts it in the box after whatever was already there.</summary>
    private async Task Transcribe(byte[] audio, string mimeType)
    {
        // Rendered before anything is awaited. The microphone has already closed by now, and when the caller is the
        // maximum duration timer rather than a button press there is no event handler whose completion would render
        // that - so the wave and the ring would sit on screen for the whole of the sign-in and the upload below.
        isTranscribing = true;
        StateHasChanged();

        try
        {
            // Recording is free, so the microphone opens for anyone. This request is the part that needs an account.
            if (await EnsureSignedInForSpeech() is false) return;

            // An ordinary request rather than the chat's own SignalR stream: this is megabytes of audio, and a new
            // message on that stream cancels whatever is being processed - see AppHub.StartChat.
            using MultipartFormDataContent form = new();
            var audioContent = new ByteArrayContent(audio);
            // The container is in the bytes as well, but the header is what a proxy or a server side format check
            // reads first. Only the media type is kept: MediaRecorder reports codecs alongside it ("audio/webm;
            // codecs=opus"), which is not a valid Content-Type parameter list for every server to parse.
            audioContent.Headers.ContentType = new(string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Split(';')[0]);
            form.Add(audioContent, name: "file", fileName: "recording");

            using var response = await httpClient.PostAsync("api/v1/Chatbot/TranscribeSpeech", form, CurrentCancellationToken);

            var transcription = await response.Content.ReadFromJsonAsync(JsonSerializerOptions.GetTypeInfo<TranscribeSpeechResponseDto>(), CurrentCancellationToken);

            // A take with nothing but silence in it transcribes to nothing, and the box is left exactly as it was.
            if (string.IsNullOrWhiteSpace(transcription?.Text)) return;

            userInput = $"{dictationPrefix}{transcription.Text.Trim()}";
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp, ExceptionDisplayKind.NonInterrupting);
        }
        finally
        {
            isTranscribing = false;
            dictationPrefix = null;
            StateHasChanged();
        }
    }

    private Task HandleDictationError(string message)
    {
        // The recorder reports failures the panel cannot do anything about - a codec giving up mid-take, the device
        // disappearing. They are logged as well as shown, because which of them really happen is only learnable from
        // what arrives in the field.
        logger.LogWarning("Dictation reported {DictationError}. Listening: {IsListening}, transcribing: {IsTranscribing}.",
                          message, isListening, isTranscribing);

        SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelDictation)],
                              Localizer[nameof(AppStrings.AiChatPanelDictationStopped), message]);

        return StopDictation();
    }

    /// <summary>
    /// Closes the microphone and stops the maximum-duration timer, saying nothing about whether what was captured is
    /// kept.
    /// </summary>
    private async ValueTask ReleaseMicrophone()
    {
        isListening = false;
        dictationPercent = 0;

        if (dictationTimeoutCts is not null)
        {
            var timeoutCts = dictationTimeoutCts;
            dictationTimeoutCts = null;
            await timeoutCts.TryCancel();
            timeoutCts.Dispose();
        }

        if (microphoneStream is not null)
        {
            var stream = microphoneStream;
            microphoneStream = null;
            await stream.DisposeAsync();
        }
    }
}
