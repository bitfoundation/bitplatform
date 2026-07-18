namespace Bit.BlazorUI;

/// <summary>
/// A Markdown extension (plugin). Implementations register block parsers, inline
/// parsers, delimiter processors, AST processors and/or renderers on the pipeline,
/// enabling a Markdown flavor beyond the basic CommonMark core.
/// </summary>
/// <remarks>
/// A built <see cref="BitMarkdownPipeline"/> is immutable and is cached and shared
/// across concurrent parses, components and (in Blazor Server) circuits. The pipeline also
/// caches and reuses every registration across parses and renders, so anything an extension
/// registers - parsers, delimiter/AST processors and renderers - MUST be stateless and
/// thread-safe: it must not retain mutable state between calls. All per-parse/per-render
/// state must live solely in the <c>state</c>/<c>builder</c> objects passed to each call.
/// Creating a new instance inside <see cref="Setup"/> does not make a stateful component safe,
/// because that single instance is still reused concurrently.
/// </remarks>
public interface IBitMarkdownExtension
{
    /// <summary>
    /// Registers this extension's components on the pipeline being built. The registered
    /// components must be stateless and thread-safe (see the remarks on
    /// <see cref="IBitMarkdownExtension"/>).
    /// </summary>
    void Setup(BitMarkdownPipelineBuilder builder);
}
