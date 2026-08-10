using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The BitFooter component renders a bar (with text and possibly other components) at the bottom of a site or an application.
/// </summary>
/// <remarks>
/// It renders a semantic <c>footer</c> element and lays its content out in a single horizontal line whose color, variant,
/// size, alignment and gutters are all parameters. It can stay in the flow of the page or be pinned to the bottom of the
/// viewport - <see cref="Fixed"/>, <see cref="Sticky"/>, or revealing itself only while the page is scrolled up
/// (<see cref="Reveal"/>).
/// </remarks>
public partial class BitFooter : BitComponentBase
{
    private bool _hidden;
    private bool _revealAttached;
    private DotNetObjectReference<BitFooter>? _dotnetObj;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the footer component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple footer components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitFooterParams.ParamName)]
    public BitFooterParams? CascadingParameters { get; set; }



    /// <summary>
    /// Gets or sets the horizontal distribution of the content of the BitFooter (the CSS justify-content of the container).
    /// </summary>
    /// <remarks>
    /// The content of the footer is laid out in a single horizontal flex line, so this controls how the remaining
    /// free space of that line is shared between and around the children.
    /// <br />
    /// Baseline and Stretch say nothing about distributing that free space, so those two act on the cross axis
    /// (the vertical alignment of the content) instead, which is centered by default.
    /// <br />
    /// When not set, the content keeps the browser default (packed to the start of the line).
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Renders a divider line on the top edge of the BitFooter to separate it from the content above.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Bordered { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered inside the BitFooter.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitFooter.
    /// </summary>
    [Parameter] public BitFooterClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the BitFooter.
    /// </summary>
    /// <remarks>
    /// The color is applied through the <see cref="Variant"/>: as the background color in the Fill variant,
    /// and as the text and border color in the Outline and Text variants.
    /// <br />
    /// When not set, the footer keeps the primary background and foreground colors of the current theme.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Renders the BitFooter with a shadow cast upwards, to lift it above the content it overlaps.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Elevated { get; set; }

    /// <summary>
    /// Renders the footer with a fixed position at the bottom of the page.
    /// </summary>
    /// <remarks>
    /// A fixed footer is taken out of the normal flow, so it overlaps the content it covers.
    /// Reserve room for it at the end of the page (for example with a bottom padding) to keep the last
    /// piece of content reachable.
    /// <br />
    /// Takes precedence over <see cref="Sticky"/> when both are set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Fixed { get; set; }

    /// <summary>
    /// Gets or sets the height of the BitFooter (in pixels).
    /// </summary>
    /// <remarks>
    /// The height includes the paddings and the border of the footer (the root element is a border-box).
    /// <br />
    /// When not set, the footer is as tall as its content plus the paddings of the current <see cref="Size"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Height { get; set; }

    /// <summary>
    /// Removes the default paddings around the content of the BitFooter, so it can span the full width of the footer.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoGutter { get; set; }

    /// <summary>
    /// Callback for when the reveal state of the footer changes. The provided value is true when the footer is revealed.
    /// </summary>
    /// <remarks>
    /// Only invoked while <see cref="Reveal"/> is enabled.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnRevealChanged { get; set; }

    /// <summary>
    /// Slides the footer out of the view while the page is scrolled down and brings it back while the page is scrolled up.
    /// </summary>
    /// <remarks>
    /// This only has an effect on a <see cref="Fixed"/> or <see cref="Sticky"/> footer, since a footer in the normal
    /// flow has nothing to slide over. The footer is always revealed at the very top and at the very end of the page.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reveal { get; set; }

    /// <summary>
    /// The size of the BitFooter, which determines the paddings around its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Renders the footer with a sticky position at the bottom of the viewport.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Fixed"/>, a sticky footer stays in the normal flow, so it never overlaps the content
    /// and needs no extra room reserved for it. It requires an ancestor that scrolls (commonly the page itself)
    /// and no ancestor with an overflow other than visible.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Sticky { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitFooter.
    /// </summary>
    [Parameter] public BitFooterClassStyles? Styles { get; set; }

    /// <summary>
    /// Softens the background of the BitFooter and blurs what passes behind it, for the frosted glass look
    /// of a footer pinned over scrolling content.
    /// </summary>
    /// <remarks>
    /// Only the Fill variant has a background to soften, so this has no effect on the Outline and Text variants,
    /// whose background is already transparent.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Translucent { get; set; }

    /// <summary>
    /// The visual variant of the BitFooter.
    /// </summary>
    /// <remarks>
    /// Fill (the default) paints the <see cref="Color"/> as the background of the footer, Outline keeps the
    /// background transparent and draws a border, and Text keeps the background transparent with no border.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Gets a value indicating whether the footer is currently revealed. It is always true unless <see cref="Reveal"/> is enabled.
    /// </summary>
    public bool IsRevealed => _hidden is false;



    /// <summary>
    /// Called by the reveal script of the footer when the scroll direction of the page flips.
    /// <br />
    /// <strong>This method is not intended to be called from application code.</strong>
    /// </summary>
    [JSInvokable("OnRevealChange")]
    public async Task _OnRevealChange(bool hidden)
    {
        if (_hidden == hidden) return;

        _hidden = hidden;

        ClassBuilder.Reset();

        await OnRevealChanged.InvokeAsync(hidden is false);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-ftr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-ftr-pri",
            BitColor.Secondary => "bit-ftr-sec",
            BitColor.Tertiary => "bit-ftr-ter",
            BitColor.Info => "bit-ftr-inf",
            BitColor.Success => "bit-ftr-suc",
            BitColor.Warning => "bit-ftr-wrn",
            BitColor.SevereWarning => "bit-ftr-swr",
            BitColor.Error => "bit-ftr-err",
            BitColor.PrimaryBackground => "bit-ftr-pbg",
            BitColor.SecondaryBackground => "bit-ftr-sbg",
            BitColor.TertiaryBackground => "bit-ftr-tbg",
            BitColor.PrimaryForeground => "bit-ftr-pfg",
            BitColor.SecondaryForeground => "bit-ftr-sfg",
            BitColor.TertiaryForeground => "bit-ftr-tfg",
            BitColor.PrimaryBorder => "bit-ftr-pbr",
            BitColor.SecondaryBorder => "bit-ftr-sbr",
            BitColor.TertiaryBorder => "bit-ftr-tbr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-ftr-fil",
            BitVariant.Outline => "bit-ftr-otl",
            BitVariant.Text => "bit-ftr-txt",
            _ => "bit-ftr-fil"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ftr-sm",
            BitSize.Medium => "bit-ftr-md",
            BitSize.Large => "bit-ftr-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Alignment switch
        {
            BitAlignment.Start => "bit-ftr-srt",
            BitAlignment.End => "bit-ftr-end",
            BitAlignment.Center => "bit-ftr-cnt",
            BitAlignment.SpaceBetween => "bit-ftr-sbt",
            BitAlignment.SpaceAround => "bit-ftr-sar",
            BitAlignment.SpaceEvenly => "bit-ftr-sev",
            BitAlignment.Baseline => "bit-ftr-bsl",
            BitAlignment.Stretch => "bit-ftr-str",
            _ => string.Empty
        });

        // Fixed takes the footer out of the flow, sticky keeps it in it, so the two positions cannot be
        // combined: Fixed wins and Sticky is ignored when both are set.
        ClassBuilder.Register(() => Fixed ? "bit-ftr-fix" : (Sticky ? "bit-ftr-stk" : string.Empty));

        ClassBuilder.Register(() => Translucent ? "bit-ftr-trs" : string.Empty);

        ClassBuilder.Register(() => Bordered ? "bit-ftr-brd" : string.Empty);

        ClassBuilder.Register(() => Elevated ? "bit-ftr-elv" : string.Empty);

        ClassBuilder.Register(() => NoGutter ? "bit-ftr-ngt" : string.Empty);

        ClassBuilder.Register(() => Reveal ? "bit-ftr-rvl" : string.Empty);

        ClassBuilder.Register(() => (Reveal && _hidden) ? "bit-ftr-hdn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Height.HasValue ? $"height:{Height}px" : string.Empty);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFooterParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        // Only a positioned footer has anything to slide over, so the scroll listener is attached for
        // those alone. Toggling any of these parameters at runtime attaches or detaches it accordingly.
        // The attached flag keeps every other render (including the ones the reveal itself causes) from
        // tearing the listener down and setting it up again.
        var shouldAttach = Reveal && (Fixed || Sticky);

        if (shouldAttach == _revealAttached) return;

        _revealAttached = shouldAttach;

        if (shouldAttach)
        {
            _dotnetObj ??= DotNetObjectReference.Create(this);

            await _js.BitFootersSetup(_Id, _dotnetObj);
        }
        else
        {
            await _js.BitFootersDispose(_Id);

            // The footer is no longer driven by the script, so it must not stay stuck in the hidden state.
            if (_hidden)
            {
                _hidden = false;

                ClassBuilder.Reset();

                StateHasChanged();
            }
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is null) return;

        _dotnetObj.Dispose();
        _dotnetObj = null;

        try
        {
            await _js.BitFootersDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
