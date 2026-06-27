namespace Bit.BlazorUI;

/// <summary>
/// Base type for every node produced by the parser. Nodes expose their mutable
/// child collections through <see cref="ChildNodes"/> / <see cref="ChildLists"/>
/// so that AST processors (plugins) can traverse and rewrite the tree generically,
/// even for node types they did not define.
/// </summary>
public abstract class BitMarkdownViewerMarkdownNode
{
    /// <summary>
    /// The node's single child collection, if it has exactly one. Container nodes
    /// override this; leaf nodes return <c>null</c>.
    /// </summary>
    public virtual IList<BitMarkdownViewerMarkdownNode>? ChildNodes => null;

    /// <summary>
    /// All mutable child collections owned by this node. Defaults to the single
    /// <see cref="ChildNodes"/> collection; nodes with several (e.g. a table's cells)
    /// override this to expose each one.
    /// </summary>
    public virtual IEnumerable<IList<BitMarkdownViewerMarkdownNode>> ChildLists
        => ChildNodes is { } c ? new[] { c } : Array.Empty<IList<BitMarkdownViewerMarkdownNode>>();
}
