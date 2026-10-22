namespace Bit.BlazorUI;

public class BitAiRendererBoundaryClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiRendererBoundary, rendered once its content has failed.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the notice (a BitTag) the BitAiRendererBoundary renders in place of its failed content.
    /// </summary>
    public BitTagClassStyles? Notice { get; set; }
}
