using Bit.BlazorUI.Markdown.Syntax;

namespace Bit.BlazorUI;

/// <summary>Convenience entry point for parsing Markdown into an AST.</summary>
public static class BitMarkdownParser
{
    /// <summary>
    /// Parses Markdown using the supplied pipeline, or <see cref="BitMarkdownPipeline.Basic"/>
    /// (basic CommonMark core only) when none is given.
    /// </summary>
    public static DocumentNode Parse(string? markdown, BitMarkdownPipeline? pipeline = null)
        => (pipeline ?? BitMarkdownPipeline.Basic).Parse(markdown);
}
