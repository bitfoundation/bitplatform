namespace Bit.BlazorUI;

/// <summary>
/// Dialogs are temporary pop-ups that take focus from the page or app and require people to interact with them.
/// </summary>
/// <remarks>
/// The Dialog is the confirmation-shaped member of the overlay family: a title, a message and a pair of
/// actions, with an Ok/Cancel result a caller can await. Reach for BitModal (or BitProModal in the Extras
/// package) where the overlay holds a form or a long document rather than a decision.
/// </remarks>
public partial class BitDialog : BitComponentBase
{
    /// <summary>
    /// The title (and aria-label) used for the close button when none is provided.
    /// </summary>
    internal const string DefaultCloseButtonTitle = "Close";

    /// <summary>
    /// How long the container carries the class that plays back a refused dismissal, in milliseconds.
    /// It outlasts the animation itself, whose duration comes from the motion tokens and collapses to
    /// nothing under prefers-reduced-motion.
    /// </summary>
    private const int DismissPreventedDuration = 300;

    private bool _isLoading;
    private float _offsetTop;
    private bool _internalIsOpen;
    private bool _dismissPrevented;
    private string _containerId = default!;
    private string _titleId = default!;
    private string _subtitleId = default!;
    private string _messageId = default!;

    // The surface itself, so the focus can be put back on it after a click that took it out of the Dialog.
    private ElementReference _containerRef;

    // The Dialog's own buttons, so AutoFocusButton can put the focus on one of them by hand.
    private ElementReference _okButtonRef;
    private ElementReference _cancelButtonRef;
    private ElementReference _closeButtonRef;

    // Whether the focus trap is currently registered on the JS side, so it is torn down exactly once
    // and only when it was actually set up.
    private bool _focusTrapped;
    // Whether an element was remembered on the JS side when this Dialog opened, so the close sequence
    // only tries to hand the focus back when there is something to hand it back to.
    private bool _focusSaved;
    // Whether scroll was actually locked during the open sequence, so the close sequence unlocks if and
    // only if it locked, regardless of later changes to AutoToggleScroll.
    private bool _scrollLocked;
    // Snapshots of the scroller target captured during open, so the close sequence unlocks the exact same
    // scroller even if ScrollerElement/ScrollerSelector changed since the Dialog was opened.
    private ElementReference? _scrollerElementOnOpen;
    private string? _scrollerSelectorOnOpen;
    // Snapshot of the drag element selector used to register the drag handlers, so teardown unregisters
    // the exact same selector even if DragElementSelector changed since the Dialog was opened.
    private string? _dragElementSelectorOnSetup;

    // The awaiter handed out by Show. It is completed exactly once, by whatever ends the showing - a
    // button, a dismissal, or the parent closing the Dialog itself - and nulled at the same time so a
    // second completion is a no-op rather than an exception.
    private TaskCompletionSource<BitDialogResult?>? _tcs;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// When true, the Dialog will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AbsolutePosition { get; set; }

    /// <summary>
    /// Moves the focus into the Dialog when it opens, onto the first focusable element it holds,
    /// falling back to the Dialog itself when it holds none.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    /// <remarks>
    /// A modal dialog that leaves the focus behind it is a dialog a keyboard cannot reach: the tab order
    /// still runs through the page underneath, which the overlay has made unclickable. Turn this off only
    /// where the focus is being placed by hand from <see cref="OnOpen"/>.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; } = true;

    /// <summary>
    /// Which of the Dialog's own buttons <see cref="AutoFocus"/> lands on, instead of the first focusable
    /// element the Dialog holds.
    /// </summary>
    /// <remarks>
    /// A Dialog that confirms something irreversible should open with the focus on the answer that does the
    /// least damage, so a stray Enter or Space cannot carry the destructive one out. A button that is not
    /// being shown is ignored, and the focus falls back to <see cref="AutoFocusSelector"/> or to the first
    /// focusable element.
    /// </remarks>
    [Parameter] public BitDialogButton? AutoFocusButton { get; set; }

    /// <summary>
    /// The CSS selector of the element inside the Dialog that <see cref="AutoFocus"/> lands on, instead of
    /// the first focusable element it holds.
    /// </summary>
    /// <remarks>
    /// The first focusable element is rarely the wrong place to start, but it is when the Dialog opens with
    /// a link or a segmented control above the field the user came to fill in. The selector is matched
    /// inside the Dialog only, and a selector that matches nothing visible falls back to the first focusable
    /// element - so a Dialog whose content varies never opens with the focus nowhere.
    /// <see cref="AutoFocusButton"/> takes precedence over this when both are set.
    /// </remarks>
    [Parameter] public string? AutoFocusSelector { get; set; }

    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Dialog.
    /// </summary>
    /// <remarks>
    /// When enabled, the element named by <see cref="ScrollerElement"/> or <see cref="ScrollerSelector"/>
    /// stops scrolling for as long as the Dialog is open, so the page behind the overlay cannot be scrolled
    /// out from under it. The scroller that was locked is remembered, so the Dialog always unlocks the very
    /// element it locked - and it unlocks it when the Dialog is disposed while still open.
    /// </remarks>
    [Parameter] public bool AutoToggleScroll { get; set; }

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
    /// The title (and aria-label) of the close button, for accessibility and localization.
    /// Defaults to "Close" when not set.
    /// </summary>
    [Parameter] public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display for the close button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CloseIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display for the close button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? CloseIconName { get; set; }

    /// <summary>
    /// Dismisses the Dialog when the Escape key is pressed while the focus is inside it.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    /// <remarks>
    /// A blocking Dialog (<see cref="IsBlocking"/>) ignores the Escape key whatever this is set to, since
    /// the point of a blocking Dialog is that it can only be answered with one of its buttons.
    /// </remarks>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// The CSS selector of the element the Dialog is dragged by. By default it is the header when the
    /// Dialog has one, and the whole container when it has none.
    /// </summary>
    /// <remarks>
    /// A title bar is what a window is dragged by everywhere else, and for good reason: a surface that is
    /// draggable all over turns selecting a word, reaching for a field or flicking through a scrolling body
    /// on a touch screen into a move. Point this at a header of your own when the Dialog is built from
    /// custom content, or at the container to make the whole surface the handle.
    /// </remarks>
    [Parameter] public string? DragElementSelector { get; set; }

    /// <summary>
    /// Used to customize how the footer inside the Dialog is rendered.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Makes the Dialog height 100% of the area it is positioned in.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the Dialog width and height 100% of the area it is positioned in.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullSize { get; set; }

    /// <summary>
    /// Makes the Dialog width 100% of the area it is positioned in.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Used to customize the header of the Dialog, replacing the Title and Subtitle while keeping the
    /// close button beside it.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless.
    /// </summary>
    [Parameter] public bool? IsAlert { get; set; }

    /// <summary>
    /// Whether the Dialog can be light dismissed by clicking outside the Dialog (on the overlay).
    /// </summary>
    /// <remarks>
    /// A blocking Dialog also ignores the Escape key, so the only ways out of it are its own buttons and
    /// the parent closing it.
    /// </remarks>
    [Parameter] public bool IsBlocking { get; set; }

    /// <summary>
    /// Whether the Dialog can be dragged around.
    /// </summary>
    [Parameter] public bool IsDraggable { get; set; }

    /// <summary>
    /// Whether the Dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the Dialog). if true: IsBlocking is ignored, there will be no overlay.
    /// </summary>
    /// <remarks>
    /// A modeless Dialog leaves the page behind it clickable and does not trap the focus, so it is
    /// announced with <c>aria-modal="false"</c>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool IsModeless { get; set; }

    /// <summary>
    /// Whether the Dialog is displayed.
    /// </summary>
    [Parameter, TwoWayBound, ResetClassBuilder]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Keeps the Dialog in the DOM while it is closed, hidden, instead of removing it.
    /// </summary>
    /// <remarks>
    /// A Dialog is unmounted when it closes, which is what a Dialog should do: nothing is rendered, nothing
    /// is measured, and the next showing starts clean. Turn this on for the Dialog whose content is
    /// expensive to build or has state worth keeping - a part-filled form the user is meant to come back
    /// to - at the cost of that content living, and rendering, for as long as the page does.
    /// </remarks>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>
    /// The message to display in the dialog.
    /// </summary>
    /// <remarks>
    /// The message is what describes the Dialog to a screen reader (<c>aria-describedby</c>) unless a
    /// <see cref="Subtitle"/> or a <see cref="SubtitleAriaId"/> takes that job instead.
    /// </remarks>
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
    /// <remarks>
    /// <see cref="DismissReason"/> reports which gesture ended the showing by the time this runs.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when a dismissal was refused: the Escape key on a Dialog that does not take
    /// it, or a click on the overlay of a blocking one.
    /// </summary>
    /// <remarks>
    /// The Dialog answers a refused dismissal on its own by shaking, so the gesture is not simply swallowed.
    /// This is for saying why - a hint under the buttons, or a message pointing at what is still unanswered.
    /// </remarks>
    [Parameter] public EventCallback<BitDialogDismissReason> OnDismissPrevented { get; set; }

    /// <summary>
    /// A callback function for when the overlay of the Dialog is clicked, whether or not the click goes on
    /// to dismiss the Dialog.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// A callback function for when the Ok button is clicked.
    /// </summary>
    /// <remarks>
    /// The Dialog waits for this callback before it closes, and shows a spinner in place of the Ok text
    /// while it waits, so an Ok that saves something can hold the Dialog open until the save is done. The
    /// showing has already been answered by then, so every other way out - the Cancel and close buttons, the
    /// Escape key, a click on the overlay - is held shut for as long as the callback runs. A callback that
    /// throws leaves the Dialog open and unanswered, ready for another try.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnOk { get; set; }

    /// <summary>
    /// A callback function for when the Dialog is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// Position of the Dialog on the screen.
    /// </summary>
    [Parameter] public BitDialogPosition Position { get; set; }

    /// <summary>
    /// Hands the focus back to whatever held it when the Dialog opened, once the Dialog closes.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool RestoreFocus { get; set; } = true;

    /// <summary>
    /// Set the element reference for which the Dialog disables its scroll if applicable.
    /// Takes precedence over <see cref="ScrollerSelector"/> when both are set.
    /// </summary>
    [Parameter] public ElementReference? ScrollerElement { get; set; }

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
    /// The secondary line of the header, under the title.
    /// </summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Dialog, if any.
    /// </summary>
    /// <remarks>
    /// Set this to point <c>aria-describedby</c> at an element of your own. When it is not set the Dialog
    /// describes itself with its own <see cref="Subtitle"/>, or with its <see cref="Message"/> when there
    /// is no subtitle.
    /// </remarks>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// The title text to display at the top of the dialog.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// ARIA id for the title of the Dialog, if any.
    /// </summary>
    /// <remarks>
    /// Set this to point <c>aria-labelledby</c> at an element of your own - the heading inside a
    /// <see cref="HeaderTemplate"/>, for instance. When it is not set the Dialog names itself with its own
    /// <see cref="Title"/>, and falls back to <see cref="BitComponentBase.AriaLabel"/> when there is none.
    /// </remarks>
    [Parameter] public string? TitleAriaId { get; set; }

    /// <summary>
    /// Keeps Tab and Shift+Tab cycling inside the Dialog while it is open.
    /// <br />
    /// The default is <strong>true</strong> for a normal Dialog and <strong>false</strong> for a modeless one.
    /// </summary>
    /// <remarks>
    /// Trapping the focus is what makes a modal dialog modal for the keyboard: without it the tab order
    /// runs on into the page behind the overlay, which is a page every click is being swallowed on.
    /// A modeless Dialog leaves the page usable on purpose, so it does not trap by default.
    /// </remarks>
    [Parameter] public bool? TrapFocus { get; set; }



    /// <summary>
    /// The result of the last showing of the Dialog: Ok or Cancel when one of those buttons ended it,
    /// and null when it was dismissed without an answer or has not been shown yet.
    /// </summary>
    public BitDialogResult? Result { get; private set; }

    /// <summary>
    /// What ended the last showing of the Dialog: the gesture that closed it, and null while it is open or
    /// before it has been shown at all.
    /// </summary>
    /// <remarks>
    /// This is set before <see cref="OnDismiss"/> and <see cref="BitDialog.IsOpenChanged"/> run, so a handler
    /// can tell an Escape from a click on the overlay - a distinction <see cref="Result"/> cannot make, since
    /// neither of them leaves an answer.
    /// </remarks>
    public BitDialogDismissReason? DismissReason { get; private set; }

    /// <summary>
    /// Opens the Dialog and waits for it to close, reporting how it closed: Ok or Cancel when one of
    /// those buttons ended it, and null when it was dismissed without an answer.
    /// </summary>
    /// <remarks>
    /// Whatever ends the showing completes the task - a button, a dismissal, or the page closing the Dialog
    /// itself - so it never hangs, and <see cref="DismissReason"/> tells the three answerless endings apart.
    /// </remarks>
    public async Task<BitDialogResult?> Show()
    {
        var tcs = new TaskCompletionSource<BitDialogResult?>(TaskCreationOptions.RunContinuationsAsynchronously);

        await InvokeAsync(async () =>
        {
            // A Show while a previous one is still pending replaces it, so the previous awaiter is
            // released with the null of a showing that was cut short rather than left hanging forever.
            CompletePending(null);

            _tcs = tcs;
            Result = null;
            DismissReason = null;

            if (await AssignIsOpen(true) is false)
            {
                // The parent owns IsOpen and offered no way to change it, so nothing was shown and there
                // is nothing to wait for.
                CompletePending(null);
                return;
            }

            StateHasChanged();
        });

        return await tcs.Task;
    }

    /// <summary>
    /// Opens the Dialog.
    /// </summary>
    public async Task Open()
    {
        if (await AssignIsOpen(true) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Closes the Dialog.
    /// </summary>
    public async Task Close()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Opens the Dialog when it is closed and closes it when it is open.
    /// </summary>
    public Task Toggle() => IsOpen ? Close() : Open();



    protected override string RootElementClass => "bit-dlg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // Only a kept-mounted Dialog is ever rendered while it is closed, and it is hidden rather than
        // shown empty: display:none takes it out of the layout, out of the tab order and out of the
        // accessibility tree, and lets the entrance animation play again on the next showing.
        ClassBuilder.Register(() => IsOpen ? string.Empty : "bit-dlg-hdn");

        ClassBuilder.Register(() => AbsolutePosition ? "bit-dlg-abs" : string.Empty);
        ClassBuilder.Register(() => IsModeless ? "bit-dlg-mls" : string.Empty);
        ClassBuilder.Register(() => (FullWidth || FullSize) ? "bit-dlg-fwi" : string.Empty);
        ClassBuilder.Register(() => (FullHeight || FullSize) ? "bit-dlg-fhe" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // The offset only makes sense for an absolutely positioned Dialog, where it re-aligns the Dialog
        // with the visible part of a scroller that was locked mid-scroll. A fixed Dialog is inset to the
        // whole screen already, so the very same declaration would simply push it off the bottom of it.
        StyleBuilder.Register(() => (AbsolutePosition && _offsetTop > 0) ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override Task OnInitializedAsync()
    {
        _containerId = $"BitDialog-{UniqueId}-container";
        _titleId = $"BitDialog-{UniqueId}-title";
        _subtitleId = $"BitDialog-{UniqueId}-subtitle";
        _messageId = $"BitDialog-{UniqueId}-message";

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        if (IsOpen)
        {
            await HandleOpened();
        }
        else
        {
            await HandleClosed();
        }
    }



    private async Task HandleOpened()
    {
        // Remembered before anything moves the focus, so the element the Dialog hands it back to is the
        // one that was carrying it when the Dialog appeared - the button that opened it, as a rule.
        if (RestoreFocus)
        {
            _focusSaved = true;
            await SaveFocus();
        }

        if (IsDraggable)
        {
            _dragElementSelectorOnSetup = GetDragElementSelector();
            await InvokeJs(_js.BitDragDropSetup(_containerId, ContainerSelector, _dragElementSelectorOnSetup));
        }

        // Reset before ToggleScroll: when AutoToggleScroll is false it returns early without
        // recalculating, which would otherwise leave a stale top-offset from a previous open.
        _offsetTop = 0;

        await ToggleScroll(true);

        if (AbsolutePosition)
        {
            // Only the absolutely positioned Dialog reads the top-offset ToggleScroll may have just
            // measured, so only it has to be re-rendered for the style to land.
            StyleBuilder.Reset();
            StateHasChanged();
        }

        await SetupFocusTrap();

        if (AutoFocus)
        {
            await FocusAutoTarget();
        }

        await OnOpen.InvokeAsync();
    }

    private async Task HandleClosed()
    {
        // A refused dismissal that was still being played back has nothing left to refuse.
        _dismissPrevented = false;

        await DisposeFocusTrap();

        await RemoveDragHandlers();

        await ToggleScroll(false);

        await RestoreSavedFocus();
    }

    // The drag handlers are torn down only where they were actually registered, so a Dialog that was never
    // draggable does not pay a round trip to the JS side every time it closes.
    private async Task RemoveDragHandlers()
    {
        if (_dragElementSelectorOnSetup is null) return;

        var selector = _dragElementSelectorOnSetup;
        _dragElementSelectorOnSetup = null;

        await InvokeJs(_js.BitDragDropRemove(_containerId, selector));
    }

    private async Task ToggleScroll(bool isOpen)
    {
        if (isOpen)
        {
            // The lock decision and its target are snapshot at open time; close reuses them instead of
            // re-reading parameters that may have changed while the Dialog was up.
            _scrollLocked = AutoToggleScroll;
            if (_scrollLocked is false) return;

            _scrollerElementOnOpen = ScrollerElement;
            _scrollerSelectorOnOpen = ScrollerSelector;
        }
        else
        {
            if (_scrollLocked is false) return;

            _scrollLocked = false;
        }

        try
        {
            _offsetTop = _scrollerElementOnOpen.HasValue
                ? await _js.BitUtilsToggleOverflow(_scrollerElementOnOpen.Value, isOpen)
                : await _js.BitUtilsToggleOverflow(_scrollerSelectorOnOpen ?? "body", isOpen);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // Puts the focus where the Dialog was asked to open it: on one of its own buttons where AutoFocusButton
    // names one that is actually being shown, on what AutoFocusSelector points at next, and otherwise on the
    // first focusable element it holds - which the JS side falls back from onto the Dialog itself when it
    // holds nothing focusable.
    private async Task FocusAutoTarget()
    {
        var target = AutoFocusButton switch
        {
            BitDialogButton.Ok when ShowOkButton => _okButtonRef,
            BitDialogButton.Cancel when ShowCancelButton => _cancelButtonRef,
            BitDialogButton.Close when ShowCloseButton => _closeButtonRef,
            _ => (ElementReference?)null
        };

        try
        {
            if (target.HasValue)
            {
                await target.Value.FocusAsync();
            }
            else
            {
                await _js.BitUtilsFocusFirstElement(_containerId, AutoFocusSelector);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task SetupFocusTrap()
    {
        if (ShouldTrapFocus is false || _focusTrapped) return;

        _focusTrapped = true;

        await InvokeJs(_js.BitUtilsSetupFocusTrap(_containerId));
    }

    private async Task DisposeFocusTrap()
    {
        if (_focusTrapped is false) return;

        _focusTrapped = false;

        await InvokeJs(_js.BitUtilsDisposeFocusTrap(_containerId));
    }

    private async Task SaveFocus()
    {
        await InvokeJs(_js.BitUtilsSaveFocus(_containerId));
    }

    private async Task RestoreSavedFocus()
    {
        if (_focusSaved is false) return;

        _focusSaved = false;

        await InvokeJs(_js.BitUtilsRestoreFocus(_containerId));
    }

    // A modal Dialog traps by default and a modeless one does not, since the page behind a modeless
    // Dialog is meant to stay usable - by the keyboard as much as by the pointer.
    private bool ShouldTrapFocus => TrapFocus ?? (IsModeless is false);

    private async Task InvokeJs(ValueTask task)
    {
        try
        {
            await task;
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }



    private void OnSetIsOpen()
    {
        if (IsOpen)
        {
            // A fresh showing starts without the answer of the previous one.
            Result = null;
            DismissReason = null;
            return;
        }

        // Whatever closed the Dialog - one of its buttons, a dismissal, or the parent setting IsOpen
        // itself - is what ends the showing, so the awaiter Show handed out is released here rather
        // than in any one of those paths. Anything that came through the Dialog's own gestures has
        // already named itself; what is left is the page closing the Dialog behind its back.
        _isLoading = false;
        DismissReason ??= BitDialogDismissReason.Programmatic;
        CompletePending(Result);
    }

    private void CompletePending(BitDialogResult? result)
    {
        var tcs = _tcs;
        if (tcs is null) return;

        _tcs = null;
        tcs.TrySetResult(result);
    }

    private async Task DismissDialog(MouseEventArgs e, BitDialogDismissReason reason)
    {
        if (IsEnabled is false) return;

        // AssignIsOpen reports success for a value it did not have to change, so an already-closed Dialog
        // is filtered out here to keep OnDismiss from firing for a dismissal that never happened.
        if (IsOpen is false) return;

        // Named before the assignment, since that is what runs OnSetIsOpen and raises IsOpenChanged - both
        // of which a handler reads the reason from.
        DismissReason = reason;

        if (await AssignIsOpen(false) is false)
        {
            DismissReason = null;
            return;
        }

        await OnDismiss.InvokeAsync(e);
    }

    // A dismissal the Dialog will not act on is answered rather than swallowed: the surface shakes once, so
    // the gesture is visibly refused instead of appearing to have missed. Without it, a blocking Dialog
    // whose way out is not obvious reads as a page that has stopped responding.
    private async Task PreventDismiss(BitDialogDismissReason reason)
    {
        await OnDismissPrevented.InvokeAsync(reason);

        // Already playing one back: restarting it mid-flight would only cut the animation short.
        if (_dismissPrevented) return;

        _dismissPrevented = true;
        StateHasChanged();

        await Task.Delay(DismissPreventedDuration);

        if (IsDisposed) return;

        _dismissPrevented = false;
        StateHasChanged();
    }

    // A click on the overlay lands on an element the focus cannot rest on, which leaves the document body
    // holding it - outside the Dialog, and so outside the trap that was keeping Tab inside it. Where the
    // Dialog is still up and still trapping, the focus is put back on its surface.
    private async Task ReclaimFocusAfterOverlayClick()
    {
        if (IsOpen is false || ShouldTrapFocus is false) return;

        try
        {
            if (await _js.BitUtilsContainsActiveElement(_containerId)) return;

            await _containerRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnOverlayClick.InvokeAsync(e);

        // The Ok callback is still running, so the showing has already been answered - a dismissal now would
        // answer it a second time, over the top of work that has not finished.
        if (_isLoading) return;

        if (IsBlocking)
        {
            await ReclaimFocusAfterOverlayClick();

            await PreventDismiss(BitDialogDismissReason.OverlayClick);
            return;
        }

        await DismissDialog(e, BitDialogDismissReason.OverlayClick);

        await ReclaimFocusAfterOverlayClick();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        if (IsEnabled is false || IsOpen is false || _isLoading) return;

        // A blocking Dialog can only be answered with its buttons, which is as true of the keyboard as
        // it is of a click on the overlay.
        if (CloseOnEscape is false || IsBlocking)
        {
            await PreventDismiss(BitDialogDismissReason.Escape);
            return;
        }

        await DismissDialog(new MouseEventArgs(), BitDialogDismissReason.Escape);
    }

    private async Task HandleOnCloseClick(MouseEventArgs e)
    {
        if (IsEnabled is false || _isLoading) return;

        await OnClose.InvokeAsync(e);

        await DismissDialog(e, BitDialogDismissReason.CloseButton);
    }

    private async Task HandleOnCancelClick(MouseEventArgs e)
    {
        if (IsEnabled is false || _isLoading) return;

        // The answer is in place before the callback runs, so a callback that closes the Dialog itself
        // still reports the answer it was given - and is taken back again when the callback throws, which
        // leaves the Dialog open and therefore unanswered.
        Result = BitDialogResult.Cancel;

        try
        {
            await OnCancel.InvokeAsync(e);
        }
        catch
        {
            Result = null;
            throw;
        }

        await DismissDialog(e, BitDialogDismissReason.CancelButton);
    }

    private async Task HandleOnOkClick(MouseEventArgs e)
    {
        // A second click while the first one is still being awaited would run the callback twice and
        // resolve the showing twice over, so the Ok button answers only once per showing.
        if (IsEnabled is false || _isLoading) return;

        Result = BitDialogResult.Ok;

        if (OnOk.HasDelegate)
        {
            // The spinner only means anything while something is actually being waited for, and it only
            // appears at all because the render is asked for here: the render that follows the handler
            // would arrive after the flag had already been put back.
            _isLoading = true;
            StateHasChanged();

            try
            {
                await OnOk.InvokeAsync(e);
            }
            catch
            {
                // The Dialog stays open for another try, so the button has to come back with it - and the
                // render that normally follows an event handler never arrives for one that faulted, which
                // would otherwise leave the spinner turning over a Dialog that is waiting for nothing.
                _isLoading = false;
                Result = null;
                StateHasChanged();
                throw;
            }

            _isLoading = false;
        }

        await DismissDialog(e, BitDialogDismissReason.OkButton);
    }

    private string GetRole() => (IsAlert ?? (IsBlocking && IsModeless is false)) ? "alertdialog" : "dialog";

    private string GetAriaModal() => (IsModeless is false).ToString().ToLowerInvariant();

    private string? GetLabelledBy()
    {
        if (TitleAriaId.HasValue()) return TitleAriaId;

        return Title.HasValue() && HeaderTemplate is null ? _titleId : null;
    }

    private string? GetDescribedBy()
    {
        if (SubtitleAriaId.HasValue()) return SubtitleAriaId;

        // aria-describedby takes a list, so a Dialog carrying both a subtitle and a message describes itself
        // with the two of them rather than dropping whichever came second - and the message is usually the
        // half that says what is actually being decided.
        if (Subtitle.HasValue() && HeaderTemplate is null)
        {
            return Message.HasValue() ? $"{_subtitleId} {_messageId}" : _subtitleId;
        }

        return Message.HasValue() ? _messageId : null;
    }

    // The same condition the header is rendered under, so the parts that have to know whether there is one
    // - the drag handle, above all - cannot drift away from what was actually rendered.
    private bool HasHeader => HeaderTemplate is not null || Title.HasValue() || Subtitle.HasValue() || ShowCloseButton;

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
        BitDialogPosition.TopStart => "bit-dlg-ts",
        BitDialogPosition.TopEnd => "bit-dlg-te",
        BitDialogPosition.CenterStart => "bit-dlg-cs",
        BitDialogPosition.CenterEnd => "bit-dlg-ce",
        BitDialogPosition.BottomStart => "bit-dlg-bs",
        BitDialogPosition.BottomEnd => "bit-dlg-be",
        _ => "bit-dlg-ctr",
    };

    // An attribute selector rather than an id one, since an id is only a valid CSS identifier by accident:
    // a consumer-supplied Id can hold characters (a leading digit, a dot, a colon) that would make "#id"
    // parse as something else entirely.
    private string ContainerSelector => $"[id=\"{_containerId}\"]";

    // A window is dragged by its title bar: making the whole surface the handle costs the content underneath
    // it - text selection, pointer-driven fields, touch scrolling - for a gesture the header alone offers
    // just as well. The container is the handle only where there is no header to grab.
    private string GetDragElementSelector()
    {
        if (DragElementSelector.HasValue()) return DragElementSelector!;

        return HasHeader ? $"{ContainerSelector} > .bit-dlg-hdr" : ContainerSelector;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // A Dialog that goes away while it is open still has to release whatever it took: the awaiter of
        // its showing, the focus trap, the drag handlers, and above all the scroll lock - a page left with
        // overflow:hidden on its body cannot be scrolled again without a reload.
        CompletePending(Result = null);

        try
        {
            if (_internalIsOpen)
            {
                await DisposeFocusTrap();

                if (_dragElementSelectorOnSetup is not null)
                {
                    await _js.BitDragDropRemove(_containerId, _dragElementSelectorOnSetup);
                    _dragElementSelectorOnSetup = null;
                }

                await ToggleScroll(false);
            }

            if (_focusSaved)
            {
                _focusSaved = false;

                // The Dialog is going away rather than closing, so there is no telling whether handing the
                // focus back is what the page wants: the remembered element is only released here.
                await _js.BitUtilsClearSavedFocus(_containerId);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
