namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitHeader"/> component.
/// </summary>
public class BitHeaderParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitHeader"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitHeader value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitHeader)}";



    public string Name => ParamName;



    /// <summary>
    /// Renders the header with an absolute position at the top of its nearest positioned ancestor.
    /// </summary>
    public bool? Absolute { get; set; }

    /// <summary>
    /// Gets or sets the horizontal distribution of the content of the header.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Renders a divider line on the bottom edge of the header.
    /// </summary>
    public bool? Bordered { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the header.
    /// </summary>
    public BitHeaderClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the header.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// How far (in pixels) the scroll has to travel from the top before an ElevateOnScroll header lifts itself off the content.
    /// </summary>
    public int? ElevateOffset { get; set; }

    /// <summary>
    /// Keeps the header flat while the scrolling area sits at its top and lets it cast its shadow only once the content has been scrolled underneath it.
    /// </summary>
    public bool? ElevateOnScroll { get; set; }

    /// <summary>
    /// Renders the header with a shadow cast downwards.
    /// </summary>
    public bool? Elevated { get; set; }

    /// <summary>
    /// Renders the header with a fixed position at the top of the page.
    /// </summary>
    public bool? Fixed { get; set; }

    /// <summary>
    /// The space between the children of the header.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// The height of the header (in pixels).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Slides the header out of the view, and brings it back when it is turned off again.
    /// </summary>
    public bool? Hidden { get; set; }

    /// <summary>
    /// The maximum width of the content of the header, which is then centered in the header.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Removes the default paddings around the content of the header.
    /// </summary>
    public bool? NoGutter { get; set; }

    /// <summary>
    /// Slides the header out of the view while the page is scrolled down and brings it back while the page is scrolled up.
    /// </summary>
    public bool? Reveal { get; set; }

    /// <summary>
    /// How far (in pixels) the scroll has to travel from the top before a Reveal header starts hiding itself.
    /// </summary>
    public int? RevealOffset { get; set; }

    /// <summary>
    /// Reserves the height of the header at the top of the scrolling area, so nothing scrolled to ever lands underneath a pinned header.
    /// </summary>
    public bool? ScrollPadding { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling drives the header.
    /// </summary>
    public string? ScrollTarget { get; set; }

    /// <summary>
    /// The size of the header, which determines the paddings around its content.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// The target of the skip link of the header, which is what makes it render at all.
    /// </summary>
    public string? SkipLinkHref { get; set; }

    /// <summary>
    /// The text of the skip link of the header.
    /// </summary>
    public string? SkipLinkText { get; set; }

    /// <summary>
    /// Renders the header with a sticky position at the top of the viewport.
    /// </summary>
    public bool? Sticky { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the header.
    /// </summary>
    public BitHeaderClassStyles? Styles { get; set; }

    /// <summary>
    /// Softens the background of the header and blurs what passes behind it.
    /// </summary>
    public bool? Translucent { get; set; }

    /// <summary>
    /// The visual variant of the header.
    /// </summary>
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// The vertical alignment of the content of the header.
    /// </summary>
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Lets the content of the header wrap onto more than one line.
    /// </summary>
    public bool? Wrap { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitHeader"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitHeader"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitHeader"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitHeader"/>.
    /// </remarks>
    /// <param name="bitHeader">
    /// The <see cref="BitHeader"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitHeader bitHeader)
    {
        if (bitHeader is null) return;

        UpdateBaseParameters(bitHeader);

        // The three positions decide whether the header is pinned to the top of the screen, which is what
        // the safe area inset added to an explicit Height keys off, so each of them resets the styles as
        // well as the classes.
        if (Absolute.HasValue && bitHeader.HasNotBeenSet(nameof(Absolute)))
        {
            bitHeader.Absolute = Absolute.Value;

            bitHeader.ClassBuilder.Reset();
            bitHeader.StyleBuilder.Reset();
        }

        if (Alignment.HasValue && bitHeader.HasNotBeenSet(nameof(Alignment)))
        {
            bitHeader.Alignment = Alignment.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (Bordered.HasValue && bitHeader.HasNotBeenSet(nameof(Bordered)))
        {
            bitHeader.Bordered = Bordered.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (Classes is not null && bitHeader.HasNotBeenSet(nameof(Classes)))
        {
            bitHeader.Classes = Classes;

            bitHeader.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitHeader.HasNotBeenSet(nameof(Color)))
        {
            bitHeader.Color = Color.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (ElevateOffset.HasValue && bitHeader.HasNotBeenSet(nameof(ElevateOffset)))
        {
            bitHeader.ElevateOffset = ElevateOffset.Value;
        }

        if (ElevateOnScroll.HasValue && bitHeader.HasNotBeenSet(nameof(ElevateOnScroll)))
        {
            bitHeader.ElevateOnScroll = ElevateOnScroll.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (Elevated.HasValue && bitHeader.HasNotBeenSet(nameof(Elevated)))
        {
            bitHeader.Elevated = Elevated.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (Fixed.HasValue && bitHeader.HasNotBeenSet(nameof(Fixed)))
        {
            bitHeader.Fixed = Fixed.Value;

            bitHeader.ClassBuilder.Reset();
            bitHeader.StyleBuilder.Reset();
        }

        if (Gap is not null && bitHeader.HasNotBeenSet(nameof(Gap)))
        {
            bitHeader.Gap = Gap;

            bitHeader.StyleBuilder.Reset();
        }

        if (Height.HasValue && bitHeader.HasNotBeenSet(nameof(Height)))
        {
            bitHeader.Height = Height.Value;

            bitHeader.StyleBuilder.Reset();
        }

        if (Hidden.HasValue && bitHeader.HasNotBeenSet(nameof(Hidden)))
        {
            bitHeader.Hidden = Hidden.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (MaxWidth is not null && bitHeader.HasNotBeenSet(nameof(MaxWidth)))
        {
            bitHeader.MaxWidth = MaxWidth;

            bitHeader.StyleBuilder.Reset();
        }

        if (NoGutter.HasValue && bitHeader.HasNotBeenSet(nameof(NoGutter)))
        {
            bitHeader.NoGutter = NoGutter.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (Reveal.HasValue && bitHeader.HasNotBeenSet(nameof(Reveal)))
        {
            bitHeader.Reveal = Reveal.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (RevealOffset.HasValue && bitHeader.HasNotBeenSet(nameof(RevealOffset)))
        {
            bitHeader.RevealOffset = RevealOffset.Value;
        }

        if (ScrollPadding.HasValue && bitHeader.HasNotBeenSet(nameof(ScrollPadding)))
        {
            bitHeader.ScrollPadding = ScrollPadding.Value;
        }

        if (ScrollTarget is not null && bitHeader.HasNotBeenSet(nameof(ScrollTarget)))
        {
            bitHeader.ScrollTarget = ScrollTarget;
        }

        if (Size.HasValue && bitHeader.HasNotBeenSet(nameof(Size)))
        {
            bitHeader.Size = Size.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (SkipLinkHref is not null && bitHeader.HasNotBeenSet(nameof(SkipLinkHref)))
        {
            bitHeader.SkipLinkHref = SkipLinkHref;
        }

        if (SkipLinkText is not null && bitHeader.HasNotBeenSet(nameof(SkipLinkText)))
        {
            bitHeader.SkipLinkText = SkipLinkText;
        }

        if (Sticky.HasValue && bitHeader.HasNotBeenSet(nameof(Sticky)))
        {
            bitHeader.Sticky = Sticky.Value;

            bitHeader.ClassBuilder.Reset();
            bitHeader.StyleBuilder.Reset();
        }

        if (Styles is not null && bitHeader.HasNotBeenSet(nameof(Styles)))
        {
            bitHeader.Styles = Styles;

            bitHeader.StyleBuilder.Reset();
        }

        if (Translucent.HasValue && bitHeader.HasNotBeenSet(nameof(Translucent)))
        {
            bitHeader.Translucent = Translucent.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (Variant.HasValue && bitHeader.HasNotBeenSet(nameof(Variant)))
        {
            bitHeader.Variant = Variant.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (VerticalAlign.HasValue && bitHeader.HasNotBeenSet(nameof(VerticalAlign)))
        {
            bitHeader.VerticalAlign = VerticalAlign.Value;

            bitHeader.ClassBuilder.Reset();
        }

        if (Wrap.HasValue && bitHeader.HasNotBeenSet(nameof(Wrap)))
        {
            bitHeader.Wrap = Wrap.Value;

            bitHeader.ClassBuilder.Reset();
        }
    }
}
