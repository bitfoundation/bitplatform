using Bit.Bmotion.Components;

namespace Bit.Bmotion.Context;

/// <summary>
/// Cascaded by <see cref="AnimatePresence"/> to signal exit state to child Motion components.
/// </summary>
public class PresenceContext
{
    private readonly List<Motion> _children = new();

    /// <summary>True while the children are playing their exit animation.</summary>
    public bool IsExiting { get; internal set; }

    internal void Register(Motion child)
    {
        if (!_children.Contains(child)) _children.Add(child);
    }
    internal void Unregister(Motion child) => _children.Remove(child);

    internal int ChildCount => _children.Count;

    private readonly HashSet<Motion> _completedChildren = new();

    internal void NotifyExitComplete(Motion child)
    {
        // Ignore unregistered children and guard against double-counting.
        if (!_children.Contains(child)) return;
        if (!_completedChildren.Add(child)) return;

        if (_completedChildren.Count >= _children.Count)
            AllExitsComplete?.Invoke();
    }

    /// <summary>
    /// Clears exit-completion bookkeeping for a fresh enter cycle. Registered children are left
    /// intact — they remove themselves via <see cref="Unregister"/> when disposed, so clearing the
    /// list here would desynchronise the count for any children that are reused across a toggle.
    /// </summary>
    internal void Reset() { _completedChildren.Clear(); }

    /// <summary>Fired when every registered child has finished its exit animation.</summary>
    internal event Action? AllExitsComplete;
}
