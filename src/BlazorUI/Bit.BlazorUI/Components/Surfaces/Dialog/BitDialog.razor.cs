namespace Bit.BlazorUI;

/// <summary>
/// Dialogs are temporary pop-ups that take focus from the page or app and require people to interact with them.
/// </summary>
public partial class BitDialog : BitComponentBase, IAsyncDisposable
{
    private int _offsetTop;
    private bool _disposed;
    private bool _isLoading;
    private bool _internalIsOpen;
    private string _containerId = default!;
    private TaskCompletionSource<BitDialogResult?>? _tcs = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Dialog.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

    /// <summary>
    /// When true, the Dialog will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter] public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Alias for child content.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The text of the cancel button.
    /// </summary>
    [Parameter] public string? CancelText { get; set; } = "Cancel";

    /// <summary>
    /// The content of the Dialog, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitDialog component.
    /// </summary>
    [Parameter] public BitDialogClassStyles? Classes { get; set; }

    /// <summary>
    /// The CSS selector of the drag element. by default it's the Dialog container.
    /// </summary>
    [Parameter] public string? DragElementSelector { get; set; }

    /// <summary>
    /// Used to customize how the footer inside the Dialog is rendered.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless.
    /// </summary>
    [Parameter] public bool? IsAlert { get; set; }

    /// <summary>
    /// Whether the Dialog can be light dismissed by clicking outside the Dialog (on the overlay).
    /// </summary>
    [Parameter] public bool IsBlocking { get; set; }

    /// <summary>
    /// Whether the Dialog can be dragged around.
    /// </summary>
    [Parameter] public bool IsDraggable { get; set; }

    /// <summary>
    /// Whether the Dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the Dialog). if true: IsBlocking is ignored, there will be no overlay.
    /// </summary>
    [Parameter] public bool IsModeless { get; set; }

    /// <summary>
    /// Whether the Dialog is displayed.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The message to display in the dialog.
    /// </summary>
    [Parameter] public string? Message { get; set; }

    /// <summary>
    /// The text of the ok button.
    /// </summary>
    [Parameter] public string? OkText { get; set; } = "Ok";

    /// <summary>
    /// A callback function for when the Cancel button is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnCancel { get; set; }

    /// <summary>
    /// A callback function for when the Close button is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClose { get; set; }

    /// <summary>
    /// A callback function for when the the dialog is dismissed (closed).
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the Ok button is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnOk { get; set; }

    /// <summary>
    /// Position of the Dialog on the screen.
    /// </summary>
    [Parameter] public BitDialogPosition Position { get; set; }

    /// <summary>
    /// Set the element selector for which the Dialog disables its scroll if applicable.
    /// </summary>
    [Parameter] public string ScrollerSelector { get; set; } = "body";

    /// <summary>
    /// Shows or hides the cancel button of the Dialog.
    /// </summary>
    [Parameter] public bool ShowCancelButton { get; set; } = true;

    /// <summary>
    /// Shows or hides the close button of the Dialog.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Shows or hides the ok button of the Dialog.
    /// </summary>
    [Parameter] public bool ShowOkButton { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the BitDialog component.
    /// </summary>
    [Parameter] public BitDialogClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Dialog, if any.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// The title text to display at the top of the dialog.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// ARIA id for the title of the Dialog, if any.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }



    public BitDialogResult? Result { get; set; }

    public async Task<BitDialogResult?> Show()
    {
        await InvokeAsync(async () =>
        {
            _ = _js.BitUtilsToggleOverflow(ScrollerSelector, true);

            Result = null;

            _tcs?.SetResult(null);

            await AssignIsOpen(true);

            StateHasChanged();
        });

        _tcs = new();

        return await _tcs.Task;
    }



    protected override string RootElementClass => "bit-dlg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => AbsolutePosition ? "bit-dlg-abs" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _offsetTop > 0 ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override Task OnInitializedAsync()
    {
        _containerId = $"BitDialog-{UniqueId}-container";

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        if (IsOpen)
        {
            if (IsDraggable)
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

        if (AutoToggleScroll is false) return;

        _offsetTop = await _js.BitUtilsToggleOverflow(ScrollerSelector, IsOpen);

        if (AbsolutePosition is false) return;

        StyleBuilder.Reset();
        StateHasChanged();
    }



    private async Task DismissDialog(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        await OnDismiss.InvokeAsync(e);
    }

    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (IsBlocking) return;

        await DismissDialog(e);
    }

    private async Task HandleOnCloseClick(MouseEventArgs e)
    {
        await OnClose.InvokeAsync(e);

        await DismissDialog(e);
    }

    private async Task HandleOnCancelClick(MouseEventArgs e)
    {
        Result = BitDialogResult.Cancel;

        _tcs?.SetResult(Result);
        _tcs = null;

        await OnCancel.InvokeAsync(e);

        await DismissDialog(e);
    }

    private async Task HandleOnOkClick(MouseEventArgs e)
    {
        Result = BitDialogResult.Ok;

        _tcs?.SetResult(Result);
        _tcs = null;

        _isLoading = true;

        try
        {
            await OnOk.InvokeAsync(e);

            await DismissDialog(e);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string GetPositionClass() => Position switch
    {
        BitDialogPosition.Center => "bit-dlg-ctr",
        BitDialogPosition.TopLeft => "bit-dlg-tl",
        BitDialogPosition.TopCenter => "bit-dlg-tc",
        BitDialogPosition.TopRight => "bit-dlg-tr",
        BitDialogPosition.CenterLeft => "bit-dlg-cl",
        BitDialogPosition.CenterRight => "bit-dlg-cr",
        BitDialogPosition.BottomLeft => "bit-dlg-bl",
        BitDialogPosition.BottomCenter => "bit-dlg-bc",
        BitDialogPosition.BottomRight => "bit-dlg-br",
        _ => "bit-dlg-ctr",
    };

    private string GetDragElementSelector() => DragElementSelector ?? $"#{_containerId}";



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
            await _js.BitModalRemoveDragDrop(_containerId, GetDragElementSelector());
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _tcs?.SetResult(Result = null);
        _tcs = null;

        _disposed = true;
    }
}
