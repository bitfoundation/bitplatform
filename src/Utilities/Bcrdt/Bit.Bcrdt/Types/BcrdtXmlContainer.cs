using System.Collections;
using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// What <see cref="BcrdtXmlFragment"/> and <see cref="BcrdtXmlElement"/> share: an ordered list of child
/// nodes.
/// </summary>
/// <remarks>
/// <para>
/// The children are the type's sequence, exactly as a <see cref="BcrdtArray"/>'s elements are, so
/// concurrent insertions keep each author's run together and a concurrent deletion of the same child is
/// idempotent. Each child is a nested type - an element or a text - and edits inside it merge with
/// everybody else's edits inside it, which is what makes a tree a better model for a structured document
/// than a map holding markup: two people editing different paragraphs, or the same one, never overwrite
/// each other.
/// </para>
/// <para>
/// Only elements and texts are children. Inserting anything else here throws, but the wire format cannot
/// stop a peer that addresses this type through the generic sequence operations - or that declared the
/// same root under another kind - from putting a plain value or another kind of type in its sequence.
/// Every replica ignores such an entry identically: it takes no index, is not enumerated, is not
/// rendered and raises no child change, while staying in the replicated state so that replicas still
/// converge byte for byte.
/// </para>
/// <para>
/// Indices count children only. Reading by index walks the list, which suits a document's block-level
/// nodes; to visit every child, enumerate rather than index.
/// </para>
/// </remarks>
public abstract class BcrdtXmlContainer : BcrdtAbstractType, IEnumerable<IBcrdtXmlNode>
{
    private List<IBcrdtXmlNode>? _pendingChildren;

    // Only the library's own fragment and element derive from this: a container's children have to be
    // nodes the library can encode, render and recognise on every replica.
    private protected BcrdtXmlContainer() => _pendingChildren = [];

    /// <summary>The number of child nodes.</summary>
    public int Count
    {
        get
        {
            if (_pendingChildren is not null) return _pendingChildren.Count;

            int count = 0;
            for (BcrdtItem? item = Start; item is not null; item = item.Right)
            {
                if (IsChild(item)) count++;
            }
            return count;
        }
    }

    /// <summary>The child node at an index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the children.</exception>
    public IBcrdtXmlNode this[int index]
    {
        get
        {
            if (_pendingChildren is not null)
            {
                ValidateIndex(index, _pendingChildren.Count, inclusive: false);
                return _pendingChildren[index];
            }

            BcrdtItem item = FindChild(index)
                ?? throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is outside {Count} child node(s).");
            return (IBcrdtXmlNode)((BcrdtContentType)item.Content).Type;
        }
    }

    /// <inheritdoc/>
    internal override int PendingHeight(int limit)
    {
        if (_pendingChildren is not { Count: > 0 } children || limit <= 1) return 1;

        int deepest = 0;
        foreach (IBcrdtXmlNode child in children)
        {
            deepest = Math.Max(deepest, ((BcrdtAbstractType)child).PendingHeight(limit - 1));
            if (deepest >= limit) break;
        }
        return 1 + deepest;
    }

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtXmlEvent(this, transaction);

    protected internal override void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        base.Integrate(doc, item);

        if (_pendingChildren is null) return;
        List<IBcrdtXmlNode> buffered = _pendingChildren;
        _pendingChildren = null;
        if (buffered.Count > 0) Insert(0, [.. buffered]);
    }

    /// <summary>Inserts child nodes at an index.</summary>
    /// <param name="index">Where to insert, between zero and <see cref="Count"/> inclusive.</param>
    /// <param name="nodes">The nodes to insert, in order: new elements and texts that are not in a document yet.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the children.</exception>
    /// <exception cref="ArgumentException">A node is null, or is not a <see cref="BcrdtXmlElement"/> or <see cref="BcrdtXmlText"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// A node already belongs somewhere else, would contain itself, or would nest deeper than
    /// <see cref="BcrdtAbstractType.MaxNestingDepth"/>.
    /// </exception>
    public void Insert(int index, params IBcrdtXmlNode[] nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        if (_pendingChildren is not null)
        {
            ValidateIndex(index, _pendingChildren.Count, inclusive: true);
            object?[] pending = ToValues(nodes);
            if (pending.Length == 0) return;

            BcrdtTypeOperations.PreparePendingValues(pending, this);
            _pendingChildren.InsertRange(index, nodes);
            return;
        }

        // One walk finds both the block to insert after and whether the index is in range.
        BcrdtItem? left = null;
        if (index > 0)
        {
            left = FindChild(index - 1);
            if (left is null) ValidateIndex(index, Count, inclusive: true);
        }
        else
        {
            ValidateIndex(index, 0, inclusive: true);
        }

        object?[] values = ToValues(nodes);
        if (values.Length == 0) return;

        Doc.Transact(transaction =>
        {
            // Only this type's own operations keep index caches in step, and they never consult them.
            InvalidateSearchMarkers();
            BcrdtTypeOperations.ListInsertAfter(transaction, this, left, values);
        });
    }

    /// <summary>Appends child nodes.</summary>
    /// <remarks>
    /// The nodes go after the last block of the sequence, as Yjs's <c>push</c> places them, found from where the
    /// previous append ended rather than by counting the children: building an element one child at a time
    /// costs a step per child, not a walk of the whole list.
    /// </remarks>
    /// <exception cref="ArgumentException">A node is null, or is not an element or a text.</exception>
    /// <exception cref="InvalidOperationException">A node already belongs somewhere else.</exception>
    public void Push(params IBcrdtXmlNode[] nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        object?[] values = ToValues(nodes);
        if (values.Length == 0) return;

        if (_pendingChildren is not null)
        {
            BcrdtTypeOperations.PreparePendingValues(values, this);
            _pendingChildren.AddRange(nodes);
            return;
        }

        Doc.Transact(transaction =>
        {
            InvalidateSearchMarkers();
            BcrdtTypeOperations.ListInsertAfter(transaction, this, Tail(), values);
        });
    }

    /// <summary>Checks that every node can be a child, and returns them as sequence values.</summary>
    private static object?[] ToValues(IBcrdtXmlNode[] nodes)
    {
        object?[] values = new object?[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            values[i] = nodes[i] switch
            {
                BcrdtXmlElement or BcrdtXmlText => nodes[i],
                null => throw new ArgumentException("A child node cannot be null.", nameof(nodes)),
                _ => throw new ArgumentException(
                    $"Only a {nameof(BcrdtXmlElement)} or a {nameof(BcrdtXmlText)} can be a child node, not a {nodes[i].GetType().Name}.",
                    nameof(nodes)),
            };
        }
        return values;
    }

    // Where the sequence ended when it was last looked for: a hint, checked before use, since a block that
    // was merged into its left neighbour is no longer in the list.
    private BcrdtItem? _tailHint;

    /// <summary>The last block of the sequence, deleted or not, or null when it is empty.</summary>
    private BcrdtItem? Tail()
    {
        BcrdtItem? item = _tailHint is { } hint && IsLinked(hint) ? hint : Start;
        if (item is null) return null;
        while (item.Right is not null) item = item.Right;
        return _tailHint = item;
    }

    private bool IsLinked(BcrdtItem item) =>
        ReferenceEquals(item.Parent, this) && (item.Left is null ? ReferenceEquals(Start, item) : ReferenceEquals(item.Left.Right, item));

    /// <summary>Removes a run of child nodes, and everything inside them.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The run extends outside the children.</exception>
    public void Delete(int index, int length = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        int count = Count;
        ValidateIndex(index, count, inclusive: true);
        if (length > count - index)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), $"Cannot delete {length} child node(s) at index {index} of {count}.");
        }
        if (length == 0) return;

        if (_pendingChildren is not null)
        {
            for (int i = index; i < index + length; i++) BcrdtTypeOperations.ReleasePending(_pendingChildren[i], this);
            _pendingChildren.RemoveRange(index, length);
            return;
        }

        Doc.Transact(transaction =>
        {
            InvalidateSearchMarkers();
            int remaining = length;
            for (BcrdtItem? item = FindChild(index); item is not null && remaining > 0; item = item.Right)
            {
                if (!IsChild(item)) continue;
                item.Delete(transaction);
                remaining--;
            }
        });
    }

    /// <summary>Removes every child node, as a single transaction.</summary>
    public void Clear() => Delete(0, Count);

    /// <summary>A snapshot of the child nodes.</summary>
    public List<IBcrdtXmlNode> ToArray() => [.. this];

    /// <summary>
    /// Every node below this one, in document order: each child, then that child's own descendants,
    /// before the next child.
    /// </summary>
    /// <remarks>
    /// The walk is lazy and reads the tree as it is at each step, so change the tree only after it ends -
    /// <c>Descendants().ToList()</c> first, when the loop itself edits. Filter it with LINQ to find nodes:
    /// <c>fragment.Descendants().OfType&lt;BcrdtXmlElement&gt;().Where(e =&gt; e.Name == "img")</c>.
    /// </remarks>
    public IEnumerable<IBcrdtXmlNode> Descendants()
    {
        var stack = new Stack<IEnumerator<IBcrdtXmlNode>>();
        stack.Push(GetEnumerator());
        try
        {
            while (stack.Count > 0)
            {
                IEnumerator<IBcrdtXmlNode> current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop().Dispose();
                    continue;
                }

                IBcrdtXmlNode node = current.Current;
                yield return node;
                if (node is BcrdtXmlElement element) stack.Push(element.GetEnumerator());
            }
        }
        finally
        {
            while (stack.Count > 0) stack.Pop().Dispose();
        }
    }

    /// <summary>The tree below this type as XML markup.</summary>
    /// <remarks>
    /// <para>
    /// Attributes are written in ordinal order of their names, so converged replicas render identical
    /// markup. Text and attribute values are escaped; a character XML cannot carry at all - a control
    /// character, a lone surrogate - is written as U+FFFD. An attribute value that is not a string takes
    /// its invariant-culture form, with lists and maps as JSON.
    /// </para>
    /// <para>
    /// A <see cref="BcrdtXmlText"/> renders its characters only: its rich-text marks are not turned into
    /// markup and its embeds are left out. Read a text's <see cref="BcrdtText.ToDelta"/> where marks matter.
    /// An element with no children is written with an explicit end tag, <c>&lt;p&gt;&lt;/p&gt;</c>, which
    /// HTML reads the same way XML does.
    /// </para>
    /// </remarks>
    public abstract string ToXmlString();

    /// <summary>Writes each child's markup.</summary>
    private protected void AppendChildren(StringBuilder builder)
    {
        foreach (IBcrdtXmlNode node in this) AppendNode(builder, node);
    }

    /// <summary>Writes one node's markup.</summary>
    internal static void AppendNode(StringBuilder builder, IBcrdtXmlNode node)
    {
        switch (node)
        {
            case BcrdtXmlElement element:
                element.AppendXml(builder);
                break;
            case BcrdtXmlText text:
                BcrdtXmlSyntax.AppendEscaped(builder, text.ToString(), attribute: false);
                break;
        }
    }

    /// <summary>The children's plain-object projections, in order.</summary>
    private protected List<object?> ChildrenToJson() => [.. this.Select(node => ((BcrdtAbstractType)node).ToJson())];

    /// <summary>Detached copies of the children, for <see cref="BcrdtAbstractType.Clone"/>.</summary>
    private protected IBcrdtXmlNode[] CloneChildren() => [.. this.Select(node => (IBcrdtXmlNode)((BcrdtAbstractType)node).Clone())];

    /// <summary>Whether a block is one of this type's visible child nodes.</summary>
    internal static bool IsChild(BcrdtItem item) => !item.Deleted && IsNodeBlock(item);

    /// <summary>Whether a block holds a node, deleted or not.</summary>
    internal static bool IsNodeBlock(BcrdtItem item) => item.Content is BcrdtContentType { Type: BcrdtXmlElement or BcrdtXmlText };

    /// <summary>The block holding the child at <paramref name="index"/>, or null past the end.</summary>
    private BcrdtItem? FindChild(int index)
    {
        if (index < 0) return null;
        for (BcrdtItem? item = Start; item is not null; item = item.Right)
        {
            if (!IsChild(item)) continue;
            if (index-- == 0) return item;
        }
        return null;
    }

    private static void ValidateIndex(int index, int count, bool inclusive)
    {
        if (index < 0 || index > count || (!inclusive && index == count))
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is outside {count} child node(s).");
        }
    }

    /// <inheritdoc/>
    public IEnumerator<IBcrdtXmlNode> GetEnumerator() => (_pendingChildren ?? EnumerateChildren()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IEnumerable<IBcrdtXmlNode> EnumerateChildren()
    {
        for (BcrdtItem? item = Start; item is not null; item = item.Right)
        {
            if (IsChild(item)) yield return (IBcrdtXmlNode)((BcrdtContentType)item.Content).Type;
        }
    }

    /// <summary>The container a node sits in, attached or waiting detached.</summary>
    internal static BcrdtXmlContainer? ParentOf(BcrdtAbstractType node) =>
        node.ContainingItem is { } item ? item.Parent as BcrdtXmlContainer : node.PendingOwner as BcrdtXmlContainer;
}
