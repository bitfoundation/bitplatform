namespace Bit.BlazorUI;

/// <summary>
/// Post-processes the parsed AST. Used by flavors such as task lists, autolinks,
/// emoji and auto-identifiers that operate after the tree has been built.
/// </summary>
public abstract class BitMarkdownViewerAstProcessor
{
    /// <summary>Relative priority. Lower runs first.</summary>
    public virtual int Order => 100;

    public abstract void Process(BitMarkdownViewerDocumentNode document, BitMarkdownViewerPipeline pipeline);
}
