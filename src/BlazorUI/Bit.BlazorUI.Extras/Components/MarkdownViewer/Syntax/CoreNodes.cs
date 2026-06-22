namespace Bit.BlazorUI.Markdown.Syntax;

// ---------------------------------------------------------------------------
// Core (basic CommonMark) node types. Flavor-specific nodes live alongside their
// extensions in the Bit.BlazorUI.Markdown.Extensions namespace.
// ---------------------------------------------------------------------------

/// <summary>Root of a parsed document.</summary>
public sealed class DocumentNode : MarkdownNode
{
    public List<MarkdownNode> Children { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Children;
}

/// <summary>An ATX (<c># Heading</c>) or setext heading. <see cref="Level"/> is 1-6.</summary>
public sealed class HeadingNode : MarkdownNode
{
    public int Level { get; init; }
    /// <summary>Optional element id (e.g. set by the auto-identifier extension).</summary>
    public string? Id { get; set; }
    public List<MarkdownNode> Inlines { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Inlines;
}

/// <summary>A paragraph of inline content.</summary>
public sealed class ParagraphNode : MarkdownNode
{
    public List<MarkdownNode> Inlines { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Inlines;
}

/// <summary>A fenced or indented code block, rendered verbatim.</summary>
public sealed class CodeBlockNode : MarkdownNode
{
    /// <summary>The info string of a fenced block (e.g. the language), or null.</summary>
    public string? Info { get; init; }
    public string Content { get; init; } = string.Empty;
}

/// <summary>A block quote containing nested blocks.</summary>
public sealed class BlockquoteNode : MarkdownNode
{
    public List<MarkdownNode> Children { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Children;
}

/// <summary>An ordered or unordered list.</summary>
public sealed class ListNode : MarkdownNode
{
    public bool Ordered { get; init; }
    /// <summary>Starting number for ordered lists.</summary>
    public int Start { get; init; } = 1;
    /// <summary>Tight lists render item text without wrapping &lt;p&gt; tags.</summary>
    public bool Tight { get; set; } = true;
    public List<ListItemNode> Items { get; } = new();

    // Surface the list items themselves so generic traversal can visit each
    // ListItemNode (their inner blocks are reached via ListItemNode.ChildNodes).
    // A live view over Items (rather than a detached snapshot) keeps generic AST
    // rewrites of the item collection reflected on this node.
    public override IEnumerable<IList<MarkdownNode>> ChildLists
        => new IList<MarkdownNode>[] { new ListItemListView(Items) };
}

/// <summary>
/// A live <see cref="IList{MarkdownNode}"/> view over a <see cref="ListNode"/>'s strongly
/// typed <see cref="ListNode.Items"/>. Exposing the real collection (instead of a copy)
/// lets generic AST processors add, remove, or replace list items and have those edits
/// reflected on the source. Non-<see cref="ListItemNode"/> insertions are rejected.
/// </summary>
internal sealed class ListItemListView(List<ListItemNode> items) : IList<MarkdownNode>
{
    private static ListItemNode AsItem(MarkdownNode node)
        => node as ListItemNode ?? throw new ArgumentException($"A {nameof(ListNode)} can only contain {nameof(ListItemNode)} children.", nameof(node));

    public MarkdownNode this[int index]
    {
        get => items[index];
        set => items[index] = AsItem(value);
    }

    public int Count => items.Count;
    public bool IsReadOnly => false;
    public void Add(MarkdownNode item) => items.Add(AsItem(item));
    public void Clear() => items.Clear();
    public bool Contains(MarkdownNode item) => item is ListItemNode li && items.Contains(li);
    public void CopyTo(MarkdownNode[] array, int arrayIndex) { foreach (var i in items) array[arrayIndex++] = i; }
    public int IndexOf(MarkdownNode item) => item is ListItemNode li ? items.IndexOf(li) : -1;
    public void Insert(int index, MarkdownNode item) => items.Insert(index, AsItem(item));
    public bool Remove(MarkdownNode item) => item is ListItemNode li && items.Remove(li);
    public void RemoveAt(int index) => items.RemoveAt(index);
    public IEnumerator<MarkdownNode> GetEnumerator() { foreach (var i in items) yield return i; }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>A single list item containing nested blocks.</summary>
public sealed class ListItemNode : MarkdownNode
{
    public List<MarkdownNode> Children { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Children;
}

/// <summary>A horizontal rule / thematic break.</summary>
public sealed class ThematicBreakNode : MarkdownNode
{
}

// -- Inline core nodes ------------------------------------------------------

/// <summary>Plain literal text.</summary>
public sealed class TextNode : MarkdownNode
{
    public string Text { get; set; } = string.Empty;
    public TextNode() { }
    public TextNode(string text) => Text = text;
}

/// <summary>Emphasis, rendered as <c>&lt;em&gt;</c>.</summary>
public sealed class EmphasisNode : MarkdownNode
{
    public List<MarkdownNode> Children { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Children;
}

/// <summary>Strong emphasis, rendered as <c>&lt;strong&gt;</c>.</summary>
public sealed class StrongNode : MarkdownNode
{
    public List<MarkdownNode> Children { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Children;
}

/// <summary>Inline code span.</summary>
public sealed class CodeSpanNode : MarkdownNode
{
    public string Content { get; init; } = string.Empty;
}

/// <summary>A hyperlink.</summary>
public sealed class LinkNode : MarkdownNode
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public List<MarkdownNode> Children { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Children;
}

/// <summary>An image.</summary>
public sealed class ImageNode : MarkdownNode
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string Alt { get; init; } = string.Empty;
}

/// <summary>A line break. Hard breaks render as <c>&lt;br /&gt;</c>.</summary>
public sealed class LineBreakNode : MarkdownNode
{
    public bool Hard { get; init; }
}
