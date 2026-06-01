namespace Bit.BlazorUI;

/// <summary>
/// ProModal is an advanced version of normal Modal with additional features that tailored to more usual use-cases.
/// </summary>
public partial class BitProModal : BitComponentBase
{
    private bool _internIsOpen;



    /// <summary>
    /// When true, the Modal will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AbsolutePosition { get; set; }

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
    /// Custom CSS classes for different parts of the BitProModal component.
    /// </summary>
    [Parameter] public BitProModalClassStyles? Classes { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the close button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CloseIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CloseIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the close button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Cancel</c>).
    /// <br />
    /// For external icon libraries, use <see cref="CloseIcon"/> instead.
    /// </remarks>
    [Parameter] public string? CloseIconName { get; set; }

    /// <summary>
    /// The CSS selector of the drag element. by default it's the Modal container.
    /// </summary>
    [Parameter] public string? DragElementSelector { get; set; }

    /// <summary>
    /// Whether the Modal can be dragged around.
    /// </summary>
    [Parameter] public bool Draggable { get; set; }

    /// <summary>
    /// The template used to render the footer section of the Modal.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The text of the footer section of the Modal.
    /// </summary>
    [Parameter] public string? FooterText { get; set; }

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
    /// The template used to render the header section of the Modal.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the header section of the Modal.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

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
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ModeFull { get; set; }

    /// <summary>
    /// Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal). if true: Blocking is ignored, there will be no overlay.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// Removes the default top border of the Modal.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the Modal is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// Position of the Modal on the screen.
    /// </summary>
    [Parameter] public BitPosition? Position { get; set; }

    /// <summary>
    /// Set the element selector for which the Modal disables its scroll if applicable.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the Modal.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitProModal component.
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
        ClassBuilder.Register(() => ModeFull ? "bit-pmd-mfl" : string.Empty);
        ClassBuilder.Register(() => NoBorder ? string.Empty : "bit-pmd-tbr");
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen)
        {
            if (_internIsOpen is false)
            {
                _internIsOpen = true;
                OnOpen.InvokeAsync();
            }
        }
        else
        {
            _internIsOpen = false;
        }

        return base.OnAfterRenderAsync(firstRender);
    }



    private async Task CloseModal(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        _ = OnDismiss.InvokeAsync(e);
    }
}
