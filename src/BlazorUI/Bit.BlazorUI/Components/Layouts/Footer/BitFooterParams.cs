namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitFooter"/> component.
/// </summary>
public class BitFooterParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitFooter"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitFooter value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitFooter)}";



    public string Name => ParamName;



    /// <summary>
    /// Renders the footer with an absolute position at the bottom of its nearest positioned ancestor.
    /// </summary>
    public bool? Absolute { get; set; }

    /// <summary>
    /// Gets or sets the horizontal distribution of the content of the footer.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Renders a divider line on the top edge of the footer.
    /// </summary>
    public bool? Bordered { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the footer.
    /// </summary>
    public BitFooterClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the footer.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Keeps the footer flat at the end of its scrolling area and shadowed while content is left underneath it.
    /// </summary>
    public bool? ElevateOnScroll { get; set; }

    /// <summary>
    /// Renders the footer with a shadow cast upwards.
    /// </summary>
    public bool? Elevated { get; set; }

    /// <summary>
    /// Renders the footer with a fixed position at the bottom of the page.
    /// </summary>
    public bool? Fixed { get; set; }

    /// <summary>
    /// The space between the children of the footer.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// The height of the footer (in pixels).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Slides the footer out of the view, and brings it back when it is turned off again.
    /// </summary>
    public bool? Hidden { get; set; }

    /// <summary>
    /// The maximum width of the content of the footer, which is then centered in the footer.
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Removes the default paddings around the content of the footer.
    /// </summary>
    public bool? NoGutter { get; set; }

    /// <summary>
    /// Slides the footer out of the view while the page is scrolled down and brings it back while the page is scrolled up.
    /// </summary>
    public bool? Reveal { get; set; }

    /// <summary>
    /// How far (in pixels) the scroll has to travel from the top before a Reveal footer starts hiding itself.
    /// </summary>
    public int? RevealOffset { get; set; }

    /// <summary>
    /// Reserves the height of the footer at the bottom of the scrolling area, so nothing scrolled to lands underneath it.
    /// </summary>
    public bool? ScrollPadding { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling drives the footer.
    /// </summary>
    public string? ScrollTarget { get; set; }

    /// <summary>
    /// The size of the footer, which determines the paddings around its content.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Renders the footer with a sticky position at the bottom of the viewport.
    /// </summary>
    public bool? Sticky { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the footer.
    /// </summary>
    public BitFooterClassStyles? Styles { get; set; }

    /// <summary>
    /// Softens the background of the footer and blurs what passes behind it.
    /// </summary>
    public bool? Translucent { get; set; }

    /// <summary>
    /// The visual variant of the footer.
    /// </summary>
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// The vertical alignment of the content of the footer.
    /// </summary>
    public BitAlignment? VerticalAlign { get; set; }

    /// <summary>
    /// Lets the content of the footer wrap onto more than one line.
    /// </summary>
    public bool? Wrap { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitFooter"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitFooter"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitFooter"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitFooter"/>.
    /// </remarks>
    /// <param name="bitFooter">
    /// The <see cref="BitFooter"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitFooter bitFooter)
    {
        if (bitFooter is null) return;

        UpdateBaseParameters(bitFooter);

        if (Absolute.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Absolute), Absolute.Value, static f => f.Absolute, static (f, v) => f.Absolute = v);
        }

        if (Alignment.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Alignment), Alignment.Value, static f => f.Alignment, static (f, v) => f.Alignment = v);
        }

        if (Bordered.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Bordered), Bordered.Value, static f => f.Bordered, static (f, v) => f.Bordered = v);
        }

        if (Classes is not null)
        {
            bitFooter.TakeFromCascade(nameof(Classes), Classes, static f => f.Classes, static (f, v) => f.Classes = v);
        }

        if (Color.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Color), Color.Value, static f => f.Color, static (f, v) => f.Color = v);
        }

        if (ElevateOnScroll.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(ElevateOnScroll), ElevateOnScroll.Value, static f => f.ElevateOnScroll, static (f, v) => f.ElevateOnScroll = v);
        }

        if (Elevated.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Elevated), Elevated.Value, static f => f.Elevated, static (f, v) => f.Elevated = v);
        }

        if (Fixed.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Fixed), Fixed.Value, static f => f.Fixed, static (f, v) => f.Fixed = v);
        }

        if (Gap is not null)
        {
            bitFooter.TakeFromCascade(nameof(Gap), Gap, static f => f.Gap, static (f, v) => f.Gap = v);
        }

        if (Height.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Height), Height.Value, static f => f.Height, static (f, v) => f.Height = v);
        }

        if (Hidden.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Hidden), Hidden.Value, static f => f.Hidden, static (f, v) => f.Hidden = v);
        }

        if (MaxWidth is not null)
        {
            bitFooter.TakeFromCascade(nameof(MaxWidth), MaxWidth, static f => f.MaxWidth, static (f, v) => f.MaxWidth = v);
        }

        if (NoGutter.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(NoGutter), NoGutter.Value, static f => f.NoGutter, static (f, v) => f.NoGutter = v);
        }

        if (Reveal.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Reveal), Reveal.Value, static f => f.Reveal, static (f, v) => f.Reveal = v);
        }

        if (RevealOffset.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(RevealOffset), RevealOffset.Value, static f => f.RevealOffset, static (f, v) => f.RevealOffset = v);
        }

        if (ScrollPadding.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(ScrollPadding), ScrollPadding.Value, static f => f.ScrollPadding, static (f, v) => f.ScrollPadding = v);
        }

        if (ScrollTarget is not null)
        {
            bitFooter.TakeFromCascade(nameof(ScrollTarget), ScrollTarget, static f => f.ScrollTarget, static (f, v) => f.ScrollTarget = v);
        }

        if (Size.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Size), Size.Value, static f => f.Size, static (f, v) => f.Size = v);
        }

        if (Sticky.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Sticky), Sticky.Value, static f => f.Sticky, static (f, v) => f.Sticky = v);
        }

        if (Styles is not null)
        {
            bitFooter.TakeFromCascade(nameof(Styles), Styles, static f => f.Styles, static (f, v) => f.Styles = v);
        }

        if (Translucent.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Translucent), Translucent.Value, static f => f.Translucent, static (f, v) => f.Translucent = v);
        }

        if (Variant.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Variant), Variant.Value, static f => f.Variant, static (f, v) => f.Variant = v);
        }

        if (VerticalAlign.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(VerticalAlign), VerticalAlign.Value, static f => f.VerticalAlign, static (f, v) => f.VerticalAlign = v);
        }

        if (Wrap.HasValue)
        {
            bitFooter.TakeFromCascade(nameof(Wrap), Wrap.Value, static f => f.Wrap, static (f, v) => f.Wrap = v);
        }
    }
}
