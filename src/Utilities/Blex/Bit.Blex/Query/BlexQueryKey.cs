using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bit.Blex.Query;

/// <summary>
/// Identifies a query in the <see cref="BlexQueryClient"/> cache: an ordered list of parts, from the
/// general to the specific -- <c>["todos"]</c>, <c>["todos", 42]</c>, <c>["todos", "list", filter]</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two keys are equal when they have the same parts in the same order, compared with each part's own
/// <see cref="object.Equals(object)"/>: strings, numbers, enums, <see cref="Guid"/>s and records compare by
/// value. A part that only compares by reference (a mutable class, an array) makes a key that only ever
/// equals itself -- use a record or the values it is made of instead.
/// </para>
/// <para>
/// A key <see cref="StartsWith">starts with</see> every key made of its leading parts, which is what
/// invalidation matches on: invalidating <c>["todos"]</c> covers <c>["todos", 42]</c> and
/// <c>["todos", "list", filter]</c> alike.
/// </para>
/// </remarks>
/// <example><code>
/// BlexQueryKey all = ["todos"];
/// BlexQueryKey one = ["todos", 42];
/// one.StartsWith(all);   // true
/// </code></example>
[CollectionBuilder(typeof(BlexQueryKey), nameof(Create))]
public sealed class BlexQueryKey : IEquatable<BlexQueryKey>, IReadOnlyList<object?>
{
    private readonly object?[] _parts;
    private readonly int _hash;
    private string? _text;

    /// <summary>Creates a key from its parts, general to specific.</summary>
    public BlexQueryKey(params object?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Length == 0)
            throw new ArgumentException("A query key needs at least one part.", nameof(parts));

        _parts = (object?[])parts.Clone();
        var hash = new HashCode();
        foreach (var part in _parts)
            hash.Add(part);

        _hash = hash.ToHashCode();
    }

    /// <summary>Creates a key from a collection expression (<c>["todos", 42]</c>).</summary>
    public static BlexQueryKey Create(ReadOnlySpan<object?> parts) => new(parts.ToArray());

    /// <summary>A one-part key.</summary>
    public static implicit operator BlexQueryKey(string part) => new(part);

    /// <summary>The number of parts.</summary>
    public int Count => _parts.Length;

    /// <summary>The part at <paramref name="index"/>.</summary>
    public object? this[int index] => _parts[index];

    /// <summary>
    /// Whether this key begins with every part of <paramref name="prefix"/>, in order -- including when the
    /// two are equal.
    /// </summary>
    public bool StartsWith(BlexQueryKey prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        if (prefix._parts.Length > _parts.Length)
            return false;

        for (var i = 0; i < prefix._parts.Length; i++)
        {
            if (!Equals(_parts[i], prefix._parts[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool Equals(BlexQueryKey? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || other._hash != _hash || other._parts.Length != _parts.Length)
            return false;

        for (var i = 0; i < _parts.Length; i++)
        {
            if (!Equals(_parts[i], other._parts[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as BlexQueryKey);

    /// <inheritdoc />
    public override int GetHashCode() => _hash;

    /// <summary>Compares two keys part by part.</summary>
    public static bool operator ==(BlexQueryKey? left, BlexQueryKey? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two keys part by part.</summary>
    public static bool operator !=(BlexQueryKey? left, BlexQueryKey? right) => !(left == right);

    /// <summary>
    /// The key as JSON-like text -- <c>["todos",42]</c> -- the form diagnostics show and the prerender handoff
    /// matches entries by. Strings, numbers, booleans, enums, <see cref="Guid"/>s and dates are written as
    /// values; anything else as its <see cref="object.ToString"/>, so give such a part a stable one (a record does).
    /// </summary>
    public override string ToString() => _text ??= Format();

    private string Format()
    {
        var text = new StringBuilder("[");
        for (var i = 0; i < _parts.Length; i++)
        {
            if (i > 0)
                text.Append(',');

            switch (_parts[i])
            {
                case null:
                    text.Append("null");
                    break;
                case bool b:
                    text.Append(b ? "true" : "false");
                    break;
                case string s:
                    AppendString(text, s);
                    break;
                case Enum e:
                    AppendString(text, e.ToString());
                    break;
                case DateTime dt:
                    AppendString(text, dt.ToString("O", CultureInfo.InvariantCulture));
                    break;
                case DateTimeOffset dto:
                    AppendString(text, dto.ToString("O", CultureInfo.InvariantCulture));
                    break;
                case IFormattable number when IsNumber(number):
                    text.Append(number.ToString(null, CultureInfo.InvariantCulture));
                    break;
                case var other:
                    AppendString(text, Convert.ToString(other, CultureInfo.InvariantCulture) ?? "");
                    break;
            }
        }

        return text.Append(']').ToString();

        static bool IsNumber(object value)
            => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

        static void AppendString(StringBuilder text, string value)
        {
            text.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': text.Append("\\\""); break;
                    case '\\': text.Append("\\\\"); break;
                    case < ' ': text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); break;
                    default: text.Append(c); break;
                }
            }

            text.Append('"');
        }
    }

    /// <inheritdoc />
    public IEnumerator<object?> GetEnumerator() => ((IEnumerable<object?>)_parts).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _parts.GetEnumerator();
}
