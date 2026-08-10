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
    /// Renders the footer with a shadow cast upwards.
    /// </summary>
    public bool? Elevated { get; set; }

    /// <summary>
    /// Renders the footer with a fixed position at the bottom of the page.
    /// </summary>
    public bool? Fixed { get; set; }

    /// <summary>
    /// The height of the footer (in pixels).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Removes the default paddings around the content of the footer.
    /// </summary>
    public bool? NoGutter { get; set; }

    /// <summary>
    /// Slides the footer out of the view while the page is scrolled down and brings it back while the page is scrolled up.
    /// </summary>
    public bool? Reveal { get; set; }

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

        if (Alignment.HasValue && bitFooter.HasNotBeenSet(nameof(Alignment)))
        {
            bitFooter.Alignment = Alignment.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Bordered.HasValue && bitFooter.HasNotBeenSet(nameof(Bordered)))
        {
            bitFooter.Bordered = Bordered.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Classes is not null && bitFooter.HasNotBeenSet(nameof(Classes)))
        {
            bitFooter.Classes = Classes;

            bitFooter.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitFooter.HasNotBeenSet(nameof(Color)))
        {
            bitFooter.Color = Color.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Elevated.HasValue && bitFooter.HasNotBeenSet(nameof(Elevated)))
        {
            bitFooter.Elevated = Elevated.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Fixed.HasValue && bitFooter.HasNotBeenSet(nameof(Fixed)))
        {
            bitFooter.Fixed = Fixed.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Height.HasValue && bitFooter.HasNotBeenSet(nameof(Height)))
        {
            bitFooter.Height = Height.Value;

            bitFooter.StyleBuilder.Reset();
        }

        if (NoGutter.HasValue && bitFooter.HasNotBeenSet(nameof(NoGutter)))
        {
            bitFooter.NoGutter = NoGutter.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Reveal.HasValue && bitFooter.HasNotBeenSet(nameof(Reveal)))
        {
            bitFooter.Reveal = Reveal.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitFooter.HasNotBeenSet(nameof(Size)))
        {
            bitFooter.Size = Size.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Sticky.HasValue && bitFooter.HasNotBeenSet(nameof(Sticky)))
        {
            bitFooter.Sticky = Sticky.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Styles is not null && bitFooter.HasNotBeenSet(nameof(Styles)))
        {
            bitFooter.Styles = Styles;

            bitFooter.StyleBuilder.Reset();
        }

        if (Translucent.HasValue && bitFooter.HasNotBeenSet(nameof(Translucent)))
        {
            bitFooter.Translucent = Translucent.Value;

            bitFooter.ClassBuilder.Reset();
        }

        if (Variant.HasValue && bitFooter.HasNotBeenSet(nameof(Variant)))
        {
            bitFooter.Variant = Variant.Value;

            bitFooter.ClassBuilder.Reset();
        }
    }
}
