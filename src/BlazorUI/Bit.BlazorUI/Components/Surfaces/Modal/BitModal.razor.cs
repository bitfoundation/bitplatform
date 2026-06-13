namespace Bit.BlazorUI;

/// <summary>
/// Modals are temporary pop-ups that take focus from the page or app and require people to interact with them.
/// </summary>
/// <remarks>
/// There are two different modal components available for different purposes: BitModal is a basic, lightweight modal
/// for simple pop-up content, while BitProModal (in the Bit.BlazorUI.Extras package) is an advanced modal with extra
/// features such as dragging, blocking, modeless, positioning, full-size and scroll handling. Use BitProModal if you
/// need any of those advanced behaviors.
/// </remarks>
public partial class BitModal : BitComponentBase
{
    private bool _internalIsOpen;
    private string _containerId = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether the Modal should be announced as modal to assistive technologies.
    /// </summary>
    [Parameter] public bool AriaModal { get; set; } = true;

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
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

    [CascadingParameter]
    private BitModalParameters ModalParameters { get => modalParameters; set { modalParameters = value; modalParameters.SetModal(this); } }
    private BitModalParameters modalParameters = new();


    /// <summary>
    /// Makes the Modal height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    [Parameter] public bool? IsAlert { get; set; }

    /// <summary>
    /// Whether the Modal is displayed.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// Whether the overlay should be rendered.
    /// </summary>
    [Parameter] public bool ShowOverlay { get; set; } = true;

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

        ClassBuilder.Register(() => ModalParameters.FullHeight ? "bit-mdl-fhe" : string.Empty);
        ClassBuilder.Register(() => ModalParameters.FullWidth ? "bit-mdl-fwi" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
        StyleBuilder.Register(() => ModalParameters.Styles?.Root);
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
    }



    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (ModalParameters.IsEnabled is false) return;

        await ModalParameters.OnOverlayClick.InvokeAsync(e);

        if (Blocking) return;

        if (await AssignIsOpen(false) is false) return;
    }

    private string GetRole()
    {
        return (ModalParameters.IsAlert ?? false) ? "alertdialog" : "dialog";
    }

    private void OnSetIsOpen()
    {
        if (IsOpen || IsRendered is false) return;

        _ = ModalParameters.OnDismiss.InvokeAsync().ContinueWith(_ => InvokeAsync(StateHasChanged));
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);
    }
}
