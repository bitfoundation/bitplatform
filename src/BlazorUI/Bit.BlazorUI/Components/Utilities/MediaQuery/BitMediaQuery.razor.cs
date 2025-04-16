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
    /// The content of the element.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The content to be rendered if the provided Query is matched.
    /// </summary>
    [Parameter] public RenderFragment? Matched { get; set; }

    /// <summary>
    /// The content to be rendered if the provided Query is not matched.
    /// </summary>
    [Parameter] public RenderFragment? NotMatched { get; set; }

    /// <summary>
    /// The event callback to be called when the state of the media query has been changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Specified the custom query to be matched.
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

        var query = Query ?? GetQuery(ScreenQuery);

        if (query.HasValue() && query != _query)
        {
            _query = query;
            await _js.BitMediaQuerySetup(_Id, _query, _dotnetObj);
        }
    }



    private static string GetQuery(BitScreenQuery? query)
    {
        return query switch
        {
            BitScreenQuery.Xs => "(max-width: 600px)",
            BitScreenQuery.Sm => "(min-width: 601px) and (max-width: 960px)",
            BitScreenQuery.Md => "(min-width: 961px) and (max-width: 1280px)",
            BitScreenQuery.Lg => "(min-width: 1281px) and (max-width: 1920px)",
            BitScreenQuery.Xl => "(min-width: 1921px) and (max-width: 2560px)",
            BitScreenQuery.Xxl => "(min-width: 2561px)",

            BitScreenQuery.LtSm => "(max-width: 600px)",
            BitScreenQuery.LtMd => "(max-width: 960px)",
            BitScreenQuery.LtLg => "(max-width: 1280px)",
            BitScreenQuery.LtXl => "(max-width: 1920px)",
            BitScreenQuery.LtXxl => "(max-width: 2560px)",

            BitScreenQuery.GtXs => "(min-width: 601px)",
            BitScreenQuery.GtSm => "(min-width: 961px)",
            BitScreenQuery.GtMd => "(min-width: 1281px)",
            BitScreenQuery.GtLg => "(min-width: 1921px)",
            BitScreenQuery.GtXl => "(min-width: 2561px)",
            _ => string.Empty
        };
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
