using System.Collections.ObjectModel;
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
    private readonly ReadOnlyDictionary<string, object?> _readOnlyValues;

    internal RouteParameters(IDictionary<string, object?> values)
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
        catch
        {
            value = null;
            return false;
        }
    }
}
