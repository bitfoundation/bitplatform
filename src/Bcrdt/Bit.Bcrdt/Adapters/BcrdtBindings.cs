using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
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

/// <summary>
/// Binds the properties of a plain object to the keys of a collaborative map.
/// </summary>
/// <remarks>
/// <para>
/// Most collaborative features are not documents - they are forms, settings panels and entity
/// editors whose shape is already expressed as a C# type. This lets that type stay the model:
/// reading a property reads the map, writing one writes the map, and remote changes raise
/// <see cref="INotifyPropertyChanged"/> so existing binding machinery just works.
/// </para>
/// <para>
/// Note that scalar properties carry the map's last-writer-wins semantics. Two people editing the
/// same field concurrently means one value survives - which is usually right for a form field, and
/// wrong for anything a user types into at length. Use a nested <see cref="BcrdtText"/> for the
/// latter. Different fields are independent: <see cref="FromObject"/> writes only the properties the
/// caller actually changed, so saving a form never reverts a field somebody else edited meanwhile.
/// </para>
/// </remarks>
public sealed class BcrdtObjectBinding<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : INotifyPropertyChanged, IDisposable where T : class, new()
{
    // Indexers are excluded: they cannot be read or written without an argument, and have no key.
    private static readonly PropertyInfo[] Properties = [.. typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)];

    private readonly BcrdtMap _map;
    private readonly IDisposable _subscription;
    private readonly Dictionary<string, object?> _baseline = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>Binds a map as an object of type <typeparamref name="T"/>.</summary>
    public BcrdtObjectBinding(BcrdtMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _map = map;
        _subscription = map.Observe(OnChanged);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The map this binding projects.</summary>
    public BcrdtMap Source => _map;

    /// <summary>Reads a property.</summary>
    public TValue? Get<TValue>(Expression<Func<T, TValue>> property) => _map.Get<TValue>(NameOf(property));

    /// <summary>Writes a property.</summary>
    public void Set<TValue>(Expression<Func<T, TValue>> property, TValue value) =>
        _map.Set(NameOf(property), BcrdtAnyCodec.FromObject(value));

    /// <summary>Materializes the whole object.</summary>
    /// <remarks>
    /// The values read become the baseline <see cref="FromObject"/> compares against, so a property
    /// the caller leaves untouched is not written back.
    /// </remarks>
    public T ToObject()
    {
        var instance = new T();
        foreach (PropertyInfo property in Properties)
        {
            object? stored = _map.Get(property.Name);
            if (stored is not null)
            {
                try
                {
                    object? converted = property.PropertyType.IsInstanceOfType(stored)
                        ? stored
                        : ConvertTo(stored, property.PropertyType);
                    property.SetValue(instance, converted);
                }
                catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException
                                               or ArgumentException or JsonException or NotSupportedException)
                {
                    // A peer wrote something that does not fit this property's type. Skip the property
                    // rather than failing the whole read: a schema disagreement between two versions of
                    // an application must not make a document unopenable.
                }
            }

            _baseline[property.Name] = Project(property.GetValue(instance));
        }
        return instance;
    }

    /// <summary>
    /// Writes the properties of an object that differ from what was last read, as one transaction.
    /// </summary>
    /// <remarks>
    /// A property is written only if the caller changed it since the last <see cref="ToObject"/> and
    /// it differs from what the map holds now. Writing every property instead would turn "save this
    /// form" into "revert every field anybody else changed since I loaded it", including fields this
    /// user never touched, and would overwrite a value this version could not read with a default.
    /// Without a preceding <see cref="ToObject"/>, every property that differs from the map is written.
    /// </remarks>
    public void FromObject(T instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        List<(string Name, object? Value)> writes = [];
        foreach (PropertyInfo property in Properties)
        {
            object? projected = Project(property.GetValue(instance));

            if (_baseline.TryGetValue(property.Name, out object? original) && BcrdtAnyCodec.DeepEquals(projected, original))
            {
                continue;
            }

            if (_map.ContainsKey(property.Name) && BcrdtAnyCodec.DeepEquals(_map.Get(property.Name), projected))
            {
                continue;
            }

            writes.Add((property.Name, projected));
        }

        if (writes.Count == 0) return;

        _map.Doc.Transact(_ =>
        {
            foreach ((string name, object? value) in writes) _map.Set(name, value);
        });

        foreach ((string name, object? value) in writes) _baseline[name] = value;
    }

    private static object? Project(object? value) => BcrdtAnyCodec.FromObject(value);

    /// <summary>The inverse of <see cref="BcrdtAnyCodec.FromObject"/> for a single property.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only a value outside the collaborative value model reaches the serializer; such a type has to be preserved by the app (or stored as value-model data) in a trimmed or native AOT publish, as the type documentation says.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only a value outside the collaborative value model reaches the serializer; such a type has to be preserved by the app (or stored as value-model data) in a trimmed or native AOT publish, as the type documentation says.")]
    private static object? ConvertTo(object value, Type target)
    {
        if (value is BcrdtAbstractType)
        {
            throw new InvalidCastException($"A nested collaborative type cannot be read as {target.Name}.");
        }

        Type underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (underlying.IsEnum)
        {
            if (value is string name) return Enum.Parse(underlying, name, ignoreCase: true);
            if (value is double or float or decimal)
            {
                decimal number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (number != decimal.Truncate(number)) throw new FormatException($"{number} is not a valid {underlying.Name}.");
            }
            return Enum.ToObject(underlying, Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }

        // Numbers, booleans and strings convert directly. Everything else - dates, durations, lists,
        // nested objects - was flattened through JSON on the way in, so it is rehydrated the same way.
        if (value is IConvertible && Type.GetTypeCode(underlying) is >= TypeCode.Boolean and <= TypeCode.Decimal or TypeCode.String)
        {
            return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        }

        return JsonSerializer.Deserialize(JsonSerializer.Serialize(value), target);
    }

    private static string NameOf<TValue>(Expression<Func<T, TValue>> property) =>
        property.Body is MemberExpression member
            ? member.Member.Name
            : throw new ArgumentException("Expression must name a property.", nameof(property));

    private void OnChanged(BcrdtEvent crdtEvent, BcrdtTransaction transaction)
    {
        foreach (string key in crdtEvent.Keys.Keys)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _subscription.Dispose();
        PropertyChanged = null;
    }
}
