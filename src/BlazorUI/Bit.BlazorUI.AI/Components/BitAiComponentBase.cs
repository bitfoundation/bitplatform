namespace Bit.BlazorUI;

/// <summary>
/// The base class of the Bit.BlazorUI.AI components: a <see cref="BitComponentBase"/> that also knows the AI options in
/// force, their string table, and how to re-render at most once per frame interval.
/// </summary>
/// <remarks>
/// The re-render helper is the reason this class exists. Streaming raises change events far faster than a browser can
/// paint, and calling <c>StateHasChanged</c> on each one is the single easiest way to make an AI chat UI feel slow.
/// Derived components call <see cref="RequestRender"/> instead, which collapses any number of requests into one render
/// per <see cref="BitAiUiOptions.RenderInterval"/>.
/// </remarks>
public abstract class BitAiComponentBase : BitComponentBase
{
    private BitAiRenderScheduler? _scheduler;
    private BitAiUiOptions? _registeredOptions;



    /// <summary>
    /// The AI options cascaded by the nearest <see cref="BitAiOptionsProvider"/>, if any.
    /// </summary>
    [CascadingParameter] public BitAiUiOptions? CascadedOptions { get; set; }

    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;



    /// <summary>
    /// The options this component uses: the cascaded ones, else the ones registered with
    /// <c>AddBitBlazorUIAIServices</c> (<see cref="BitAiBuilder.ConfigureUi"/>), else <see cref="BitAiUiOptions.Default"/>.
    /// </summary>
    protected BitAiUiOptions Options => CascadedOptions ?? RegisteredOptions;

    /// <summary>
    /// The user-facing text of the options in force.
    /// </summary>
    protected BitAiStrings Strings => Options.Strings;

    private BitAiUiOptions RegisteredOptions => _registeredOptions ??=
        _serviceProvider?.GetService(typeof(BitAiUiOptions)) as BitAiUiOptions ?? BitAiUiOptions.Default;



    public override Task SetParametersAsync(ParameterView parameters)
    {
        // The cascade is taken here, the rest is handed on to BitComponentBase through ParametersCache - the same
        // hand-over the generated code of every derived component makes.
        var parametersDictionary = TakeParametersDictionary(parameters);

        CascadedOptions = parametersDictionary.Remove(nameof(CascadedOptions), out var cascadedOptions)
            ? (BitAiUiOptions?)cascadedOptions
            : null;

        ParametersCache = parametersDictionary;
        return base.SetParametersAsync(ParameterView.Empty);
    }



    /// <summary>
    /// Asks for a re-render, at most once per <see cref="BitAiUiOptions.RenderInterval"/>.
    /// </summary>
    /// <remarks>
    /// Safe to call from any thread and on every streamed token.
    /// </remarks>
    protected void RequestRender()
    {
        if (IsDisposing) return;

        _scheduler ??= new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged), Options.RenderInterval);
        _scheduler.Request();
    }

    /// <summary>
    /// Renders immediately, bypassing the interval. Call this when a stream ends so the final state is not left waiting
    /// on a timer.
    /// </summary>
    /// <returns>A task that completes when the render has been requested.</returns>
    protected Task RenderNowAsync() => _scheduler?.FlushAsync() ?? InvokeAsync(StateHasChanged);



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_scheduler is not null)
        {
            await _scheduler.DisposeAsync();
            _scheduler = null;
        }

        await base.DisposeAsync(disposing);
    }
}
