using Microsoft.AspNetCore.Components;

namespace Bit.Blex.Blazor;

/// <summary>
/// Optional base component that re-renders automatically when subscribed stores change.
/// Call <see cref="Subscribe(ReadOnlySpan{IBlexStore})"/> in <c>OnInitialized</c>; unsubscription is automatic.
/// </summary>
public abstract class BlexComponentBase : ComponentBase, IDisposable
{
    private readonly List<IBlexStore> _subscriptions = [];
    private readonly List<IDisposable> _selectorSubscriptions = [];
    private bool _disposed;

    /// <summary>Subscribes to one or more stores; the component re-renders on any change.</summary>
    protected void Subscribe(params ReadOnlySpan<IBlexStore> stores)
    {
        // Subscribing after disposal would attach a handler nothing will ever detach.
        if (_disposed)
            return;

        foreach (var store in stores)
        {
            if (store is null || _subscriptions.Contains(store))
                continue;
            store.StateChanged += OnStoreChanged;
            _subscriptions.Add(store);
        }
    }

    /// <summary>
    /// Subscribes to a projection of a store; the component re-renders only when the selected
    /// value changes, ignoring unrelated mutations to the same store. Useful for stores with many
    /// independent <c>[BlexStateAttribute]</c> fields or large collections.
    /// </summary>
    /// <example><code>Subscribe(Store, () => Store.Count);</code></example>
    protected void Subscribe<T>(IBlexStore store, Func<T> selector, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        if (_disposed)
            return;

        _selectorSubscriptions.Add(store.Subscribe(selector, (Action<T>)(_ => OnStoreChanged()), comparer));
    }

    /// <summary>
    /// Registers an arbitrary subscription token to be disposed with the component (e.g. a
    /// cross-store <c>manager.SubscribeTo&lt;T&gt;(...)</c> token or a selector subscription
    /// created manually).
    /// </summary>
    protected void OwnsSubscription(IDisposable subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        if (_disposed)
        {
            // Nothing left to own it; dispose immediately rather than leak it.
            subscription.Dispose();
            return;
        }

        _selectorSubscriptions.Add(subscription);
    }

    private void OnStoreChanged()
    {
        if (_disposed)
            return;

        var task = InvokeAsync(StateHasChanged);
        if (task.IsCompletedSuccessfully)
            return;

        // Discarding the task would turn a teardown race -- a store notifying just after the
        // renderer went away -- into an unobserved task exception. Observe it, ignore the shutdown
        // cases, and route anything else to the error boundary the way Blazor expects.
        _ = ObserveAsync(task);

        async Task ObserveAsync(Task pending)
        {
            try
            {
                await pending;
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
            {
                // Renderer or circuit gone; the render is moot.
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var store in _subscriptions)
            store.StateChanged -= OnStoreChanged;
        _subscriptions.Clear();
        foreach (var sub in _selectorSubscriptions)
            sub.Dispose();
        _selectorSubscriptions.Clear();
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override to release additional resources when the component is disposed (the store
    /// subscriptions are already detached when this runs).
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}
