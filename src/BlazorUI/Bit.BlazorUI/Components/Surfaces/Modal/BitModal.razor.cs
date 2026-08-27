namespace Bit.BlazorUI;

/// <summary>
/// Modals are temporary pop-ups that take focus from the page or app and require people to interact with them.
/// </summary>
/// <remarks>
/// There are two different modal components available for different purposes: BitModal is a basic, lightweight modal
/// for simple pop-up content, while BitProModal (in the Bit.BlazorUI.Extras package) is an advanced modal with extra
/// features such as dragging, blocking, modeless, positioning, full-size and scroll handling. Use BitProModal if you
/// need any of those advanced behaviors.
/// <br/>
/// Whichever of the two is used, the dialog behaviors every modal owes its user are handled here: the focus moves
/// into the Modal when it opens, Tab keeps cycling inside it while it is open, Escape dismisses it, and the focus
/// goes back to whatever opened it once it closes. Each of those can be turned off on its own.
/// </remarks>
public partial class BitModal : BitComponentBase
{
    private bool _internalIsOpen;
    private bool _focusTrapped;
    private bool _focusStored;
    private string _containerId = default!;

    // Stable EventCallback wrappers created once (in OnInitialized) instead of on every
    // BuildParameters call. These are only invoked internally (not passed to a child), so
    // re-creating them per render did not defeat change detection, but it did allocate two
    // closures each OnParametersSet. Their bodies read the current property / cascaded
    // parameter values at invoke time, so they remain correct while avoiding the allocations.
    private EventCallback<MouseEventArgs> _onDismiss;
    private EventCallback<MouseEventArgs> _onOverlayClick;
    private EventCallback _onOpen;

    // Memoizes the merged HtmlAttributes dictionary so BuildParameters doesn't re-run the
    // Concat/GroupBy/ToDictionary allocation on every OnParametersSet when neither the own nor the
    // cascaded HtmlAttributes content changed. The last-seen sources are stored as content snapshots
    // (copies) rather than references so that in-place mutations of the live dictionaries are detected.
    private Dictionary<string, object>? _mergedHtmlAttributes;
    private Dictionary<string, object>? _lastOwnHtmlAttributes;
    private Dictionary<string, object>? _lastCascadedHtmlAttributes;

    // Snapshots of the scalar values the class/style builders consume. The Classes/Styles inputs are
    // mutable: their members can change without the instance reference changing (e.g. via
    // BitModalService.Refresh), so we compare against these value snapshots rather than references.
    private string? _lastClassesRoot;
    private string? _lastParamsClassesRoot;
    private string? _lastStylesRoot;
    private string? _lastParamsStylesRoot;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether the Modal should be announced as modal to assistive technologies.
    /// </summary>
    /// <remarks>
    /// This is also what decides whether the Modal behaves as a modal dialog: a Modal announced as one keeps
    /// the keyboard inside itself while it is open, and one that is not (a modeless Modal) leaves the page
    /// behind it reachable with the keyboard the way it is reachable with the pointer.
    /// </remarks>
    [Parameter] public bool AriaModal { get; set; } = true;

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    /// <remarks>
    /// Only the overlay click is blocked. Escape still dismisses the Modal unless <see cref="NoDismissOnEscape"/>
    /// is also set, so that a Modal which blocks the pointer can still choose whether it blocks the keyboard.
    /// </remarks>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the Modal, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The initial opening state of the Modal in the uncontrolled mode, which is when the
    /// <see cref="IsOpen"/> parameter is not set.
    /// </summary>
    [Parameter] public bool? DefaultIsOpen { get; set; }

    private BitModalParameters _modalParameters = new();
    [CascadingParameter]
    private BitModalParameters? ModalParameters
    {
        // Tolerate a null cascading value (e.g. ModalParameters="null"): fall back to a fresh
        // instance so downstream consumers never NRE.
        get => _modalParameters;
        set => _modalParameters = value ?? new();
    }

    // The effective parameters: this component's own parameters merged with the cascaded
    // BitModalParameters (the latter supplied by the BitModalService). The component's own
    // parameters take precedence. Rebuilt in OnParametersSet whenever either source changes.
    private BitModalParameters _params = new();


    /// <summary>
    /// Makes the Modal height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    [Parameter] public bool? IsAlert { get; set; }

    /// <summary>
    /// Whether the Modal is displayed.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Prevents the Modal from moving the focus into itself when it opens, for the cases where the focus is
    /// placed by the consumer instead.
    /// </summary>
    /// <remarks>
    /// By default the focus moves to the first focusable element of the content, or to the content itself
    /// when it holds none, so the keyboard is where the Modal is. An element inside the content marked with
    /// the <c>data-autofocus</c> attribute takes the focus instead of the first one, which is how a Modal
    /// whose first focusable element is not the one worth starting at (a close button ahead of the field the
    /// Modal was opened to fill in) names the one that is.
    /// <br/>
    /// Setting this leaves the focus wherever it was, which is only worth doing when the consumer places it
    /// itself: the focus trap and the Escape dismissal both work off the keys pressed inside the Modal, so
    /// neither of them applies for as long as the focus stays outside of it.
    /// </remarks>
    [Parameter] public bool NoAutoFocus { get; set; }

    /// <summary>
    /// Prevents the Modal from being dismissed by pressing the Escape key.
    /// </summary>
    [Parameter] public bool NoDismissOnEscape { get; set; }

    /// <summary>
    /// Prevents the Modal from keeping the keyboard focus inside itself while it is open.
    /// </summary>
    /// <remarks>
    /// A modal dialog holds the tab sequence: without the trap, Tab walks out of the Modal and into the page
    /// behind it, where the overlay swallows every click that could bring the focus back. The trap is only
    /// set up for a Modal that reports itself modal (see <see cref="AriaModal"/>) in the first place.
    /// </remarks>
    [Parameter] public bool NoFocusTrap { get; set; }

    /// <summary>
    /// Prevents the Modal from handing the focus back to the element that had it before the Modal opened.
    /// </summary>
    /// <remarks>
    /// The focus is only handed back when nothing else has taken it in the meantime, so a close handler that
    /// deliberately moves the focus somewhere keeps it there whether or not this is set.
    /// </remarks>
    [Parameter] public bool NoRestoreFocus { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the Modal is opened.
    /// </summary>
    /// <remarks>
    /// Invoked after the Modal has rendered and its focus handling has run, so the content is in the page
    /// and can be measured or scripted from here.
    /// </remarks>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    /// <remarks>
    /// Invoked for every overlay click, including the ones a <see cref="Blocking"/> Modal refuses to be
    /// dismissed by, which is what makes it the place to react to a click that was turned down.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// Whether the overlay should be rendered.
    /// </summary>
    [Parameter] public bool ShowOverlay { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitModalClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    [Parameter] public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
    /// </summary>
    [Parameter] public string? TitleAriaId { get; set; }



    /// <summary>
    /// Opens the Modal.
    /// </summary>
    public async Task Open()
    {
        if (await AssignIsOpen(true) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Closes the Modal.
    /// </summary>
    public async Task Close()
    {
        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Toggles the Modal between its open and closed states.
    /// </summary>
    public async Task Toggle()
    {
        if (await AssignIsOpen(IsOpen is false) is false) return;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-mdl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => _params.Classes?.Root);

        ClassBuilder.Register(() => (_params.FullHeight ?? false) ? "bit-mdl-fhe" : string.Empty);
        ClassBuilder.Register(() => (_params.FullWidth ?? false) ? "bit-mdl-fwi" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
        StyleBuilder.Register(() => _params.Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _containerId = $"BitModal-{UniqueId}-container";

        // The uncontrolled starting state, which only applies while the consumer is not driving IsOpen
        // itself. It is read once here rather than every time the parameters are set, so that closing an
        // uncontrolled Modal is not undone by the next render.
        if (IsOpenHasBeenSet is false && DefaultIsOpen.HasValue)
        {
            IsOpen = DefaultIsOpen.Value;
        }

        // Create the event callbacks once. They read the current OnXxx properties and the
        // cascaded ModalParameters at invoke time, so they stay correct without being rebuilt
        // every render.
        _onDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, async (MouseEventArgs e) =>
        {
            await OnDismiss.InvokeAsync(e);
            await ModalParameters!.OnDismiss.InvokeAsync(e);
        });
        _onOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, async (MouseEventArgs e) =>
        {
            await OnOverlayClick.InvokeAsync(e);
            await ModalParameters!.OnOverlayClick.InvokeAsync(e);
        });
        _onOpen = EventCallback.Factory.Create(this, async () =>
        {
            await OnOpen.InvokeAsync();
            await ModalParameters!.OnOpen.InvokeAsync();
        });

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        var previous = _params;

        _params = BuildParameters();

        // The [ResetClassBuilder] attribute only resets ClassBuilder when this component's own
        // parameters change. However, the registered class/style lambdas also read the (own and
        // cascaded) Classes/Styles values. Those are mutable inputs: mutating their members in place
        // (e.g. via BitModalService.Refresh) doesn't change the instance reference, so a reference
        // comparison can miss the change and leave the builders with stale cached values. Compare the
        // actual scalar values the builders consume against the previous snapshot so that any change
        // is detected, regardless of whether the instance reference changed.
        var classesRoot = Classes?.Root;
        var paramsClassesRoot = _params.Classes?.Root;
        if (previous.FullHeight != _params.FullHeight ||
            previous.FullWidth != _params.FullWidth ||
            _lastClassesRoot != classesRoot ||
            _lastParamsClassesRoot != paramsClassesRoot)
        {
            ClassBuilder.Reset();
        }
        _lastClassesRoot = classesRoot;
        _lastParamsClassesRoot = paramsClassesRoot;

        var stylesRoot = Styles?.Root;
        var paramsStylesRoot = _params.Styles?.Root;
        if (_lastStylesRoot != stylesRoot ||
            _lastParamsStylesRoot != paramsStylesRoot)
        {
            StyleBuilder.Reset();
        }
        _lastStylesRoot = stylesRoot;
        _lastParamsStylesRoot = paramsStylesRoot;

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // The focus trap is registered against the open Modal, so turning it on or off while the Modal is
        // open has to reach the already registered one rather than wait for the next time it opens.
        if (IsRendered is false || IsOpen is false || _internalIsOpen is false) return;

        if (ShouldTrapFocus)
        {
            await SetupFocusTrap();
        }
        else
        {
            await DisposeFocusTrap();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        if (IsOpen)
        {
            await HandleOnOpened();
        }
        else
        {
            await HandleOnClosed();
        }
    }



    private async Task HandleOnOpened()
    {
        // The focus is recorded before anything is done with it, while it is still on whatever opened the
        // Modal: this is the element it goes back to once the Modal closes.
        await StoreFocus();

        await SetupFocusTrap();

        await FocusContent();

        await _params.OnOpen.InvokeAsync();
    }

    private async Task HandleOnClosed()
    {
        await DisposeFocusTrap();

        await RestoreFocus();
    }

    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (_params.IsEnabled is false) return;

        await _params.OnOverlayClick.InvokeAsync(e);

        if (_params.Blocking ?? false) return;

        await AssignIsOpen(false);
    }

    // Escape dismisses the Modal from anywhere inside it, as the dialog pattern requires. The key is only
    // seen while the focus is inside the Modal, which is where it is put when the Modal opens.
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (_params.IsEnabled is false) return;

        if (e.Key is not "Escape") return;

        if (_params.NoDismissOnEscape ?? false) return;

        await AssignIsOpen(false);
    }

    private string GetRole()
    {
        return (_params.IsAlert ?? false) ? "alertdialog" : "dialog";
    }

    // Null rather than an empty string when there is nothing to render, so a Modal that was given no
    // styles carries no style attribute at all instead of an empty one.
    private string? GetOverlayStyles()
    {
        return JoinStyles(Styles?.Overlay, _params.Styles?.Overlay);
    }

    private string GetOverlayClasses()
    {
        return JoinClasses("bit-mdl-ovl", Classes?.Overlay, _params.Classes?.Overlay);
    }

    private string? GetContentStyles()
    {
        return JoinStyles(Styles?.Content, _params.Styles?.Content);
    }

    private string GetContentClasses()
    {
        return JoinClasses("bit-mdl-ctn", Classes?.Content, _params.Classes?.Content);
    }

    // Two class lists are one attribute value while a single space stands between them, and an empty part
    // in the middle would otherwise leave a double space (or a trailing one) in the rendered attribute.
    // Written out rather than joined off a collection: this runs per part per render, and the case that
    // matters - a Modal that was given no classes of its own - then costs no allocation at all.
    private static string JoinClasses(string baseClass, string? ownClass, string? paramsClass)
    {
        if (ownClass.HasNoValue()) return paramsClass.HasNoValue() ? baseClass : $"{baseClass} {paramsClass}";

        if (paramsClass.HasNoValue()) return $"{baseClass} {ownClass}";

        return $"{baseClass} {ownClass} {paramsClass}";
    }

    private void OnSetIsOpen()
    {
        if (IsOpen || IsRendered is false) return;

        // Fire-and-forget the dismiss callback, then re-render. Wrapped in a local async method
        // (instead of ContinueWith) so a throwing OnDismiss surfaces through Blazor's normal async
        // error handling via the renderer dispatcher rather than being swallowed on an unobserved task.
        _ = InvokeAsync(async () =>
        {
            await _params.OnDismiss.InvokeAsync(new MouseEventArgs());
            StateHasChanged();
        });
    }

    // Whether the keyboard is the Modal's to hold. Only a Modal that reports itself modal takes the tab
    // sequence over: a modeless one is meant to leave the page behind it usable, and a trap would take the
    // keyboard half of that away while leaving the pointer half in place.
    private bool ShouldTrapFocus => (_params.NoFocusTrap ?? false) is false && (_params.AriaModal ?? true);

    private async Task FocusContent()
    {
        if (_params.NoAutoFocus ?? false) return;

        if (IsDisposed) return;

        try
        {
            await _js.BitUtilsFocusFirstElement(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task StoreFocus()
    {
        // Nothing is handed back to a Modal that was told not to hand anything back, so nothing is recorded
        // for it either - the map would otherwise keep the element alive until the Modal closes.
        if ((_params.NoRestoreFocus ?? false) || _focusStored || IsDisposed) return;

        _focusStored = true;

        try
        {
            await _js.BitUtilsStoreFocus(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RestoreFocus()
    {
        // Only what was recorded is handed back, so a Modal that never took the focus over never places it
        // either - and the recording is taken back whether or not the hand-back went through.
        if (_focusStored is false) return;

        _focusStored = false;

        if (IsDisposed) return;

        try
        {
            // The Modal is out of the page by now, which drops the focus it was holding on the body: that
            // is the state the restore is for. A focus that has since moved somewhere else - a close
            // handler that placed it deliberately - belongs to whoever moved it, so it is left alone.
            await _js.BitUtilsRestoreFocus(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task SetupFocusTrap()
    {
        if (ShouldTrapFocus is false || _focusTrapped || IsDisposed) return;

        _focusTrapped = true;

        try
        {
            await _js.BitUtilsSetupFocusTrap(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeFocusTrap()
    {
        if (_focusTrapped is false) return;

        _focusTrapped = false;

        try
        {
            await _js.BitUtilsDisposeFocusTrap(_containerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    /// <summary>
    /// Builds the effective parameters by merging this component's own parameters with the cascaded
    /// <see cref="BitModalParameters"/>. The component's own values take precedence, preserving the
    /// behavior previously provided by the parameters object reading back from the component.
    /// </summary>
    /// <remarks>
    /// Nullable values use a simple "own value, else cascaded" precedence (<c>Own ?? p.Own</c>).
    /// Non-nullable bools cannot distinguish "not set" from "explicitly false", so they merge
    /// asymmetrically and the component param only expresses the "stronger" intent for that flag:
    /// <list type="bullet">
    /// <item><see cref="Blocking"/>, <see cref="FullHeight"/>, <see cref="FullWidth"/>,
    /// <see cref="NoAutoFocus"/>, <see cref="NoDismissOnEscape"/>, <see cref="NoFocusTrap"/>,
    /// <see cref="NoRestoreFocus"/>: the component param can only force the behavior <b>on</b>
    /// (<c>X ? true : p.X</c>); it can never force it off.</item>
    /// <item><see cref="AriaModal"/>, <see cref="ShowOverlay"/>, <see cref="BitComponentBase.IsEnabled"/>:
    /// the component param can only force the behavior <b>off</b> (<c>X is false ? false : p.X</c>); it
    /// can never force it on. These default to <c>true</c>, so opting out is the meaningful override.</item>
    /// </list>
    /// To express the opposite (non-overridable) intent, set the value through the cascaded
    /// <see cref="BitModalParameters"/> (e.g. via the <see cref="BitModalService"/>) rather than the component parameter.
    /// </remarks>
    private BitModalParameters BuildParameters()
    {
        var p = ModalParameters!;

        return new BitModalParameters
        {
            // Can only force off (default is enabled): see remarks on asymmetric merge.
            IsEnabled = IsEnabled is false ? false : p.IsEnabled,
            // HtmlAttributes on both sources are externally settable (non-nullable) properties, so a
            // caller can still assign null. Coalesce to empty dictionaries so the Concat in
            // MergeHtmlAttributes (and the snapshot copies) never NRE, mirroring BitModalParameters.Merge.
            HtmlAttributes = MergeHtmlAttributes(p.HtmlAttributes ?? [], HtmlAttributes ?? []),
            Dir = Dir ?? p.Dir,
            AriaLabel = AriaLabel ?? p.AriaLabel,
            // Can only force off (default is enabled): see remarks on asymmetric merge.
            AriaModal = AriaModal is false ? false : p.AriaModal,
            // Can only force on (default is off): see remarks on asymmetric merge.
            Blocking = Blocking ? true : p.Blocking,
            Classes = p.Classes,
            // Can only force on (default is off): see remarks on asymmetric merge.
            FullHeight = FullHeight ? true : p.FullHeight,
            // Can only force on (default is off): see remarks on asymmetric merge.
            FullWidth = FullWidth ? true : p.FullWidth,
            IsAlert = IsAlert ?? p.IsAlert,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoAutoFocus = NoAutoFocus ? true : p.NoAutoFocus,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoDismissOnEscape = NoDismissOnEscape ? true : p.NoDismissOnEscape,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoFocusTrap = NoFocusTrap ? true : p.NoFocusTrap,
            // Can only force on (default is off): see remarks on asymmetric merge.
            NoRestoreFocus = NoRestoreFocus ? true : p.NoRestoreFocus,
            OnDismiss = _onDismiss,
            OnOpen = _onOpen,
            OnOverlayClick = _onOverlayClick,
            // Can only force off (default is enabled): see remarks on asymmetric merge.
            ShowOverlay = ShowOverlay is false ? false : p.ShowOverlay,
            Styles = p.Styles,
            SubtitleAriaId = SubtitleAriaId ?? p.SubtitleAriaId,
            TitleAriaId = TitleAriaId ?? p.TitleAriaId,
        };
    }

    /// <summary>
    /// Merges the cascaded and own HtmlAttributes (own values win), reusing the previous result when
    /// neither source dictionary changed by content to avoid a per-render allocation. Content (rather
    /// than reference) comparison is used so in-place mutations of these mutable inputs are detected.
    /// </summary>
    private Dictionary<string, object> MergeHtmlAttributes(Dictionary<string, object> cascaded, Dictionary<string, object> own)
    {
        if (_mergedHtmlAttributes is not null &&
            DictionaryContentEqual(_lastCascadedHtmlAttributes, cascaded) &&
            DictionaryContentEqual(_lastOwnHtmlAttributes, own))
        {
            return _mergedHtmlAttributes;
        }

        // Store independent content snapshots (copies) so a later in-place mutation of the live source
        // dictionaries differs from what was captured here and forces a rebuild.
        _lastCascadedHtmlAttributes = new Dictionary<string, object>(cascaded);
        _lastOwnHtmlAttributes = new Dictionary<string, object>(own);
        _mergedHtmlAttributes = cascaded.Concat(own).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Last().Value);

        return _mergedHtmlAttributes;
    }

    private static bool DictionaryContentEqual(Dictionary<string, object>? a, Dictionary<string, object>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        foreach (var kv in a)
        {
            if (b.TryGetValue(kv.Key, out var value) is false) return false;
            if (Equals(kv.Value, value) is false) return false;
        }

        return true;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // A Modal disposed while it is still open never reaches its close, so the registrations it made on
        // the JS side are taken back here instead. The stored focus is dropped rather than restored: there
        // is no close for it to be handed back on, and the map would keep the element alive without this.
        if (_focusTrapped || _focusStored)
        {
            var trapped = _focusTrapped;
            var stored = _focusStored;
            _focusTrapped = false;
            _focusStored = false;

            try
            {
                if (trapped)
                {
                    await _js.BitUtilsDisposeFocusTrap(_containerId);
                }

                if (stored)
                {
                    await _js.BitUtilsForgetFocus(_containerId);
                }
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        await base.DisposeAsync(disposing);
    }
}
