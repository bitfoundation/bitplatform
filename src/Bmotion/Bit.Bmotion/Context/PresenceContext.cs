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

    internal void Register(Motion child) => _children.Add(child);
    internal void Unregister(Motion child) => _children.Remove(child);

    internal int ChildCount => _children.Count;

    private int _completedExits;

    internal void NotifyExitComplete(Motion child)
    {
        _completedExits++;
        if (_completedExits >= _children.Count)
            AllExitsComplete?.Invoke();
    }

    internal void Reset() { _completedExits = 0; _children.Clear(); }

    /// <summary>Fired when every registered child has finished its exit animation.</summary>
    internal event Action? AllExitsComplete;
}
