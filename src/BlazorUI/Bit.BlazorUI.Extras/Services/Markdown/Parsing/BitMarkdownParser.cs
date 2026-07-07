namespace Bit.BlazorUI;

/// <summary>Convenience entry point for parsing Markdown into an AST.</summary>
public static class BitMarkdownViewerParser
{
    /// <summary>
    /// Parses Markdown using the supplied pipeline, or <see cref="BitMarkdownViewerPipeline.Basic"/>
    /// (basic CommonMark core only) when none is given.
    /// </summary>
    public static BitMarkdownViewerDocumentNode Parse(string? markdown, BitMarkdownViewerPipeline? pipeline = null)
        => (pipeline ?? BitMarkdownViewerPipeline.Basic).Parse(markdown);
}
