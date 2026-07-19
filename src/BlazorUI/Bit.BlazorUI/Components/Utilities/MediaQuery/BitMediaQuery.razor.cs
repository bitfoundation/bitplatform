namespace Bit.BlazorUI;

/// <summary>
/// A component to easily use predefined bit BlazorUI media queries in Blazor components.
/// </summary>
public partial class BitMediaQuery : BitComponentBase
{
    private string? _query;
    private bool _isMatched;
    private DotNetObjectReference<BitMediaQuery>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The content of the element to render if the specified query is matched.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The content to be rendered if the provided query is matched (an alias for ChildContent).
    /// </summary>
    [Parameter] public RenderFragment? Matched { get; set; }

    /// <summary>
    /// The content to be rendered if the provided query is not matched.
    /// </summary>
    [Parameter] public RenderFragment? NotMatched { get; set; }

    /// <summary>
    /// The event callback to be called when the state of the media query has been changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Specifies the custom query to be matched.
    /// </summary>
    [Parameter] public string? Query { get; set; }

    /// <summary>
    /// Defines the screen query to be matched, amongst the predefined Bit screen media queries.
    /// </summary>
    [Parameter] public BitScreenQuery? ScreenQuery { get; set; }



    [JSInvokable("OnMatchChange")]
    public async ValueTask _OnMatchChange(bool isMatched)
    {
        _isMatched = isMatched;

        await InvokeAsync(StateHasChanged);

        _ = OnChange.InvokeAsync(isMatched);
    }



    protected override string RootElementClass => "bit-mdq";

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
            if (effectiveKey != _query || screenQuery is not null)
            {
                _query = effectiveKey;
                await _js.BitMediaQuerySetup(_Id, customQuery, screenQuery, _dotnetObj);
            }
        }
        else if (_query is not null)
        {
            // Neither a Query nor a ScreenQuery resolves anymore: tear down the previous listener
            // and reset so a later (re)assignment sets up cleanly.
            _query = null;
            try
            {
                await _js.BitMediaQueryDispose(_Id);
            }
            catch (JSDisconnectedException) { } // circuit gone; nothing to tear down
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitMediaQueryDispose(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
