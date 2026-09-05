using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Bit.Butil;

/// <summary>
/// The <c>Headers</c> half of the fetch object model: an ordered list of name/value pairs where a
/// name may appear more than once, matched case-insensitively the way HTTP defines it.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Headers">https://developer.mozilla.org/en-US/docs/Web/API/Headers</see>
/// </summary>
/// <remarks>
/// A dictionary cannot express a response's headers: <c>Link</c>, <c>Vary</c>, <c>Warning</c> and
/// <c>Set-Cookie</c> all repeat. This keeps the repeats - <see cref="GetAll(string)"/> returns them
/// all, while <see cref="Get(string)"/> joins them with ", " as the Headers specification does.
/// <br/>
/// What a response actually hands back is a separate question: fetch exposes only the permitted
/// response headers - the CORS-safelisted ones plus whatever <c>Access-Control-Expose-Headers</c>
/// names, and never the forbidden ones. <c>Set-Cookie</c> is filtered out of a fetch response by
/// every browser, so it is absent here regardless of what the server sent.
/// <br/>
/// It converts to and from <see cref="Dictionary{TKey, TValue}"/> implicitly, so code written
/// against the dictionary this replaced keeps working; the conversion to a dictionary is the lossy
/// direction, joining any repeated name.
/// </remarks>
[JsonConverter(typeof(FetchHeadersJsonConverter))]
public class FetchHeaders : IEnumerable<KeyValuePair<string, string>>
{
    private readonly List<KeyValuePair<string, string>> _entries = [];

    /// <summary>An empty header list.</summary>
    public FetchHeaders() { }

    /// <summary>A header list holding <paramref name="entries"/>, in the order given.</summary>
    public FetchHeaders(IEnumerable<KeyValuePair<string, string>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            Append(entry.Key, entry.Value);
        }
    }

    /// <summary>How many name/value pairs there are - a name that repeats counts once per occurrence.</summary>
    [JsonIgnore]
    public int Count => _entries.Count;

    /// <summary>
    /// The combined value of <paramref name="name"/>, or null when it is absent. Assigning replaces
    /// every occurrence of the name; assigning null removes it.
    /// </summary>
    [JsonIgnore]
    public string? this[string name]
    {
        get => Get(name);
        set => Set(name, value);
    }

    /// <summary>
    /// Adds a header, keeping any that is already there under the same name. This is
    /// <c>Headers.append()</c>, and the method to use for a name that legitimately repeats.
    /// </summary>
    public FetchHeaders Append(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        _entries.Add(new KeyValuePair<string, string>(name, value ?? string.Empty));
        return this;
    }

    /// <summary>Adds a header. The name this type needs for collection initializers; identical to <see cref="Append(string, string)"/>.</summary>
    public void Add(string name, string value) => Append(name, value);

    /// <summary>
    /// Replaces every occurrence of <paramref name="name"/> with a single header of
    /// <paramref name="value"/>, or removes the name when <paramref name="value"/> is null.
    /// </summary>
    public FetchHeaders Set(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Remove(name);
        return value is null ? this : Append(name, value);
    }

    /// <summary>
    /// The value of <paramref name="name"/>, with repeats joined by ", " as the Headers
    /// specification defines, or null when the header is absent.
    /// </summary>
    public string? Get(string name)
    {
        var values = GetAll(name);
        return values.Length == 0 ? null : string.Join(", ", values);
    }

    /// <summary>Every value sent under <paramref name="name"/>, in order. Empty when the header is absent.</summary>
    public string[] GetAll(string name)
        => [.. _entries.Where(e => Matches(e.Key, name)).Select(e => e.Value)];

    /// <summary>Whether the header is present at all - the distinction between absent and present-but-empty.</summary>
    public bool Has(string name) => _entries.Exists(e => Matches(e.Key, name));

    /// <summary>Removes every occurrence of <paramref name="name"/>. Returns whether anything was removed.</summary>
    public bool Remove(string name) => _entries.RemoveAll(e => Matches(e.Key, name)) > 0;

    /// <summary>Removes every header.</summary>
    public void Clear() => _entries.Clear();

    /// <summary>The distinct header names present, in the order they first appear.</summary>
    [JsonIgnore]
    public IEnumerable<string> Names => _entries.Select(e => e.Key).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The headers as a dictionary, repeats joined by ", ". Lossy by construction - use
    /// <see cref="GetAll(string)"/> when the repeats matter.
    /// </summary>
    public Dictionary<string, string> ToDictionary()
        => Names.ToDictionary(name => name, name => Get(name)!, StringComparer.OrdinalIgnoreCase);

    /// <summary>Reads a dictionary of headers as a <see cref="FetchHeaders"/>.</summary>
    public static implicit operator FetchHeaders(Dictionary<string, string> headers) => new(headers);

    /// <summary>Flattens the headers into a dictionary - see <see cref="ToDictionary"/> for what that loses.</summary>
    public static implicit operator Dictionary<string, string>(FetchHeaders headers) => headers.ToDictionary();

    /// <summary>Enumerates every name/value pair, repeats included, in order.</summary>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // HTTP field names are case-insensitive, and the browser lowercases the ones it hands back - so
    // a response looked up by the casing it was sent with has to still be found.
    private static bool Matches(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
