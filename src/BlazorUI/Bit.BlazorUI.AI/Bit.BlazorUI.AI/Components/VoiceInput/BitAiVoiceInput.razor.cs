namespace Bit.BlazorUI;

/// <summary>
/// BitAiVoiceInput is a dictation button: speech is turned into text by the speech API of the browser, or - when the app
/// supplies a transcriber - by the app's own speech-to-text service, from a recording made in the browser.
/// </summary>
/// <remarks>
/// <para>
/// Through the speech API of the browser there is no round trip to the app's own server, but that does not keep the
/// audio on the device: the speech service of the browser may send it to its vendor to be transcribed (Chrome's does).
/// Each finalised phrase is raised through <see cref="OnTranscript"/>, and the provisional text through
/// <see cref="OnInterim"/> while the user is still speaking.
/// </para>
/// <para>
/// With a <see cref="Transcriber"/>, or an <see cref="IBitAiSpeechToText"/> registered through
/// <c>AddBitBlazorUIAIServices(ai =&gt; ai.UseSpeechToText&lt;T&gt;())</c>, the microphone is recorded instead (see
/// <see cref="Mode"/>), for at most <see cref="MaxRecordingDuration"/> with the time left shown beside the button, and
/// the whole take is handed to the transcriber once the user stops; what it heard is raised through
/// <see cref="OnTranscript"/>. That is what makes dictation work where the browser has no speech API (Firefox, Android
/// WebView), and keeps the audio between the browser and the app.
/// </para>
/// <para>
/// The button hides itself entirely when it cannot dictate, rather than showing a control that does nothing when
/// pressed. One dictation runs at a time in a page: starting it in a second instance stops it in the first, which is
/// told so (<see cref="OnListeningChanged"/>) as if it had stopped on its own. A reply read aloud
/// (<see cref="BitAiReadAloud"/>) falls silent while it listens, so the microphone does not pick it up.
/// </para>
/// </remarks>
public partial class BitAiVoiceInput : BitAiComponentBase
{
    /// <summary>The default of <see cref="MaxRecordingDuration"/>: one minute.</summary>
    public static readonly TimeSpan DefaultMaxRecordingDuration = TimeSpan.FromMinutes(1);

    /// <summary>The default of <see cref="MaxRecordingSize"/>: 10 MB.</summary>
    public const long DefaultMaxRecordingSize = 10 * 1024 * 1024;

    // How long before a recording stops on its own the time left is announced, once.
    private static readonly TimeSpan TimeLeftWarning = TimeSpan.FromSeconds(10);

    private BitAiIconClassesCache<BitToggleButtonClassStyles> _buttonClasses;
    private int _handle;
    private Engine _engineInUse;
    private bool _listening;
    private bool _starting;
    private bool _transcribing;
    private bool _hasInterim;
    private bool _probed;
    private bool _speechSupported;
    private bool _recordingProbed;
    private bool _recordingSupported;
    private bool _timeLeftAnnounced;
    private string? _status;
    private string? _countdown;
    private DateTimeOffset _recordingDeadline;
    private CancellationTokenSource? _countdownCts;
    private CancellationTokenSource? _transcribeCts;
    private IBitAiSpeechToText? _speechToText;
    private DotNetObjectReference<BitAiVoiceInput>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the voice input component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple voice input components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiVoiceInputParams.ParamName)]
    public BitAiVoiceInputParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the voice input.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiVoiceInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The recognition language, as a BCP 47 language tag such as <c>en-US</c>. When null, the language of the browser
    /// is used - or, with a transcriber, whatever the transcriber decides.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// The longest a recording for the transcriber may last; it is transcribed, not thrown away, when it reaches it. The
    /// time left is shown beside the button while it records.
    /// <br />
    /// The default value is <strong>1 minute</strong>.
    /// </summary>
    /// <remarks>
    /// Keep it short enough for the recording to stay inside everything it crosses: the transcriber's own limit, and on
    /// Blazor Server the circuit, which it is streamed over in chunks (see <see cref="MaxRecordingSize"/>).
    /// </remarks>
    [Parameter] public TimeSpan MaxRecordingDuration { get; set; } = DefaultMaxRecordingDuration;

    /// <summary>
    /// The largest recording handed to the transcriber, in bytes; a larger one fails with <c>too-large</c>.
    /// <br />
    /// The default value is <strong>10 MB</strong>.
    /// </summary>
    [Parameter] public long MaxRecordingSize { get; set; } = DefaultMaxRecordingSize;

    /// <summary>
    /// How speech is turned into text: through the app's transcriber, through the speech API of the browser, or through
    /// the one with the other as the fallback.
    /// <br />
    /// The default value is <strong><see cref="BitAiDictationMode.Auto"/></strong>.
    /// </summary>
    [Parameter] public BitAiDictationMode Mode { get; set; }

    /// <summary>
    /// Raised when the dictation fails, with the name of what failed: as the browser reported it through its speech API
    /// (<c>not-allowed</c> when the microphone is refused, <c>audio-capture</c> when there is none, ...), or for a
    /// recording <c>not-allowed</c>, <c>audio-capture</c>, <c>recording-failed</c>, <c>too-large</c> or
    /// <c>transcription-failed</c>.
    /// </summary>
    [Parameter] public EventCallback<string> OnError { get; set; }

    /// <summary>
    /// Raised with the provisional text as the user speaks, and with an empty one once that text is final or gone.
    /// Only the speech API of the browser has provisional text; a transcriber hears the whole recording at once.
    /// </summary>
    [Parameter] public EventCallback<string> OnInterim { get; set; }

    /// <summary>
    /// Raised when the dictation starts or stops listening, however that happens. A recording stops listening as it ends,
    /// before its transcription (see <see cref="IsTranscribing"/>).
    /// </summary>
    [Parameter] public EventCallback<bool> OnListeningChanged { get; set; }

    /// <summary>
    /// Raised with each finalised phrase - or, with a transcriber, with what it heard in the recording.
    /// </summary>
    [Parameter] public EventCallback<string> OnTranscript { get; set; }

    /// <summary>
    /// Ends a recording for the transcriber once the user has said something and then been quiet for this long. When
    /// null, a recording ends when the user stops it, or at <see cref="MaxRecordingDuration"/>.
    /// <br />
    /// The default value is <strong>null</strong>.
    /// </summary>
    [Parameter] public TimeSpan? SilenceTimeout { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the voice input.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiVoiceInputClassStyles? Styles { get; set; }

    /// <summary>
    /// The app's speech-to-text, given a recording (read from its start, and disposed once the returned task completes),
    /// its media type (such as <c>audio/webm</c>) and a token cancelled when the voice input goes away; it returns what
    /// was said. When null, an <see cref="IBitAiSpeechToText"/> service registered through
    /// <c>AddBitBlazorUIAIServices(ai =&gt; ai.UseSpeechToText&lt;T&gt;())</c> is used, if there is one.
    /// </summary>
    /// <remarks>
    /// With a transcriber the microphone is recorded in the browser and the take transcribed by the app (see
    /// <see cref="Mode"/>), streamed to .NET in chunks on Blazor Server and WebAssembly alike.
    /// </remarks>
    [Parameter] public Func<Stream, string, CancellationToken, Task<string?>>? Transcriber { get; set; }



    /// <summary>
    /// Whether the dictation is listening: the speech API of the browser is hearing, or the microphone is recorded.
    /// </summary>
    public bool IsListening => _listening;

    /// <summary>
    /// Whether a recording is being transcribed by the app's transcriber.
    /// </summary>
    public bool IsTranscribing => _transcribing;



    /// <summary>
    /// Called by the dictation script with recognized speech.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="final">The text the recognizer has committed to.</param>
    /// <param name="interim">The provisional text that may still change.</param>
    /// <returns>A task that completes once the callbacks have run.</returns>
    [JSInvokable("OnTranscriptReceived")]
    public async Task _OnTranscriptReceived(string? final, string? interim)
    {
        if (IsDisposing) return;

        if (string.IsNullOrWhiteSpace(final) is false)
        {
            await OnTranscript.InvokeAsync(final.Trim());
        }

        // An empty interim clears the provisional text the previous one showed.
        _hasInterim = string.IsNullOrEmpty(interim) is false;
        await OnInterim.InvokeAsync(interim ?? string.Empty);

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Called by the dictation and recording scripts when the dictation fails.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="error">The name of the error the browser reported.</param>
    /// <returns>A task that completes once the callback has run.</returns>
    [JSInvokable("OnDictationError")]
    public async Task _OnDictationError(string error)
    {
        if (IsDisposing) return;

        await FailAsync(error);

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Called by the dictation script when the transcription stops on its own, or because another instance started one.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <returns>A task that completes when the state has been applied.</returns>
    [JSInvokable("OnDictationEnded")]
    public async Task _OnDictationEnded()
    {
        if (IsDisposing) return;

        _handle = 0;
        if (_listening)
        {
            await SetListeningAsync(false, Strings.DictationStopped);
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Called by the recording script with the take once the recording has ended - stopped by the user, at its maximum
    /// length, on silence, or because another capture of the microphone started.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="audio">The recording, or null when nothing was recorded.</param>
    /// <param name="contentType">The media type the browser recorded it in, with its parameters.</param>
    /// <param name="size">The size of the recording, in bytes.</param>
    /// <returns>A task that completes once the recording has been transcribed and the transcript raised.</returns>
    [JSInvokable("OnRecordingCompleted")]
    public async Task _OnRecordingCompleted(IJSStreamReference? audio, string? contentType, long size)
    {
        try
        {
            if (IsDisposing) return;

            _handle = 0;
            StopCountdown();

            if (audio is null || size <= 0)
            {
                _transcribing = false;
                await SetListeningAsync(false, Strings.DictationStopped);
                await InvokeAsync(StateHasChanged);
                return;
            }

            if (size > MaxRecordingSize)
            {
                _transcribing = false;
                await FailAsync("too-large");
                await InvokeAsync(StateHasChanged);
                return;
            }

            _transcribing = true;
            await SetListeningAsync(false, Strings.Transcribing);
            await InvokeAsync(StateHasChanged);

            var cts = new CancellationTokenSource();
            _transcribeCts = cts;

            string? text;
            try
            {
                // Read whole before it is handed over, so the transcriber gets a stream it can seek and read at its own
                // pace, whatever the host streams it from (the circuit, on Blazor Server).
                using var buffer = new MemoryStream((int)Math.Min(size, int.MaxValue));
                await using (var stream = await audio.OpenReadStreamAsync(MaxRecordingSize, cts.Token))
                {
                    await stream.CopyToAsync(buffer, cts.Token);
                }

                buffer.Position = 0;

                text = await TranscribeAsync(buffer, MediaType(contentType), cts.Token);
            }
            catch (Exception) when (cts.IsCancellationRequested || IsDisposing)
            {
                return;
            }
            catch (Exception)
            {
                // The text of the exception is not shown: it may say more about the server than a reader should see.
                _transcribing = false;
                await FailAsync("transcription-failed");
                await InvokeAsync(StateHasChanged);
                return;
            }
            finally
            {
                if (ReferenceEquals(_transcribeCts, cts)) _transcribeCts = null;
                cts.Dispose();
            }

            _transcribing = false;
            _status = Strings.DictationStopped;

            if (string.IsNullOrWhiteSpace(text) is false)
            {
                await OnTranscript.InvokeAsync(text.Trim());
            }

            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            if (audio is not null)
            {
                try { await audio.DisposeAsync(); }
                catch (JSDisconnectedException) { }
                catch (ObjectDisposedException) { }
            }
        }
    }



    protected override string RootElementClass => "bit-aivci";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root)
                    .Register(() => _listening && _engineInUse is Engine.Recording ? "bit-aivci-rec" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _speechToText = _services.GetService(typeof(IBitAiSpeechToText)) as IBitAiSpeechToText;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        // A disabled button cannot be pressed to stop the dictation, so disabling it while it listens stops it here
        // rather than leaving the microphone running with nothing able to stop it. A recording stopped that way is still
        // transcribed, as a phrase the speech API was hearing is still delivered.
        if (Disabled && _listening)
        {
            await StopAsync();
        }

        // A transcriber supplied after the first render needs to know whether the browser can record.
        if (_probed && HasTranscriber && _recordingProbed is false && IsDisposing is false)
        {
            await ProbeRecordingAsync();
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || IsDisposing) return;

        // Only what the mode can use is asked about, so a page dictating through the app's transcriber never loads the
        // script of the speech API, and one without a transcriber never loads the recorder's.
        if (HasTranscriber && Mode is not BitAiDictationMode.Browser)
        {
            await ProbeRecordingAsync();
        }

        if (Mode is not BitAiDictationMode.Recording && (CurrentEngine is Engine.None || Mode is BitAiDictationMode.Browser))
        {
            _speechSupported = await _js.BitAiVoiceInputIsDictationSupported();
        }

        if (HasTranscriber && Mode is BitAiDictationMode.Browser && _speechSupported is false)
        {
            await ProbeRecordingAsync();
        }

        _probed = true;

        if (CurrentEngine is not Engine.None && IsDisposing is false)
        {
            StateHasChanged();
        }
    }



    private bool HasTranscriber => Transcriber is not null || _speechToText is not null;

    // Which way a press dictates, from what the app supplies and what the browser can do.
    private Engine CurrentEngine
    {
        get
        {
            var recording = HasTranscriber && _recordingSupported;

            return Mode switch
            {
                BitAiDictationMode.Recording => recording ? Engine.Recording : Engine.None,
                BitAiDictationMode.Browser => _speechSupported ? Engine.Speech : recording ? Engine.Recording : Engine.None,
                _ => recording ? Engine.Recording : _speechSupported ? Engine.Speech : Engine.None,
            };
        }
    }

    // Shown while the dictation is live, and while it is busy with what it heard - even when the mode or the transcriber
    // has changed since, so it can still be stopped.
    private bool Visible => CurrentEngine is not Engine.None || _listening || _transcribing;

    private async Task ProbeRecordingAsync()
    {
        _recordingProbed = true;
        _recordingSupported = await _js.BitAiVoiceInputIsRecordingSupported();
    }

    private async Task ToggleAsync()
    {
        if (Disabled || _starting || _transcribing) return;

        if (_listening)
        {
            await StopAsync();
            return;
        }

        var engine = CurrentEngine;
        if (engine is Engine.None) return;

        _dotnetObj ??= DotNetObjectReference.Create(this);
        _starting = true;

        try
        {
            if (engine is Engine.Recording)
            {
                await StartRecordingAsync();
            }
            else
            {
                _handle = await _js.BitAiVoiceInputStartDictation(_dotnetObj, Language, UniqueId);

                if (_handle > 0)
                {
                    _engineInUse = Engine.Speech;
                    await SetListeningAsync(true, Strings.Listening);
                }
            }
        }
        finally
        {
            _starting = false;
        }
    }

    private async Task StartRecordingAsync()
    {
        var max = MaxRecordingDuration > TimeSpan.Zero ? MaxRecordingDuration : DefaultMaxRecordingDuration;
        var silence = SilenceTimeout is { } timeout && timeout > TimeSpan.Zero ? timeout.TotalMilliseconds : 0;

        var handle = await _js.BitAiVoiceInputStartRecording(_dotnetObj!, max.TotalMilliseconds, silence);

        if (IsDisposing)
        {
            if (handle > 0) await _js.BitAiVoiceInputStopRecording(handle, false);
            return;
        }

        if (handle > 0)
        {
            _handle = handle;
            _engineInUse = Engine.Recording;
            _recordingDeadline = DateTimeOffset.UtcNow + max;
            _timeLeftAnnounced = max <= TimeLeftWarning;
            _countdown = Strings.RecordingCountdown(max);

            await SetListeningAsync(true, Strings.RecordingStarted(max));

            StartCountdown();
        }
        else if (handle < 0)
        {
            await FailAsync(handle switch
            {
                -1 => "not-allowed",
                -2 => "audio-capture",
                _ => "recording-failed",
            });
        }
    }

    private async Task StopAsync()
    {
        var handle = _handle;
        _handle = 0;

        if (_engineInUse is Engine.Recording)
        {
            StopCountdown();

            // The take comes back through OnRecordingCompleted, which transcribes it; until then the button waits for it.
            _transcribing = true;
            await SetListeningAsync(false, Strings.Transcribing);
            await _js.BitAiVoiceInputStopRecording(handle, true);
            return;
        }

        await _js.BitAiVoiceInputStopDictation(handle);
        await SetListeningAsync(false, Strings.DictationStopped);
    }

    private Task<string?> TranscribeAsync(Stream audio, string contentType, CancellationToken cancellationToken)
    {
        if (Transcriber is { } transcriber) return transcriber(audio, contentType, cancellationToken);

        if (_speechToText is { } speechToText) return speechToText.TranscribeAsync(audio, contentType, Language, cancellationToken);

        return Task.FromResult<string?>(null);
    }

    private async Task FailAsync(string error)
    {
        _handle = 0;
        StopCountdown();

        await SetListeningAsync(false, Strings.DictationFailed(error));
        await OnError.InvokeAsync(error);
    }

    // The time left counts down beside the button while it records, and is announced once as the end nears: a deadline
    // the user can see coming rather than be surprised by.
    private void StartCountdown()
    {
        CancelCountdown();

        var cts = new CancellationTokenSource();
        _countdownCts = cts;

        _ = RunCountdownAsync(cts.Token);
    }

    private async Task RunCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested || _listening is false || IsDisposing) return;

                    var remaining = _recordingDeadline - DateTimeOffset.UtcNow;
                    if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

                    var changed = false;

                    if (_timeLeftAnnounced is false && remaining <= TimeLeftWarning)
                    {
                        _timeLeftAnnounced = true;
                        _status = Strings.RecordingTimeLeft(remaining);
                        changed = true;
                    }

                    var countdown = Strings.RecordingCountdown(remaining);
                    if (countdown != _countdown)
                    {
                        _countdown = countdown;
                        changed = true;
                    }

                    if (changed) StateHasChanged();
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { } // the renderer went first
    }

    private void StopCountdown()
    {
        _countdown = null;

        CancelCountdown();
    }

    private void CancelCountdown()
    {
        if (_countdownCts is { } cts)
        {
            _countdownCts = null;
            cts.Cancel();
            cts.Dispose();
        }
    }

    // The glyph comes from the icon font of this package, since the one of the core carries only the few glyphs its own
    // components draw (see BitAiIconClasses). Built again only when the classes of the app for the button are other
    // ones, so the button is not handed a new object, and rebuilds its classes, on every render.
    private BitToggleButtonClassStyles _ButtonClasses =>
        _buttonClasses.Get("bit-icon-ai bit-icon-ai--Microphone", Classes?.Button, BitAiIconClasses.With);

    private async Task SetListeningAsync(bool listening, string status)
    {
        var changed = _listening != listening;

        _listening = listening;
        _status = status;

        // The root says that it records (bit-aivci-rec), for a style of the app's own.
        if (changed) ClassBuilder.Reset();

        // Provisional text still showing when the dictation stops, fails or is taken over will never be made final by this
        // recognizer, so it is cleared - with the empty interim OnInterim promises once the text is final or gone. A phrase
        // the recognizer was still hearing as it stopped arrives through OnTranscript all the same.
        if (listening is false && _hasInterim)
        {
            _hasInterim = false;
            await OnInterim.InvokeAsync(string.Empty);
        }

        if (changed)
        {
            await OnListeningChanged.InvokeAsync(listening);
        }
    }

    // A transcriber is handed the media type alone: MediaRecorder reports the codecs with it ("audio/webm;codecs=opus"),
    // which is not a parameter list every server parses as a Content-Type.
    private static string MediaType(string? contentType)
    {
        var type = contentType?.Split(';', 2)[0].Trim();

        return string.IsNullOrEmpty(type) ? "application/octet-stream" : type;
    }



    /// <remarks>
    /// Removing the button must not leave the microphone running with nothing able to stop it - a recording is thrown
    /// away, not transcribed for a component that is gone - and the object reference goes with the component, or the
    /// browser would keep calling into a ghost.
    /// </remarks>
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        StopCountdown();

        if (_transcribeCts is { } transcribing)
        {
            _transcribeCts = null;
            transcribing.Cancel();
        }

        try
        {
            if (_listening)
            {
                _listening = false;

                if (_engineInUse is Engine.Recording)
                {
                    await _js.BitAiVoiceInputStopRecording(_handle, false);
                }
                else
                {
                    await _js.BitAiVoiceInputStopDictation(_handle);
                }
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the microphone of its page with it
        catch (ObjectDisposedException) { } // the JS runtime of a WebAssembly host may go first
        finally
        {
            _dotnetObj?.Dispose();
            _dotnetObj = null;
        }

        await base.DisposeAsync(disposing);
    }



    private enum Engine
    {
        None,
        Speech,
        Recording,
    }
}
