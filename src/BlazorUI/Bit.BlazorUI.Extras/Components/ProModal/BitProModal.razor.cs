namespace Bit.BlazorUI;

public partial class BitProModal : BitComponentBase
{
    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// When true, the Modal will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog). If this is set, it will override the ARIA role determined by Blocking and Modeless.
    /// </summary>
    [Parameter] public bool? Alert { get; set; }

    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// Whether the Modal can be light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The content of the Modal, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitProModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The CSS selector of the drag element. by default it's the Modal container.
    /// </summary>
    [Parameter] public string? DragElementSelector { get; set; }

    /// <summary>
    /// Whether the Modal can be dragged around.
    /// </summary>
    [Parameter] public bool Draggable { get; set; }

    /// <summary>
    /// The template used to render the footer section of the panel.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The text of the footer section of the panel.
    /// </summary>
    [Parameter] public string? FooterText { get; set; }

    /// <summary>
    /// The template used to render the header section of the panel.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the header section of the panel.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

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
    /// Whether the Modal is displayed.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ModeFull { get; set; }

    /// <summary>
    /// Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal). if true: Blocking is ignored, there will be no overlay.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// Removes the default top border of the modal.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

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
    /// Shows the close button of the Panel.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitProModalClassStyles? Styles { get; set; }



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



    protected override string RootElementClass => "bit-pmd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => FullSize || FullHeight ? "bit-pmd-fhe" : string.Empty);
        ClassBuilder.Register(() => FullSize || FullWidth ? "bit-pmd-fwi" : string.Empty);

        ClassBuilder.Register(() => ModeFull ? "bit-pmd-mfl" : string.Empty);
        ClassBuilder.Register(() => NoBorder ? "" : "bit-pmd-tbr");
    }



    private async Task CloseModal(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        await OnDismiss.InvokeAsync(e);
    }
}
