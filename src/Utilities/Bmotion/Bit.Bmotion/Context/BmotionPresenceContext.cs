namespace Bit.Bmotion;
/// <summary>
/// The presence a piece of content sits in - cascaded by <see cref="BmotionAnimatePresence"/>,
/// <see cref="BmotionPresenceSwitch{TItem}"/> and <see cref="BmotionPresenceGroup{TItem}"/> to everything inside one
/// entry. Every <see cref="Bmotion"/> in it takes part in its exits automatically; a component of your own takes part
/// through <see cref="Register()"/> (motion.dev's <c>usePresence</c>), so the content stays mounted until its own exit
/// - a CSS animation, a canvas fade - is over:
/// <code>
/// [CascadingParameter] public BmotionPresenceContext? Presence { get; set; }
/// private BmPresenceRegistration? _presence;
/// private bool _leaving;
///
/// protected override void OnInitialized() => _presence = Presence?.Register();
///
/// protected override async Task OnParametersSetAsync()
/// {
///     if (_presence is not { IsPresent: false } || _leaving) return; // re-renders mid-exit start nothing new
///     _leaving = true;
///     await PlayMyExitAsync();
///     _leaving = false;
///     _presence.SafeToRemove(); // ignored if the content came back meanwhile
/// }
///
/// public void Dispose() => _presence?.Dispose();
/// </code>
/// </summary>
public sealed class BmotionPresenceContext
{
    // What takes part in the exits: Bmotion elements and registrations.
    private readonly List<object> _children = new();

    internal BmotionPresenceContext() { }

    /// <summary>True while the content is on its way out: removed, and playing its exit.</summary>
    public bool IsExiting
    {
        get => _isExiting;
        internal set
        {
            // Every exit starts counting from zero: a completion recorded during an earlier exit
            // that was cancelled (the content re-entered) must not stand in for this one.
            if (value && !_isExiting) _completedChildren.Clear();
            _isExiting = value;
        }
    }
    private bool _isExiting;

    /// <summary>Whether the content is present - <c>false</c> once it has been removed and is exiting.</summary>
    public bool IsPresent => !IsExiting;

    /// <summary>
    /// When true, exiting children pop out of the layout flow (position: absolute at their
    /// current spot) before playing their exit, so siblings reflow immediately.
    /// </summary>
    public bool PopLayout { get; internal set; }

    /// <summary>
    /// Takes part in this presence's exits: while the content exits, the presence keeps it mounted until every
    /// <see cref="Bmotion"/> in it has finished and every registration has called
    /// <see cref="BmPresenceRegistration.SafeToRemove"/> (or been disposed). Register once, as the component
    /// initialises, and dispose the registration when the component goes away.
    /// </summary>
    public BmPresenceRegistration Register()
    {
        var registration = new BmPresenceRegistration(this);
        Add(registration);
        return registration;
    }

    internal void Register(Bmotion child) => Add(child);

    internal void Unregister(Bmotion child) => Remove(child);

    internal void NotifyExitComplete(Bmotion child) => Complete(child);

    private void Add(object child)
    {
        if (!_children.Contains(child)) _children.Add(child);
    }

    internal void Remove(object child)
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

    private readonly HashSet<object> _completedChildren = new();

    internal bool HasCompleted(object child) => _completedChildren.Contains(child);

    internal void Complete(object child)
    {
        // An exit that finishes after it was cancelled (the content re-entered mid-exit) completes
        // nothing; ignore unregistered children and guard against double-counting.
        if (!IsExiting) return;
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

/// <summary>
/// A component's part in a presence's exits (see <see cref="BmotionPresenceContext.Register()"/>). Dispose it when the
/// component is disposed: a registration that is gone holds nothing up.
/// </summary>
public sealed class BmPresenceRegistration : IDisposable
{
    private BmotionPresenceContext? _presence;

    internal BmPresenceRegistration(BmotionPresenceContext presence) => _presence = presence;

    /// <summary>Whether the content is present - <c>false</c> once it has been removed and is exiting.</summary>
    public bool IsPresent => _presence?.IsPresent ?? true;

    /// <summary>Whether this registration has reported the current exit over (<see cref="SafeToRemove"/>).</summary>
    public bool IsExitComplete => _presence?.HasCompleted(this) == true;

    /// <summary>
    /// Reports this component's exit over: once everything else in the presence has finished too, the content is
    /// removed. Ignored while the content is present - an exit cancelled by a re-entry completes nothing - so it is
    /// safe to call from an exit that was overtaken.
    /// </summary>
    public void SafeToRemove() => _presence?.Complete(this);

    /// <summary>Stops taking part: a presence waiting on this registration no longer does.</summary>
    public void Dispose()
    {
        var presence = _presence;
        _presence = null;
        presence?.Remove(this);
    }
}
