
namespace Bit.Bmotion;
/// <summary>
/// Cascaded by <see cref="BmotionAnimatePresence"/> to signal exit state to child Bmotion components.
/// </summary>
internal class BmotionPresenceContext
{
    private readonly List<Bmotion> _children = new();

    /// <summary>True while the children are playing their exit animation.</summary>
    public bool IsExiting { get; internal set; }

    /// <summary>
    /// When true, exiting children pop out of the layout flow (position: absolute at their
    /// current spot) before playing their exit, so siblings reflow immediately.
    /// </summary>
    public bool PopLayout { get; internal set; }

    internal void Register(Bmotion child)
    {
        if (!_children.Contains(child)) _children.Add(child);
    }
    internal void Unregister(Bmotion child)
    {
        _children.Remove(child);
        _completedChildren.Remove(child);
        // A child disposed mid-exit removes itself here. Re-evaluate completion now so
        // AllExitsComplete still fires when either every remaining child has already completed, or
        // no children remain at all (the last one just unregistered during an active exit).
        if (IsExiting && _completedChildren.Count >= _children.Count)
            AllExitsComplete?.Invoke();
    }

    internal int ChildCount => _children.Count;

    private readonly HashSet<Bmotion> _completedChildren = new();

    internal void NotifyExitComplete(Bmotion child)
    {
        // Ignore unregistered children and guard against double-counting.
        if (!_children.Contains(child)) return;
        if (!_completedChildren.Add(child)) return;

        if (_completedChildren.Count >= _children.Count)
            AllExitsComplete?.Invoke();
    }

    /// <summary>
    /// Clears exit-completion bookkeeping for a fresh enter cycle. Registered children are left
    /// intact - they remove themselves via <see cref="Unregister"/> when disposed, so clearing the
    /// list here would desynchronise the count for any children that are reused across a toggle.
    /// </summary>
    internal void Reset() { _completedChildren.Clear(); }

    /// <summary>Fired when every registered child has finished its exit animation.</summary>
    internal event Action? AllExitsComplete;
}
