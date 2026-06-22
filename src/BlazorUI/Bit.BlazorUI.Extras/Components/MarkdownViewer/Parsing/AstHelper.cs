using Bit.BlazorUI.Markdown.Syntax;

namespace Bit.BlazorUI.Markdown.Parsing;

/// <summary>Helpers for traversing and rewriting the AST, used by AST processors.</summary>
public static class AstHelper
{
    /// <summary>
    /// Invokes <paramref name="action"/> for every child collection in the tree
    /// (depth-first). The action may mutate the collection in place (e.g. to split a
    /// text node into several nodes).
    /// </summary>
    public static void VisitChildLists(MarkdownNode node, Action<IList<MarkdownNode>> action)
    {
        // Iterative depth-first traversal to avoid stack overflow on deeply nested input.
        var stack = new Stack<MarkdownNode>();
        stack.Push(node);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            // Collect children after invoking the action, since the action may
            // replace entries in the list (e.g. splitting a text node).
            var children = new List<MarkdownNode>();
            foreach (var list in current.ChildLists)
            {
                action(list);
                children.AddRange(list);
            }
            // Push in reverse so children are processed in document order.
            for (int i = children.Count - 1; i >= 0; i--)
                stack.Push(children[i]);
        }
    }

    /// <summary>Enumerates every node in the tree (excluding the root).</summary>
    public static IEnumerable<MarkdownNode> Descendants(MarkdownNode node)
    {
        // Iterative pre-order traversal to avoid stack overflow on deeply nested input.
        var stack = new Stack<MarkdownNode>();
        PushChildrenReversed(node, stack);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            PushChildrenReversed(current, stack);
        }
    }

    private static void PushChildrenReversed(MarkdownNode node, Stack<MarkdownNode> stack)
    {
        // Flatten children across all child lists (in order), then push them
        // reversed so they pop in document (pre-order) order.
        var children = new List<MarkdownNode>();
        foreach (var list in node.ChildLists)
            children.AddRange(list);
        for (int i = children.Count - 1; i >= 0; i--)
            stack.Push(children[i]);
    }
}
