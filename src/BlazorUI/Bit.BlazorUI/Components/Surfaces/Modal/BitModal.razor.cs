namespace Bit.BlazorUI;

/// <summary>
/// Modals are temporary pop-ups that take focus from the page or app and require people to interact with them.
/// </summary>
public partial class BitModal : BitComponentBase, IAsyncDisposable
{
    private int _offsetTop;
    private bool _disposed;
    private bool _internalIsOpen;
    private string _containerId = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    [CascadingParameter]
    private BitModalParameters ModalParameters { get => modalParameters; set { modalParameters = value; modalParameters.SetModal(this); } }
    private BitModalParameters modalParameters = new();


    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// When true, the Modal will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Whether the Modal can be light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the Modal, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The CSS selector of the drag element. by default it's the Modal container.
    /// </summary>
    [Parameter] public string? DragElementSelector { get; set; }

    /// <summary>
    /// Whether the Modal can be dragged around.
    /// </summary>
    [Parameter] public bool Draggable { get; set; }

    /// <summary>
    /// Makes the Modal height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width and height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullSize { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog). If this is set, it will override the ARIA role determined by Blocking and Modeless.
    /// </summary>
    [Parameter] public bool? IsAlert { get; set; }

    /// <summary>
    /// Whether the Modal is displayed.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal). if true: Blocking is ignored, there will be no overlay.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// Position of the Modal on the screen.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPosition? Position { get; set; }

    /// <summary>
    /// Set the element selector for which the Modal disables its scroll if applicable.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitModalClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }



    protected override string RootElementClass => "bit-mdl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => ModalParameters.Classes?.Root);

        ClassBuilder.Register(() => ModalParameters.AbsolutePosition ? "bit-mdl-abs" : string.Empty);

        ClassBuilder.Register(() => ModalParameters.FullSize || ModalParameters.FullHeight ? "bit-mdl-fhe" : string.Empty);
        ClassBuilder.Register(() => ModalParameters.FullSize || ModalParameters.FullWidth ? "bit-mdl-fwi" : string.Empty);

        ClassBuilder.Register(() => ModalParameters.Position switch
        {
            BitPosition.Center => "bit-mdl-ctr",
            BitPosition.TopLeft => "bit-mdl-tl",
            BitPosition.TopCenter => "bit-mdl-tc",
            BitPosition.TopRight => "bit-mdl-tr",
            BitPosition.CenterLeft => "bit-mdl-cl",
            BitPosition.CenterRight => "bit-mdl-cr",
            BitPosition.BottomLeft => "bit-mdl-bl",
            BitPosition.BottomCenter => "bit-mdl-bc",
            BitPosition.BottomRight => "bit-mdl-br",
            _ => "bit-mdl-ctr"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => ModalParameters.Styles?.Root);

        StyleBuilder.Register(() => _offsetTop > 0 ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override void OnInitialized()
    {
        _containerId = $"BitModal-{UniqueId}-container";

        ModalParameters.SetModal(this);

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        if (IsOpen)
        {
            if (ModalParameters.Draggable)
            {
                _ = _js.BitModalSetupDragDrop(_containerId, GetDragElementSelector());
            }
            else
            {
                _ = _js.BitModalRemoveDragDrop(_containerId, GetDragElementSelector());
            }
        }
        else
        {
            _ = _js.BitModalRemoveDragDrop(_containerId, GetDragElementSelector());
        }

        _offsetTop = 0;

        //if (ModalParameters.AutoToggleScroll is false) return;
        //_offsetTop = await _js.ToggleOverflow(ModalParameters.ScrollerSelector ?? "body", IsOpen);
        await ToggleScroll(IsOpen);

        if (ModalParameters.AbsolutePosition is false) return;

        StyleBuilder.Reset();
        StateHasChanged();
    }



    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (ModalParameters.IsEnabled is false) return;

        if (ModalParameters.Blocking) return;

        await ModalParameters.OnOverlayClick.InvokeAsync(e);

        if (await AssignIsOpen(false) is false) return;

        await ModalParameters.OnDismiss.InvokeAsync(e);
    }

    private string GetDragElementSelector()
    {
        return ModalParameters.DragElementSelector ?? $"#{_containerId}";
    }

    private string GetRole()
    {
        return (ModalParameters.IsAlert ?? (ModalParameters.Blocking && ModalParameters.Modeless is false)) ? "alertdialog" : "dialog";
    }

    private async Task ToggleScroll(bool isOpen)
    {
        if (ModalParameters.AutoToggleScroll is false) return;

        _offsetTop = await _js.BitUtilsToggleOverflow(ModalParameters.ScrollerSelector ?? "body", isOpen);
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
            await ToggleScroll(false);
            await _js.BitModalRemoveDragDrop(_containerId, GetDragElementSelector());
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
