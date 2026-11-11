namespace Bit.Bdnd;

/// <summary>
/// What dropping a node of a tree somewhere else in it takes: telling whether the place is inside the node itself (a
/// node cannot become its own descendant), finding the node's parent, and moving it - children and all - into another
/// node's children. The tree is the app's own: <c>children</c> gives a node's list of child nodes.
/// </summary>
/// <remarks>
/// Nodes are compared with <see cref="EqualityComparer{T}.Default"/>, and the tree is walked without recursion, so a
/// deep tree does not exhaust the stack. A drop target that refuses a node's own subtree uses <see cref="Contains"/>
/// in its <c>CanDropPredicate</c>; the drop calls <see cref="PlanMove"/> and applies the move it answers.
/// </remarks>
public static class BdndTree
{
    /// <summary>
    /// Whether <paramref name="node"/> is <paramref name="ancestor"/> or a descendant of it - when it is, dropping
    /// <paramref name="ancestor"/> into <paramref name="node"/> would make a cycle.
    /// </summary>
    public static bool Contains<T>(T ancestor, T node, Func<T, IEnumerable<T>> children) where T : class
    {
        ArgumentNullException.ThrowIfNull(ancestor);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(children);
        var pending = new Stack<T>();
        pending.Push(ancestor);
        while (pending.TryPop(out var current))
        {
            if (EqualityComparer<T>.Default.Equals(current, node)) return true;
            foreach (var child in children(current)) pending.Push(child);
        }
        return false;
    }

    /// <summary>
    /// The node of the tree under <paramref name="root"/> (<paramref name="root"/> included) whose children
    /// hold <paramref name="node"/>, or null when it is not in the tree or is <paramref name="root"/>.
    /// </summary>
    public static T? ParentOf<T>(T root, T node, Func<T, IEnumerable<T>> children) where T : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(children);
        var pending = new Stack<T>();
        pending.Push(root);
        while (pending.TryPop(out var current))
        {
            foreach (var child in children(current))
            {
                if (EqualityComparer<T>.Default.Equals(child, node)) return current;
                pending.Push(child);
            }
        }
        return null;
    }

    /// <summary>
    /// Works out moving <paramref name="node"/> - with everything under it - into the children of
    /// <paramref name="parent"/> at <paramref name="insertionIndex"/> (an index in those children as they are before
    /// the move, what <see cref="BdndList.InsertionIndex"/> answers). Nothing changes until
    /// <see cref="BdndListMove{T}.Apply"/> is called, and <see cref="BdndListMove{T}.Revert"/> takes it back. Answers
    /// null when the move would put the node inside itself, the node is not in the tree under <paramref name="root"/>,
    /// the index is out of range, or the node would stay where it is.
    /// </summary>
    public static BdndListMove<T>? PlanMove<T>(T root, T node, T parent, int insertionIndex, Func<T, IList<T>> children) where T : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(children);
        if (Contains(node, parent, children) || ParentOf(root, node, children) is not { } from) return null;
        var siblings = children(from);
        return BdndList.PlanMove(siblings, siblings.IndexOf(node), children(parent), insertionIndex);
    }
}
