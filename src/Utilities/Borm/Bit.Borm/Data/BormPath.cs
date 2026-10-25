using System.Globalization;

namespace Bit.Borm;

/// <summary>
/// Parses and represents dotted/indexed form paths such as <c>address.city</c> or <c>items[0].name</c>.
/// </summary>
public static class BormPath
{
    private static readonly IReadOnlyList<BormPathSegment> NoSegments = Array.AsReadOnly(Array.Empty<BormPathSegment>());

    // Every read and write of a value parses its path, and a render reads every field's: the same few
    // hundred strings, parsed again and again into the same segments. Remembered here, read-only so
    // no caller can change what the next one is handed, and bounded, because row paths are built from
    // indices and a long-lived process should not keep every one it ever saw.
    private const int CacheLimit = 16_384;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, IReadOnlyList<BormPathSegment>> Cache = new(StringComparer.Ordinal);

    /// <summary>Parses a path string into segments.</summary>
    /// <exception cref="FormatException">The path has an unterminated or non-numeric index.</exception>
    public static IReadOnlyList<BormPathSegment> Parse(string path)
    {
        if (string.IsNullOrEmpty(path)) return NoSegments;
        if (Cache.TryGetValue(path, out var cached)) return cached;

        var parsed = Array.AsReadOnly(ParseUncached(path).ToArray());
        if (Cache.Count >= CacheLimit) Cache.Clear();
        Cache[path] = parsed;
        return parsed;
    }

    private static List<BormPathSegment> ParseUncached(string path)
    {
        var segments = new List<BormPathSegment>();

        var i = 0;
        var current = new System.Text.StringBuilder();

        void FlushName()
        {
            if (current.Length > 0)
            {
                segments.Add(BormPathSegment.Property(current.ToString()));
                current.Clear();
            }
        }

        while (i < path.Length)
        {
            var c = path[i];
            switch (c)
            {
                case '.':
                    FlushName();
                    i++;
                    break;
                case '[':
                    FlushName();
                    var close = path.IndexOf(']', i);
                    if (close < 0) throw new FormatException($"Unterminated index in path '{path}'.");
                    var inner = path[(i + 1)..close];
                    if (!int.TryParse(inner, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) || index < 0)
                        throw new FormatException($"Invalid index '{inner}' in path '{path}'; expected a non-negative integer.");
                    segments.Add(BormPathSegment.At(index));
                    i = close + 1;
                    break;
                default:
                    current.Append(c);
                    i++;
                    break;
            }
        }

        FlushName();
        return segments;
    }

    /// <summary>Returns true when <paramref name="path"/> is a well-formed form path.</summary>
    public static bool IsValid(string path)
    {
        // A path parsed before is valid by definition, and asking is a dictionary read.
        try
        {
            Parse(path);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Joins a parent path and a child key into a single path.</summary>
    public static string Combine(string parent, string child)
        => string.IsNullOrEmpty(parent) ? child : $"{parent}.{child}";

    /// <summary>Builds an indexed path, e.g. <c>Combine("items", 2) =&gt; "items[2]"</c>.</summary>
    public static string Combine(string parent, int index)
        => string.Create(CultureInfo.InvariantCulture, $"{parent}[{index}]");

    /// <summary>
    /// The path of the container holding <paramref name="path"/>: <c>Lines[2].Total</c> yields
    /// <c>Lines[2]</c>, and a root-level path yields an empty string.
    /// </summary>
    public static string Parent(string path)
    {
        var cut = Math.Max(path.LastIndexOf('.'), path.LastIndexOf('['));
        return cut <= 0 ? string.Empty : path[..cut];
    }

    /// <summary>
    /// True when <paramref name="path"/> is <paramref name="ancestor"/> itself or nested beneath it —
    /// the test used to scope validation messages to a subtree.
    /// </summary>
    public static bool IsAtOrUnder(string path, string ancestor)
        => path.Equals(ancestor, StringComparison.OrdinalIgnoreCase)
           || path.StartsWith(ancestor + ".", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith(ancestor + "[", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Rewrites the index of the array element a path points into, e.g.
    /// <c>Reindex("items[2].name", "items", 1)</c> returns <c>items[1].name</c>. Returns null when the
    /// path does not address an element of <paramref name="arrayPath"/>.
    /// </summary>
    public static string? Reindex(string path, string arrayPath, int newIndex)
    {
        var prefix = arrayPath + "[";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var close = path.IndexOf(']', prefix.Length);
        if (close < 0) return null;
        return string.Concat(arrayPath, "[", newIndex.ToString(CultureInfo.InvariantCulture), "]", path[(close + 1)..]);
    }

    /// <summary>
    /// Extracts the index of the array element a path points into, e.g.
    /// <c>IndexIn("items[2].name", "items")</c> returns 2. Returns null when the path is not an element
    /// of <paramref name="arrayPath"/>.
    /// </summary>
    public static int? IndexIn(string path, string arrayPath)
    {
        var prefix = arrayPath + "[";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var close = path.IndexOf(']', prefix.Length);
        if (close < 0) return null;
        return int.TryParse(path[prefix.Length..close], NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            ? i
            : null;
    }
}
