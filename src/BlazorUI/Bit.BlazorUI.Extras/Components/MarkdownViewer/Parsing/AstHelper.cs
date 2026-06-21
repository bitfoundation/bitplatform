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
        foreach (var list in node.ChildLists)
        {
            action(list);
            // Snapshot because the action may have replaced entries.
            foreach (var child in list.ToArray())
                VisitChildLists(child, action);
        }
    }

    /// <summary>Enumerates every node in the tree (excluding the root).</summary>
    public static IEnumerable<MarkdownNode> Descendants(MarkdownNode node)
    {
        foreach (var list in node.ChildLists)
        {
            foreach (var child in list)
            {
                yield return child;
                foreach (var d in Descendants(child))
                    yield return d;
            }
        }
    }
}
