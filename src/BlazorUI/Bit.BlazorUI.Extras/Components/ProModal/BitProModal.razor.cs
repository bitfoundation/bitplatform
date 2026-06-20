namespace Bit.BlazorUI;

/// <summary>
/// ProModal is an advanced version of normal Modal with additional features that tailored to more usual use-cases.
/// </summary>
public partial class BitProModal : BitComponentBase
{
    /// <summary>
    /// The default title (and aria-label) used for the close button when none is provided.
    /// </summary>
    internal const string DefaultCloseButtonTitle = "Close";

    private bool _internalIsOpen;
    private float _offsetTop;
    // Captures whether scroll was actually locked during the open sequence, so the close sequence
    // unlocks if and only if it locked, regardless of later changes to AutoToggleScroll.
    private bool _scrollLockedOnOpen;
    // Snapshots the scroller target captured during open, so the close sequence unlocks the exact
    // same scroller even if ScrollerElement/ScrollerSelector changed since the modal was opened.
    private ElementReference? _scrollerElementOnOpen;
    private string? _scrollerSelectorOnOpen;
    // Snapshots the drag element selector used to register drag handlers, so teardown unregisters
    // the exact same selector even if DragElementSelector changed since the modal was opened.
    private string? _dragElementSelectorOnSetup;

    // Stable EventCallback wrappers created once (in OnInitialized) instead of on every
    // BuildParameters call. Re-creating them per render produced new delegate instances each
    // time, which Blazor's change detection treats as changed parameters, defeating change
    // detection on the inner BitModal. Their bodies read the current property / cascaded
    // parameter values at invoke time, so they remain correct while staying reference-stable.
    private EventCallback _onOpen;
    private EventCallback<MouseEventArgs> _onDismiss;
    private EventCallback<MouseEventArgs> _onOverlayClick;

    // Memoizes the merged HtmlAttributes dictionary so BuildParameters doesn't re-run the
    // Concat/GroupBy/ToDictionary allocation on every OnParametersSet when neither the own nor the
    // cascaded HtmlAttributes reference changed. In-place mutation through the service is still
    // reflected because BitProModalParameters.Merge allocates a fresh dictionary on every Refresh,
    // changing the reference and invalidating this cache (mirrors BitModal's behavior).
    private Dictionary<string, object>? _mergedHtmlAttributes;
    private Dictionary<string, object>? _lastOwnHtmlAttributes;
    private Dictionary<string, object>? _lastCascadedHtmlAttributes;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    private BitProModalParameters _proModalParameters = new();
    [CascadingParameter]
    private BitProModalParameters? ProModalParameters
    {
        // Tolerate a null cascading value (e.g. ProModalParameters="null"): fall back to a fresh
        // instance so downstream consumers (_classes, _styles, BuildParameters) never NRE.
        get => _proModalParameters;
        set => _proModalParameters = value ?? new();
    }

    // The effective parameters: the component's own parameters merged with the cascaded
    // BitProModalParameters (the latter supplied by the BitProModalService). The component's
    // own parameters take precedence. This is rebuilt in OnParametersSet whenever either source changes.
    private BitProModalParameters _params = new();

    // The merged class/style maps used by the razor. These are computed once per
    // OnParametersSet (like _params) instead of on every property access, since the
    // razor reads them many times per render and Merge allocates a new object each call.
    private BitProModalClassStyles? _classes;
    private BitProModalClassStyles? _styles;



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
    /// Defaults to "Close" when not set.
    /// </summary>
    [Parameter] public string? CloseButtonTitle { get; set; }

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
        ClassBuilder.Register(() => (_params.ModeFull ?? false) ? "bit-pmd-mfl" : string.Empty);
        ClassBuilder.Register(() => (_params.NoBorder ?? false) ? string.Empty : "bit-pmd-nbr");
        ClassBuilder.Register(() => (_params.AbsolutePosition ?? false) ? "bit-pmd-abs" : string.Empty);
        ClassBuilder.Register(() => _params.Position switch
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

    protected override void OnInitialized()
    {
        // Create the event callbacks once. They read the current OnXxx properties and the
        // cascaded ProModalParameters at invoke time, so they stay correct without being
        // rebuilt every render.
        _onDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, async (MouseEventArgs e) =>
        {
            await OnDismiss.InvokeAsync(e);
            await ProModalParameters!.OnDismiss.InvokeAsync(e);
        });
        _onOpen = EventCallback.Factory.Create(this, async () =>
        {
            await OnOpen.InvokeAsync();
            await ProModalParameters!.OnOpen.InvokeAsync();
        });
        _onOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, async (MouseEventArgs e) =>
        {
            await OnOverlayClick.InvokeAsync(e);
            await ProModalParameters!.OnOverlayClick.InvokeAsync(e);
        });

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        var previous = _params;

        _params = BuildParameters();
        _classes = BitProModalClassStyles.Merge(Classes, ProModalParameters.Classes);
        _styles = BitProModalClassStyles.Merge(Styles, ProModalParameters.Styles);

        // The [ResetClassBuilder] attribute only resets ClassBuilder when this component's own
        // parameters change. However, the registered class lambdas read the merged _params values,
        // which also incorporate the cascaded BitProModalParameters. When those cascaded values are
        // mutated in place (e.g. via BitProModalService.Refresh), no own-parameter change occurs, so
        // the ClassBuilder would otherwise keep its cached (stale) value. Reset it here when any
        // class-affecting merged value actually changed.
        if (previous.ModeFull != _params.ModeFull ||
            previous.NoBorder != _params.NoBorder ||
            previous.AbsolutePosition != _params.AbsolutePosition ||
            previous.Position != _params.Position)
        {
            ClassBuilder.Reset();
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen)
        {
            if (_internalIsOpen is false)
            {
                _internalIsOpen = true;

                if (_params.Draggable ?? false)
                {
                    _dragElementSelectorOnSetup = _dragElementSelector;
                    await _js.BitDragDropSetup(_containerSelector, _containerSelector, _dragElementSelectorOnSetup);
                }

                // Reset _offsetTop before ToggleScroll. When AutoToggleScroll is false,
                // ToggleScroll returns early and won't recalculate _offsetTop, so this
                // guards against a stale top-offset from a previous open.
                _offsetTop = 0;

                await ToggleScroll(true);

                // Only when AbsolutePosition is set do we reset the StyleBuilder and
                // re-render, so the top-offset style (which ToggleScroll may have updated)
                // gets applied on the next render.
                if (_params.AbsolutePosition ?? false)
                {
                    StyleBuilder.Reset();
                    StateHasChanged();
                }

                await _params.OnOpen.InvokeAsync();
            }
        }
        else
        {
            if (_internalIsOpen)
            {
                _internalIsOpen = false;

                await _js.BitDragDropRemove(_containerSelector, _dragElementSelectorOnSetup ?? _dragElementSelector);

                await ToggleScroll(false);
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private string _modalId => Id ?? UniqueId;
    private string _containerSelector => $"#{_modalId} .bit-mdl-ctn";
    private string _dragElementSelector => _params.DragElementSelector ?? _containerSelector;

    private async Task HandleInnerIsOpenChanged(bool open)
    {
        await AssignIsOpen(open);
    }

    private async Task CloseModal(MouseEventArgs e)
    {
        if (_params.IsEnabled is false) return;

        await AssignIsOpen(false);
    }

    private async Task ToggleScroll(bool isOpen)
    {
        if (isOpen)
        {
            // Snapshot the lock decision at open time; close reuses it instead of re-reading
            // AutoToggleScroll, which may have changed since the modal was opened.
            _scrollLockedOnOpen = _params.AutoToggleScroll ?? false;
            if (_scrollLockedOnOpen is false) return;

            // Snapshot the scroller target at open time so close unlocks the same scroller,
            // even if ScrollerElement/ScrollerSelector changed in the meantime.
            _scrollerElementOnOpen = _params.ScrollerElement;
            _scrollerSelectorOnOpen = _params.ScrollerSelector;
        }
        else
        {
            // Only unlock if we actually locked during open, regardless of the current parameter value.
            if (_scrollLockedOnOpen is false) return;
        }

        if (_scrollerElementOnOpen.HasValue)
        {
            _offsetTop = await _js.BitUtilsToggleOverflow(_scrollerElementOnOpen.Value, isOpen);
        }
        else
        {
            _offsetTop = await _js.BitUtilsToggleOverflow(_scrollerSelectorOnOpen ?? "body", isOpen);
        }
    }

    /// <summary>
    /// Builds the effective parameters by merging this component's own parameters with the cascaded
    /// <see cref="BitProModalParameters"/>. The component's own values take precedence, preserving the
    /// behavior previously provided by the parameters object reading back from the component.
    /// </summary>
    /// <remarks>
    /// Nullable values use a simple "own value, else cascaded" precedence (<c>Own ?? p.Own</c>).
    /// Non-nullable bools cannot distinguish "not set" from "explicitly false", so they merge
    /// asymmetrically and the component param only expresses the "stronger" intent for that flag:
    /// <list type="bullet">
    /// <item>The feature flags below (<see cref="Blocking"/>, <see cref="FullHeight"/>, etc.) can only
    /// force the behavior <b>on</b> (<c>X ? true : p.X</c>); they can never force it off, since they
    /// default to <c>false</c> and enabling is the meaningful override.</item>
    /// <item><see cref="BitComponentBase.IsEnabled"/> can only force the behavior <b>off</b>
    /// (<c>X is false ? false : p.X</c>); it can never force it on, since it defaults to <c>true</c>
    /// and disabling is the meaningful override.</item>
    /// </list>
    /// To express the opposite (non-overridable) intent, set the value through the cascaded
    /// <see cref="BitProModalParameters"/> (e.g. via the <see cref="BitProModalService"/>) rather than the component parameter.
    /// </remarks>
    private BitProModalParameters BuildParameters()
    {
        var p = ProModalParameters;

        // Non-nullable bools below follow the "can only force on" rule (see remarks);
        // IsEnabled is the lone "can only force off" exception.
        return new BitProModalParameters
        {
            AbsolutePosition = AbsolutePosition ? true : p.AbsolutePosition,
            AriaLabel = AriaLabel ?? p.AriaLabel,
            AutoToggleScroll = AutoToggleScroll ? true : p.AutoToggleScroll,
            Blocking = Blocking ? true : p.Blocking,
            CloseButtonTitle = CloseButtonTitle ?? p.CloseButtonTitle,
            CloseIcon = CloseIcon ?? p.CloseIcon,
            CloseIconName = CloseIconName ?? p.CloseIconName,
            Dir = Dir ?? p.Dir,
            DragElementSelector = DragElementSelector ?? p.DragElementSelector,
            Draggable = Draggable ? true : p.Draggable,
            Footer = Footer ?? p.Footer,
            FooterText = FooterText ?? p.FooterText,
            FullHeight = FullHeight ? true : p.FullHeight,
            FullSize = FullSize ? true : p.FullSize,
            FullWidth = FullWidth ? true : p.FullWidth,
            Header = Header ?? p.Header,
            HeaderText = HeaderText ?? p.HeaderText,
            HtmlAttributes = MergeHtmlAttributes(p.HtmlAttributes, HtmlAttributes),
            IsAlert = IsAlert ?? p.IsAlert,
            // Can only force off (default is enabled): the lone exception to the "force on" rule above.
            IsEnabled = IsEnabled is false ? false : p.IsEnabled,
            ModeFull = ModeFull ? true : p.ModeFull,
            Modeless = Modeless ? true : p.Modeless,
            NoBorder = NoBorder ? true : p.NoBorder,
            OnDismiss = _onDismiss,
            OnOpen = _onOpen,
            OnOverlayClick = _onOverlayClick,
            Position = Position ?? p.Position,
            ScrollerElement = ScrollerElement ?? p.ScrollerElement,
            ScrollerSelector = ScrollerSelector ?? p.ScrollerSelector,
            ShowCloseButton = ShowCloseButton ? true : p.ShowCloseButton,
            SubtitleAriaId = SubtitleAriaId ?? p.SubtitleAriaId,
            TitleAriaId = TitleAriaId ?? p.TitleAriaId,
            // Can only force off (default is Visible): own value wins only when it's a meaningful
            // (non-default) override, otherwise the cascaded value is used.
            Visibility = Visibility != BitVisibility.Visible ? Visibility : p.Visibility,
        };
    }

    /// <summary>
    /// Merges the cascaded and own HtmlAttributes (own values win), reusing the previous result when
    /// neither source dictionary reference changed to avoid a per-render allocation.
    /// </summary>
    private Dictionary<string, object> MergeHtmlAttributes(Dictionary<string, object>? cascaded, Dictionary<string, object>? own)
    {
        if (_mergedHtmlAttributes is not null &&
            ReferenceEquals(_lastCascadedHtmlAttributes, cascaded) &&
            ReferenceEquals(_lastOwnHtmlAttributes, own))
        {
            return _mergedHtmlAttributes;
        }

        _lastCascadedHtmlAttributes = cascaded;
        _lastOwnHtmlAttributes = own;
        _mergedHtmlAttributes = (cascaded ?? []).Concat(own ?? []).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Last().Value);

        return _mergedHtmlAttributes;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            if (_internalIsOpen)
            {
                await _js.BitDragDropRemove(_containerSelector, _dragElementSelectorOnSetup ?? _dragElementSelector);
                await ToggleScroll(false);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
