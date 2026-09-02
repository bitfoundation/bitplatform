namespace Bit.BlazorUI;

/// <summary>
/// A component to render content based on CSS media queries, using the browser's matchMedia API.
/// It offers the predefined bit BlazorUI screen queries, built from the live theme breakpoints so
/// customized themes are honored, and also accepts any custom media query, including non-viewport
/// features such as orientation or prefers-color-scheme.
/// </summary>
public partial class BitMediaQuery : BitComponentBase
{
    private string? _query;
    private string? _setupId;
    private bool _isMatched;
    private DotNetObjectReference<BitMediaQuery>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the element to render if the specified query is matched.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The initial matched state to render with until the actual result of the query arrives from
    /// the browser. Useful to avoid a flash of the wrong content during prerendering (or before the
    /// JavaScript runtime becomes available), where the query cannot be evaluated yet.
    /// </summary>
    [Parameter] public bool DefaultMatched { get; set; }

    /// <summary>
    /// The content to be rendered if the provided query is matched (an alias for ChildContent).
    /// </summary>
    [Parameter] public RenderFragment? Matched { get; set; }

    /// <summary>
    /// The content to be rendered if the provided query is not matched.
    /// </summary>
    [Parameter] public RenderFragment? NotMatched { get; set; }

    /// <summary>
    /// Renders the active content directly, without the wrapping root element.
    /// </summary>
    /// <remarks>
    /// Since no element is rendered, everything that describes one - the class, the style, the id,
    /// the direction and the splatted attributes - has nowhere to land and is ignored, and
    /// <see cref="BitComponentBase.RootElement"/> is never captured. The one exception is a
    /// <see cref="BitVisibility.Collapsed"/> <see cref="BitComponentBase.Visibility"/>, which asks
    /// for the component to be out of the DOM and needs no element of its own to say so: nothing is
    /// rendered at all, not even the content. A <see cref="BitScreenQuery"/>
    /// then resolves its breakpoints from the document root rather than the component's own themed
    /// scope, so the breakpoints of an enclosing BitThemeProvider are not picked up in this mode.
    /// </remarks>
    [Parameter] public bool NoWrapper { get; set; }

    /// <summary>
    /// The event callback to be called when the state of the media query has been changed.
    /// It is also called once with the initial matched state, right after the query gets evaluated
    /// by the browser for the first time.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Specifies the custom query to be matched. Any valid CSS media query is accepted, including
    /// non-viewport features such as orientation, pointer, or prefers-color-scheme.
    /// Takes precedence over <see cref="ScreenQuery"/> when both are provided.
    /// </summary>
    [Parameter] public string? Query { get; set; }

    /// <summary>
    /// Defines the screen query to be matched, amongst the predefined Bit screen media queries.
    /// The actual query is built at runtime from the live theme breakpoints (the
    /// <c>--bit-bp-*</c> CSS variables), so customized theme breakpoints are honored.
    /// </summary>
    [Parameter] public BitScreenQuery? ScreenQuery { get; set; }



    /// <summary>
    /// Gets the current matched state of the provided query: the latest result reported by the
    /// browser, or <see cref="DefaultMatched"/> while no result has arrived yet.
    /// </summary>
    public bool IsMatched => _isMatched;



    [JSInvokable("OnMatchChange")]
    public async ValueTask _OnMatchChange(bool isMatched)
    {
        if (IsDisposed) return;

        _isMatched = isMatched;

        await InvokeAsync(StateHasChanged);

        await OnChange.InvokeAsync(isMatched);
    }



    protected override string RootElementClass => "bit-mdq";

    protected override void OnInitialized()
    {
        // Render with the DefaultMatched state until the browser reports the actual result of the
        // query (e.g. during prerendering); the first JS notification then takes over.
        _isMatched = DefaultMatched;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        if (IsDisposed) return;

        // A custom Query takes precedence; otherwise defer to the predefined ScreenQuery, whose
        // media query is built on the JS side from the live --bit-bp-* theme breakpoints so a
        // customized BitTheme.Layout.Breakpoints is honored (rather than baking fixed px here).
        // A blank Query is treated as absent so a bound-but-empty value still lets ScreenQuery win.
        var customQuery = Query.HasValue() ? Query : null;
        var screenQuery = customQuery is null ? ScreenQuery?.ToString() : null;
        var effectiveKey = customQuery ?? screenQuery;

        if (effectiveKey.HasValue())
        {
            // For a predefined ScreenQuery the actual media-query expression is resolved on the JS
            // side from the live --bit-bp-* theme breakpoints, so it can change while the enum name
            // stays the same (e.g. after new breakpoints are applied). Re-invoke setup on every
            // render in that case and let the JS side reuse the existing listener when the resolved
            // expression is unchanged; a custom Query is verbatim, so the key comparison suffices.
            // The JS listener is keyed by the element id, so a changed Id parameter also needs a
            // re-setup (after tearing the old key down, or its listener would leak).
            if (effectiveKey != _query || _Id != _setupId || screenQuery is not null)
            {
                try
                {
                    if (_setupId is not null && _setupId != _Id)
                    {
                        await _js.BitMediaQueryDispose(_setupId);
                    }

                    _query = effectiveKey;
                    _setupId = _Id;

                    // In NoWrapper mode no element of this component's own is rendered, so the id
                    // is only the JS listener key: the flag tells the JS side to read the theme
                    // breakpoints off the document root instead of off whatever element happens to
                    // carry that id (the rendered content itself, when it is given the same id).
                    await _js.BitMediaQuerySetup(_Id, customQuery, screenQuery, NoWrapper, _dotnetObj);
                }
                catch (JSDisconnectedException) { } // circuit gone; nothing to set up
            }
        }
        else if (_setupId is not null)
        {
            // Neither a Query nor a ScreenQuery resolves anymore: tear down the previous listener
            // and reset so a later (re)assignment sets up cleanly.
            var setupId = _setupId;
            _query = null;
            _setupId = null;
            try
            {
                await _js.BitMediaQueryDispose(setupId);
            }
            catch (JSDisconnectedException) { } // circuit gone; nothing to tear down
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_setupId is not null)
        {
            // Tear the JS listener down before disposing the .NET reference, so a media change
            // firing in between cannot invoke an already disposed object.
            try
            {
                await _js.BitMediaQueryDispose(_setupId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _dotnetObj?.Dispose();
    }
}
