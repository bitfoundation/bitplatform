namespace Bit.BlazorUI;

/// <summary>
/// The custom CSS classes or styles of the parts of the <see cref="BitSplitter"/> component.
/// </summary>
public class BitSplitterClassStyles
{
    /// <summary>
    /// The custom CSS class/style for the root element of the BitSplitter.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// The custom CSS class/style for the control on the gutter that folds the first panel of the BitSplitter
    /// away and brings it back.
    /// </summary>
    public string? CollapseButton { get; set; }

    /// <summary>
    /// The custom CSS class/style for the icon of the control on the gutter that folds the first panel of the
    /// BitSplitter away and brings it back.
    /// </summary>
    public string? CollapseButtonIcon { get; set; }

    /// <summary>
    /// The custom CSS class/style for the first panel of the BitSplitter.
    /// </summary>
    public string? FirstPanel { get; set; }

    /// <summary>
    /// The custom CSS class/style for the gutter (the separator) of the BitSplitter.
    /// </summary>
    public string? Gutter { get; set; }

    /// <summary>
    /// The custom CSS class/style for the icon rendered inside the gutter of the BitSplitter.
    /// </summary>
    public string? GutterIcon { get; set; }

    /// <summary>
    /// The custom CSS class/style for the default grip indicator rendered inside the gutter of the BitSplitter.
    /// </summary>
    public string? GutterIndicator { get; set; }

    /// <summary>
    /// The custom CSS class/style for the line a lazy drag moves in place of the panels of the BitSplitter.
    /// </summary>
    public string? Preview { get; set; }

    /// <summary>
    /// The custom CSS class/style for the second panel of the BitSplitter.
    /// </summary>
    public string? SecondPanel { get; set; }
}
