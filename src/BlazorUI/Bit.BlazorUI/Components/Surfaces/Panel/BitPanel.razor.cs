namespace Bit.BlazorUI;

/// <summary>
/// Panels are overlays that contain supplementary content and are used for complex creation, edit, or management experiences.
/// </summary>
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
    /// Custom CSS styles for different parts of the panel component.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// The swiping point (difference percentage) based on the width of the panel container to trigger the close action (default is 0.25m).
    /// </summary>
    [Parameter] public decimal? SwipeTrigger { get; set; }



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
        var start = (Position == BitPanelPosition.Start || Position == BitPanelPosition.End) ? startX : startY;
        await OnSwipeStart.InvokeAsync(start);
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal diffX, decimal diffY)
    {
        var diff = (Position == BitPanelPosition.Start || Position == BitPanelPosition.End) ? diffX : diffY;
        await OnSwipeMove.InvokeAsync(diff);
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal diffX, decimal diffY)
    {
        var diff = (Position == BitPanelPosition.Start || Position == BitPanelPosition.End) ? diffX : diffY;
        await OnSwipeEnd.InvokeAsync(diff);
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
            var position = Position ?? BitPanelPosition.End;
            var orientationLock = position == BitPanelPosition.Start || position == BitPanelPosition.End
                                    ? BitSwipeOrientation.Horizontal 
                                    : BitSwipeOrientation.Vertical;
            await _js.BitSwipesSetup(_containerId, SwipeTrigger ?? 0.25m, position, Dir == BitDir.Rtl, orientationLock, dotnetObj, false);
        }

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        _offsetTop = 0;

        if (AutoToggleScroll is false) return;

        _offsetTop = await _js.BitUtilsToggleOverflow(ScrollerSelector ?? "body", IsOpen);

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

    private string GetContainerCssStyles()
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

    private string GetContainerCssClasses()
    {
        List<string> classes = ["bit-pnl-cnt"];

        classes.Add(Position switch
        {
            BitPanelPosition.Start => "bit-pnl-start",
            BitPanelPosition.End => "bit-pnl-end",
            BitPanelPosition.Top => "bit-pnl-top",
            BitPanelPosition.Bottom => "bit-pnl-bottom",
            _ => "bit-pnl-end"
        });

        if (Classes?.Container is string containerClass && containerClass.HasValue())
        {
            classes.Add(containerClass);
        }

        return string.Join(' ', classes);
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
            await _js.BitSwipesDispose(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
