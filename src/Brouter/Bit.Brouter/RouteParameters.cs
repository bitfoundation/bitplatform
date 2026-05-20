using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.Brouter;

/// <summary>
/// A typed view over the matched route parameters. Inspired by <c>useParams</c> in React Router
/// and Angular's <c>ActivatedRoute.snapshot.params</c>, with type-safe accessors.
/// </summary>
public sealed class RouteParameters
{
    /// <summary>An empty parameters instance.</summary>
    public static readonly RouteParameters Empty = new(new Dictionary<string, object?>());

    private readonly Dictionary<string, object?> _values;

    internal RouteParameters(IDictionary<string, object?> values) =>
        _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);

    /// <summary>Raw parameter values keyed by name (read-only).</summary>
    public IReadOnlyDictionary<string, object?> Values => _values;

    /// <summary>Returns the raw value or null if missing.</summary>
    public object? this[string key] => _values.TryGetValue(key, out var v) ? v : null;

    /// <summary>Returns true when a parameter with the given name exists.</summary>
    public bool Contains(string key) => _values.ContainsKey(key);

    /// <summary>
    /// Returns the parameter as <typeparamref name="T"/>. Throws if missing or not convertible.
    /// </summary>
    public T Get<T>(string key)
    {
        if (TryGet<T>(key, out var value)) return value!;
        throw new KeyNotFoundException($"Route parameter '{key}' is missing or cannot be converted to {typeof(T).Name}.");
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

        try
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
            value = Convert.ChangeType(raw, underlying, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}
