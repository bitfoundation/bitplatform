namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// Turns speech into text for a field, with the browser's own speech recognition through Butil: the microphone starts and
/// stops it, the text appears in the field as it is heard, and the menu beside it picks the language it listens for.
/// </summary>
public partial class DictationButton
{
    private bool isSupported;
    private bool isListening;
    private IAsyncDisposable? session;
    // Counts starts and stops, so a start that returns after a stop, or after the component is gone, lets its session go.
    private int starts;
    // The field's text when dictation started, what was heard for good since, and what is still being heard.
    private string before = string.Empty;
    private string heard = string.Empty;
    private string hearing = string.Empty;

    [AutoInject] private SpeechRecognition speechRecognition = default!;
    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private ILogger<DictationButton> logger = default!;

    /// <summary>The field's text, which what is heard is added to.</summary>
    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    [Parameter] public BitSize? Size { get; set; }

    [Parameter] public string? Class { get; set; }

    /// <summary>The culture dictation listens for: the one picked in its menu, or the Workbench's own.</summary>
    private CultureInfo Language => store.DictationCulture is { } name && CultureInfoManager.SupportedCultures.FirstOrDefault(sc => sc.Culture.Name == name) is { } picked
        ? picked.Culture
        : CultureInfo.CurrentUICulture;

    private string HeaderTitle => isListening
        ? Localizer[nameof(WorkbenchStrings.StopDictation)]
        : Localizer[nameof(WorkbenchStrings.DictateIn), Language.NativeName];

    private IEnumerable<BitMenuButtonItem> LanguageItems => CultureInfoManager.SupportedCultures.Select(sc => new BitMenuButtonItem
    {
        Key = sc.Culture.Name,
        Text = sc.DisplayName,
        IconName = sc.Culture.Name == Language.Name ? BitIconName.CheckMark : null,
        OnClick = _ => store.SetDictationCulture(sc.Culture.Name)
    });

    protected override void OnInitialized()
    {
        Subscribe(store);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        try
        {
            isSupported = await speechRecognition.IsSupported();
        }
        catch (JSException)
        {
            isSupported = false;
        }

        if (isSupported)
        {
            StateHasChanged();
        }
    }

    /// <summary>The microphone's half of the button starts or stops listening; a language picked from the menu is handled by its item.</summary>
    private async Task OnClick(BitMenuButtonItem? item)
    {
        if (item is not null)
            return;

        if (isListening)
        {
            await Stop();
            return;
        }

        before = string.IsNullOrWhiteSpace(Value) ? string.Empty : Value.TrimEnd();
        heard = string.Empty;
        hearing = string.Empty;
        isListening = true;

        var start = ++starts;

        try
        {
            var started = await speechRecognition.Start(new() { Lang = Language.Name, Continuous = true, InterimResults = true }, OnResult, OnError, OnEnd);

            // Stopped, ended or gone while it started, so nothing else would let it go.
            if (start != starts || CurrentCancellationToken.IsCancellationRequested)
            {
                await Release(started);
                return;
            }

            session = started;
        }
        catch (JSException)
        {
            isListening = false;
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.DictationFailed)]);
        }
    }

    private void OnResult(SpeechRecognitionResult result) => Dispatch(async () =>
    {
        if (isListening is false)
            return;

        if (result.IsFinal)
        {
            heard = Join(heard, result.Transcript.Trim());
            hearing = string.Empty;
        }
        else
        {
            hearing = result.Transcript.Trim();
        }

        await ValueChanged.InvokeAsync(Join(Join(before, heard), hearing));
    });

    /// <summary>A refused microphone and a missing connection say so; silence and a stop just end it.</summary>
    private void OnError(string error) => Dispatch(async () =>
    {
        var message = error switch
        {
            "not-allowed" or "service-not-allowed" => Localizer[nameof(WorkbenchStrings.MicrophoneBlocked)].Value,
            "network" => Localizer[nameof(WorkbenchStrings.DictationNeedsNetwork)].Value,
            "language-not-supported" => Localizer[nameof(WorkbenchStrings.DictationLanguageNotSupported), Language.NativeName].Value,
            "no-speech" or "aborted" => null,
            _ => Localizer[nameof(WorkbenchStrings.DictationFailed)].Value
        };

        if (message is not null)
        {
            SnackBarService.Error(message);
        }

        await Stop();
    });

    private void OnEnd() => Dispatch(Stop);

    /// <summary>
    /// Stops listening and lets the recognition go, whether the user stopped it or it ended by itself, which also removes its
    /// listener; what was still being heard stays in the field as it shows.
    /// </summary>
    private async Task Stop()
    {
        var stopping = session;

        isListening = false;
        session = null;
        starts++;

        if (CurrentCancellationToken.IsCancellationRequested is false)
        {
            StateHasChanged();
        }

        if (stopping is not null)
        {
            await Release(stopping);
        }
    }

    private static async Task Release(IAsyncDisposable recognition)
    {
        try
        {
            await recognition.DisposeAsync();
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Runs a recognition callback on the component's dispatcher; nothing awaits it there, so what fails is logged.</summary>
    private void Dispatch(Func<Task> callback) => _ = InvokeAsync(async () =>
    {
        try
        {
            await callback();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "A speech recognition callback failed.");
        }
    });

    /// <summary>Joins two pieces of text with a space, leaving out an empty one.</summary>
    private static string Join(string first, string second) => first.Length == 0 ? second : second.Length == 0 ? first : $"{first} {second}";

    public override async ValueTask DisposeAsync()
    {
        starts++;

        if (session is { } running)
        {
            session = null;
            await Release(running);
        }

        await base.DisposeAsync();
    }
}
