namespace Bit.BlazorUI;

public partial class BitPanel : BitComponentBase
{
    private int _offsetTop;
    private bool _internalIsOpen;
    private string _containerId = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Panel.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// Whether the Panel can be dismissed by clicking outside the Panel (on the overlay).
    /// </summary>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the Panel, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPanel component.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// Used to customize how the footer inside the Panel is rendered.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Used to customize how the header inside the Panel is rendered.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Header text of Panel.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

    /// <summary>
    /// Whether the Panel is displayed.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Whether the Panel should be rendered without an overlay.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// A callback function for when the Panel is dismissed light dismiss, before the animation completes.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// Position of the modal on the screen.
    /// </summary>
    [Parameter] public BitPanelPosition? Position { get; set; }

    /// <summary>
    /// Provides Height or Width for the Panel.
    /// </summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>
    /// Set the element selector for which the Panel disables its scroll if applicable.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows or hides the close button of the Panel.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitPanel component.
    /// </summary>
    [Parameter] public BitPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Panel, if any.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Panel, if any.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public decimal? TouchTrigger { get; set; }



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
        //await OnPullStart.InvokeAsync(new BitPullToRefreshPullStartArgs(top, left, width));
    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal diffX, decimal diffY)
    {
        //await OnPullMove.InvokeAsync(diff);
    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal diffX, decimal diffY)
    {
        //await OnPullEnd.InvokeAsync(diff);
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
            await _js.BitPanelSetup(_containerId, TouchTrigger ?? 0.25m, Position ?? BitPanelPosition.End, Dir == BitDir.Rtl, dotnetObj);
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

        if ((Styles?.Container ?? string.Empty).HasValue())
        {
            styles.Add(Styles!.Container!);
        }

        return string.Join(';', styles);
    }
}
