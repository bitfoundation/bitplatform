namespace Bit.BlazorUI;

/// <summary>Helpers for traversing and rewriting the AST, used by AST processors.</summary>
public static class BitMarkdownAstHelper
{
    /// <summary>
    /// Invokes <paramref name="action"/> for every child collection in the tree
    /// (depth-first). The action may mutate the collection in place (e.g. to split a
    /// text node into several nodes).
    /// </summary>
    public static void VisitChildLists(BitMarkdownNode node, Action<IList<BitMarkdownNode>> action)
    {
        // Iterative depth-first traversal (over child lists) to avoid stack overflow
        // on deeply nested input. A list stack is used instead of a node stack so that
        // each list and its descendants are fully visited before the next sibling list,
        // preserving depth-first order even when a node exposes multiple child lists.
        var stack = new Stack<IList<BitMarkdownNode>>();
        PushListsReversed(node, stack);
        while (stack.Count > 0)
        {
            var list = stack.Pop();
            // Invoke before reading children, since the action may replace entries
            // in the list (e.g. splitting a text node into several nodes).
            action(list);
            // Push the child lists of this list's nodes in reverse document order so
            // they pop (and are processed) in document order, ahead of any sibling list.
            for (int i = list.Count - 1; i >= 0; i--)
                PushListsReversed(list[i], stack);
        }
    }

    private static void PushListsReversed(BitMarkdownNode node, Stack<IList<BitMarkdownNode>> stack)
    {
        // ChildLists may be a computed property; read it once.
        var childLists = node.ChildLists;
        var lists = childLists as IList<IList<BitMarkdownNode>> ?? childLists.ToList();
        for (int i = lists.Count - 1; i >= 0; i--)
            stack.Push(lists[i]);
    }

    /// <summary>Enumerates every node in the tree (excluding the root).</summary>
    public static IEnumerable<BitMarkdownNode> Descendants(BitMarkdownNode node)
    {
        // Iterative pre-order traversal to avoid stack overflow on deeply nested input.
        var stack = new Stack<BitMarkdownNode>();
        PushChildrenReversed(node, stack);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            PushChildrenReversed(current, stack);
        }
    }

    private static void PushChildrenReversed(BitMarkdownNode node, Stack<BitMarkdownNode> stack)
    {
        // Push children across all child lists in reverse document order (last list
        // first, last node first within each) so they pop in pre-order without
        // allocating an intermediate flattened list.
        var childLists = node.ChildLists;
        var lists = childLists as IList<IList<BitMarkdownNode>> ?? childLists.ToList();
        for (int i = lists.Count - 1; i >= 0; i--)
        {
            var list = lists[i];
            for (int j = list.Count - 1; j >= 0; j--)
                stack.Push(list[j]);
        }
    }
}
