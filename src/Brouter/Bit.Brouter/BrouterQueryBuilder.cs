using System.Text;

namespace Bit.Brouter;

/// <summary>
/// Mutable view of a URL's query string for functional updates via
/// <see cref="IBrouter.NavigateWithQuery"/>: seeded with the current query's pairs, mutated with
/// <see cref="Set"/>/<see cref="SetAll"/>/<see cref="Remove"/>/<see cref="Clear"/>, then serialized
/// back. Untouched parameters are preserved - the point of the API (TanStack Router's functional
/// search updates / <c>retainSearchParams</c>).
/// </summary>
public sealed class BrouterQueryBuilder
{
    // Insertion-ordered so the emitted query keeps a stable, predictable parameter order.
    private readonly List<string> _order = [];
    private readonly Dictionary<string, List<string>> _values = new(StringComparer.OrdinalIgnoreCase);

    internal BrouterQueryBuilder(BrouterLocation location)
    {
        foreach (var pair in location.QueryParams)
        {
            _order.Add(pair.Key);
            _values[pair.Key] = [.. pair.Value];
        }
    }

    /// <summary>
    /// Sets <paramref name="key"/> to a single value, replacing any existing values. A null value
    /// removes the parameter (mirroring how null route parameters mean "absent"). Non-string values
    /// are formatted invariantly the same way <see cref="IBrouter.ResolveUrl"/> formats them.
    /// </summary>
    public BrouterQueryBuilder Set(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        if (value is null) return Remove(key);

        if (_values.ContainsKey(key) is false) _order.Add(key);
        _values[key] = [BrouterService.FormatRouteValue(value)];
        return this;
    }

    /// <summary>Sets <paramref name="key"/> to multiple values (<c>?tag=a&amp;tag=b</c>), replacing existing ones. Null items are skipped; an empty set removes the key.</summary>
    public BrouterQueryBuilder SetAll(string key, IEnumerable<object?> values)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(values);

        List<string> formatted = [];
        foreach (var value in values)
        {
            if (value is null) continue;
            formatted.Add(BrouterService.FormatRouteValue(value));
        }
        if (formatted.Count == 0) return Remove(key);

        if (_values.ContainsKey(key) is false) _order.Add(key);
        _values[key] = formatted;
        return this;
    }

    /// <summary>Removes <paramref name="key"/> entirely.</summary>
    public BrouterQueryBuilder Remove(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        if (_values.Remove(key))
        {
            _order.RemoveAll(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        }
        return this;
    }

    /// <summary>Removes every parameter.</summary>
    public BrouterQueryBuilder Clear()
    {
        _order.Clear();
        _values.Clear();
        return this;
    }

    /// <summary>The current single value for <paramref name="key"/> (first when multi-valued), or null.</summary>
    public string? Get(string key) =>
        _values.TryGetValue(key, out var list) && list.Count > 0 ? list[0] : null;

    /// <summary>Whether <paramref name="key"/> is present.</summary>
    public bool Contains(string key) => _values.ContainsKey(key);

    /// <summary>Serializes back to a query string including the leading '?', or an empty string when empty.</summary>
    public string ToQueryString()
    {
        if (_order.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var key in _order)
        {
            if (_values.TryGetValue(key, out var values) is false) continue;
            foreach (var value in values)
            {
                sb.Append(sb.Length == 0 ? '?' : '&');
                sb.Append(Uri.EscapeDataString(key));
                if (value.Length > 0)
                {
                    sb.Append('=').Append(Uri.EscapeDataString(value));
                }
            }
        }
        return sb.ToString();
    }
}
