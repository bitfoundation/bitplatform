using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Bit.Borm;

/// <summary>
/// Enforces JSON Schema's <c>uniqueItems</c> on an array field. Items are compared by <em>content</em>,
/// so the rule works equally on a dictionary-backed form and a typed model.
/// </summary>
/// <remarks>
/// <para>
/// Content means what JSON Schema means by it. Two dictionary rows are equal when they hold the same
/// values under the same keys, two lists when they hold equal items in the same order — all the way
/// down, since that is the whole of the data. Items used to be compared by their invariant
/// <em>text</em>, and the text of a dictionary or of a class that does not override <c>ToString</c> is
/// its type name: every row of a repeater was "a duplicate" of every other, and a form with
/// <c>uniqueItems</c> on a list of objects could not be submitted with two rows in it.
/// </para>
/// <para>
/// A model row is compared by its public <em>scalar</em> properties — text, numbers, dates, enums,
/// GUIDs — and nothing else. Its reference-typed members are never read: on an entity they are
/// navigation properties, where reading means lazy-loading a related table per row, and following
/// them (a <c>Child.Parent</c> back to the owner and on to every sibling) made the rule quadratic.
/// </para>
/// <para>
/// Numbers are equal when their values are, whatever type holds them: <c>1</c>, <c>1L</c>, <c>1.0</c>
/// and <c>1.0m</c> are one number. Text is compared exactly, and is never equal to a number.
/// </para>
/// </remarks>
public sealed class BormUniqueItemsRule(string? message = null) : IBormValidationRule
{
    // Deep enough for any real JSON-shaped row; it only exists to stop a structure that loops.
    private const int MaxDepth = 32;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ScalarProperties = new();

    /// <inheritdoc />
    public string Key => "uniqueItems";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        if (ctx.Value is not IEnumerable items || ctx.Value is string)
            return new(BormRuleResult.Success());

        // The list itself and the form's root are never part of an item's content: a row that refers
        // back to either is cut off there rather than walked into.
        var guard = new HashSet<object>(ReferenceEqualityComparer.Instance) { items };
        if (ctx.Data.Root is { } root) guard.Add(root);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (!seen.Add(KeyOf(item, guard)))
                return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.UniqueItems)));
        }
        return new(BormRuleResult.Success());
    }

    /// <summary>A text key that two items share exactly when their content is equal.</summary>
    internal static string KeyOf(object? item, HashSet<object>? guard = null)
    {
        var key = new StringBuilder();
        Append(key, item, guard ?? new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
        return key.ToString();
    }

    /// <remarks>
    /// Every piece is tagged and every piece of text is length-prefixed, so no combination of values can
    /// spell the key of a different one: <c>["a,b"]</c> and <c>["a","b"]</c> never collide.
    /// </remarks>
    private static void Append(StringBuilder key, object? value, HashSet<object> guard, int depth)
    {
        // A JsonElement the store has not reached yet is read as the CLR value it stands for.
        value = BormDictionaryDataAccessor.FromJson(value);

        if (AppendScalar(key, value)) return;

        if (depth >= MaxDepth || !guard.Add(value!))
        {
            key.Append('~');
            return;
        }

        try
        {
            switch (value)
            {
                case IDictionary<string, object?> dictionary:
                    Entries(key, dictionary.Select(p => (p.Key, p.Value)), guard, depth);
                    break;
                case IDictionary dictionary:
                    Entries(key, dictionary.Cast<DictionaryEntry>()
                        .Select(e => (Convert.ToString(e.Key, CultureInfo.InvariantCulture) ?? string.Empty, e.Value)), guard, depth);
                    break;
                case IEnumerable sequence:
                    key.Append('[');
                    foreach (var item in sequence) Append(key, item, guard, depth + 1);
                    key.Append(']');
                    break;
                default:
                    Members(key, value!);
                    break;
            }
        }
        finally
        {
            guard.Remove(value!);
        }
    }

    /// <summary>Appends a value that has no parts — text, a number, a date, an enum member — and says whether it was one.</summary>
    private static bool AppendScalar(StringBuilder key, object? value)
    {
        switch (value)
        {
            case null:
                key.Append('n');
                return true;
            case string s:
                Text(key, 's', s);
                return true;
            case char c:
                Text(key, 's', c.ToString());
                return true;
            case bool b:
                key.Append(b ? 'T' : 'F');
                return true;
            case Enum e:
                Text(key, 'e', e.ToString());
                return true;
            case Uri uri:
                Text(key, 'v', uri.OriginalString);
                return true;
        }

        if (BormNumber.Canonical(value) is { } number)
        {
            Text(key, '#', number);
            return true;
        }

        // Dates, times, GUIDs and the like: values with a canonical invariant spelling of their own.
        if (value is IFormattable formattable)
        {
            Text(key, 'v', formattable.ToString(null, CultureInfo.InvariantCulture));
            return true;
        }

        return false;
    }

    /// <summary>
    /// An object's entries, in a fixed order. Names are folded to one case, because a path names a key
    /// without regard to case everywhere else in the library: <c>Name</c> and <c>name</c> are one field.
    /// </summary>
    private static void Entries(StringBuilder key, IEnumerable<(string Name, object? Value)> entries,
        HashSet<object> guard, int depth)
    {
        key.Append('{');
        foreach (var (name, entry) in entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            Text(key, 'k', name.ToUpperInvariant());
            Append(key, entry, guard, depth + 1);
        }
        key.Append('}');
    }

    /// <summary>
    /// A model object by its public scalar properties, one level deep. An object with none (a
    /// <c>ValueTuple</c>, which keeps its parts in fields) is taken by its text.
    /// </summary>
    private static void Members(StringBuilder key, object value)
    {
        var properties = ScalarProperties.GetOrAdd(value.GetType(), static type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0
                        && BormModelDataAccessor.IsPubliclyReadable(p)
                        && IsScalar(p.PropertyType))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray());

        if (properties.Length == 0)
        {
            Text(key, 'v', value.ToString() ?? string.Empty);
            return;
        }

        key.Append('{');
        foreach (var property in properties)
        {
            object? member;
            try
            {
                member = property.GetValue(value);
            }
            catch (TargetInvocationException)
            {
                // A property that throws says nothing about equality.
                member = null;
            }

            Text(key, 'k', property.Name.ToUpperInvariant());
            if (!AppendScalar(key, member)) Text(key, 'v', member?.ToString() ?? string.Empty);
        }
        key.Append('}');
    }

    /// <summary>Whether a property's type is a single value rather than something with parts of its own.</summary>
    private static bool IsScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum
               || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid) || t == typeof(Uri)
               || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(DateOnly)
               || t == typeof(TimeOnly) || t == typeof(TimeSpan)
               || t == typeof(Half) || t == typeof(Int128) || t == typeof(UInt128);
    }

    private static void Text(StringBuilder key, char tag, string text)
        => key.Append(tag).Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
}
