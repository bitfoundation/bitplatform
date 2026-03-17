namespace Bit.BlazorUI;

/// <summary>
/// The BitSplitter component divides a container into two adjustable sections, either horizontally or vertically. Users can resize these sections by dragging the divider.
/// </summary>
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
    /// The icon for the BitSplitter gutter using <see cref="BitIconInfo"/> for external icon library support.
    /// Takes precedence over <see cref="GutterIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="GutterIconName"/> instead.
    /// When assigning a plain <see cref="string"/> to this property, it is converted to a <see cref="BitIconInfo"/> instance and
    /// treated as the raw CSS class name(s) for the external icon (for example, <c>"fa fa-home"</c>), not as a Fluent UI icon name.
    /// To render built-in Fluent UI icons, use <see cref="GutterIconName"/> instead; passing a Fluent icon name as a string to
    /// <see cref="GutterIcon"/> will compile but will not render a Fluent icon unless you also configure
    /// <see cref="BitIconInfo.BaseClass"/> and/or <see cref="BitIconInfo.Prefix"/> for a custom icon set.
    /// </remarks>
    [Parameter]
    public BitIconInfo? GutterIcon { get; set; }

    /// <summary>
    /// The name of the built-in Fluent UI icon to render in the BitSplitter gutter.
    /// Ignored when <see cref="GutterIcon"/> is also set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.GripperDotsVertical</c>).
    /// For external icon libraries, use <see cref="GutterIcon"/> instead, 
    /// where string values are interpreted as CSS class name(s)
    /// for the external icon rather than as Fluent UI icon identifiers.
    /// </remarks>
    [Parameter]
    public string? GutterIconName { get; set; }

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
    [CallOnSet(nameof(OnSetVertical))]
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



    private void OnSetVertical()
    {
        _ = _js.BitSplitterResetPaneDimensions(_firstPanelRef);
        _ = _js.BitSplitterResetPaneDimensions(_secondPanelRef);
    }

    private async Task OnDraggingStart(double position)
    {
        _isDragging = true;
        ClassBuilder.Reset();

        _initialPosition = position;

        _initialFirstPanelWidth = await _js.BitSplitterGetSplitterWidth(_firstPanelRef);
        _initialSecondPanelWidth = await _js.BitSplitterGetSplitterWidth(_secondPanelRef);

        _initialFirstPanelHeight = await _js.BitSplitterGetSplitterHeight(_firstPanelRef);
        _initialSecondPanelHeight = await _js.BitSplitterGetSplitterHeight(_secondPanelRef);
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
                await _js.BitSplitterSetSplitterHeight(_firstPanelRef, newPrimaryHeight);
                await _js.BitSplitterSetSplitterHeight(_secondPanelRef, newSecondaryHeight);
            }
            else
            {
                var newPrimaryWidth = _initialFirstPanelWidth + delta;
                var newSecondaryWidth = _initialSecondPanelWidth - delta;
                await _js.BitSplitterSetSplitterWidth(_firstPanelRef, newPrimaryWidth);
                await _js.BitSplitterSetSplitterWidth(_secondPanelRef, newSecondaryWidth);
            }
        }
    }

    private async Task OnDraggingEnd()
    {
        _isDragging = false;
        ClassBuilder.Reset();

        await _js.BitSplitterHandleSplitterDraggingEnd();
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
        await _js.BitSplitterHandleSplitterDragging(e);

        await OnDraggingStart(Vertical ? e.Touches[0].ClientY : e.Touches[0].ClientX);
    }

    private async Task OnTouchMove(TouchEventArgs e)
    {
        await OnDragging(Vertical ? e.Touches[0].ClientY : e.Touches[0].ClientX);
    }
}
