namespace Bit.BlazorUI;

/// <summary>
/// A base class for the text-based input components of bit BlazorUI.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public abstract class BitTextInputBase<TValue> : BitInputBase<TValue>
{
    private readonly BitInputRateLimiter<ChangeEventArgs> _rateLimiter = new();

    // The parameters of this class are taken out of the ParameterView here, before it reaches the classes below,
    // so their sets never see them: a cascade filling in what the markup left unset reads this one instead.
    private readonly HashSet<string> _assignedTextInputParameters = [];


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



    /// <summary>
    /// Whether the named parameter of <see cref="BitTextInputBase{TValue}"/> was left unset on this component,
    /// which is what a <see cref="BitParams"/> cascade fills in: the text-input tier of
    /// <see cref="BitInputBase{TValue}.HasNotBeenSetOnInput"/>, since the parameters of this class are taken
    /// out of the ParameterView before either of the sets below it sees them.
    /// </summary>
    protected internal bool HasNotBeenSetOnTextInput(string name) => _assignedTextInputParameters.Contains(name) is false;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        _assignedTextInputParameters.Clear();

        var parametersDictionary = (ParametersCache ??= parameters.ToDictionary() as Dictionary<string, object?>); ;

        foreach (var parameter in parametersDictionary!)
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

        return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary!));
    }

    private protected override bool IsSetByMarkup(string name) => _assignedTextInputParameters.Contains(name) || base.IsSetByMarkup(name);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || IsEnabled is false) return;

        if (AutoFocus)
        {
            await InputElement.FocusAsync();
        }
    }



    /// <summary>
    /// Handler for the OnChange event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual async Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        await SetCurrentValueAsStringAsync(e.Value?.ToString());
    }

    /// <summary>
    /// Handler for the OnInput event, with an optional delay to avoid to raise the <see cref="BitInputBase{TValue}.ValueChanged"/> event too often.
    /// </summary>
    /// <param name="e"></param>
    protected virtual async Task HandleOnStringValueInputAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        if (Immediate is false) return;

        await _rateLimiter.Run(e, DebounceTime, ThrottleTime, async args =>
            await InvokeAsync(async () => await HandleOnStringValueChangeAsync(args)));
    }



    /// <summary>
    /// Drops any input event still waiting out its <see cref="DebounceTime"/> or <see cref="ThrottleTime"/>,
    /// for a derived component that has just committed that text by other means.
    /// </summary>
    protected void ResetInputRateLimiter() => _rateLimiter.Reset();



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _rateLimiter.Reset();

        await base.DisposeAsync(disposing);
    }
}
