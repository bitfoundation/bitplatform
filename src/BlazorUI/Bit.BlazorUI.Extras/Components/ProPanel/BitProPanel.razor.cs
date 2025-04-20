namespace Bit.BlazorUI;

/// <summary>
/// ProPanel is an advanced version of normal Panel with additional features that tailored to more usual use-cases.
/// </summary>
public partial class BitProPanel : BitComponentBase
{
    private bool _internIsOpen;



    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the panel.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// The alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

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
    [Parameter] public BitProPanelClassStyles? Classes { get; set; }

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
    /// Determines the openness of the panel.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ModeFull { get; set; }

    /// <summary>
    /// Removes the overlay element of the panel.
    /// </summary>
    [Parameter] public bool Modeless { get; set; }

    /// <summary>
    /// A callback function for when the panel is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the panel is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

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



    protected override string RootElementClass => "bit-ppl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => ModeFull ? "bit-ppl-mfl" : string.Empty);
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



    private async Task ClosePanel(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        _ = OnDismiss.InvokeAsync(e);
    }
}
