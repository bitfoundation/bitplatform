namespace Bit.Blex;

/// <summary>
/// A typed descriptor for a parameterless action of <typeparamref name="TStore"/>, generated into the
/// store's nested <c>Actions</c> class (<c>CounterStore.Actions.Increment</c>).
/// </summary>
/// <typeparam name="TStore">The store the action belongs to.</typeparam>
/// <example><code>
/// using var sub = manager.SubscribeToAction(CounterStore.Actions.Increment, counter => Log(counter.Count));
/// await CounterStore.Actions.Increment.InvokeAsync(counter);
/// </code></example>
public sealed class BlexAction<TStore> : BlexActionDescriptor
    where TStore : class, IBlexStore
{
    private readonly Func<TStore, Task> _invoke;

    /// <summary>Creates a descriptor. Called by generated code.</summary>
    /// <param name="name">The action name as dispatched (its display label when it has one).</param>
    /// <param name="invoke">Calls the generated wrapper.</param>
    public BlexAction(string name, Func<TStore, Task> invoke)
        : base(typeof(TStore), name, [])
    {
        ArgumentNullException.ThrowIfNull(invoke);
        _invoke = invoke;
    }

    /// <summary>Dispatches the action on <paramref name="store"/> (equivalent to calling its generated method).</summary>
    /// <returns>The action's task; already completed for a synchronous action.</returns>
    public Task InvokeAsync(TStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return _invoke(store);
    }

    /// <inheritdoc />
    public override Task InvokeAsync(IBlexStore store, IReadOnlyList<BlexActionArg>? args = null)
        => InvokeAsync(CastStore<TStore>(store));
}
