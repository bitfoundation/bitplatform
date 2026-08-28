namespace Bit.BlazorUI;

/// <summary>
/// ProPanel is an advanced version of normal Panel with additional features that tailored to more usual use-cases.
/// </summary>
public partial class BitProPanel : BitComponentBase
{
    private string _headerId = default!;



    /// <summary>
    /// Lays the panel out against the nearest positioned ancestor instead of against the screen, so that the
    /// panel - and the overlay that comes with it - stay inside a container of the page rather than covering
    /// all of it.
    /// </summary>
    [Parameter] public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Holds the page still while the panel is open, by taking the scrollbar off the element named by
    /// <see cref="ScrollerSelector"/> - the body of the document by default - and giving it back on close.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// The alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// Keeps a click on the overlay from dismissing the panel, for the panels whose content has to be
    /// completed or cancelled through the panel itself.
    /// </summary>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the panel.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the panel.
    /// </summary>
    [Parameter] public BitProPanelClassStyles? Classes { get; set; }

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
    /// The accessible name of the close button, which is what a screen reader reads out for it and what the
    /// pointer shows as its tooltip. It defaults to "Close".
    /// </summary>
    [Parameter] public string? CloseButtonAriaLabel { get; set; }

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
    /// Stretches the panel to the full size of the screen along the axis it is sized on, which takes over
    /// from <see cref="Size"/> and from the cap that otherwise leaves a strip of the page showing beside it.
    /// </summary>
    [Parameter] public bool FullSize { get; set; }

    /// <summary>
    /// Reports the panel to assistive technologies as an alert dialog rather than a plain one, for the panels
    /// that carry an urgent message the user is expected to deal with before carrying on.
    /// </summary>
    [Parameter] public bool IsAlert { get; set; }

    /// <summary>
    /// Determines the openness of the panel.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Keeps the content of the panel out of the page until the panel is opened for the first time. Once
    /// rendered it stays, so whatever state the content holds survives the panel closing.
    /// </summary>
    [Parameter] public bool LazyRender { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ModeFull { get; set; }

    /// <summary>
    /// Leaves the page its own clicks while the panel is open, by not rendering the overlay that otherwise
    /// covers it.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// Leaves the focus where it is when the panel opens, instead of moving it into the panel.
    /// </summary>
    [Parameter] public bool NoAutoFocus { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the panel, for the panels that are only meant to be closed
    /// through their own content.
    /// </summary>
    [Parameter] public bool NoDismissOnEscape { get; set; }

    /// <summary>
    /// Lets the keyboard leave the panel while it is open, instead of cycling Tab and Shift+Tab inside it.
    /// </summary>
    [Parameter] public bool NoFocusTrap { get; set; }

    /// <summary>
    /// Turns off the swipe gesture that otherwise dismisses the panel when it is dragged towards the edge it
    /// slid in from.
    /// </summary>
    [Parameter] public bool NoSwipe { get; set; }

    /// <summary>
    /// A callback function for when the panel is dismissed.
    /// </summary>
    /// <remarks>
    /// It is called for every closing of the panel, however it happened: the close button, a click on the
    /// overlay, the Escape key, a swipe, the Close and Toggle methods, and the IsOpen parameter being set to
    /// false from the outside.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the panel is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// A callback function for when a click lands on the overlay of the panel.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

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
    /// A callback function for when the panel opens or closes, called with the new open state.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

    /// <summary>
    /// The position of the panel to show on the screen.
    /// </summary>
    [Parameter] public BitPanelPosition? Position { get; set; }

    /// <summary>
    /// The value of the height or width (based on the position) of the panel.
    /// </summary>
    [Parameter] public double? Size { get; set; }

    /// <summary>
    /// Specifies the element selector for which the panel disables its scroll if applicable.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the panel.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the panel component.
    /// </summary>
    [Parameter] public BitProPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// The ARIA id of the element that describes the panel, which is what a screen reader reads out after the
    /// name of the panel when it opens.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// The swiping point (difference percentage) based on the width of the panel container to trigger the close action (default is 0.25m).
    /// </summary>
    [Parameter] public decimal? SwipeTrigger { get; set; }

    /// <summary>
    /// The ARIA id of the element that names the panel. It defaults to the header of the panel, which is the
    /// name the panel is already showing, and <see cref="BitComponentBase.AriaLabel"/> takes precedence over
    /// both.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }



    /// <summary>
    /// Opens the panel, unless it is disabled.
    /// </summary>
    public async Task Open()
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(true) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Closes the panel.
    /// </summary>
    public async Task Close()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Opens the panel when it is closed, and closes it when it is open.
    /// </summary>
    public Task Toggle() => IsOpen ? Close() : Open();



    protected override string RootElementClass => "bit-ppl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ModeFull ? "bit-ppl-mfl" : string.Empty);
    }

    protected override void OnInitialized()
    {
        _headerId = $"BitProPanel-{UniqueId}-header";

        base.OnInitialized();
    }



    // The panel underneath raises OnDismiss for every way it can be closed, and the close button is one of
    // them, so the callback is left to it rather than raised here as well - it would otherwise reach the
    // consumer twice for a single dismissal.
    private async Task ClosePanel(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    // A dialog needs an accessible name, and a panel that renders a header of its own is already showing the
    // name it should be given. A name set by hand wins over it, and so does an AriaLabel, which the panel
    // underneath renders instead of pointing at an element.
    private string? GetTitleAriaId()
    {
        if (TitleAriaId.HasValue()) return TitleAriaId;

        if (AriaLabel.HasValue()) return null;

        return (Header is not null || HeaderText is not null) ? _headerId : null;
    }
}
