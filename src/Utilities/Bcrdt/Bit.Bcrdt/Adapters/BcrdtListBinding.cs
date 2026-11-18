using System.Collections;
using System.Collections.Specialized;
using System.Text.Json;

namespace Bit.Bcrdt;

/// <summary>
/// A live, typed, observable projection of a collaborative sequence.
/// </summary>
/// <remarks>
/// <para>
/// Rendering a collaborative list means answering "what does it contain now?" many times a second
/// while it changes underneath you. Reading the whole sequence on every render is wasteful, and
/// caching it without invalidation is wrong. This keeps a materialized snapshot, refreshes it when
/// the underlying type raises a change, and reports the change through
/// <see cref="INotifyCollectionChanged"/> so a view can patch rather than rebuild.
/// </para>
/// <para>
/// Elements are projected whenever the sequence changes: nested collaborative types come back as
/// themselves and primitives come back as <typeparamref name="T"/>. The binding always has exactly as
/// many elements as <see cref="Source"/>, index for index; an element that cannot be represented as
/// <typeparamref name="T"/> appears as <c>default(T)</c>.
/// </para>
/// </remarks>
public sealed class BcrdtListBinding<T> : IReadOnlyList<T>, INotifyCollectionChanged, IDisposable
{
    private readonly BcrdtArray _array;
    private readonly IDisposable _subscription;
    private List<T> _snapshot;
    private bool _disposed;

    /// <summary>Binds to a collaborative sequence.</summary>
    public BcrdtListBinding(BcrdtArray array)
    {
        ArgumentNullException.ThrowIfNull(array);

        _array = array;
        _snapshot = Project();
        _subscription = array.Observe(OnChanged);
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>Raised after the snapshot has been refreshed, for consumers that just want to re-render.</summary>
    public event Action? Changed;

    /// <inheritdoc/>
    public int Count => _snapshot.Count;

    /// <inheritdoc/>
    public T this[int index] => _snapshot[index];

    /// <summary>The sequence this binding projects.</summary>
    public BcrdtArray Source => _array;

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => _snapshot.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnChanged(BcrdtEvent crdtEvent, BcrdtTransaction transaction)
    {
        _snapshot = Project();

        // A precise incremental notification is possible from the delta, but a reset is correct for
        // every shape of change and lets consumers choose their own granularity from Delta.
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Changed?.Invoke();
    }

    /// <remarks>
    /// Every element of the source produces exactly one element here, so an index into the binding is
    /// always the same index into <see cref="Source"/>. An element that is <see langword="null"/>, or
    /// that a peer stored in a shape <typeparamref name="T"/> cannot represent, appears as
    /// <c>default(T)</c> rather than being dropped - dropping it would shift every later index, and
    /// an edit made through such an index would land on the wrong element. A conversion failure must
    /// also never escape, because this runs inside the document's change notification.
    /// </remarks>
    private List<T> Project()
    {
        List<object?> raw = _array.ToArray();
        var projected = new List<T>(raw.Count);
        foreach (object? value in raw)
        {
            T item = default!;
            if (value is T typed)
            {
                item = typed;
            }
            else if (value is not null and not BcrdtAbstractType)
            {
                try
                {
                    item = BcrdtAnyCodec.ToObject<T>(value)!;
                }
                catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException
                                               or ArgumentException or FormatException or OverflowException)
                {
                    // Not representable as T: keep the slot, so indices stay aligned with the source.
                }
            }
            projected.Add(item);
        }
        return projected;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _subscription.Dispose();
        CollectionChanged = null;
        Changed = null;
    }
}
