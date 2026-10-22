using System.Text.Json.Nodes;

namespace Bit.Blex.Testing;

/// <summary>
/// Records every action dispatched through a <see cref="BlexManager"/> for assertions in tests.
/// Dispose (or use a <c>using</c>) to detach. Obtain one via <see cref="BlexTestExtensions.RecordActions"/>.
/// </summary>
public sealed class BlexActionLog : IDisposable
{
    private readonly IDisposable _subscription;
    private readonly List<BlexRecordedAction> _actions = [];

    internal BlexActionLog(BlexManager manager)
    {
        _subscription = manager.Subscribe(ctx => _actions.Add(new BlexRecordedAction(
            ctx.Store, ctx.ActionName, ctx.QualifiedName, ctx.Sequence, ctx.GlobalState, ctx.Args, ctx.IsNested)));
    }

    /// <summary>All recorded actions, in dispatch order.</summary>
    public IReadOnlyList<BlexRecordedAction> Actions => _actions;

    /// <summary>The most recently recorded action, or <c>null</c> if none.</summary>
    public BlexRecordedAction? Last => _actions.Count > 0 ? _actions[^1] : null;

    /// <summary>Number of recorded actions.</summary>
    public int Count => _actions.Count;

    /// <summary>The bare action names in order.</summary>
    public IReadOnlyList<string> Names => _actions.Select(a => a.ActionName).ToList();

    /// <summary>The qualified action names in order.</summary>
    public IReadOnlyList<string> QualifiedNames => _actions.Select(a => a.QualifiedName).ToList();

    /// <summary>Whether an action with the given bare or qualified name was recorded.</summary>
    public bool Contains(string actionName)
        => _actions.Any(a => a.ActionName == actionName || a.QualifiedName == actionName);

    /// <summary>How many times an action with the given bare or qualified name was recorded.</summary>
    public int CountOf(string actionName)
        => _actions.Count(a => a.ActionName == actionName || a.QualifiedName == actionName);

    /// <summary>Whether the action a generated descriptor names (<c>CounterStore.Actions.Add</c>) was recorded.</summary>
    public bool Contains(BlexActionDescriptor action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return _actions.Any(a => action.Matches(a.Store, a.ActionName));
    }

    /// <summary>How many times the action a generated descriptor names was recorded.</summary>
    public int CountOf(BlexActionDescriptor action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return _actions.Count(a => action.Matches(a.Store, a.ActionName));
    }

    /// <summary>
    /// The payload of every recorded run of one action, in order, as the type the action takes -- the
    /// argument itself for a single-parameter action, a named tuple for several.
    /// </summary>
    /// <example><code>
    /// harness.Store.Add(2);
    /// harness.Store.Add(3);
    /// CollectionAssert.AreEqual(new[] { 2, 3 }, harness.Log.PayloadsOf(CounterStore.Actions.Add).ToArray());
    /// </code></example>
    public IReadOnlyList<TPayload> PayloadsOf<TStore, TPayload>(BlexAction<TStore, TPayload> action)
        where TStore : class, IBlexStore
    {
        ArgumentNullException.ThrowIfNull(action);
        return _actions
            .Where(a => action.Matches(a.Store, a.ActionName))
            .Select(a => action.ReadPayload(a.Args))
            .ToList();
    }

    /// <summary>Clears the recorded actions.</summary>
    public void Clear() => _actions.Clear();

    /// <inheritdoc />
    public void Dispose() => _subscription.Dispose();
}
