namespace Bit.BlazorUI;

public class BitAiArtifactViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiArtifactView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the overlay laid over the page behind the full-screen panel of the BitAiArtifactView.
    /// </summary>
    public string? Overlay { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the panel - the header and the body - of the BitAiArtifactView.
    /// </summary>
    public string? Panel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header of the BitAiArtifactView.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the kind of the artifact in the header of the BitAiArtifactView.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title of the BitAiArtifactView.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitEllipsisLoading the header of the BitAiArtifactView shows while the artifact streams.
    /// </summary>
    public BitLoadingClassStyles? Loading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitDropdown that picks the version shown by the BitAiArtifactView.
    /// </summary>
    public BitDropdownClassStyles? VersionDropdown { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the download BitButton of the BitAiArtifactView.
    /// </summary>
    public BitButtonClassStyles? DownloadButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the full-screen toggle BitButton of the BitAiArtifactView.
    /// </summary>
    public BitButtonClassStyles? ExpandButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of the BitAiArtifactView.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of a document or a JSON tree in the body of the BitAiArtifactView.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image an image or an SVG artifact is shown as by the BitAiArtifactView.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the sandboxed frame an HTML artifact is previewed in by the BitAiArtifactView.
    /// </summary>
    public string? Frame { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the box of the table a CSV or TSV artifact is shown as by the BitAiArtifactView.
    /// </summary>
    public string? Table { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each header cell of the table of a CSV or TSV artifact of the BitAiArtifactView.
    /// </summary>
    public string? HeaderCell { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each body cell of the table of a CSV or TSV artifact of the BitAiArtifactView.
    /// </summary>
    public string? Cell { get; set; }
}
