namespace Bit.BlazorUI;

/// <summary>
/// Custom CSS classes/styles for different parts of the <see cref="BitRichTextEditor"/>.
/// </summary>
public class BitRichTextEditorClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root of the BitRichTextEditor.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the toolbar of the BitRichTextEditor.
    /// </summary>
    public string? Toolbar { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the toolbar groups of the BitRichTextEditor.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the toolbar buttons of the BitRichTextEditor.
    /// </summary>
    public string? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the editor (content) area of the BitRichTextEditor.
    /// </summary>
    public string? Editor { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the HTML source view textarea of the BitRichTextEditor.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the character/word count footer of the BitRichTextEditor.
    /// </summary>
    public string? Count { get; set; }
}
