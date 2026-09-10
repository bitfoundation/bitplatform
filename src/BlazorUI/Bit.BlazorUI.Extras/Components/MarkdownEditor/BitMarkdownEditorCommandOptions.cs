namespace Bit.BlazorUI;

/// <summary>
/// The knobs the <see cref="BitMarkdownEditorCommands"/> transformations read: everything a
/// command needs to know that is not the text or the selection.
/// </summary>
public class BitMarkdownEditorCommandOptions
{
    /// <summary>
    /// The string inserted per indent level, used by Indent and Outdent. Defaults to two spaces.
    /// </summary>
    public string IndentUnit { get; set; } = "  ";

    /// <summary>
    /// How many columns the Table command's template has. Defaults to 2.
    /// </summary>
    public int TableColumns { get; set; } = 2;

    /// <summary>
    /// How many body rows the Table command's template has, beside its header row. Defaults to 1.
    /// </summary>
    public int TableRows { get; set; } = 1;

    /// <summary>
    /// The characters the Bold command wraps a selection in. Defaults to <c>**</c>.
    /// </summary>
    public BitMarkdownEditorEmphasisStyle BoldStyle { get; set; }

    /// <summary>
    /// The character the Italic command wraps a selection in. Defaults to <c>*</c>.
    /// </summary>
    public BitMarkdownEditorEmphasisStyle ItalicStyle { get; set; }

    /// <summary>
    /// The character an unordered or task list item starts with. Defaults to <c>-</c>.
    /// </summary>
    public BitMarkdownEditorBulletStyle BulletStyle { get; set; }

    internal string BoldMarker => BoldStyle is BitMarkdownEditorEmphasisStyle.Underscore ? "__" : "**";

    internal string ItalicMarker => ItalicStyle is BitMarkdownEditorEmphasisStyle.Underscore ? "_" : "*";

    internal string Bullet => BulletStyle switch
    {
        BitMarkdownEditorBulletStyle.Asterisk => "*",
        BitMarkdownEditorBulletStyle.Plus => "+",
        _ => "-"
    };

    internal static readonly BitMarkdownEditorCommandOptions Default = new();
}
