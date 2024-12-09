namespace Bit.BlazorUI;

public partial class BitPanel : BitComponentBase, IAsyncDisposable
{
    private int _offsetTop;
    private bool _disposed;
    private bool _internalIsOpen;
    private string _containerId = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the panel.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// Whether the panel can be dismissed by clicking outside of it on the overlay.
    /// </summary>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the panel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the panel.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The template used to render the footer section of the panel.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// The template used to render the header section of the panel.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The text of the header section of the panel.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

    /// <summary>
    /// Determines the openness of the panel.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Removes the overlay element of the panel.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// A callback function for when the Panel is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// The event callback for when the swipe action starts on the container of the panel.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnSwipeStart { get; set; }

    /// <summary>
    /// The event callback for when the swipe action moves on the container of the panel.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnSwipeMove { get; set; }

    /// <summary>
    /// The event callback for when the swipe action ends on the container of the panel.
    /// </summary>
    [Parameter] public EventCallback<decimal> OnSwipeEnd { get; set; }

    /// <summary>
    /// The position of the panel to show on the screen.
    /// </summary>
    [Parameter] public BitPanelPosition? Position { get; set; }

    /// <summary>
    /// The value of the height or width (based on the position) of the Panel.
    /// </summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>
    /// Specifies the element selector for which the Panel disables its scroll if applicable.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the Panel.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the panel component.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// Specifies the id for the aria-describedby attribute of the panel.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// The swiping point (difference percentage) based on the width of the panel container to trigger the close action (default is 0.25m).
    /// </summary>
    [Parameter] public decimal? SwipeTrigger { get; set; }

    /// <summary>
    /// Specifies the id for the aria-labelledby attribute of the panel.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }



    public async Task Open()
    {
        if (await AssignIsOpen(true) is false) return;

        StateHasChanged();
    }

    public async Task Close()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }



    [JSInvokable("OnStart")]
    public async Task _OnStart(decimal startX, decimal startY)
    {
        await OnSwipeStart.InvokeAsync((Position == BitPanelPosition.Start || Position == BitPanelPosition.End) ? startX : startY);
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal diffX, decimal diffY)
    {
        await OnSwipeMove.InvokeAsync((Position == BitPanelPosition.Start || Position == BitPanelPosition.End) ? diffX : diffY);
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal diffX, decimal diffY)
    {
        await OnSwipeEnd.InvokeAsync((Position == BitPanelPosition.Start || Position == BitPanelPosition.End) ? diffX : diffY);
    }

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await ClosePanel(new());
        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-pnl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _offsetTop > 0 ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override Task OnInitializedAsync()
    {
        _containerId = $"BitPanel-{UniqueId}-container";

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            var dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitPanelSetup(_containerId, SwipeTrigger ?? 0.25m, Position ?? BitPanelPosition.End, Dir == BitDir.Rtl, dotnetObj);
        }

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        _offsetTop = 0;

        if (AutoToggleScroll is false) return;

        _offsetTop = await _js.ToggleOverflow(ScrollerSelector ?? "body", IsOpen);

        StyleBuilder.Reset();
        StateHasChanged();
    }



    private async Task ClosePanel(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        await OnDismiss.InvokeAsync(e);
    }

    private async Task OnOverlayClicked(MouseEventArgs e)
    {
        if (Blocking is true) return;

        await ClosePanel(e);
    }

    private async Task OnCloseButtonClicked(MouseEventArgs e)
    {
        await ClosePanel(e);
    }

    private string GetPositionClass() => Position switch
    {
        BitPanelPosition.Start => "bit-pnl-start",
        BitPanelPosition.End => "bit-pnl-end",
        BitPanelPosition.Top => "bit-pnl-top",
        BitPanelPosition.Bottom => "bit-pnl-bottom",
        _ => "bit-pnl-end"
    };

    private string GetPanelStyle()
    {
        List<string> styles = [];

        if (IsOpen)
        {
            styles.Add("transform:translate3d(0,0,0);opacity:1");
        }

        if (Size is not null)
        {
            var prop = Position is BitPanelPosition.Top or BitPanelPosition.Bottom ? "height" : "width";
            styles.Add(FormattableString.Invariant($"{prop}:{Size}px"));
        }

        if (Styles?.Container is string containerStyle && containerStyle.HasValue())
        {
            styles.Add(containerStyle);
        }

        return string.Join(';', styles);
    }


    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        try
        {
            await _js.BitPanelDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
