namespace Bit.BlazorUI;

public class BitAiArtifactPartViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiArtifactPartView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiArtifactView the BitAiArtifactPartView shows the artifact in.
    /// </summary>
    public BitAiArtifactViewClassStyles? ArtifactView { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitTag the BitAiArtifactPartView shows when the conversation does not hold the artifact.
    /// </summary>
    public BitTagClassStyles? Placeholder { get; set; }
}
