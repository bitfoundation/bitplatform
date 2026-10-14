using System.Collections;
using System.Globalization;

namespace Bit.Belemetry;

/// <summary>
/// An ordered set of OpenTelemetry attributes (key/value pairs) attached to a resource, span, span event,
/// log record or metric point.
/// </summary>
/// <remarks>
/// Values are normalized on the way in to the types the OTLP <c>AnyValue</c> can carry: <see cref="string"/>,
/// <see cref="bool"/>, <see cref="long"/>, <see cref="double"/>, <c>byte[]</c>, and arrays of
/// <see cref="string"/> / <see cref="bool"/> / <see cref="long"/> / <see cref="double"/>. Anything else is
/// stored as its invariant-culture string. Keys are unique: setting an existing key replaces its value, and
/// setting <see langword="null"/> removes it. The number of attributes is capped (see
/// <see cref="DefaultLimit"/>); attributes past the cap are counted in <see cref="DroppedCount"/>, as the
/// OpenTelemetry attribute-limits spec requires, instead of growing without bound.
/// </remarks>
public sealed class BelemetryAttributes : IEnumerable<KeyValuePair<string, object>>
{
    /// <summary>The default maximum number of attributes per collection (the OpenTelemetry default attribute count limit).</summary>
    public const int DefaultLimit = 128;

    private readonly List<KeyValuePair<string, object>> _items;
    private readonly int _limit;

    /// <summary>Creates an empty collection.</summary>
    public BelemetryAttributes() : this(DefaultLimit) { }

    /// <summary>Creates an empty collection holding at most <paramref name="limit"/> attributes.</summary>
    public BelemetryAttributes(int limit)
    {
        _limit = limit <= 0 ? DefaultLimit : limit;
        _items = [];
    }

    /// <summary>The number of attributes.</summary>
    public int Count => _items.Count;

    /// <summary>The number of attributes rejected because the collection was full.</summary>
    public int DroppedCount { get; private set; }

    /// <summary>Gets the value of <paramref name="key"/>, or <see langword="null"/> when absent. Setting follows <see cref="Set"/>.</summary>
    public object? this[string key]
    {
        get => TryGetValue(key, out var value) ? value : null;
        set => Set(key, value);
    }

    /// <summary>
    /// Sets <paramref name="key"/> to the normalized <paramref name="value"/>, replacing an existing value;
    /// a <see langword="null"/> value removes the key.
    /// </summary>
    public BelemetryAttributes Set(string key, object? value)
    {
        if (string.IsNullOrEmpty(key)) return this;

        var normalized = Normalize(value);
        var index = IndexOf(key);

        if (normalized is null)
        {
            if (index >= 0) _items.RemoveAt(index);
            return this;
        }

        if (index >= 0)
        {
            _items[index] = new(key, normalized);
        }
        else if (_items.Count < _limit)
        {
            _items.Add(new(key, normalized));
        }
        else
        {
            DroppedCount++;
        }

        return this;
    }

    /// <summary>Sets every pair of <paramref name="attributes"/>.</summary>
    public BelemetryAttributes SetRange(IEnumerable<KeyValuePair<string, object?>>? attributes)
    {
        if (attributes is null) return this;
        foreach (var (key, value) in attributes) Set(key, value);
        return this;
    }

    /// <summary>Sets <paramref name="key"/> only when it is not present yet.</summary>
    public BelemetryAttributes SetIfAbsent(string key, object? value)
    {
        if (IndexOf(key) < 0) Set(key, value);
        return this;
    }

    /// <summary>Gets the value of <paramref name="key"/>.</summary>
    public bool TryGetValue(string key, out object? value)
    {
        var index = IndexOf(key);
        value = index >= 0 ? _items[index].Value : null;
        return index >= 0;
    }

    /// <summary>Whether <paramref name="key"/> is present.</summary>
    public bool Contains(string key) => IndexOf(key) >= 0;

    /// <summary>Gets the value of <paramref name="key"/> as a string (formatting non-string values with the invariant culture).</summary>
    public string? GetString(string key) => TryGetValue(key, out var value) ? ToInvariantString(value) : null;

    /// <summary>Gets the value of <paramref name="key"/> as a 64-bit integer, when it is numeric or a parsable string.</summary>
    public long? GetInt64(string key)
    {
        if (TryGetValue(key, out var value) is false) return null;
        return value switch
        {
            long l => l,
            double d when d is >= long.MinValue and <= long.MaxValue => (long)d,
            string s when long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    /// <summary>Gets the value of <paramref name="key"/> as a double, when it is numeric or a parsable string.</summary>
    public double? GetDouble(string key)
    {
        if (TryGetValue(key, out var value) is false) return null;
        return value switch
        {
            double d => d,
            long l => l,
            string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    /// <summary>Removes <paramref name="key"/>.</summary>
    public bool Remove(string key)
    {
        var index = IndexOf(key);
        if (index < 0) return false;
        _items.RemoveAt(index);
        return true;
    }

    /// <summary>Removes every attribute for which <paramref name="predicate"/> returns <see langword="true"/>.</summary>
    public int RemoveWhere(Func<string, object, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _items.RemoveAll(kv => predicate(kv.Key, kv.Value));
    }

    /// <summary>Removes every attribute.</summary>
    public void Clear()
    {
        _items.Clear();
        DroppedCount = 0;
    }

    /// <summary>Creates an independent copy.</summary>
    public BelemetryAttributes Clone()
    {
        var clone = new BelemetryAttributes(_limit);
        clone._items.AddRange(_items);
        clone.DroppedCount = DroppedCount;
        return clone;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int IndexOf(string key)
    {
        // Linear search on purpose: attribute sets are small (typically < 20), where a scan over a list beats
        // the hashing and allocation of a dictionary, and the list keeps insertion order for stable output.
        for (var i = 0; i < _items.Count; i++)
        {
            if (string.Equals(_items[i].Key, key, StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    /// <summary>Formats an attribute value the way the Azure Monitor exporter and the console exporter print it.</summary>
    public static string ToInvariantString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "true" : "false",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            string[] a => "[" + string.Join(",", a) + "]",
            long[] a => "[" + string.Join(",", a.Select(x => x.ToString(CultureInfo.InvariantCulture))) + "]",
            double[] a => "[" + string.Join(",", a.Select(x => x.ToString("R", CultureInfo.InvariantCulture))) + "]",
            bool[] a => "[" + string.Join(",", a.Select(x => x ? "true" : "false")) + "]",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    internal static object? Normalize(object? value)
    {
        switch (value)
        {
            case null: return null;
            case string s: return s;
            case bool: return value;
            case long: return value;
            case double: return value;
            case int i: return (long)i;
            case short s16: return (long)s16;
            case byte b: return (long)b;
            case sbyte sb: return (long)sb;
            case ushort us: return (long)us;
            case uint ui: return (long)ui;
            case ulong ul: return ul <= long.MaxValue ? (long)ul : ul.ToString(CultureInfo.InvariantCulture);
            case float f: return (double)f;
            case decimal m: return (double)m;
            case char c: return c.ToString();
            case byte[]: return value;
            case string[]: return value;
            case long[]: return value;
            case double[]: return value;
            case bool[]: return value;
            case int[] ia: return ia.Select(x => (long)x).ToArray();
            case float[] fa: return fa.Select(x => (double)x).ToArray();
            case Enum e: return e.ToString();
            case DateTime dt: return dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            case DateTimeOffset dto: return dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            case TimeSpan ts: return ts.TotalMilliseconds;
            case Guid g: return g.ToString();
            case Uri u: return u.ToString();
            case IEnumerable<string> strings: return strings.ToArray();
            case IEnumerable enumerable:
                {
                    var list = new List<string>();
                    foreach (var item in enumerable) list.Add(ToInvariantString(Normalize(item)));
                    return list.ToArray();
                }
            case IFormattable formattable: return formattable.ToString(null, CultureInfo.InvariantCulture);
            default: return value.ToString();
        }
    }
}
