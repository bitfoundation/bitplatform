namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitText"/> component.
/// </summary>
public class BitTextParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the BitText cascading parameters within BitParams.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitText value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitText)}";



    public string Name => ParamName;



    /// <summary>
    /// Sets the horizontal alignment of the text content.
    /// </summary>
    public BitTextAlign? Align { get; set; }

    /// <summary>
    /// Sets the level of the heading the text is announced as, without changing the rendered tag.
    /// </summary>
    public int? AriaLevel { get; set; }

    /// <summary>
    /// Renders the text as a block level element.
    /// </summary>
    public bool? Block { get; set; }

    /// <summary>
    /// Breaks a word that is too long for its line rather than letting it overflow.
    /// </summary>
    public bool? BreakWord { get; set; }

    /// <summary>
    /// The general color of the text.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    public string? Element { get; set; }

    /// <summary>
    /// Forces the text to always break at the end.
    /// </summary>
    public bool? ForceBreak { get; set; }

    /// <summary>
    /// The kind of the foreground color of the text.
    /// </summary>
    public BitColorKind? Foreground { get; set; }

    /// <summary>
    /// Paints the glyphs of the text with a CSS gradient instead of with a flat color.
    /// </summary>
    public string? Gradient { get; set; }

    /// <summary>
    /// If true, the text will have a bottom margin.
    /// </summary>
    public bool? Gutter { get; set; }

    /// <summary>
    /// Hyphenates the words that are broken across two lines.
    /// </summary>
    public bool? Hyphenate { get; set; }

    /// <summary>
    /// Renders the text in italics.
    /// </summary>
    public bool? Italic { get; set; }

    /// <summary>
    /// The language of the text, written as the "lang" attribute of the rendered element.
    /// </summary>
    public string? Lang { get; set; }

    /// <summary>
    /// Truncates the text after the given number of lines with an ellipsis.
    /// </summary>
    public int? LineClamp { get; set; }

    /// <summary>
    /// Renders the text in the theme's monospaced family.
    /// </summary>
    public bool? Monospace { get; set; }

    /// <summary>
    /// Prevents the text from being selected.
    /// </summary>
    public bool? NoSelect { get; set; }

    /// <summary>
    /// If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.
    /// Note that text overflow can only happen with block or inline-block level elements(the element needs to have a width in order to overflow).
    /// </summary>
    public bool? NoWrap { get; set; }

    /// <summary>
    /// Renders the digits of the text at a single width, so that they line up across the lines.
    /// </summary>
    public bool? Numeric { get; set; }

    /// <summary>
    /// Renders the line breaks and the runs of spaces of the content as they were written.
    /// </summary>
    public bool? PreserveWhitespace { get; set; }

    /// <summary>
    /// Draws a line through the text.
    /// </summary>
    public bool? Strikethrough { get; set; }

    /// <summary>
    /// The capitalization of the text.
    /// </summary>
    public BitTextTransform? Transform { get; set; }

    /// <summary>
    /// Trims the half-leading off the top, the bottom or both edges of the box the text draws in.
    /// </summary>
    public BitTextTrim? Trim { get; set; }

    /// <summary>
    /// The typography of the text.
    /// </summary>
    public BitTypography? Typography { get; set; }

    /// <summary>
    /// Underlines the text.
    /// </summary>
    public bool? Underline { get; set; }

    /// <summary>
    /// Removes the text from the page while keeping it available to assistive technologies.
    /// </summary>
    public bool? VisuallyHidden { get; set; }

    /// <summary>
    /// How the lines of the text are broken.
    /// </summary>
    public BitTextWrap? Wrap { get; set; }

    /// <summary>
    /// The font weight of the text.
    /// </summary>
    public BitFontWeight? Weight { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitText"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitText"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitText"/> will be updated. 
    /// This method does not overwrite existing values on <paramref name="bitText"/>.
    /// </remarks>
    /// <param name="bitText">
    /// The <see cref="BitText"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitText bitText)
    {
        if (bitText is null) return;

        UpdateBaseParameters(bitText);

        if (Align.HasValue && bitText.HasNotBeenSet(nameof(Align)))
        {
            bitText.Align = Align.Value;

            bitText.StyleBuilder.Reset();
        }

        if (AriaLevel.HasValue && bitText.HasNotBeenSet(nameof(AriaLevel)))
        {
            bitText.AriaLevel = AriaLevel.Value;
        }

        if (Block.HasValue && bitText.HasNotBeenSet(nameof(Block)))
        {
            bitText.Block = Block.Value;

            bitText.ClassBuilder.Reset();
        }

        if (BreakWord.HasValue && bitText.HasNotBeenSet(nameof(BreakWord)))
        {
            bitText.BreakWord = BreakWord.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitText.HasNotBeenSet(nameof(Color)))
        {
            bitText.Color = Color.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Element.HasValue() && bitText.HasNotBeenSet(nameof(Element)))
        {
            bitText.Element = Element;
        }

        if (ForceBreak.HasValue && bitText.HasNotBeenSet(nameof(ForceBreak)))
        {
            bitText.ForceBreak = ForceBreak.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Foreground.HasValue && bitText.HasNotBeenSet(nameof(Foreground)))
        {
            bitText.Foreground = Foreground.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Gradient.HasValue() && bitText.HasNotBeenSet(nameof(Gradient)))
        {
            bitText.Gradient = Gradient;

            bitText.ClassBuilder.Reset();
            bitText.StyleBuilder.Reset();
        }

        if (Gutter.HasValue && bitText.HasNotBeenSet(nameof(Gutter)))
        {
            bitText.Gutter = Gutter.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Hyphenate.HasValue && bitText.HasNotBeenSet(nameof(Hyphenate)))
        {
            bitText.Hyphenate = Hyphenate.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Italic.HasValue && bitText.HasNotBeenSet(nameof(Italic)))
        {
            bitText.Italic = Italic.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Lang.HasValue() && bitText.HasNotBeenSet(nameof(Lang)))
        {
            bitText.Lang = Lang;
        }

        if (LineClamp.HasValue && bitText.HasNotBeenSet(nameof(LineClamp)))
        {
            bitText.LineClamp = LineClamp.Value;

            bitText.ClassBuilder.Reset();
            bitText.StyleBuilder.Reset();
        }

        if (Monospace.HasValue && bitText.HasNotBeenSet(nameof(Monospace)))
        {
            bitText.Monospace = Monospace.Value;

            bitText.ClassBuilder.Reset();
        }

        if (NoSelect.HasValue && bitText.HasNotBeenSet(nameof(NoSelect)))
        {
            bitText.NoSelect = NoSelect.Value;

            bitText.ClassBuilder.Reset();
        }

        if (NoWrap.HasValue && bitText.HasNotBeenSet(nameof(NoWrap)))
        {
            bitText.NoWrap = NoWrap.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Numeric.HasValue && bitText.HasNotBeenSet(nameof(Numeric)))
        {
            bitText.Numeric = Numeric.Value;

            bitText.ClassBuilder.Reset();
        }

        if (PreserveWhitespace.HasValue && bitText.HasNotBeenSet(nameof(PreserveWhitespace)))
        {
            bitText.PreserveWhitespace = PreserveWhitespace.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Strikethrough.HasValue && bitText.HasNotBeenSet(nameof(Strikethrough)))
        {
            bitText.Strikethrough = Strikethrough.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Transform.HasValue && bitText.HasNotBeenSet(nameof(Transform)))
        {
            bitText.Transform = Transform.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Trim.HasValue && bitText.HasNotBeenSet(nameof(Trim)))
        {
            bitText.Trim = Trim.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Typography.HasValue && bitText.HasNotBeenSet(nameof(Typography)))
        {
            bitText.Typography = Typography.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Underline.HasValue && bitText.HasNotBeenSet(nameof(Underline)))
        {
            bitText.Underline = Underline.Value;

            bitText.ClassBuilder.Reset();
        }

        if (VisuallyHidden.HasValue && bitText.HasNotBeenSet(nameof(VisuallyHidden)))
        {
            bitText.VisuallyHidden = VisuallyHidden.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Wrap.HasValue && bitText.HasNotBeenSet(nameof(Wrap)))
        {
            bitText.Wrap = Wrap.Value;

            bitText.ClassBuilder.Reset();
        }

        if (Weight.HasValue && bitText.HasNotBeenSet(nameof(Weight)))
        {
            bitText.Weight = Weight.Value;

            bitText.ClassBuilder.Reset();
        }
    }
}
