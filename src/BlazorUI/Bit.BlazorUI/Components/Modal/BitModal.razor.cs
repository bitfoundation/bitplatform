namespace Bit.BlazorUI;

public partial class BitModal
{
    protected override bool UseVisual => false;

    private bool IsOpenHasBeenSet;
    private bool isOpen;


    private bool _isAlertRole;
    private int _offsetTop;
    private bool _internalIsOpen;


    [Inject] private IJSRuntime _js { get; set; } = default!;


    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; } = true;

    /// <summary>
    /// When true, the Modal will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter] public bool AbsolutePosition { get; set; }

    /// <summary>
    /// The content of the Modal, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Determines the ARIA role of the dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless.
    /// </summary>
    [Parameter]
    public bool? IsAlert
    {
        get => _isAlertRole;
        set
        {
            _isAlertRole = value ?? (IsBlocking && !IsModeless);
        }
    }

    /// <summary>
    /// Whether the dialog can be light dismissed by clicking outside the dialog (on the overlay).
    /// </summary>
    [Parameter] public bool IsBlocking { get; set; }

    /// <summary>
    /// Whether the dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the dialog). if true: IsBlocking is ignored, there will be no overlay.
    /// </summary>
    [Parameter] public bool IsModeless { get; set; }

    /// <summary>
    /// Whether the dialog is displayed.
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
    /// A callback function for when the Modal is dismissed light dismiss, before the animation completes.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// Position of the modal on the screen.
    /// </summary>
    [Parameter] public BitModalPosition Position { get; set; } = BitModalPosition.Center;

    /// <summary>
    /// Set the element selector for which the Modal disables its scroll if applicable.
    /// </summary>
    [Parameter] public string ScrollerSelector { get; set; } = "body";

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    [Parameter] public string SubtitleAriaId { get; set; } = string.Empty;

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
    /// </summary>
    [Parameter] public string TitleAriaId { get; set; } = string.Empty;


    protected override string RootElementClass => "bit-mdl";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => AbsolutePosition ? "absolute" : "");

        StyleBuilder.Register(() => _offsetTop > 0 ? $"top:{_offsetTop}px" : "");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        _offsetTop = 0;

        if (AutoToggleScroll is false) return;

        _offsetTop = await _js.ToggleModalScroll(ScrollerSelector, IsOpen);

        if (AbsolutePosition is false) return;

        StyleBuilder.Reset();
        StateHasChanged();
    }

    private void CloseModal(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsBlocking is not false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;

        _ = OnDismiss.InvokeAsync(e);
    }

    private string GetPositionClass() => Position switch
    {
        BitModalPosition.Center => $"center",

        BitModalPosition.TopLeft => $"top-left",
        BitModalPosition.TopCenter => $"top-center",
        BitModalPosition.TopRight => $"top-right",

        BitModalPosition.CenterLeft => $"center-left",
        BitModalPosition.CenterRight => $"center-right",

        BitModalPosition.BottomLeft => $"bottom-left",
        BitModalPosition.BottomCenter => $"bottom-center",
        BitModalPosition.BottomRight => $"bottom-right",

        _ => $"center",
    };
}
