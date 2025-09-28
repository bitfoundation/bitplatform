namespace Bit.BlazorUI;

/// <summary>
/// Represents a Quill custom module specifications.
/// </summary>
public class BitRichTextEditorModule
{
    /// <summary>
    /// The name of the Quill custom module.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The script src of the Quill custom module to load at firt render.
    /// </summary>
    public required string Src { get; set; }

    /// <summary>
    /// The configuration object that applies the settings of the Quill custom module.
    /// </summary>
    public required object Config { get; set; }
}
