namespace Bit.BlazorUI;

/// <summary>
/// A Markdown extension (plugin). Implementations register block parsers, inline
/// parsers, delimiter processors, AST processors and/or renderers on the pipeline,
/// enabling a Markdown flavor beyond the basic CommonMark core.
/// </summary>
public interface IBitMarkdownExtension
{
    /// <summary>Registers this extension's components on the pipeline being built.</summary>
    void Setup(BitMarkdownPipelineBuilder builder);
}
