namespace Bit.BlazorUI;

/// <summary>
/// A component to render content based on CSS media queries, using the browser's matchMedia API.
/// It offers the predefined bit BlazorUI screen queries, built from the live theme breakpoints so
/// customized themes are honored, and also accepts any custom media query, including non-viewport
/// features such as orientation or prefers-color-scheme.
/// </summary>
/// <remarks>
/// This is the layout decision CSS cannot express: rendering a different component, or none at all,
/// rather than restyling one. The two states are written as <see cref="Matched"/> and
/// <see cref="NotMatched"/>, or as the single <see cref="Template"/> that receives the state and
/// keeps its content across the flip. The state itself is readable from <see cref="IsMatched"/>,
/// bindable with <c>@bind-IsMatched</c>, and reported through <see cref="OnChange"/>, so a page can
/// take the answer without rendering anything through the component at all.
/// </remarks>
public partial class BitMediaQuery : BitComponentBase
{
    private string? _query;
    private bool _isSetup;
    private DotNetObjectReference<BitMediaQuery>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the theme cascaded from an enclosing <see cref="BitThemeProvider"/>.
    /// </summary>
    /// <remarks>
    /// Only the breakpoints of the theme are read, and only to resolve a <see cref="ScreenQuery"/>.
    /// They take precedence over the <c>--bit-bp-*</c> CSS variables of the rendered element, which
    /// is what keeps a scoped theme reachable in the two cases where there is no element of this
    /// component's own to read them from: <see cref="NoWrapper"/>, and a usage with no content at all.
    /// </remarks>
    [CascadingParameter] public BitTheme? CascadingTheme { get; set; }



    /// <summary>
    /// The content of the element to render if the specified query is matched.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The initial matched state to render with until the actual result of the query arrives from
    /// the browser. Useful to avoid a flash of the wrong content during prerendering (or before the
    /// JavaScript runtime becomes available), where the query cannot be evaluated yet.
    /// </summary>
    /// <remarks>
    /// Ignored when <see cref="IsMatched"/> is bound, since the value handed over is then the initial
    /// state already.
    /// </remarks>
    [Parameter] public bool DefaultMatched { get; set; }

    /// <summary>
    /// Gets or sets the current matched state of the provided query.
    /// </summary>
    /// <remarks>
    /// This is an output of the component rather than an input: the browser owns the state, and the
    /// component writes the latest result it reports here. Bind it with <c>@bind-IsMatched</c> to
    /// keep a field of the page in step with the query without handling <see cref="OnChange"/>.
    /// <br />
    /// Set one way (without a <c>Changed</c> callback beside it) the value belongs to the page, which
    /// freezes the state at whatever the page hands over; to seed the state before the browser has
    /// answered, leave this alone and use <see cref="DefaultMatched"/> instead.
    /// </remarks>
    [Parameter, TwoWayBound] public bool IsMatched { get; set; }

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
    /// rendered at all, not even the content.
    /// <br />
    /// A <see cref="ScreenQuery"/> is unaffected: with no element to read the <c>--bit-bp-*</c>
    /// variables from, the breakpoints of an enclosing <see cref="BitThemeProvider"/> are taken from
    /// the cascading theme (see <see cref="CascadingTheme"/>) and the document root answers for the
    /// rest, so a scoped theme is honored here as it is anywhere else.
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
    /// The content to be rendered for both states of the query, receiving the current matched state.
    /// </summary>
    /// <remarks>
    /// This is the one fragment that spans the flip, for the common case where the two states are
    /// the same markup told apart by a value: a size, a variant, a class, an attribute. Since it is
    /// one fragment in one position of the render tree, the content is updated rather than replaced
    /// when the query flips - which is what keeps the state of the components inside it (a form that
    /// is half filled in, a scroll position, a running animation) across a change of the viewport.
    /// <br />
    /// It takes precedence over <see cref="Matched"/>, <see cref="ChildContent"/> and
    /// <see cref="NotMatched"/>, which are not rendered while it is set.
    /// </remarks>
    [Parameter] public RenderFragment<bool>? Template { get; set; }



    [JSInvokable("OnMatchChange")]
    public async ValueTask _OnMatchChange(bool isMatched)
    {
        if (IsDisposed) return;

        try
        {
            // The state can already be the reported one - the first notification carries the initial
            // result of the query, which a DefaultMatched may well have guessed right. Nothing on
            // screen changes then, so the render is skipped; the callback below still runs, since a
            // handler waiting for the first real answer of the browser has not had one yet.
            if (IsMatched != isMatched)
            {
                await InvokeAsync(async () =>
                {
                    await AssignIsMatched(isMatched);

                    StateHasChanged();
                });
            }

            await OnChange.InvokeAsync(isMatched);
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            // This method is called from JavaScript, so an exception leaving it rejects the call
            // there - and the only thing the JS side can read into a rejected call is that the .NET
            // object is gone, which stops the listener for good. A handler of this page throwing
            // once would otherwise silently take the media query down with it, so the exception is
            // handed to Blazor's own error handling (an error boundary, the circuit, the logger)
            // instead of being reported back over the interop call. A disposal racing the
            // notification is the one case the JS side is right to read that way, and is rethrown.
            await DispatchExceptionAsync(ex);
        }
    }



    protected override string RootElementClass => "bit-mdq";

    protected override async Task OnInitializedAsync()
    {
        // Render with the DefaultMatched state until the browser reports the actual result of the
        // query (e.g. during prerendering); the first JS notification then takes over. A bound
        // IsMatched hands its own initial value over and owns this instead.
        if (IsMatchedHasBeenSet is false && DefaultMatched)
        {
            await AssignIsMatched(true);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        // Created after the disposal check above, so a component torn down before its first render
        // callback ran does not leave a reference behind that nothing disposes any more.
        _dotnetObj ??= DotNetObjectReference.Create(this);

        // A custom Query takes precedence; otherwise defer to the predefined ScreenQuery, whose
        // media query is built on the JS side from the live theme breakpoints so a customized
        // BitTheme.Layout.Breakpoints is honored (rather than baking fixed px here).
        // A blank Query is treated as absent so a bound-but-empty value still lets ScreenQuery win.
        var customQuery = Query.HasValue() ? Query!.Trim() : null;
        var screenQuery = customQuery is null ? ScreenQuery?.ToString() : null;
        var effectiveKey = customQuery ?? screenQuery;

        if (effectiveKey.HasValue())
        {
            // For a predefined ScreenQuery the actual media-query expression is resolved on the JS
            // side from the live theme breakpoints, so it can change while the enum name stays the
            // same (e.g. after new breakpoints are applied, or when the element the tokens are read
            // from moves into another themed scope). Re-invoke setup on every render in that case
            // and let the JS side reuse the existing listener when the resolved expression is
            // unchanged; a custom Query is verbatim, so the key comparison suffices. The listener is
            // keyed by the component's own unique id, so nothing else here depends on the Id.
            if (effectiveKey != _query || _isSetup is false || screenQuery is not null)
            {
                _query = effectiveKey;
                _isSetup = true;

                try
                {
                    await _js.BitMediaQuerySetup(UniqueId, _ElementId, customQuery, screenQuery, _ThemeBreakpoints, _dotnetObj);
                }
                catch (JSDisconnectedException)
                {
                    // Circuit gone; nothing to set up. Recorded as not set up so a later render -
                    // there is none on a gone circuit, but the state stays honest either way -
                    // tries again rather than assuming a listener that was never created.
                    _isSetup = false;
                }
            }
        }
        else if (_isSetup)
        {
            // Neither a Query nor a ScreenQuery resolves anymore: tear down the previous listener
            // and reset so a later (re)assignment sets up cleanly.
            _query = null;
            _isSetup = false;
            try
            {
                await _js.BitMediaQueryDispose(UniqueId);
            }
            catch (JSDisconnectedException) { } // circuit gone; nothing to tear down
        }
    }



    // The id of the element the theme breakpoints are read from, or null when this component renders
    // no element of its own - in no-wrapper mode, and when there is nothing at all to render. The id
    // is not the listener key, so nothing but the breakpoint lookup depends on it: any other element
    // that happens to carry the same id (the rendered content itself, in no-wrapper mode) is not this
    // component's themed scope and is deliberately not read.
    private string? _ElementId => NoWrapper is false && _HasContent ? _Id : null;

    private bool _HasContent => Template is not null || Matched is not null || ChildContent is not null || NotMatched is not null;

    // The breakpoints an enclosing BitThemeProvider overrides, as the JS side wants them. Only the
    // ones actually set are sent: everything else is left to the CSS variables and the built-in
    // defaults, so a provider that re-values a single breakpoint does not flatten the rest of the
    // scale to whatever this theme object happens to hold.
    private Dictionary<string, string>? _ThemeBreakpoints
    {
        get
        {
            var breakpoints = CascadingTheme?.Layout?.Breakpoints;
            if (breakpoints is null) return null;

            Dictionary<string, string>? result = null;

            if (breakpoints.Xs.HasValue()) (result ??= [])["xs"] = breakpoints.Xs!;
            if (breakpoints.Sm.HasValue()) (result ??= [])["sm"] = breakpoints.Sm!;
            if (breakpoints.Md.HasValue()) (result ??= [])["md"] = breakpoints.Md!;
            if (breakpoints.Lg.HasValue()) (result ??= [])["lg"] = breakpoints.Lg!;
            if (breakpoints.Xl.HasValue()) (result ??= [])["xl"] = breakpoints.Xl!;
            if (breakpoints.Xxl.HasValue()) (result ??= [])["xxl"] = breakpoints.Xxl!;

            return result;
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        try
        {
            if (_isSetup)
            {
                // Tear the JS listener down before disposing the .NET reference, so a media change
                // firing in between cannot invoke an already disposed object.
                await _js.BitMediaQueryDispose(UniqueId);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            // In a finally so the reference is released even where the teardown above failed for a
            // reason of its own: it is a .NET object, and nothing on the JS side can free it.
            _dotnetObj?.Dispose();
        }
    }
}
