namespace Bit.BlazorUI;

public class BitAppShellClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root of the BitAppShell.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the top area of the BitAppShell.
    /// </summary>
    public string? Top { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the center area of the BitAppShell.
    /// </summary>
    public string? Center { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the leading side inset bar of the BitAppShell, which is the one on
    /// the left of a left-to-right app shell and on the right of a right-to-left one.
    /// </summary>
    public string? Left { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the main container of the BitAppShell, which is the one part of it
    /// that scrolls and the one the content is rendered into.
    /// </summary>
    public string? Main { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the trailing side inset bar of the BitAppShell, which is the one on
    /// the right of a left-to-right app shell and on the left of a right-to-left one.
    /// </summary>
    public string? Right { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the bottom area of the BitAppShell.
    /// </summary>
    public string? Bottom { get; set; }
}
