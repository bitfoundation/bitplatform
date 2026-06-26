namespace Bit.BlazorUI;

/// <summary>
/// A Markdown extension (plugin). Implementations register block parsers, inline
/// parsers, delimiter processors, AST processors and/or renderers on the pipeline,
/// enabling a Markdown flavor beyond the basic CommonMark core.
/// </summary>
/// <remarks>
/// A built <see cref="BitMarkdownViewerPipeline"/> is immutable and is cached and shared
/// across concurrent parses, components and (in Blazor Server) circuits. Therefore every
/// component an extension registers — parsers, delimiter/AST processors and renderers —
/// MUST be stateless and thread-safe: they must not retain mutable state between calls and
/// must keep all per-parse/per-render state in the local <c>state</c>/<c>builder</c> objects
/// passed to them. If statelessness cannot be guaranteed, the extension must register a fresh
/// instance per registration rather than reusing a shared one.
/// </remarks>
public interface IBitMarkdownViewerExtension
{
    /// <summary>
    /// Registers this extension's components on the pipeline being built. The registered
    /// components must be stateless and thread-safe (see the remarks on
    /// <see cref="IBitMarkdownViewerExtension"/>).
    /// </summary>
    void Setup(BitMarkdownViewerPipelineBuilder builder);
}
