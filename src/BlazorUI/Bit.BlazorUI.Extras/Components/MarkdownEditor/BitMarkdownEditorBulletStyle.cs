namespace Bit.BlazorUI;

/// <summary>
/// Which character the <see cref="BitMarkdownEditor"/> starts an unordered (or task) list item
/// with. Every one of them is a valid bullet; which to use is a house style.
/// </summary>
public enum BitMarkdownEditorBulletStyle
{
    /// <summary>
    /// <c>- item</c>. The default.
    /// </summary>
    Dash,

    /// <summary>
    /// <c>* item</c>.
    /// </summary>
    Asterisk,

    /// <summary>
    /// <c>+ item</c>.
    /// </summary>
    Plus
}
