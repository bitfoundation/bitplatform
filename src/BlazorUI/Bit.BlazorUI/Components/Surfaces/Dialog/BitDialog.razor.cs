namespace Bit.BlazorUI;

public partial class BitDialog : BitComponentBase, IDisposable
{
    private bool IsOpenHasBeenSet;



    private bool isAlertRole;



    private int _offsetTop;
    private bool _disposed;
    private bool _internalIsOpen;
    private string _containerId = default!;
    private TaskCompletionSource<BitDialogResult?>? _tcs = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Dialog.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; } = true;

    /// <summary>
    /// When true, the Dialog will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter] public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Alias for childcontent
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
    [Parameter]
    public bool? IsAlert
    {
        get => isAlertRole;
        set
        {
            isAlertRole = value ?? (IsBlocking && !IsModeless);
        }
    }

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
    [Parameter] public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

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
    [Parameter] public BitDialogPosition Position { get; set; } = BitDialogPosition.Center;

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
            _ = _js.ToggleOverflow(ScrollerSelector, true);

            Result = null;

            _tcs?.SetResult(null);

            IsOpen = true;
            await IsOpenChanged.InvokeAsync(IsOpen);

            StateHasChanged();
        });

        _tcs = new();

        return await _tcs.Task;
    }



    protected override string RootElementClass => "bit-dlg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => AbsolutePosition ? $"{RootElementClass}-abs" : string.Empty);
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

        _offsetTop = await _js.ToggleOverflow(ScrollerSelector, IsOpen);

        if (AbsolutePosition is false) return;

        StyleBuilder.Reset();
        StateHasChanged();
    }



    private void DismissDialog(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
        _ = IsOpenChanged.InvokeAsync(IsOpen);
        _ = OnDismiss.InvokeAsync(e);
    }

    private void HandleOnOverlayClick(MouseEventArgs e)
    {
        if (IsBlocking) return;

        DismissDialog(e);
    }

    private void HandleOnCloseClick(MouseEventArgs e)
    {
        _ = OnClose.InvokeAsync(e);

        DismissDialog(e);
    }

    private void HandleOnCancelClick(MouseEventArgs e)
    {
        Result = BitDialogResult.Cancel;

        _tcs?.SetResult(Result);
        _tcs = null;

        _ = OnCancel.InvokeAsync(e);

        DismissDialog(e);
    }

    private void HandleOnOkClick(MouseEventArgs e)
    {
        Result = BitDialogResult.Ok;

        _tcs?.SetResult(Result);
        _tcs = null;

        _ = OnOk.InvokeAsync(e);

        DismissDialog(e);
    }

    private string GetPositionClass() => Position switch
    {
        BitDialogPosition.Center => $"{RootElementClass}-ctr",

        BitDialogPosition.TopLeft => $"{RootElementClass}-tl",
        BitDialogPosition.TopCenter => $"{RootElementClass}-tc",
        BitDialogPosition.TopRight => $"{RootElementClass}-tr",

        BitDialogPosition.CenterLeft => $"{RootElementClass}-cl",
        BitDialogPosition.CenterRight => $"{RootElementClass}-cr",

        BitDialogPosition.BottomLeft => $"{RootElementClass}-bl",
        BitDialogPosition.BottomCenter => $"{RootElementClass}-bc",
        BitDialogPosition.BottomRight => $"{RootElementClass}-br",

        _ => $"{RootElementClass}-ctr",
    };

    private string GetDragElementSelector() => DragElementSelector ?? $"#{_containerId}";


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _ = _js.BitModalRemoveDragDrop(_containerId, GetDragElementSelector());

        _tcs?.SetResult(Result = null);
        _tcs = null;

        _disposed = true;
    }
}
