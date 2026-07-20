using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// Buttons enable users to take actions with a single tap. They're commonly used in forms, dialog panels, and specialized for tasks like navigation or repeated actions.
/// </summary>
public partial class BitButton : BitComponentBase
{
    private string? _rel;
    private string? _tabIndex;
    private bool _dragging;
    private BitButtonType _buttonType;
    private DotNetObjectReference<BitButton>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>.
    /// The value is coming from the cascading value provided by the EditForm.
    /// </summary>
    [CascadingParameter] public EditContext? EditContext { get; set; }



    /// <summary>
    /// Keeps the disabled button focusable and discoverable by screen readers, rendering <c>aria-disabled</c> instead of the
    /// native <c>disabled</c> attribute when <see cref="BitComponentBase.IsEnabled"/> is false, preserving a consistent tab order.
    /// Set it to false to render the native <c>disabled</c> attribute and remove the button from the tab order.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; } = true;

    /// <summary>
    /// Detailed description of the button for the benefit of screen readers (rendered into <c>aria-describedby</c>).
    /// </summary>
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
    /// Makes the Float/FloatAbsolute button draggable on the page.
    /// </summary>
    [Parameter] public bool Draggable { get; set; }

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
    [CallOnSet(nameof(OnSetHrefAndRel))]
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
    /// The loading label text to show next to the spinner icon.
    /// </summary>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The position of the loading Label in regards to the spinner icon.
    /// </summary>
    [Parameter] public BitLabelPosition LoadingLabelPosition { get; set; } = BitLabelPosition.End;

    /// <summary>
    /// The custom template used to replace the default loading text inside the button in the loading state.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

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
    [Parameter] public bool Reclickable { get; set; }

    /// <summary>
    /// Sets the <c>rel</c> attribute for link-rendered buttons when <see cref="Href"/> is a non-anchor URL; ignored for empty or hash-only hrefs.
    /// The <c>rel</c> attribute specifies the relationship between the current document and the linked document.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRels? Rel { get; set; }

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
    [CallOnSet(nameof(OnSetHrefAndRel))]
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



    [JSInvokable("OnDragStart")]
    public async ValueTask _OnDragStart()
    {
        //_dragging = true;
    }

    [JSInvokable("OnDragging")]
    public async ValueTask _OnDragging()
    {
        _dragging = true;
    }

    [JSInvokable("OnDragEnd")]
    public async ValueTask _OnDragEnd()
    {
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

        ClassBuilder.Register(() => IsLoading ? "bit-btn-lda" : string.Empty);

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

        ClassBuilder.Register(() => FixedColor ? "bit-btn-fxc" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-btn-flw" : string.Empty);

        ClassBuilder.Register(() => FloatAbsolute ? "bit-btn-fab"
                                  : Float ? "bit-btn-ffx" : string.Empty);

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

        StyleBuilder.Register(() => FloatOffset.HasValue() ? $"--bit-btn-float-offset:{FloatOffset}" : string.Empty);
    }

    protected override void OnParametersSet()
    {
        if (IsEnabled is false)
        {
            // anchors without an href are not focusable, so an explicit tabindex is required to keep them in the tab order
            _tabIndex = AllowDisabledFocus ? (Href.HasValue() ? "0" : null) : "-1";
        }
        else if (Href.HasValue() && IsLoading)
        {
            // the href is removed while loading, so an explicit tabindex is required to keep the anchor focusable
            _tabIndex = TabIndex ?? "0";
        }
        else
        {
            // falls back to the browser default so the disabled state's tabindex does not stick around after re-enabling
            _tabIndex = TabIndex;
        }

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (Float || FloatAbsolute)
        {
            if (IsDisposed) return;

            if (Draggable)
            {
                _dotnetObj ??= DotNetObjectReference.Create(this);

                await _js.BitDraggablesEnable(_Id, _dotnetObj);
            }
            else
            {
                await _js.BitDraggablesDisable(_Id);
            }
        }
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

        var isLoading = IsLoading;

        if (AutoLoading)
        {
            if (await AssignIsLoading(true) is false) return;
        }

        if (_dragging)
        {
            _dragging = false;
        }
        else
        {
            await OnClick.InvokeAsync(isLoading);
        }

        if (AutoLoading)
        {
            await AssignIsLoading(false);
        }
    }

    private void OnSetHrefAndRel()
    {
        if (Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        if (Rel.HasValue)
        {
            _rel = BitLinkRelUtils.GetRels(Rel.Value);
            return;
        }

        // protects against reverse-tabnabbing when opening the link in a new browsing context
        _rel = Target == "_blank" ? "noopener" : null;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

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
