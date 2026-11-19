namespace Bit.Blex;

/// <summary>Builds and combines <see cref="IBlexRollback"/>s.</summary>
public static class BlexRollback
{
    /// <summary>A rollback that runs <paramref name="rollback"/>.</summary>
    public static IBlexRollback From(Action rollback)
    {
        ArgumentNullException.ThrowIfNull(rollback);
        return new DelegateRollback(rollback);
    }

    /// <summary>One rollback for several changes, undone in reverse order -- the last applied, the first undone.</summary>
    public static IBlexRollback Combine(params IBlexRollback?[] rollbacks)
    {
        ArgumentNullException.ThrowIfNull(rollbacks);
        return From(() =>
        {
            for (var i = rollbacks.Length - 1; i >= 0; i--)
                rollbacks[i]?.Rollback();
        });
    }

    private sealed class DelegateRollback(Action rollback) : IBlexRollback
    {
        private Action? _rollback = rollback;

        public void Rollback() => Interlocked.Exchange(ref _rollback, null)?.Invoke();
    }
}
