using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.Brouter;

/// <summary>
/// A typed view over the matched route parameters. Inspired by <c>useParams</c> in React Router
/// and Angular's <c>ActivatedRoute.snapshot.params</c>, with type-safe accessors.
/// </summary>
public sealed class BrouterRouteParameters
{
    /// <summary>An empty parameters instance.</summary>
    public static readonly BrouterRouteParameters Empty = new(new Dictionary<string, object?>());

    private readonly Dictionary<string, object?> _values;
    private readonly ReadOnlyDictionary<string, object?> _readOnlyValues;

    internal BrouterRouteParameters(IReadOnlyDictionary<string, object?> values)
    {
        _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        _readOnlyValues = new ReadOnlyDictionary<string, object?>(_values);
    }

    /// <summary>Raw parameter values keyed by name (read-only).</summary>
    public IReadOnlyDictionary<string, object?> Values => _readOnlyValues;

    /// <summary>Returns the raw value or null if missing.</summary>
    public object? this[string key] => _values.TryGetValue(key, out var v) ? v : null;

    /// <summary>Returns true when a parameter with the given name exists.</summary>
    public bool Contains(string key) => _values.ContainsKey(key);

    /// <summary>
    /// Returns the parameter as <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no parameter named <paramref name="key"/> is present. Use
    /// <see cref="GetOrDefault{T}"/> or <see cref="TryGet{T}(string, out T)"/> when absence
    /// is expected.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the parameter exists but cannot be converted to <typeparamref name="T"/>
    /// (e.g. the URL provided <c>"abc"</c> for an <c>int</c> parameter that wasn't constrained
    /// at the template). The parameter name and target type are both included in the message.
    /// </exception>
    public T Get<T>(string key)
    {
        // Differentiate "missing" from "present but unconvertible": the two failure modes
        // call for different reactions at the call site (one is a routing/template bug, the
        // other is a value/typing mismatch). Throwing KeyNotFoundException for both, as the
        // previous version did, conflated them.
        if (Contains(key) is false)
            throw new KeyNotFoundException($"Route parameter '{key}' is missing.");

        if (TryGet<T>(key, out var value)) return value!;

        throw new InvalidOperationException(
            $"Route parameter '{key}' is present but cannot be converted to {typeof(T).Name}. " +
            "Add a route constraint (e.g. {{id:int}}) or use TryGet/GetOrDefault to handle the conversion failure explicitly.");
    }

    /// <summary>Returns the parameter as <typeparamref name="T"/> or <paramref name="defaultValue"/> when missing/unconvertible.</summary>
    public T? GetOrDefault<T>(string key, T? defaultValue = default) =>
        TryGet<T>(key, out var value) ? value : defaultValue;

    /// <summary>Tries to read the parameter as <typeparamref name="T"/>.</summary>
    public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
    {
        if (TryGetWeak(key, typeof(T), out var raw) && raw is T t)
        {
            value = t;
            return true;
        }
        value = default;
        return false;
    }

    internal bool TryGetWeak(string key, Type targetType, out object? value)
    {
        value = null;
        if (_values.TryGetValue(key, out var raw) is false || raw is null) return false;

        if (targetType.IsInstanceOfType(raw))
        {
            value = raw;
            return true;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Convert.ChangeType doesn't support string -> Guid or string -> Enum, so handle them
        // explicitly before falling back. Nullable<T> is honored because we resolved the
        // underlying type above; assigning a boxed Guid/enum value is compatible with the
        // Nullable<T> field assignment performed by the caller.
        if (raw is string str)
        {
            if (underlying == typeof(Guid))
            {
                if (Guid.TryParse(str, out var guidVal))
                {
                    value = guidVal;
                    return true;
                }
                return false;
            }

            if (underlying.IsEnum)
            {
                if (Enum.TryParse(underlying, str, ignoreCase: true, out var enumVal))
                {
                    value = enumVal;
                    return true;
                }
                return false;
            }
        }

        try
        {
            value = Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            // Narrow the catch so genuine programming errors (e.g. NullReferenceException
            // from a buggy IConvertible implementation, OutOfMemoryException, etc.) still
            // surface. The four types above are the documented failure modes for
            // Convert.ChangeType when given a string -> primitive/decimal/datetime conversion.
            value = null;
            return false;
        }
    }
}
