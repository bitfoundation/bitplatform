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
    /// Breaks the text wherever the line runs out, even in the middle of a word.
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
    /// Keeps the text on a single line and ends it with an ellipsis where it does not fit.
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

        if (Align.HasValue)
        {
            bitText.TakeFromCascade(nameof(Align), Align.Value, static t => t.Align, static (t, v) => t.Align = v);
        }

        if (AriaLevel.HasValue)
        {
            bitText.TakeFromCascade(nameof(AriaLevel), AriaLevel.Value, static t => t.AriaLevel, static (t, v) => t.AriaLevel = v);
        }

        if (Block.HasValue)
        {
            bitText.TakeFromCascade(nameof(Block), Block.Value, static t => t.Block, static (t, v) => t.Block = v);
        }

        if (BreakWord.HasValue)
        {
            bitText.TakeFromCascade(nameof(BreakWord), BreakWord.Value, static t => t.BreakWord, static (t, v) => t.BreakWord = v);
        }

        if (Color.HasValue)
        {
            bitText.TakeFromCascade(nameof(Color), Color.Value, static t => t.Color, static (t, v) => t.Color = v);
        }

        if (Element.HasValue())
        {
            bitText.TakeFromCascade(nameof(Element), Element, static t => t.Element, static (t, v) => t.Element = v);
        }

        if (ForceBreak.HasValue)
        {
            bitText.TakeFromCascade(nameof(ForceBreak), ForceBreak.Value, static t => t.ForceBreak, static (t, v) => t.ForceBreak = v);
        }

        if (Foreground.HasValue)
        {
            bitText.TakeFromCascade(nameof(Foreground), Foreground.Value, static t => t.Foreground, static (t, v) => t.Foreground = v);
        }

        if (Gradient.HasValue())
        {
            bitText.TakeFromCascade(nameof(Gradient), Gradient, static t => t.Gradient, static (t, v) => t.Gradient = v);
        }

        if (Gutter.HasValue)
        {
            bitText.TakeFromCascade(nameof(Gutter), Gutter.Value, static t => t.Gutter, static (t, v) => t.Gutter = v);
        }

        if (Hyphenate.HasValue)
        {
            bitText.TakeFromCascade(nameof(Hyphenate), Hyphenate.Value, static t => t.Hyphenate, static (t, v) => t.Hyphenate = v);
        }

        if (Italic.HasValue)
        {
            bitText.TakeFromCascade(nameof(Italic), Italic.Value, static t => t.Italic, static (t, v) => t.Italic = v);
        }

        if (Lang.HasValue())
        {
            bitText.TakeFromCascade(nameof(Lang), Lang, static t => t.Lang, static (t, v) => t.Lang = v);
        }

        if (LineClamp.HasValue)
        {
            bitText.TakeFromCascade(nameof(LineClamp), LineClamp.Value, static t => t.LineClamp, static (t, v) => t.LineClamp = v);
        }

        if (Monospace.HasValue)
        {
            bitText.TakeFromCascade(nameof(Monospace), Monospace.Value, static t => t.Monospace, static (t, v) => t.Monospace = v);
        }

        if (NoSelect.HasValue)
        {
            bitText.TakeFromCascade(nameof(NoSelect), NoSelect.Value, static t => t.NoSelect, static (t, v) => t.NoSelect = v);
        }

        if (NoWrap.HasValue)
        {
            bitText.TakeFromCascade(nameof(NoWrap), NoWrap.Value, static t => t.NoWrap, static (t, v) => t.NoWrap = v);
        }

        if (Numeric.HasValue)
        {
            bitText.TakeFromCascade(nameof(Numeric), Numeric.Value, static t => t.Numeric, static (t, v) => t.Numeric = v);
        }

        if (PreserveWhitespace.HasValue)
        {
            bitText.TakeFromCascade(nameof(PreserveWhitespace), PreserveWhitespace.Value, static t => t.PreserveWhitespace, static (t, v) => t.PreserveWhitespace = v);
        }

        if (Strikethrough.HasValue)
        {
            bitText.TakeFromCascade(nameof(Strikethrough), Strikethrough.Value, static t => t.Strikethrough, static (t, v) => t.Strikethrough = v);
        }

        if (Transform.HasValue)
        {
            bitText.TakeFromCascade(nameof(Transform), Transform.Value, static t => t.Transform, static (t, v) => t.Transform = v);
        }

        if (Trim.HasValue)
        {
            bitText.TakeFromCascade(nameof(Trim), Trim.Value, static t => t.Trim, static (t, v) => t.Trim = v);
        }

        if (Typography.HasValue)
        {
            bitText.TakeFromCascade(nameof(Typography), Typography.Value, static t => t.Typography, static (t, v) => t.Typography = v);
        }

        if (Underline.HasValue)
        {
            bitText.TakeFromCascade(nameof(Underline), Underline.Value, static t => t.Underline, static (t, v) => t.Underline = v);
        }

        if (VisuallyHidden.HasValue)
        {
            bitText.TakeFromCascade(nameof(VisuallyHidden), VisuallyHidden.Value, static t => t.VisuallyHidden, static (t, v) => t.VisuallyHidden = v);
        }

        if (Wrap.HasValue)
        {
            bitText.TakeFromCascade(nameof(Wrap), Wrap.Value, static t => t.Wrap, static (t, v) => t.Wrap = v);
        }

        if (Weight.HasValue)
        {
            bitText.TakeFromCascade(nameof(Weight), Weight.Value, static t => t.Weight, static (t, v) => t.Weight = v);
        }
    }
}
