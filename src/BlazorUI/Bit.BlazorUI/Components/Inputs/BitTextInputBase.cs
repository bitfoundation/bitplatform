namespace Bit.BlazorUI;

/// <summary>
/// A base class for the text-based input components of bit BlazorUI.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public abstract class BitTextInputBase<TValue> : BitInputBase<TValue>
{
    private readonly BitInputRateLimiter<ChangeEventArgs> _rateLimiter = new();

    // How many input events are being committed through HandleOnStringValueChangeAsync right now, which is what tells
    // them apart from the change event of the input element on the way through it (see PrepareStringValueCommit).
    private int _committingInputEvents;

    // The parameters of this class are taken out of the ParameterView here, before it reaches the classes below,
    // so their sets never see them: IsSetByMarkup adds this one to theirs, which is what HasNotBeenSet reads.
    private readonly HashSet<string> _assignedTextInputParameters = [];

    // The text the input element reported with its last input event, and the value (or the part of it the element
    // edits) that text was typed over (see TypedText).
    private string? _typedText;
    private string? _typedTextBase;


    /// <summary>
    /// Specifies the value of the autocomplete attribute of the input component.
    /// </summary>
    [Parameter] public string? AutoComplete { get; set; }

    /// <summary>
    /// Determines if the text input is auto focused on first render.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// The debounce time in milliseconds.
    /// </summary>
    [Parameter] public int DebounceTime { get; set; }

    /// <summary>
    /// Change the content of the input field when the user write text (based on 'oninput' HTML event).
    /// </summary>
    [Parameter] public bool Immediate { get; set; }

    /// <summary>
    /// The throttle time in milliseconds.
    /// </summary>
    [Parameter] public int ThrottleTime { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        _assignedTextInputParameters.Clear();

        var parametersDictionary = TakeParametersDictionary(parameters);

        foreach (var parameter in parametersDictionary)
        {
            switch (parameter.Key)
            {
                case nameof(AutoComplete):
                    _assignedTextInputParameters.Add(nameof(AutoComplete));
                    AutoComplete = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(AutoFocus):
                    _assignedTextInputParameters.Add(nameof(AutoFocus));
                    AutoFocus = (bool)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(DebounceTime):
                    _assignedTextInputParameters.Add(nameof(DebounceTime));
                    DebounceTime = (int)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Immediate):
                    _assignedTextInputParameters.Add(nameof(Immediate));
                    Immediate = (bool)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(ThrottleTime):
                    _assignedTextInputParameters.Add(nameof(ThrottleTime));
                    ThrottleTime = (int)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
            }
        }

        // What is left reaches the base through ParametersCache, handed over right before the call and taken by the base
        // as it starts, so a pass that throws on the way leaves none behind; a view built from it would only be an
        // allocation nobody reads.
        ParametersCache = parametersDictionary;
        return base.SetParametersAsync(ParameterView.Empty);
    }

    private protected override bool IsSetByMarkup(string name) => _assignedTextInputParameters.Contains(name) || base.IsSetByMarkup(name);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || Disabled) return;

        if (AutoFocus)
        {
            await InputElement.FocusSafelyAsync();
        }
    }



    /// <summary>
    /// Handler for the OnChange event, which is also what commits the input events of an <see cref="Immediate"/> input.
    /// </summary>
    /// <remarks>
    /// Called with anything but an input event - the change event, or a commit a derived component makes - it drops the
    /// input events still waiting out their <see cref="DebounceTime"/> or <see cref="ThrottleTime"/>, whose text is older
    /// than what it commits, and under <see cref="Immediate"/> it leaves a value every commit has already settled on
    /// unreported, as the input events reported it while it was typed.
    /// </remarks>
    /// <param name="e"></param>
    protected virtual async Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        if (Disabled || ReadOnly) return;

        var reportSettledValue = PrepareStringValueCommit();

        await SetCurrentValueAsStringAsync(e.Value?.ToString(), bypass: false, reportSettledValue);
    }

    /// <summary>
    /// Handler for the OnInput event, with an optional delay to avoid to raise the <see cref="BitInputBase{TValue}.ValueChanged"/> event too often.
    /// </summary>
    /// <param name="e"></param>
    protected virtual async Task HandleOnStringValueInputAsync(ChangeEventArgs e)
    {
        if (Disabled || ReadOnly) return;

        if (Immediate is false) return;

        await _rateLimiter.Run(e, DebounceTime, ThrottleTime, async args => await InvokeAsync(async () =>
        {
            _committingInputEvents++;
            try
            {
                await HandleOnStringValueChangeAsync(args);
            }
            finally
            {
                _committingInputEvents--;
            }
        }));
    }



    /// <summary>
    /// Drops any input event still waiting out its <see cref="DebounceTime"/> or <see cref="ThrottleTime"/>,
    /// for a derived component that has just committed that text by other means.
    /// </summary>
    protected void ResetInputRateLimiter() => _rateLimiter.Reset();

    /// <summary>
    /// The text the input element reported with its last input event (<see cref="RecordTypedText"/>), while it is held;
    /// null once it has been let go of.
    /// </summary>
    /// <remarks>
    /// A text input binds the input event of its element rather than only handling it, and renders the element with
    /// this text for as long as what the text was typed over has not moved - Immediate off, a debounce or a throttle
    /// still waiting, a value bound one way. Rendering the value in between would write it back over what is being
    /// typed: the render of an older keystroke, a round trip behind the typing on a server circuit, would put back the
    /// shorter text, and the next key would go onto it.
    /// </remarks>
    private protected string? TypedText => _typedText;

    /// <summary>
    /// Holds the text an input event carried, typed over <paramref name="source"/>: the value, or the part of it the
    /// element edits. Recorded before anything is awaited, since a render that runs meanwhile is drawn from it.
    /// </summary>
    private protected void RecordTypedText(string? text, string? source)
    {
        _typedText = text;
        _typedTextBase = source;
    }

    /// <summary>
    /// Whether the typed text is held and <paramref name="source"/> is still what it was typed over.
    /// </summary>
    private protected bool HoldsTypedTextFor(string? source)
    {
        return _typedText is not null && string.Equals(source, _typedTextBase, StringComparison.Ordinal);
    }

    /// <summary>
    /// Lets go of the typed text for good once <paramref name="source"/> has moved from what it was typed over - the
    /// page, a commit or another part of the component moving it: a source coming back to that one later (a value
    /// committed and then cleared) would otherwise bring back a text typed long before.
    /// </summary>
    private protected void ReleaseTypedTextIfMoved(string? source)
    {
        if (_typedText is not null && HoldsTypedTextFor(source) is false)
        {
            _typedText = null;
        }
    }

    /// <summary>
    /// Settles the typed text after the element's text has been committed: it is let go of once
    /// <paramref name="sourceText"/> shows it, and otherwise <paramref name="source"/> is what it is typed over from
    /// now on - what the commit made of the text (trimmed, refused, laid out) is the component's own doing, not a
    /// change from the page.
    /// </summary>
    private protected void SettleTypedText(string? source, string? sourceText)
    {
        if (_typedText is null) return;

        if (string.Equals(_typedText, sourceText ?? string.Empty, StringComparison.Ordinal))
        {
            _typedText = null;
        }
        else
        {
            _typedTextBase = source;
        }
    }

    /// <summary>
    /// Lets go of the typed text, for a component that has just put a text of its own into the element.
    /// </summary>
    private protected void ReleaseTypedText() => _typedText = null;

    /// <summary>
    /// Readies the commit of the text that reached <see cref="HandleOnStringValueChangeAsync"/> - the one of the base
    /// class, or that of a derived component committing it by other means - and tells whether a value every commit has
    /// already settled on is reported again (see <see cref="BitInputBase{TValue}.SetCurrentValueAsStringAsync(string?, bool, bool)"/>).
    /// </summary>
    /// <remarks>
    /// An input event is reported whatever it carries, every keystroke of it. Anything else is taken for the change
    /// event, which carries the latest text the input holds: an input event still waiting out its debounce or throttle
    /// is older than that and is dropped, as landing after it - or after a value set since, a clearing say - it would put
    /// back the text it was raised for. Under <see cref="Immediate"/> the input events reported the edit as it was typed,
    /// so the change event raised on blur does not report it once more: a re-render the app runs again on the way out
    /// can swallow the press that took the focus away.
    /// </remarks>
    private protected bool PrepareStringValueCommit()
    {
        if (_committingInputEvents > 0) return true;

        _rateLimiter.Reset();

        return Immediate is false;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _rateLimiter.Reset();

        await base.DisposeAsync(disposing);
    }
}
