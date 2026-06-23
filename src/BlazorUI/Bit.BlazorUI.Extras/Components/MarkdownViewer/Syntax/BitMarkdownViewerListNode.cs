namespace Bit.BlazorUI;

/// <summary>An ordered or unordered list.</summary>
public sealed class BitMarkdownViewerListNode : BitMarkdownViewerMarkdownNode
{
    public bool Ordered { get; init; }
    /// <summary>Starting number for ordered lists.</summary>
    public int Start { get; init; } = 1;
    /// <summary>Tight lists render item text without wrapping &lt;p&gt; tags.</summary>
    public bool Tight { get; set; } = true;
    public List<BitMarkdownViewerListItemNode> Items { get; } = new();

    // Surface the list items themselves so generic traversal can visit each
    // BitMarkdownViewerListItemNode (their inner blocks are reached via BitMarkdownViewerListItemNode.ChildNodes).
    // A live view over Items (rather than a detached snapshot) keeps generic AST
    // rewrites of the item collection reflected on this node. The wrapper is cached
    // so repeated traversals don't allocate a new array/view on every access.
    private IList<BitMarkdownViewerMarkdownNode>[]? _childLists;
    public override IEnumerable<IList<BitMarkdownViewerMarkdownNode>> ChildLists
        => _childLists ??= new IList<BitMarkdownViewerMarkdownNode>[] { new BitMarkdownViewerListItemListView(Items) };
}
