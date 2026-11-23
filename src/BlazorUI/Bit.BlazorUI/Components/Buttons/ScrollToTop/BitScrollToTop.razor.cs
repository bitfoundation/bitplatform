using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// BitScrollToTop is the button that takes the reader back to the top of a long page or pane: in the flow of the page (the
/// "back to top" of a footer), or floating in a corner of the screen and revealed once the reader has scrolled far enough
/// away from the top.
/// </summary>
/// <remarks>
/// It works before the app is interactive: the scroll, the reveal and the focus are done by a script of the library that
/// is loaded as the app starts and acts on the markup of the button (its data-bit-stt attributes), so a prerendered page
/// scrolls back up before its WebAssembly runtime or its circuit has started, with no handler of its own in .NET.
/// <br />
/// Once it has scrolled, it moves the keyboard focus to the top as well - onto the scrolling area itself, which is given a
/// tab stop only code can reach for that - so the next Tab goes on from the top of the page (a skip link, the header)
/// rather than from the bottom the button was pressed at.
/// </remarks>
public partial class BitScrollToTop : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the scroll to top component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple scroll to top components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitScrollToTopParams.ParamName)]
    public BitScrollToTopParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for the different parts of the scroll to top button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitScrollToTopClassStyles? Classes { get; set; }

    /// <summary>
    /// Floats the button over the content, pinned to a corner or an edge of the viewport (<see cref="FloatPosition"/>).
    /// </summary>
    /// <remarks>
    /// Left unset, the button sits in the flow of the page where it is written, as the "back to top" of a footer does.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Float { get; set; }

    /// <summary>
    /// Floats the button over the content, pinned to a corner or an edge of its nearest positioned ancestor
    /// (<see cref="FloatPosition"/>) rather than of the viewport.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="Float"/> when both are set, so a <see cref="BitParams"/> ancestor does not supply
    /// it to a button that sets <see cref="Float"/> itself.
    /// </remarks>
    [Parameter, ResetClassBuilder, Outranks(nameof(Float))]
    public bool FloatAbsolute { get; set; }

    /// <summary>
    /// The inset of a floating button from the edge it is pinned to.
    /// </summary>
    /// <remarks>
    /// Any CSS length (<c>1rem</c>, <c>5%</c>, a <c>calc()</c>), or a bare number, which is read as pixels. It wins over
    /// <c>--bit-ScrollToTop-float-offset</c>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? FloatOffset { get; set; }

    /// <summary>
    /// Where a floating button is pinned (<see cref="Float"/>, <see cref="FloatAbsolute"/>). Defaults to
    /// <see cref="BitPosition.BottomEnd"/>: the bottom right corner, and the bottom left one in a right-to-left page.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPosition? FloatPosition { get; set; }

    /// <summary>
    /// The icon of the button, provided as custom CSS classes of an external icon library.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </remarks>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the button, from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// A button with no <see cref="Text"/> shows an arrow pointing up (<c>Up</c>) when no icon is given, and one with a
    /// text shows no icon unless one is given.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// How far (in pixels) the scroll has to travel from the top before the button is revealed.
    /// </summary>
    /// <remarks>
    /// The button stays hidden - out of the view, the tab order and the accessibility tree - while the scrolling area is
    /// within this offset of its top, where there is nothing to go back to yet, and fades in once it is scrolled past it.
    /// It is hidden in the markup as well, so a prerendered page shows no button at its top before the script that
    /// reveals it has run.
    /// <br />
    /// When not set, the button is always shown.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public int? RevealOffset { get; set; }

    /// <summary>
    /// Gets or sets the CSS selector of the element whose scrolling the button scrolls back to its top, and reveals itself by.
    /// </summary>
    /// <remarks>
    /// By default the button finds its own scrolling area by walking up from itself, which covers the page and any pane
    /// the button sits inside. A button that does not sit inside the box it has to scroll - an app shell whose
    /// scrolling content is a sibling of the button - names that box here instead.
    /// <br />
    /// A selector that matches nothing falls back to the walk, so a target rendered later never leaves the button
    /// without a scrolling area at all.
    /// </remarks>
    [Parameter] public string? ScrollTarget { get; set; }

    /// <summary>
    /// Scrolls back to the top smoothly rather than in a single jump.
    /// </summary>
    /// <remarks>
    /// Like every other animation in the library it collapses to an instant jump when the operating system or the
    /// browser reports 'prefers-reduced-motion: reduce', unless <see cref="BitComponentBase.ForceAnimation"/> opts this
    /// button out of that.
    /// </remarks>
    [Parameter] public bool Smooth { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for the different parts of the scroll to top button.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitScrollToTopClassStyles? Styles { get; set; }

    /// <summary>
    /// The text of the button, shown beside its icon, which also names the button.
    /// </summary>
    /// <remarks>
    /// When not set, the button shows its icon alone and is named by the <see cref="BitComponentBase.AriaLabel"/>, or else
    /// by <see cref="BitStrings.ScrollToTop"/> of <see cref="BitStrings.Current"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? Text { get; set; }



    /// <summary>
    /// The icon the button shows: the one given, or the arrow pointing up for a button with nothing else to show.
    /// </summary>
    private BitIconInfo? _Icon => BitIconInfo.From(Icon, IconName ?? (Text.HasValue() ? null : "Up"));

    /// <summary>
    /// The reveal offset the script reads, never negative: a negative offset would keep the button revealed at the top.
    /// </summary>
    private string? _RevealOffset => RevealOffset.HasValue
                                        ? Math.Max(0, RevealOffset.Value).ToString(CultureInfo.InvariantCulture)
                                        : null;



    protected override string RootElementClass => "bit-stt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // Nothing but the glyph to measure the box by, so the button is a square of the control height.
        ClassBuilder.Register(() => Text.HasValue() ? string.Empty : "bit-stt-ion");

        // Hidden from the first render on, so a prerendered page shows no button at its top before the script that
        // reveals it (data-bit-stt-shown) has run.
        ClassBuilder.Register(() => RevealOffset.HasValue ? "bit-stt-rvl" : string.Empty);

        ClassBuilder.Register(() => FloatAbsolute ? "bit-stt-fab"
                                  : Float ? "bit-stt-ffx" : string.Empty);

        ClassBuilder.Register(() => (Float || FloatAbsolute) ? FloatPosition switch
        {
            BitPosition.TopLeft => "bit-stt-tlf",
            BitPosition.TopCenter => "bit-stt-tcr",
            BitPosition.TopRight => "bit-stt-trg",
            BitPosition.TopStart => "bit-stt-tst",
            BitPosition.TopEnd => "bit-stt-ten",
            BitPosition.CenterLeft => "bit-stt-clf",
            BitPosition.Center => "bit-stt-ctr",
            BitPosition.CenterRight => "bit-stt-crg",
            BitPosition.CenterStart => "bit-stt-cst",
            BitPosition.CenterEnd => "bit-stt-cen",
            BitPosition.BottomLeft => "bit-stt-blf",
            BitPosition.BottomCenter => "bit-stt-bcr",
            BitPosition.BottomRight => "bit-stt-brg",
            BitPosition.BottomStart => "bit-stt-bst",
            _ => "bit-stt-ben"
        } : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => FloatOffset.HasValue() ? $"--bit-stt-flo:{NormalizeFloatOffset(FloatOffset!)}" : string.Empty);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    // A bare number is not a CSS length, and an invalid value takes down every inset that reads it, so a number - what
    // an offset bound to a numeric input arrives as - is read as pixels, as BitButton's FloatOffset is.
    private static string NormalizeFloatOffset(string floatOffset)
    {
        var offset = floatOffset.Trim();

        return double.TryParse(offset, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? $"{offset}px" : offset;
    }
}
