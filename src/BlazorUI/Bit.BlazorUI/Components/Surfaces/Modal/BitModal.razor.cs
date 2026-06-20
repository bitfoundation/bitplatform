namespace Bit.BlazorUI;

/// <summary>
/// Modals are temporary pop-ups that take focus from the page or app and require people to interact with them.
/// </summary>
/// <remarks>
/// There are two different modal components available for different purposes: BitModal is a basic, lightweight modal
/// for simple pop-up content, while BitProModal (in the Bit.BlazorUI.Extras package) is an advanced modal with extra
/// features such as dragging, blocking, modeless, positioning, full-size and scroll handling. Use BitProModal if you
/// need any of those advanced behaviors.
/// </remarks>
public partial class BitModal : BitComponentBase
{
    private bool _internalIsOpen;
    private string _containerId = default!;

    // Stable EventCallback wrappers created once (in OnInitialized) instead of on every
    // BuildParameters call. These are only invoked internally (not passed to a child), so
    // re-creating them per render did not defeat change detection, but it did allocate two
    // closures each OnParametersSet. Their bodies read the current property / cascaded
    // parameter values at invoke time, so they remain correct while avoiding the allocations.
    private EventCallback<MouseEventArgs> _onDismiss;
    private EventCallback<MouseEventArgs> _onOverlayClick;

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



    /// <summary>
    /// Whether the Modal should be announced as modal to assistive technologies.
    /// </summary>
    [Parameter] public bool AriaModal { get; set; } = true;

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    [Parameter] public bool Blocking { get; set; }

    /// <summary>
    /// The content of the Modal, it can be any custom tag or text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitModal component.
    /// </summary>
    [Parameter] public BitModalClassStyles? Classes { get; set; }

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
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;
    }



    private async Task HandleOnOverlayClick(MouseEventArgs e)
    {
        if (_params.IsEnabled is false) return;

        await _params.OnOverlayClick.InvokeAsync(e);

        if (_params.Blocking ?? false) return;

        if (await AssignIsOpen(false) is false) return;
    }

    private string GetRole()
    {
        return (_params.IsAlert ?? false) ? "alertdialog" : "dialog";
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
    /// <item><see cref="Blocking"/>, <see cref="FullHeight"/>, <see cref="FullWidth"/>: the component
    /// param can only force the behavior <b>on</b> (<c>X ? true : p.X</c>); it can never force it off.</item>
    /// <item><see cref="AriaModal"/>, <see cref="ShowOverlay"/>, <see cref="BitComponentBase.IsEnabled"/>:
    /// the component param can only force the behavior <b>off</b> (<c>X is false ? false : p.X</c>); it
    /// can never force it on. These default to <c>true</c>, so opting out is the meaningful override.</item>
    /// </list>
    /// To express the opposite (non-overridable) intent, set the value through the cascaded
    /// <see cref="BitModalParameters"/> (e.g. via the <see cref="BitModalService"/>) rather than the component parameter.
    /// </remarks>
    private BitModalParameters BuildParameters()
    {
        var p = ModalParameters;

        return new BitModalParameters
        {
            // Can only force off (default is enabled): see remarks on asymmetric merge.
            IsEnabled = IsEnabled is false ? false : p.IsEnabled,
            // HtmlAttributes on both sources are externally settable (non-nullable) properties, so a
            // caller can still assign null. Coalesce to empty dictionaries so the Concat in
            // MergeHtmlAttributes (and the snapshot copies) never NRE, mirroring BitModalParameters.Merge.
            HtmlAttributes = MergeHtmlAttributes(p.HtmlAttributes ?? [], HtmlAttributes ?? []),
            Dir = Dir ?? p.Dir,
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
            OnDismiss = _onDismiss,
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

        await base.DisposeAsync(disposing);
    }
}
