namespace Bit.BlazorUI;

/// <summary>
/// ProModal is an advanced version of normal Modal with additional features that tailored to more usual use-cases.
/// </summary>
public partial class BitProModal : BitComponentBase
{
    private bool _internalIsOpen;
    private float _offsetTop;



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
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
    /// The title (and aria-label) of the close button for accessibility and localization.
    /// </summary>
    [Parameter] public string CloseButtonTitle { get; set; } = "Close";

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
    [Parameter] public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width and height 100% of its parent container.
    /// </summary>
    [Parameter] public bool FullSize { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
    /// </summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>
    /// The template used to render the header section of the Modal.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the header section of the Modal.
    /// </summary>
    [Parameter] public string? HeaderText { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
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
    [Parameter, ResetClassBuilder]
    public bool Modeless { get; set; }

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
    [Parameter, ResetClassBuilder]
    public BitPosition? Position { get; set; }

    /// <summary>
    /// Set the element reference for which the Modal disables its scroll if applicable.
    /// </summary>
    [Parameter] public ElementReference? ScrollerElement { get; set; }

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

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
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



    protected override string RootElementClass => "bit-pmd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ModeFull ? "bit-pmd-mfl" : string.Empty);
        ClassBuilder.Register(() => NoBorder ? string.Empty : "bit-pmd-tbr");
        ClassBuilder.Register(() => AbsolutePosition ? "bit-pmd-abs" : string.Empty);
        ClassBuilder.Register(() => Position switch
        {
            BitPosition.TopLeft => "bit-pmd-tlf",
            BitPosition.TopCenter => "bit-pmd-tcr",
            BitPosition.TopRight => "bit-pmd-trg",
            BitPosition.TopStart => "bit-pmd-tst",
            BitPosition.TopEnd => "bit-pmd-ten",
            BitPosition.CenterLeft => "bit-pmd-clf",
            BitPosition.Center => "bit-pmd-ctr",
            BitPosition.CenterRight => "bit-pmd-crg",
            BitPosition.CenterStart => "bit-pmd-cst",
            BitPosition.CenterEnd => "bit-pmd-cen",
            BitPosition.BottomLeft => "bit-pmd-blf",
            BitPosition.BottomCenter => "bit-pmd-bcr",
            BitPosition.BottomRight => "bit-pmd-brg",
            BitPosition.BottomStart => "bit-pmd-bst",
            BitPosition.BottomEnd => "bit-pmd-ben",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => _offsetTop > 0 ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen)
        {
            if (_internalIsOpen is false)
            {
                _internalIsOpen = true;

                if (Draggable)
                {
                    _ = _js.BitDragDropSetup(_containerSelector, _containerSelector, _dragElementSelector);
                }

                // Reset _offsetTop before ToggleScroll. When AutoToggleScroll is false,
                // ToggleScroll returns early and won't recalculate _offsetTop, so this
                // guards against a stale top-offset from a previous open.
                _offsetTop = 0;

                await ToggleScroll(true);

                // Only when AbsolutePosition is set do we reset the StyleBuilder and
                // re-render, so the top-offset style (which ToggleScroll may have updated)
                // gets applied on the next render.
                if (AbsolutePosition)
                {
                    StyleBuilder.Reset();
                    StateHasChanged();
                }

                await OnOpen.InvokeAsync();
            }
        }
        else
        {
            if (_internalIsOpen)
            {
                _internalIsOpen = false;

                _ = _js.BitDragDropRemove(_containerSelector, _dragElementSelector);

                await ToggleScroll(false);
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private string _modalId => Id ?? UniqueId;
    private string _containerSelector => $"#{_modalId} .bit-mdl-ctn";
    private string _dragElementSelector => DragElementSelector ?? _containerSelector;

    private async Task HandleInnerIsOpenChanged(bool open)
    {
        await AssignIsOpen(open);
    }

    private async Task CloseModal(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await AssignIsOpen(false);
    }

    private async Task ToggleScroll(bool isOpen)
    {
        if (AutoToggleScroll is false) return;

        if (ScrollerElement.HasValue)
        {
            _offsetTop = await _js.BitUtilsToggleOverflow(ScrollerElement.Value, isOpen);
        }
        else
        {
            _offsetTop = await _js.BitUtilsToggleOverflow(ScrollerSelector ?? "body", isOpen);
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            if (_internalIsOpen)
            {
                await _js.BitDragDropRemove(_containerSelector, _dragElementSelector);
                await ToggleScroll(false);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
