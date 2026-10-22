using System.Text.Json.Nodes;

namespace Bit.Blex.Testing;

/// <summary>Convenience extensions for testing Blex stores and managers.</summary>
public static class BlexTestExtensions
{
    /// <summary>Starts recording dispatched actions on the manager. Dispose the result to stop.</summary>
    public static BlexActionLog RecordActions(this BlexManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return new BlexActionLog(manager);
    }

    /// <summary>Returns the store's current state as a JSON snapshot (alias for SerializeState).</summary>
    public static JsonObject Snapshot(this IBlexStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store.SerializeState();
    }

    /// <summary>
    /// Waits until <paramref name="condition"/> becomes true, re-evaluating it on every store
    /// change. Throws <see cref="TimeoutException"/> after <paramref name="timeout"/> (default
    /// 5 seconds). The async analog of "waitFor" helpers in JS testing libraries -- useful for
    /// asserting on effects without arbitrary <c>Task.Delay</c> calls.
    /// </summary>
    /// <example><code>await store.WaitForAsync(() => !store.LoadIsLoading);</code></example>
    public static async Task WaitForAsync(this IBlexStore store, Func<bool> condition, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(condition);

        if (condition())
            return;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler()
        {
            try
            {
                if (condition())
                    tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        store.StateChanged += Handler;
        using var timeoutCts = new CancellationTokenSource();
        try
        {
            // Re-check after subscribing to close the race with a change that happened in between.
            Handler();
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout ?? TimeSpan.FromSeconds(5), timeoutCts.Token)).ConfigureAwait(false);
            if (completed != tcs.Task)
                throw new TimeoutException("The store did not reach the expected state in time.");
            timeoutCts.Cancel(); // release the timer instead of letting it run out
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            store.StateChanged -= Handler;
        }
    }

    /// <summary>
    /// Waits until <paramref name="condition"/> becomes true, re-evaluating it after every action
    /// and every state restore in the application. The cross-store counterpart to
    /// <see cref="WaitForAsync(IBlexStore, Func{bool}, TimeSpan?)"/>: use it when the condition
    /// spans stores, or when the store that will satisfy it is not the one being driven.
    /// Throws <see cref="TimeoutException"/> after <paramref name="timeout"/> (default 5 seconds).
    /// </summary>
    /// <example><code>await manager.WaitForAsync(() => !cart.CheckoutIsLoading &amp;&amp; orders.Count == 1);</code></example>
    public static async Task WaitForAsync(this BlexManager manager, Func<bool> condition, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(condition);

        if (condition())
            return;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Evaluate()
        {
            try
            {
                if (condition())
                    tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        // The manager isolates subscriber exceptions, so a throwing condition would otherwise be
        // reported and swallowed rather than failing the test; Evaluate captures it instead.
        using var subscription = manager.Subscribe(_ => Evaluate());
        void OnRestored() => Evaluate();
        manager.StateRestored += OnRestored;

        using var timeoutCts = new CancellationTokenSource();
        try
        {
            // Re-check after subscribing to close the race with a change that happened in between.
            Evaluate();
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout ?? TimeSpan.FromSeconds(5), timeoutCts.Token)).ConfigureAwait(false);
            if (completed != tcs.Task)
                throw new TimeoutException("The application did not reach the expected state in time.");

            timeoutCts.Cancel(); // release the timer instead of letting it run out
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            manager.StateRestored -= OnRestored;
        }
    }

    /// <summary>
    /// Dispatches recorded actions again, in order, against the stores of <paramref name="manager"/>
    /// with the same names -- typically a fresh harness's. Each one runs through its generated wrapper,
    /// so filters, middleware and effects behave exactly as they did the first time.
    /// </summary>
    /// <param name="manager">The manager whose stores receive the actions.</param>
    /// <param name="actions">The actions to replay, usually <see cref="BlexActionLog.Actions"/>.</param>
    /// <remarks>
    /// <para>
    /// Replaying is how a state model proves it is deterministic: the same actions applied to the same
    /// initial state must arrive at the same state, whichever session produced them. It is also how a
    /// bug report becomes a test -- record the actions that led to it, then replay them.
    /// </para>
    /// <para>
    /// Generated actions and effects, <c>"Set X"</c> assignments and <c>ResetState</c> replay. A
    /// <see cref="BlexStoreBase.Batch"/> carries an arbitrary closure rather than data, so it cannot,
    /// and an <see cref="InvalidOperationException"/> names the first action that cannot be replayed.
    /// The list is copied first, so replaying a log into the manager that is recording it is safe.
    /// </para>
    /// </remarks>
    /// <example><code>
    /// using var original = BlexTestHarness.For&lt;CartStore&gt;();
    /// original.Store.Add("apple");
    /// original.Store.Add("pear");
    ///
    /// using var replica = BlexTestHarness.For&lt;CartStore&gt;();
    /// await replica.Manager.ReplayAsync(original.Log.Actions);
    /// Assert.AreEqual(original.Snapshot().ToJsonString(), replica.Snapshot().ToJsonString());
    /// </code></example>
    public static async Task ReplayAsync(this BlexManager manager, IEnumerable<BlexRecordedAction> actions)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(actions);

        foreach (var action in actions.ToList())
            await manager.DispatchAsync(action.QualifiedName, action.Args).ConfigureAwait(false);
    }

    /// <summary>
    /// Counts how many times the store notifies <see cref="IBlexStore.StateChanged"/> while running
    /// <paramref name="act"/>. Useful for asserting batching (e.g. one notification per action).
    /// </summary>
    public static int CountNotifications(this IBlexStore store, Action act)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(act);
        var count = 0;
        void Handler() => count++;
        store.StateChanged += Handler;
        try
        {
            act();
        }
        finally
        {
            store.StateChanged -= Handler;
        }

        return count;
    }
}
