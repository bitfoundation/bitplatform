namespace Bit.BlazorUI;

/// <summary>
/// BitAiVoiceInput is a dictation button: speech is turned into text by the speech API of the browser, with no round trip
/// to the app's own server.
/// </summary>
/// <remarks>
/// That does not keep the audio on the device: the speech service of the browser may send it to its vendor to be
/// transcribed (Chrome's does), so a page whose audio must not leave the device should not offer the button.
/// <br />
/// The button hides itself entirely when the browser cannot transcribe, rather than showing a control that does nothing
/// when pressed. Each finalised phrase is raised through <see cref="OnTranscript"/>, and the provisional text through
/// <see cref="OnInterim"/> while the user is still speaking. One dictation runs at a time in a page: starting it in a
/// second instance stops it in the first, which is told so (<see cref="OnListeningChanged"/>) as if it had stopped on
/// its own.
/// </remarks>
public partial class BitAiVoiceInput : BitAiComponentBase
{
    private BitAiIconClassesCache<BitToggleButtonClassStyles> _buttonClasses;
    private int _handle;
    private bool _listening;
    private bool _hasInterim;
    private bool _supported;
    private string? _status;
    private DotNetObjectReference<BitAiVoiceInput>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    /// is used.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// Raised when the transcription fails, for example when the microphone is refused. Receives the name of the error
    /// the browser reported.
    /// </summary>
    [Parameter] public EventCallback<string> OnError { get; set; }

    /// <summary>
    /// Raised with the provisional text as the user speaks, and with an empty one once that text is final or gone.
    /// </summary>
    [Parameter] public EventCallback<string> OnInterim { get; set; }

    /// <summary>
    /// Raised when the dictation starts or stops, however that happens.
    /// </summary>
    [Parameter] public EventCallback<bool> OnListeningChanged { get; set; }

    /// <summary>
    /// Raised with each finalised phrase.
    /// </summary>
    [Parameter] public EventCallback<string> OnTranscript { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the voice input.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiVoiceInputClassStyles? Styles { get; set; }



    /// <summary>
    /// Whether the dictation is running.
    /// </summary>
    public bool IsListening => _listening;



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
    /// Called by the dictation script when the transcription fails.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    /// <param name="error">The name of the error the browser reported.</param>
    /// <returns>A task that completes once the callback has run.</returns>
    [JSInvokable("OnDictationError")]
    public async Task _OnDictationError(string error)
    {
        if (IsDisposing) return;

        _handle = 0;
        await SetListeningAsync(false, Strings.DictationFailed(error));
        await OnError.InvokeAsync(error);

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



    protected override string RootElementClass => "bit-aivci";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        // A disabled button cannot be pressed to stop the dictation, so disabling it while it listens stops it here
        // rather than leaving the microphone running with nothing able to stop it.
        if (Disabled && _listening)
        {
            await StopAsync();
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || IsDisposing) return;

        _supported = await _js.BitAiVoiceInputIsDictationSupported();

        if (_supported && IsDisposing is false)
        {
            StateHasChanged();
        }
    }



    private async Task ToggleAsync()
    {
        if (Disabled) return;

        if (_listening)
        {
            await StopAsync();
            return;
        }

        _dotnetObj ??= DotNetObjectReference.Create(this);

        _handle = await _js.BitAiVoiceInputStartDictation(_dotnetObj, Language, UniqueId);

        if (_handle > 0)
        {
            await SetListeningAsync(true, Strings.Listening);
        }
    }

    private async Task StopAsync()
    {
        var handle = _handle;
        _handle = 0;

        await _js.BitAiVoiceInputStopDictation(handle);
        await SetListeningAsync(false, Strings.DictationStopped);
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



    /// <remarks>
    /// Removing the button must not leave the microphone running with nothing able to stop it, and the object reference
    /// goes with the component, or the browser would keep calling into a ghost.
    /// </remarks>
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            if (_listening)
            {
                _listening = false;
                await _js.BitAiVoiceInputStopDictation(_handle);
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
}
