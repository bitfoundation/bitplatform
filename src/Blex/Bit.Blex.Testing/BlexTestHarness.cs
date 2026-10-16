using System.Text.Json.Nodes;

namespace Bit.Blex.Testing;

/// <summary>
/// A self-contained test fixture around a single store: it creates a <see cref="BlexManager"/>,
/// registers the store, and records every action so tests can assert on dispatch behavior without
/// any Blazor or DI setup.
/// </summary>
/// <typeparam name="TStore">The store under test.</typeparam>
public sealed class BlexTestHarness<TStore> : IDisposable
    where TStore : BlexStoreBase
{
    /// <summary>Creates a harness, registering <paramref name="store"/> with a fresh manager.</summary>
    public BlexTestHarness(TStore store) : this(store, null)
    {
    }

    /// <summary>
    /// Creates a harness whose manager runs <paramref name="middleware"/>, so a store can be tested
    /// together with the veto filters and observers that wrap it in the real application.
    /// </summary>
    public BlexTestHarness(TStore store, IEnumerable<IBlexMiddleware>? middleware)
    {
        ArgumentNullException.ThrowIfNull(store);
        Store = store;
        Manager = new BlexManager(middleware);
        Manager.Register(store);
        Log = Manager.RecordActions();
        Manager.ActionFailed += _failures.Add;
    }

    private readonly List<BlexActionErrorContext> _failures = [];

    /// <summary>
    /// Every action whose body threw, in order. An effect captures its exception into
    /// <c>XxxError</c> rather than propagating it, so without this a test can only assert that
    /// <em>something</em> failed, not which action or with what arguments.
    /// </summary>
    public IReadOnlyList<BlexActionErrorContext> Failures => _failures;

    /// <summary>
    /// Registers an additional store with the harness's manager, for testing cross-store
    /// coordination without hand-wiring a second manager. Its actions appear in <see cref="Log"/>
    /// and its slice in <see cref="State"/>, exactly as the store under test's do.
    /// </summary>
    /// <returns>The store, so it can be captured inline.</returns>
    /// <example><code>
    /// using var harness = BlexTestHarness.For&lt;CartStore&gt;();
    /// var profile = harness.Register(new ProfileStore());
    /// </code></example>
    public TOther Register<TOther>(TOther store) where TOther : BlexStoreBase
    {
        ArgumentNullException.ThrowIfNull(store);
        Manager.Register(store);
        _stores.Add(store);
        return store;
    }

    private readonly List<BlexStoreBase> _stores = [];

    /// <summary>The store under test.</summary>
    public TStore Store { get; }

    /// <summary>The manager backing the harness.</summary>
    public BlexManager Manager { get; }

    /// <summary>The action log recording all dispatches.</summary>
    public BlexActionLog Log { get; }

    /// <summary>The current global state tree.</summary>
    public JsonObject State => Manager.CaptureGlobalState();

    /// <summary>The current snapshot of the store under test.</summary>
    public JsonObject Snapshot() => Store.SerializeState();

    /// <inheritdoc />
    /// <remarks>
    /// Disposes every registered store as well, so an effect still in flight when a test finishes is
    /// cancelled instead of resuming against the next test's fixture.
    /// </remarks>
    public void Dispose()
    {
        Log.Dispose();
        Manager.ActionFailed -= _failures.Add;
        Store.Dispose();
        foreach (var store in _stores)
            store.Dispose();
    }
}
