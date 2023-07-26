using System.Drawing;

namespace Bit.BlazorUI;

public partial class BitPanel
{
    private bool isOpen;
    private bool IsOpenHasBeenSet;
    private bool PanelSizeHasBeenSet;

    private int _offsetTop;
    private bool _internalIsOpen;
    private double panelSize = 320;
    private string _containerId = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;


    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Panel.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; } = true;

    /// <summary>
    /// The content of the Panel, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for different parts of the BitPanel component.
    /// </summary>
    [Parameter] public BitPanelClassStyles? ClassStyles { get; set; }

    /// <summary>
    /// Used to customize how the footer inside the Panel is rendered.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Used to customize how the header inside the Panel is rendered.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Whether the Panel can be light dismissed by clicking outside the Panel (on the overlay).
    /// </summary>
    [Parameter] public bool IsBlocking { get; set; }

    /// <summary>
    /// Whether the Panel should be modeless (e.g. not dismiss when focusing/clicking outside of the Panel). if true: IsBlocking is ignored, there will be no overlay.
    /// </summary>
    [Parameter] public bool IsModeless { get; set; }

    /// <summary>
    /// Whether the Panel is displayed.
    /// </summary>
    [Parameter]
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (value == isOpen) return;

            isOpen = value;

            _ = IsOpenChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Position of the modal on the screen.
    /// </summary>
    [Parameter] public BitPanelPosition Position { get; set; } = BitPanelPosition.Right;

    /// <summary>
    /// A callback function for when the Panel is dismissed light dismiss, before the animation completes.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// Shows or hides the close button of the Panel.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Provides Height or Width for the Panel.
    /// </summary>
    [Parameter]
    public double PanelSize
    {
        get => panelSize;
        set
        {
            if (value == panelSize) return;

            panelSize = value;

            _ = PanelSizeChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<double> PanelSizeChanged { get; set; }

    /// <summary>
    /// Shows or hides the header of the Panel.
    /// </summary>
    [Parameter] public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Set the element selector for which the Panel disables its scroll if applicable.
    /// </summary>
    [Parameter] public string ScrollerSelector { get; set; } = "body";

    /// <summary>
    /// ARIA id for the subtitle of the Panel, if any.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Panel, if any.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }

    /// <summary>
    /// Title in the header of Panel.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    protected override string RootElementClass => "bit-pnl";

    protected override void RegisterComponentClasses()
    {
        StyleBuilder.Register(() => _offsetTop > 0 ? $"top:{_offsetTop}px" : string.Empty);
    }

    protected override Task OnInitializedAsync()
    {
        _containerId = $"BitPanel-{UniqueId}-Container";

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        _offsetTop = 0;

        if (AutoToggleScroll is false) return;

        _offsetTop = await _js.InvokeAsync<int>("BitPanel.toggleScroll", ScrollerSelector, isOpen);

        StyleBuilder.Reset();
        StateHasChanged();
    }

    private void ClosePanel(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
    }

    private void OnOverlayClicked(MouseEventArgs e)
    {
        if (IsBlocking is not false) return;

        ClosePanel(e);

        _ = OnDismiss.InvokeAsync(e);
    }

    private void OnCloseButtonClicked(MouseEventArgs e)
    {
        ClosePanel(e);
    }

    private string GetPositionClass() => Position switch
    {
        BitPanelPosition.Right => $"{RootElementClass}-right",
        BitPanelPosition.Left => $"{RootElementClass}-left",
        BitPanelPosition.Top => $"{RootElementClass}-top",
        BitPanelPosition.Bottom => $"{RootElementClass}-bottom",

        _ => $"{RootElementClass}-right"
    };

    private string SetPanelSize() => Position switch
    {
        BitPanelPosition.Right => $"width:{PanelSize}px;",
        BitPanelPosition.Left => $"width:{PanelSize}px;",
        BitPanelPosition.Top => $"height:{PanelSize}px;",
        BitPanelPosition.Bottom => $"height:{PanelSize}px;",

        _ => $"width:{PanelSize}px;"
    };
}
