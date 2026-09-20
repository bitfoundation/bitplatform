using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// Buttons enable users to take actions with a single tap. They're commonly used in forms, dialog panels, and specialized for tasks like navigation or repeated actions.
/// </summary>
public partial class BitButton : BitComponentBase
{
    private string? _rel;
    private bool _dragging;
    private bool _showLoading;
    private bool _draggableEnabled;
    private BitButtonType _buttonType;
    private CancellationTokenSource? _loadingDelayCts;
    private DotNetObjectReference<BitButton>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>.
    /// The value is coming from the cascading value provided by the EditForm.
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }

    /// <summary>
    /// Gets or sets the cascading parameters for the button component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple button components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitButtonParams.ParamName)]
    public BitButtonParams? CascadingParameters { get; set; }



    /// <summary>
    /// Keeps the disabled button focusable and discoverable by screen readers, rendering <c>aria-disabled</c> instead of the
    /// native <c>disabled</c> attribute when <see cref="BitComponentBase.IsEnabled"/> is false, preserving a consistent tab order.
    /// Set it to false to render the native <c>disabled</c> attribute and remove the button from the tab order.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
    /// <remarks>
    /// It is rendered as visually hidden text beside the button and read after its name, not as part of it.
    /// An <c>aria-describedby</c> written on the component by hand is kept and this description is added to it,
    /// since the attribute is a list of ids.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, adds an <c>aria-hidden</c> attribute instructing screen readers to ignore the button.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// If true, the button automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// If true, enters the loading state automatically while awaiting the OnClick event and prevents subsequent clicks by default.
    /// </summary>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// The type of the button element; defaults to <c>submit</c> inside an <see cref="EditForm"/> otherwise <c>button</c>.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The content of primary section of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the button.
    /// </summary>
    [Parameter] public BitButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The value of the <c>download</c> attribute of the link rendered by the button when <see cref="Href"/> is provided.
    /// Instructs the browser to download the linked resource instead of navigating to it, using the provided value as the file name.
    /// </summary>
    [Parameter] public string? Download { get; set; }

    /// <summary>
    /// Makes the Float/FloatAbsolute button draggable on the page; ignored when neither is set.
    /// </summary>
    /// <remarks>
    /// The button can also be moved from the keyboard while it has the focus - the arrow keys move it by a
    /// step and Shift with an arrow by a coarser one - so the repositioning is not dragging-only
    /// (WCAG 2.2 SC 2.5.7). The click that ends a drag is swallowed, so <see cref="OnClick"/> is raised by a
    /// press and not by a move.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Draggable { get; set; }

    /// <summary>
    /// Preserves the foreground color of the button through hover and focus.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FixedColor { get; set; }

    /// <summary>
    /// Enables floating behavior for the button, allowing it to be positioned relative to the viewport.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Float { get; set; }

    /// <summary>
    /// Enables floating behavior for the button, allowing it to be positioned relative to its container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FloatAbsolute { get; set; }

    /// <summary>
    /// Specifies the offset of the floating button.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? FloatOffset { get; set; }

    /// <summary>
    /// Specifies the position of the floating button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPosition? FloatPosition { get; set; }

    /// <summary>
    /// The id of the form element that the button is associated with (rendered as the <c>form</c> attribute).
    /// Allows a submit/reset button to be placed outside of its form element.
    /// </summary>
    [Parameter] public string? FormId { get; set; }

    /// <summary>
    /// Expand the button width to 100% of the available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The value of the href attribute of the link rendered by the button. If provided, the component will be rendered as an anchor tag instead of button.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OnIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Emoji</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered.
    /// </summary>
    /// <remarks>
    /// The text stays as screen-reader-only content, so the button keeps the accessible name the label gave it
    /// instead of becoming a nameless icon. Setting <see cref="BitComponentBase.AriaLabel"/> (or an <c>aria-labelledby</c>) names
    /// the button explicitly and replaces it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// Gets or sets the position of the icon relative to the component's content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconPosition? IconPosition { get; set; }

    /// <summary>
    /// The url of the custom icon to render inside the button.
    /// </summary>
    [Parameter] public string? IconUrl { get; set; }

    /// <summary>
    /// Determines whether the button is in loading mode or not.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool IsLoading { get; set; }

    /// <summary>
    /// The delay in milliseconds before the spinner appears after the button enters the loading state.
    /// </summary>
    /// <remarks>
    /// An operation that finishes inside the delay never shows a spinner at all, which is what keeps a fast
    /// action from flashing one. The state itself is not delayed: the click is blocked and <c>aria-busy</c> is
    /// rendered as soon as the loading starts, whatever this is set to.
    /// </remarks>
    [Parameter] public int LoadingDelay { get; set; }

    /// <summary>
    /// The loading label text to show next to the spinner icon.
    /// </summary>
    /// <remarks>
    /// It is also announced by assistive technologies when the loading starts, from a live region beside the
    /// button, so that it is heard as a change of state instead of changing the name of the button itself.
    /// </remarks>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The position of the loading Label in regards to the spinner icon.
    /// </summary>
    [Parameter] public BitLabelPosition LoadingLabelPosition { get; set; } = BitLabelPosition.End;

    /// <summary>
    /// The custom template used to replace the default spinner and loading label inside the button in the loading state.
    /// </summary>
    /// <remarks>
    /// Like the spinner it replaces, it is hidden from assistive technologies: the content stacked underneath it
    /// keeps the button's accessible name while it works. Use <see cref="LoadingLabel"/> for what should be
    /// announced when the loading starts.
    /// </remarks>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Keeps each line of the button's text on a single line and ends it with an ellipsis where it does not fit.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// Raised when the button is clicked; receives a bool indicating the current loading state.
    /// </summary>
    [Parameter] public EventCallback<bool> OnClick { get; set; }

    /// <summary>
    /// The content of the primary section of the button (alias of the ChildContent).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? PrimaryTemplate { get; set; }

    /// <summary>
    /// Enables re-clicking while the button is in the loading state.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reclickable { get; set; }

    /// <summary>
    /// Sets the <c>rel</c> attribute for link-rendered buttons when <see cref="Href"/> is a non-anchor URL; ignored for empty or hash-only hrefs.
    /// The <c>rel</c> attribute specifies the relationship between the current document and the linked document.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Renders the button with fully rounded (pill shaped) corners, and an icon-only one as a circle.
    /// </summary>
    /// <remarks>
    /// It changes the corner the button falls back to, so a <c>--bit-Button-radius</c> set on the button or on
    /// an ancestor still has the last word.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Rounded { get; set; }

    /// <summary>
    /// The text of the secondary section of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? SecondaryText { get; set; }

    /// <summary>
    /// The custom template for the secondary section of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? SecondaryTemplate { get; set; }

    /// <summary>
    /// Sets the preset size for typography and padding of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom inline styles for different parts of the button.
    /// </summary>
    [Parameter] public BitButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// Specifies target attribute of the link when the button renders as an anchor (by providing the Href parameter).
    /// When set to <c>_blank</c> and no <see cref="Rel"/> is provided, <c>rel="noopener"</c> gets added automatically for security.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefRelAndTarget))]
    public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Gives focus to the root element of the button.
    /// </summary>
    public ValueTask FocusAsync() => RootElement.FocusAsync();



    // The three callbacks the drag script invokes, each with the pointer's position. The coordinates are
    // unused here, but the signatures have to take them: the interop dispatcher matches a call to a method
    // by the number of arguments and throws when they differ, and the script swallows what it throws - so a
    // handler declared without them is never actually reached, and the drag flag below never flips.
    [JSInvokable("OnDragStart")]
    public ValueTask _OnDragStart(double x, double y)
    {
        return ValueTask.CompletedTask;
    }

    [JSInvokable("OnDragging")]
    public ValueTask _OnDragging(double x, double y)
    {
        _dragging = true;

        return ValueTask.CompletedTask;
    }

    [JSInvokable("OnDragEnd")]
    public async ValueTask _OnDragEnd(double x, double y)
    {
        // The click that ends a drag arrives after the drag does, so the flag that swallows it has to outlive it.
        await Task.Delay(100);

        _dragging = false;
    }



    protected override string RootElementClass => "bit-btn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ((PrimaryTemplate ?? ChildContent) is null &&
                                    SecondaryText.HasNoValue() && SecondaryTemplate is null) ||
                                    IconOnly
                                        ? "bit-btn-ntx"
                                        : string.Empty);

        ClassBuilder.Register(() => SecondaryText.HasValue() || SecondaryTemplate is not null ? "bit-btn-hsc" : string.Empty);

        ClassBuilder.Register(() => _showLoading ? "bit-btn-lda" : string.Empty);

        ClassBuilder.Register(() => IsLoading && Reclickable is false ? "bit-btn-lnc" : string.Empty);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-btn-fil",
            BitVariant.Outline => "bit-btn-otl",
            BitVariant.Text => "bit-btn-txt",
            _ => "bit-btn-fil"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-btn-pri",
            BitColor.Secondary => "bit-btn-sec",
            BitColor.Tertiary => "bit-btn-ter",
            BitColor.Info => "bit-btn-inf",
            BitColor.Success => "bit-btn-suc",
            BitColor.Warning => "bit-btn-wrn",
            BitColor.SevereWarning => "bit-btn-swr",
            BitColor.Error => "bit-btn-err",
            BitColor.PrimaryBackground => "bit-btn-pbg",
            BitColor.SecondaryBackground => "bit-btn-sbg",
            BitColor.TertiaryBackground => "bit-btn-tbg",
            BitColor.PrimaryForeground => "bit-btn-pfg",
            BitColor.SecondaryForeground => "bit-btn-sfg",
            BitColor.TertiaryForeground => "bit-btn-tfg",
            BitColor.PrimaryBorder => "bit-btn-pbr",
            BitColor.SecondaryBorder => "bit-btn-sbr",
            BitColor.TertiaryBorder => "bit-btn-tbr",
            _ => "bit-btn-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-btn-sm",
            BitSize.Medium => "bit-btn-md",
            BitSize.Large => "bit-btn-lg",
            _ => "bit-btn-md"
        });

        ClassBuilder.Register(() => IconPosition is BitIconPosition.End ? "bit-btn-eni" : string.Empty);

        ClassBuilder.Register(() => Rounded ? "bit-btn-rnd" : string.Empty);

        ClassBuilder.Register(() => FixedColor ? "bit-btn-fxc" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-btn-flw" : string.Empty);

        ClassBuilder.Register(() => NoWrap ? "bit-btn-nwr" : string.Empty);

        ClassBuilder.Register(() => FloatAbsolute ? "bit-btn-fab"
                                  : Float ? "bit-btn-ffx" : string.Empty);

        // The grab cursor is the only thing that says a floating button can be moved before anyone tries.
        ClassBuilder.Register(() => Draggable && (Float || FloatAbsolute) ? "bit-btn-drg" : string.Empty);

        ClassBuilder.Register(() => (Float || FloatAbsolute) ? FloatPosition switch
        {
            BitPosition.TopLeft => "bit-btn-tlf",
            BitPosition.TopCenter => "bit-btn-tcr",
            BitPosition.TopRight => "bit-btn-trg",
            BitPosition.TopStart => "bit-btn-tst",
            BitPosition.TopEnd => "bit-btn-ten",
            BitPosition.CenterLeft => "bit-btn-clf",
            BitPosition.Center => "bit-btn-ctr",
            BitPosition.CenterRight => "bit-btn-crg",
            BitPosition.CenterStart => "bit-btn-cst",
            BitPosition.CenterEnd => "bit-btn-cen",
            BitPosition.BottomLeft => "bit-btn-blf",
            BitPosition.BottomCenter => "bit-btn-bcr",
            BitPosition.BottomRight => "bit-btn-brg",
            BitPosition.BottomStart => "bit-btn-bst",
            BitPosition.BottomEnd => "bit-btn-ben",
            _ => "bit-btn-brg"
        } : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => FloatOffset.HasValue() ? $"--bit-Button-float-offset:{FloatOffset}" : string.Empty);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitButtonParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        UpdateLoadingVisuals();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        // Dragging only means anything for a button that is positioned, so the two parameters are read
        // together. The result is compared against what the script was last told, rather than re-sent on
        // every render: enable() returns early on a second call, but disable() does not - a floating button
        // that is not draggable used to tear the listeners down once per render, and one that stopped
        // floating kept them for good.
        var draggable = Draggable && (Float || FloatAbsolute);

        if (draggable == _draggableEnabled) return;

        _draggableEnabled = draggable;

        if (draggable)
        {
            _dotnetObj ??= DotNetObjectReference.Create(this);

            await _js.BitDraggablesEnable(_Id, _dotnetObj);
        }
        else
        {
            await _js.BitDraggablesDisable(_Id);
        }
    }



    internal void OnSetHrefRelAndTarget()
    {
        if (Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        var rel = Rel.HasValue ? BitLinkRelUtils.GetRels(Rel.Value) : null;

        // protects against reverse-tabnabbing when opening the link in a new browsing context, unless the
        // author already asked for one of the two rels that close that hole.
        if (Target is "_blank" && (rel is null || (rel.Contains("noopener") is false && rel.Contains("noreferrer") is false)))
        {
            rel = rel.HasValue() ? $"{rel} noopener" : "noopener";
        }

        _rel = rel;
    }



    // The Target parameter's rel is resolved the moment the parameter is set. A target written by hand is not a
    // parameter and never reaches that path, so the reverse-tabnabbing guard is re-applied here against the target
    // the anchor actually renders; it is idempotent, so a rel that already carries one passes through untouched.
    private string? GetRel(string? target)
    {
        if (target is not "_blank") return _rel;

        if (Href.HasNoValue() || Href!.StartsWith('#')) return _rel;

        if (_rel is not null && (_rel.Contains("noopener") || _rel.Contains("noreferrer"))) return _rel;

        return _rel.HasValue() ? $"{_rel} noopener" : "noopener";
    }

    private string? GetTabIndex(bool ariaHidden)
    {
        // A control hidden from assistive technologies must not be reachable by Tab either, or a keyboard
        // user lands on something a screen reader has nothing to say about.
        if (ariaHidden) return "-1";

        // The hyphen-less TabIndex parameter is the one a page usually reaches for, but the attribute is also
        // splattable by its own name - and the attribute written here would otherwise overwrite that with null.
        var tabIndex = TabIndex ?? GetSplattedAttribute("tabindex");

        if (IsEnabled is false)
        {
            if (AllowDisabledFocus is false) return "-1";

            // anchors without an href are not focusable, so an explicit tabindex is required to keep them in the tab order
            return Href.HasValue() ? tabIndex ?? "0" : tabIndex;
        }

        // the href is removed while loading, so an explicit tabindex is required to keep the anchor focusable
        if (IsLoading && Href.HasValue()) return tabIndex ?? "0";

        // falls back to the browser default so the disabled state's tabindex does not stick around after re-enabling
        return tabIndex;
    }

    private string GetLabelPositionClass()
        => LoadingLabelPosition switch
        {
            BitLabelPosition.Top => "bit-btn-top",
            BitLabelPosition.Start => "bit-btn-srt",
            BitLabelPosition.End => "bit-btn-end",
            BitLabelPosition.Bottom => "bit-btn-btm",
            _ => "bit-btn-end"
        };

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (IsLoading && Reclickable is false) return;

        // The click that ends a drag is the drag's own, not a press of the button, so it is swallowed
        // before anything else happens - including the loading state, which would otherwise flash on
        // every drag of a Draggable floating button.
        if (_dragging)
        {
            _dragging = false;
            return;
        }

        var isLoading = IsLoading;

        if (AutoLoading)
        {
            if (await AssignIsLoading(true) is false) return;

            UpdateLoadingVisuals();
        }

        try
        {
            await OnClick.InvokeAsync(isLoading);
        }
        finally
        {
            // in a finally so that a handler that throws leaves the button clickable again instead of
            // stranding it in a loading state that nothing will ever clear.
            if (AutoLoading)
            {
                await AssignIsLoading(false);

                UpdateLoadingVisuals();
            }
        }
    }

    private void UpdateLoadingVisuals()
    {
        if (IsLoading)
        {
            if (_showLoading || _loadingDelayCts is not null) return;

            if (LoadingDelay < 1)
            {
                SetShowLoading(true);
                return;
            }

            _loadingDelayCts = new();
            _ = ShowLoadingAfterDelay(_loadingDelayCts.Token);
        }
        else
        {
            SetShowLoading(false);
            CancelLoadingDelay();
        }
    }

    private async Task ShowLoadingAfterDelay(CancellationToken token)
    {
        try
        {
            await Task.Delay(LoadingDelay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (IsDisposed || IsLoading is false || token.IsCancellationRequested) return;

        SetShowLoading(true);

        await InvokeAsync(StateHasChanged);
    }

    // The loading state changes the layout of the root element (the spinner is stacked over the content that
    // holds the size), so the class list has to be rebuilt whenever it flips - including the flip that comes
    // from the delay timer rather than from a parameter, which no [ResetClassBuilder] can see.
    private void SetShowLoading(bool value)
    {
        if (_showLoading == value) return;

        _showLoading = value;

        ClassBuilder.Reset();
    }

    private void CancelLoadingDelay()
    {
        if (_loadingDelayCts is null) return;

        _loadingDelayCts.Cancel();
        _loadingDelayCts.Dispose();
        _loadingDelayCts = null;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        CancelLoadingDelay();

        await base.DisposeAsync(disposing);

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitDraggablesDisable(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
