namespace Bit.BlazorUI;

/// <summary>
/// A base class for the text-based input components of bit BlazorUI.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public abstract class BitTextInputBase<TValue> : BitInputBase<TValue>
{
    private readonly BitDebouncer _debouncer = new();
    private readonly BitThrottler _throttler = new();
    private ChangeEventArgs _lastThrottleEventArgs = default!;



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
        var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;

        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(DebounceTime):
                    DebounceTime = (int)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(Immediate):
                    Immediate = (bool)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;

                case nameof(ThrottleTime):
                    ThrottleTime = (int)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
            }
        }

        return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary!));
    }



    /// <summary>
    /// Handler for the OnChange event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual async Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;

        await SetCurrentValueAsStringAsync(e.Value?.ToString());
    }

    /// <summary>
    /// Handler for the OnInput event, with an optional delay to avoid to raise the <see cref="BitInputBase{TValue}.ValueChanged"/> event too often.
    /// </summary>
    /// <param name="e"></param>
    protected virtual async Task HandleOnStringValueInputAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;

        if (Immediate is false) return;

        if (DebounceTime > 0)
        {
            await _debouncer.Do(DebounceTime, async () => await InvokeAsync(async () => await HandleOnStringValueChangeAsync(e)));
        }
        else if (ThrottleTime > 0)
        {
            _lastThrottleEventArgs = e;
            await _throttler.Do(ThrottleTime, async () => await InvokeAsync(async () => await HandleOnStringValueChangeAsync(_lastThrottleEventArgs)));
        }
        else
        {
            await HandleOnStringValueChangeAsync(e);
        }
    }
}
