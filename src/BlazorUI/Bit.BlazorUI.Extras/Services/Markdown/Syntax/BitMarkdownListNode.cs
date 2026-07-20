namespace Bit.BlazorUI;

/// <summary>An ordered or unordered list.</summary>
public sealed class BitMarkdownListNode : BitMarkdownNode
{
    public bool Ordered { get; init; }
    /// <summary>Starting number for ordered lists.</summary>
    public int Start { get; init; } = 1;
    /// <summary>Tight lists render item text without wrapping &lt;p&gt; tags.</summary>
    public bool Tight { get; set; } = true;
    public List<BitMarkdownListItemNode> Items { get; } = new();

    // Surface the list items themselves so generic traversal can visit each
    // BitMarkdownListItemNode (their inner blocks are reached via BitMarkdownListItemNode.ChildNodes).
    // A live view over Items (rather than a detached snapshot) keeps generic AST
    // rewrites of the item collection reflected on this node. The wrapper is cached
    // so repeated traversals don't allocate a new array/view on every access.
    private IList<BitMarkdownNode>[]? _childLists;
    public override IEnumerable<IList<BitMarkdownNode>> ChildLists
        => _childLists ??= new IList<BitMarkdownNode>[] { new BitMarkdownListItemListView(Items) };
}
