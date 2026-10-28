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
    private BitAiRendererRegistry? _registeredRenderers;



    /// <summary>
    /// The AI options cascaded by the nearest <see cref="BitAiOptionsProvider"/>, if any.
    /// </summary>
    [CascadingParameter] public BitAiUiOptions? CascadedOptions { get; set; }

    /// <summary>
    /// The renderer registry cascaded to this component (a <c>CascadingValue</c> of a <see cref="BitAiRendererRegistry"/>),
    /// if any.
    /// </summary>
    /// <remarks>
    /// The registered registry is app-wide - one instance for every user of a server-side app - so a page or a test that
    /// wants renderers of its own cascades a registry of its own rather than changing the registered one under every
    /// other view.
    /// </remarks>
    [CascadingParameter] public BitAiRendererRegistry? CascadedRenderers { get; set; }

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

    // The base address of the document, which a relative image is fetched against - or null where there is none to read:
    // no NavigationManager registered (a component rendered by an HtmlRenderer of the app's own), or one never
    // initialized. Looked up rather than injected for that reason, and only read when a policy needs the host of a
    // relative image (BitAiSafeUrls.Decide) or a placeholder names it.
    private string? DocumentBaseUri
    {
        get
        {
            try
            {
                return (_serviceProvider.GetService(typeof(NavigationManager)) as NavigationManager)?.BaseUri;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// What the <see cref="BitAiUiOptions.Images"/> policy in force decides about an image source already made safe
    /// (<see cref="BitAiSafeUrls.Image"/>).
    /// </summary>
    private protected BitAiImageDecision DecideImageSource(string source) =>
        BitAiSafeUrls.Decide(Options.Images, source, () => DocumentBaseUri);

    /// <summary>
    /// What the policy decides about an image source already made safe, once the user may have asked for it: an image
    /// the policy asks about first is shown once the user loaded it (<paramref name="loaded"/>), and a view handed
    /// another source asks again.
    /// </summary>
    private protected BitAiImageDecision DecideImage(string source, bool loaded)
    {
        var decision = DecideImageSource(source);

        return decision == BitAiImageDecision.AskFirst && loaded ? BitAiImageDecision.Show : decision;
    }

    /// <summary>
    /// The host an image source would be fetched from, for naming the placeholder of an image the policy holds back.
    /// </summary>
    private protected string ImageHostOf(string source) =>
        BitAiSafeUrls.HostOf(BitAiSafeUrls.Resolve(source, () => DocumentBaseUri));

    /// <summary>
    /// The renderers this component uses: the cascaded registry (<see cref="CascadedRenderers"/>), else the one registered
    /// with <c>AddBitBlazorUIAIServices</c> (<see cref="BitAiBuilder.ConfigureRenderers"/>).
    /// </summary>
    private protected BitAiRendererRegistry Renderers => CascadedRenderers ?? RegisteredRenderers;

    private BitAiUiOptions RegisteredOptions => _registeredOptions ??=
        _serviceProvider?.GetService(typeof(BitAiUiOptions)) as BitAiUiOptions ?? BitAiUiOptions.Default;

    private BitAiRendererRegistry RegisteredRenderers => _registeredRenderers ??=
        _serviceProvider?.GetService(typeof(BitAiRendererRegistry)) as BitAiRendererRegistry
        ?? throw new InvalidOperationException("No BitAiRendererRegistry is registered: call AddBitBlazorUIAIServices at startup, or cascade a registry to the component.");



    public override Task SetParametersAsync(ParameterView parameters)
    {
        // The cascade is taken here, the rest is handed on to BitComponentBase through ParametersCache - the same
        // hand-over the generated code of every derived component makes.
        var parametersDictionary = TakeParametersDictionary(parameters);

        CascadedOptions = parametersDictionary.Remove(nameof(CascadedOptions), out var cascadedOptions)
            ? (BitAiUiOptions?)cascadedOptions
            : null;

        CascadedRenderers = parametersDictionary.Remove(nameof(CascadedRenderers), out var cascadedRenderers)
            ? (BitAiRendererRegistry?)cascadedRenderers
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
