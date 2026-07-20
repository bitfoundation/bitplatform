// Core PDF object primitives: names, commands, references and dictionaries.

using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// A PDF dictionary. Keys are name strings (without the leading slash); values
/// are PDF objects. When an <see cref="IBitPdfXRef"/> is attached, <see cref="Get(string)"/>
/// transparently resolves indirect references.
/// </summary>
public sealed class BitPdfDict
{
    private readonly Dictionary<string, object?> _map = new(StringComparer.Ordinal);

    /// <summary>The cross-reference table used to resolve indirect values, if any.</summary>
    public IBitPdfXRef? XRef { get; set; }

    /// <summary>An immutable, empty dictionary.</summary>
    public static readonly BitPdfDict Empty = new();

    public BitPdfDict(IBitPdfXRef? xref = null) => XRef = xref;

    /// <summary>Number of entries.</summary>
    public int Count => _map.Count;

    /// <summary>All keys present in the dictionary.</summary>
    public IEnumerable<string> Keys => _map.Keys;

    /// <summary>The raw (unresolved) entries.</summary>
    public IReadOnlyDictionary<string, object?> RawEntries => _map;

    public void Set(string key, object? value) => _map[key] = value;
    public void Set(BitPdfName key, object? value) => _map[key.Value] = value;

    public bool Has(string key) => _map.ContainsKey(key);

    /// <summary>Gets a value, resolving an indirect reference through <see cref="XRef"/> if present.</summary>
    public object? Get(string key)
    {
        if (!_map.TryGetValue(key, out var value))
        {
            return null;
        }
        return value is BitPdfRef r && XRef is not null ? XRef.Fetch(r) : value;
    }

    /// <summary>
    /// Gets the first present value among <paramref name="key1"/>, <paramref name="key2"/>
    /// (e.g. abbreviated inline-image keys).
    /// </summary>
    public object? Get(string key1, string key2)
        => _map.ContainsKey(key1) ? Get(key1) : Get(key2);

    public object? Get(string key1, string key2, string key3)
        => _map.ContainsKey(key1) ? Get(key1)
         : _map.ContainsKey(key2) ? Get(key2)
         : Get(key3);

    /// <summary>Gets a value without resolving references.</summary>
    public object? GetRaw(string key) => _map.TryGetValue(key, out var v) ? v : null;

    /// <summary>Gets a strongly typed value, or <c>default</c> if absent / wrong type.</summary>
    public T? GetValue<T>(string key) => Get(key) is T t ? t : default;

    public override string ToString() => $"<< {string.Join(" ", _map.Keys.Select(static k => "/" + k))} >>";
}
