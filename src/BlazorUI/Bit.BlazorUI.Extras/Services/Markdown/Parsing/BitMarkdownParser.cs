namespace Bit.BlazorUI;

/// <summary>Convenience entry point for parsing Markdown into an AST.</summary>
public static class BitMarkdownParser
{
    /// <summary>
    /// Parses Markdown using the supplied pipeline, or <see cref="BitMarkdownPipelines.Basic"/>
    /// (basic CommonMark core only) when none is given.
    /// </summary>
    public static BitMarkdownDocumentNode Parse(string? markdown, BitMarkdownPipeline? pipeline = null)
        => (pipeline ?? BitMarkdownPipelines.Basic).Parse(markdown);
}
