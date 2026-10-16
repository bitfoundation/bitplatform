namespace Bit.Blex.Demo.Client.Services;

/// <summary>
/// Backs the middleware veto demo. When <see cref="IsReadOnly"/> is on, <see cref="GuardMiddleware"/>
/// cancels every action before it mutates anything, and counts what it blocked.
/// </summary>
public sealed class DemoGuard
{
    private bool _isReadOnly;

    /// <summary>Raised when the guard is toggled or a block is recorded.</summary>
    public event Action? Changed;

    /// <summary>Whether the guard currently vetoes actions.</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (_isReadOnly == value)
                return;
            _isReadOnly = value;
            Changed?.Invoke();
        }
    }

    /// <summary>How many actions the guard has vetoed.</summary>
    public int BlockedCount { get; private set; }

    /// <summary>The most recently vetoed action's qualified name.</summary>
    public string? LastBlocked { get; private set; }

    /// <summary>
    /// Actions the guard always lets through, so the demo can still be switched back off and
    /// reset. A real read-only mode would allow-list its own escape hatches the same way.
    /// </summary>
    public HashSet<string> AlwaysAllow { get; } = new(StringComparer.Ordinal);

    internal void RecordBlock(string qualifiedName)
    {
        BlockedCount++;
        LastBlocked = qualifiedName;
        Changed?.Invoke();
    }

    /// <summary>Resets the block counters.</summary>
    public void Reset()
    {
        BlockedCount = 0;
        LastBlocked = null;
        Changed?.Invoke();
    }
}

/// <summary>
/// Veto middleware. <see cref="BeforeAction"/> runs ahead of the mutation and calling
/// <see cref="BlexPreActionContext.Cancel"/> stops the action entirely -- nothing mutates,
/// nothing is recorded, no notification fires.
/// </summary>
public sealed class GuardMiddleware(DemoGuard guard) : IBlexMiddleware
{
    /// <inheritdoc />
    public void OnAction(BlexActionContext context)
    {
    }

    /// <inheritdoc />
    public void BeforeAction(BlexPreActionContext context)
    {
        if (!guard.IsReadOnly || guard.AlwaysAllow.Contains(context.QualifiedName))
            return;

        guard.RecordBlock(context.QualifiedName);
        context.Cancel();
    }
}
