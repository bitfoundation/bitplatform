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
        if (Absolute.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Absolute), Absolute.Value, static h => h.Absolute, static (h, v) => h.Absolute = v);
        }

        if (Alignment.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Alignment), Alignment.Value, static h => h.Alignment, static (h, v) => h.Alignment = v);
        }

        if (Bordered.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Bordered), Bordered.Value, static h => h.Bordered, static (h, v) => h.Bordered = v);
        }

        if (Classes is not null)
        {
            bitHeader.TakeFromCascade(nameof(Classes), Classes, static h => h.Classes, static (h, v) => h.Classes = v);
        }

        if (Color.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Color), Color.Value, static h => h.Color, static (h, v) => h.Color = v);
        }

        if (ElevateOffset.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(ElevateOffset), ElevateOffset.Value, static h => h.ElevateOffset, static (h, v) => h.ElevateOffset = v);
        }

        if (ElevateOnScroll.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(ElevateOnScroll), ElevateOnScroll.Value, static h => h.ElevateOnScroll, static (h, v) => h.ElevateOnScroll = v);
        }

        if (Elevated.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Elevated), Elevated.Value, static h => h.Elevated, static (h, v) => h.Elevated = v);
        }

        if (Fixed.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Fixed), Fixed.Value, static h => h.Fixed, static (h, v) => h.Fixed = v);
        }

        if (Gap is not null)
        {
            bitHeader.TakeFromCascade(nameof(Gap), Gap, static h => h.Gap, static (h, v) => h.Gap = v);
        }

        if (Height.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Height), Height.Value, static h => h.Height, static (h, v) => h.Height = v);
        }

        if (Hidden.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Hidden), Hidden.Value, static h => h.Hidden, static (h, v) => h.Hidden = v);
        }

        if (MaxWidth is not null)
        {
            bitHeader.TakeFromCascade(nameof(MaxWidth), MaxWidth, static h => h.MaxWidth, static (h, v) => h.MaxWidth = v);
        }

        if (NoGutter.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(NoGutter), NoGutter.Value, static h => h.NoGutter, static (h, v) => h.NoGutter = v);
        }

        if (Reveal.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Reveal), Reveal.Value, static h => h.Reveal, static (h, v) => h.Reveal = v);
        }

        if (RevealOffset.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(RevealOffset), RevealOffset.Value, static h => h.RevealOffset, static (h, v) => h.RevealOffset = v);
        }

        if (ScrollPadding.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(ScrollPadding), ScrollPadding.Value, static h => h.ScrollPadding, static (h, v) => h.ScrollPadding = v);
        }

        if (ScrollTarget is not null)
        {
            bitHeader.TakeFromCascade(nameof(ScrollTarget), ScrollTarget, static h => h.ScrollTarget, static (h, v) => h.ScrollTarget = v);
        }

        if (Size.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Size), Size.Value, static h => h.Size, static (h, v) => h.Size = v);
        }

        if (SkipLinkHref is not null)
        {
            bitHeader.TakeFromCascade(nameof(SkipLinkHref), SkipLinkHref, static h => h.SkipLinkHref, static (h, v) => h.SkipLinkHref = v);
        }

        if (SkipLinkText is not null)
        {
            bitHeader.TakeFromCascade(nameof(SkipLinkText), SkipLinkText, static h => h.SkipLinkText, static (h, v) => h.SkipLinkText = v);
        }

        if (Sticky.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Sticky), Sticky.Value, static h => h.Sticky, static (h, v) => h.Sticky = v);
        }

        if (Styles is not null)
        {
            bitHeader.TakeFromCascade(nameof(Styles), Styles, static h => h.Styles, static (h, v) => h.Styles = v);
        }

        if (Translucent.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Translucent), Translucent.Value, static h => h.Translucent, static (h, v) => h.Translucent = v);
        }

        if (Variant.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Variant), Variant.Value, static h => h.Variant, static (h, v) => h.Variant = v);
        }

        if (VerticalAlign.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(VerticalAlign), VerticalAlign.Value, static h => h.VerticalAlign, static (h, v) => h.VerticalAlign = v);
        }

        if (Wrap.HasValue)
        {
            bitHeader.TakeFromCascade(nameof(Wrap), Wrap.Value, static h => h.Wrap, static (h, v) => h.Wrap = v);
        }
    }
}
