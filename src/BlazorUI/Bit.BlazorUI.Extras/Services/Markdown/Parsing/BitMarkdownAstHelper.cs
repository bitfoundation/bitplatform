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

    /// <summary>
    /// Renders the whole subtree as plain text: what the document says, with none of the markup it
    /// says it with. Blocks are separated by a blank line, so the result reads as paragraphs rather
    /// than as one run-on sentence.
    /// </summary>
    /// <remarks>
    /// This is the text a search index, an excerpt or a meta description is built from, and it is
    /// read off the parsed tree rather than off the source, so the syntax characters, the link
    /// destinations and the reference definitions are all already gone.
    /// </remarks>
    public static string ToPlainText(BitMarkdownNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var sb = new System.Text.StringBuilder();
        Append(node, sb);
        return sb.ToString().Trim();

        static void Append(BitMarkdownNode current, System.Text.StringBuilder sb)
        {
            switch (current)
            {
                case BitMarkdownTextNode text:
                    sb.Append(text.Text);
                    return;

                case BitMarkdownCodeSpanNode code:
                    sb.Append(code.Content);
                    return;

                case BitMarkdownCodeBlockNode block:
                    sb.Append(block.Content);
                    Separate(sb);
                    return;

                case BitMarkdownImageNode image:
                    sb.Append(image.Alt);
                    return;

                case BitMarkdownLineBreakNode:
                    sb.Append(' ');
                    return;

                // Two flavors put their text on the node itself rather than in a child text run.
                case BitMarkdownMathNode math:
                    sb.Append(math.Content);
                    return;

                case BitMarkdownAbbreviationNode abbreviation:
                    sb.Append(abbreviation.Text);
                    return;
            }

            bool first = true;
            foreach (var list in current.ChildLists)
            {
                // A node with several child collections is a grid of them - a table's cells are the
                // only one - and running them together would read as one word.
                if (first is false && sb.Length > 0 && char.IsWhiteSpace(sb[^1]) is false) sb.Append(' ');
                first = false;

                foreach (var child in list) Append(child, sb);
            }

            // A block ends a line of prose; an inline does not.
            if (current is BitMarkdownParagraphNode or BitMarkdownHeadingNode or BitMarkdownListItemNode
                        or BitMarkdownBlockquoteNode or BitMarkdownThematicBreakNode
                        or BitMarkdownTableNode or BitMarkdownContainerNode or BitMarkdownFigureNode
                        or BitMarkdownDefinitionTermNode or BitMarkdownDefinitionDescriptionNode)
            {
                Separate(sb);
            }
        }

        static void Separate(System.Text.StringBuilder sb)
        {
            if (sb.Length > 0 && sb[^1] != '\n') sb.Append('\n');
            sb.Append('\n');
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
