namespace Bit.BlazorUI;

public partial class BitSplitter : BitComponentBase
{
    private bool _isDragging;
    private double _initialPosition;
    private double _initialFirstPanelWidth;
    private double _initialSecondPanelWidth;
    private double _initialFirstPanelHeight;
    private double _initialSecondPanelHeight;
    private ElementReference _firstPanelRef;
    private ElementReference _secondPanelRef;
    private ElementReference _splitterGutterRef;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The size of BitSplitter gutter in pixels.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? GutterSize { get; set; }

    /// <summary>
    /// The icon of BitSplitter gutter.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? GutterIcon { get; set; }

    /// <summary>
    /// The content for the first panel.
    /// </summary>
    [Parameter] public RenderFragment? FirstPanel { get; set; }

    /// <summary>
    /// The size of first panel.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? FirstPanelSize { get; set; }

    /// <summary>
    /// The max size of first panel.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? FirstPanelMaxSize { get; set; }

    /// <summary>
    /// The min size of first panel.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? FirstPanelMinSize { get; set; }

    /// <summary>
    /// The content for the second panel.
    /// </summary>
    [Parameter] public RenderFragment? SecondPanel { get; set; }

    /// <summary>
    /// The size of second panel.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? SecondPanelSize { get; set; }

    /// <summary>
    /// The max size of second panel.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? SecondPanelMaxSize { get; set; }

    /// <summary>
    /// The min size of second panel.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? SecondPanelMinSize { get; set; }

    /// <summary>
    /// Sets the orientation of BitSplitter to vertical.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    protected override string RootElementClass => "bit-spl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Vertical ? "bit-spl-vrt" : string.Empty);

        ClassBuilder.Register(() => _isDragging ? "bit-spl-drg" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => GutterSize.HasValue ? $"--gutter-size:{GutterSize}px" : string.Empty);

        StyleBuilder.Register(() => FirstPanelSize.HasValue ? $"--first-panel:{FirstPanelSize}px" : string.Empty);
        StyleBuilder.Register(() => FirstPanelMaxSize.HasValue ? $"--first-panel-max:{FirstPanelMaxSize}px" : string.Empty);
        StyleBuilder.Register(() => FirstPanelMinSize.HasValue ? $"--first-panel-min:{FirstPanelMinSize}px" : string.Empty);

        StyleBuilder.Register(() => SecondPanelSize.HasValue ? $"--second-panel:{SecondPanelSize}px" : string.Empty);
        StyleBuilder.Register(() => SecondPanelMaxSize.HasValue ? $"--second-panel-max:{SecondPanelMaxSize}px" : string.Empty);
        StyleBuilder.Register(() => SecondPanelMinSize.HasValue ? $"--second-panel-min:{SecondPanelMinSize}px" : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        await ResetPaneDimensions();
    }



    private async Task ResetPaneDimensions()
    {
        await _js.InvokeVoidAsync("BitSplitter.resetPaneDimensions", _firstPanelRef);
        await _js.InvokeVoidAsync("BitSplitter.resetPaneDimensions", _secondPanelRef);
    }

    private async Task OnDraggingStart(double position)
    {
        _isDragging = true;
        ClassBuilder.Reset();

        _initialPosition = position;

        _initialFirstPanelWidth = await _js.InvokeAsync<double>("BitSplitter.getSplitterWidth", _firstPanelRef);
        _initialSecondPanelWidth = await _js.InvokeAsync<double>("BitSplitter.getSplitterWidth", _secondPanelRef);

        _initialFirstPanelHeight = await _js.InvokeAsync<double>("BitSplitter.getSplitterHeight", _firstPanelRef);
        _initialSecondPanelHeight = await _js.InvokeAsync<double>("BitSplitter.getSplitterHeight", _secondPanelRef);
    }

    private async Task OnDragging(double position)
    {
        if (_isDragging)
        {
            var delta = position - _initialPosition;

            if (Vertical)
            {
                var newPrimaryHeight = _initialFirstPanelHeight + delta;
                var newSecondaryHeight = _initialSecondPanelHeight - delta;
                await _js.InvokeVoidAsync("BitSplitter.setSplitterHeight", _firstPanelRef, newPrimaryHeight);
                await _js.InvokeVoidAsync("BitSplitter.setSplitterHeight", _secondPanelRef, newSecondaryHeight);
            }
            else
            {
                var newPrimaryWidth = _initialFirstPanelWidth + delta;
                var newSecondaryWidth = _initialSecondPanelWidth - delta;
                await _js.InvokeVoidAsync("BitSplitter.setSplitterWidth", _firstPanelRef, newPrimaryWidth);
                await _js.InvokeVoidAsync("BitSplitter.setSplitterWidth", _secondPanelRef, newSecondaryWidth);
            }
        }
    }

    private async Task OnDraggingEnd()
    {
        _isDragging = false;
        ClassBuilder.Reset();

        await _js.InvokeVoidAsync("BitSplitter.handleSplitterDraggingEnd");
    }

    private async Task OnPointerDown(PointerEventArgs e)
    {
        await OnDraggingStart(Vertical ? e.ClientY : e.ClientX);
    }

    private async Task OnPointerMove(PointerEventArgs e)
    {
        await OnDragging(Vertical ? e.ClientY : e.ClientX);
    }

    private async Task OnTouchStart(TouchEventArgs e)
    {
        await _js.InvokeVoidAsync("BitSplitter.handleSplitterDragging", e);

        await OnDraggingStart(Vertical ? e.Touches[0].ClientY : e.Touches[0].ClientX);
    }

    private async Task OnTouchMove(TouchEventArgs e)
    {
        await OnDragging(Vertical ? e.Touches[0].ClientY : e.Touches[0].ClientX);
    }
}
