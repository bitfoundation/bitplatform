using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Bit.Bcrdt;

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
