using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BevTools;

public enum BevNodeKind
{
    Null,
    Primitive,
    String,
    Enum,
    Object,
    Collection,
    Dictionary,
    Redacted,
    Error,
    Special,
    Circular,
}

/// <summary>A lazily expandable view over one value. Children are resolved on demand by re-walking <see cref="Path"/> from the root object.</summary>
public sealed record BevInspectedNode(string Name, string? TypeName, string Display, BevNodeKind Kind, string Path, bool HasChildren, int? Count = null)
{
    public bool IsExpandable => HasChildren && Kind is not (BevNodeKind.Circular or BevNodeKind.Redacted or BevNodeKind.Error);
}

/// <summary>
/// Safe, bounded, lazy object inspection. Never mutates values, catches throwing getters, redacts sensitive members,
/// detects cycles along the expansion path and caps strings and collections.
/// </summary>
public sealed class BevObjectInspector
{
    private static readonly ConcurrentDictionary<Type, MemberInfo[]> MemberCache = new();
    private readonly BevRedactor _redactor;
    private readonly IReadOnlyDictionary<Type, BevToolsValueFormatter> _formatters;
    private readonly int _maxItems;
    private readonly int _maxString;
    private readonly int _maxDepth;

    public BevObjectInspector(BitBevToolsOptions options, BevRedactor redactor, IReadOnlyDictionary<Type, BevToolsValueFormatter>? formatters = null)
    {
        _redactor = redactor;
        _formatters = formatters ?? new Dictionary<Type, BevToolsValueFormatter>();
        _maxItems = Math.Max(1, options.MaxCollectionItems);
        _maxString = Math.Max(16, options.MaxStringLength);
        _maxDepth = Math.Max(1, options.MaxInspectionDepth);
    }

    public BevInspectedNode Describe(string name, object? value, string path = "", MemberInfo? member = null) => Describe(name, value, path, member, null);

    private BevInspectedNode Describe(string name, object? value, string path, MemberInfo? member, List<object>? ancestors)
    {
        // A sensitive name hides the value whatever its type: a token can be a byte[], a char[] or a custom struct
        // just as easily as a string, and over-redaction is the safe direction for a security control.
        var sensitive = member is not null ? _redactor.IsSensitive(member) : _redactor.IsSensitiveName(name);
        if (sensitive)
        {
            return new BevInspectedNode(name, value?.GetType() is { } rt ? TypeNames.Short(rt) : null, BevRedactor.RedactedValue, BevNodeKind.Redacted, path, false);
        }

        return DescribeCore(name, value, path, ancestors);
    }

    /// <summary>Resolves the children of the node at <paramref name="path"/> relative to <paramref name="root"/>.</summary>
    public IReadOnlyList<BevInspectedNode> Children(object? root, string path)
    {
        var ancestors = new List<object>();
        object? current;
        try
        {
            current = Resolve(root, path, ancestors);
        }
        catch (Exception ex)
        {
            return [new BevInspectedNode("error", null, "«" + ex.GetType().Name + ": " + ex.Message + "»", BevNodeKind.Error, path, false)];
        }

        if (current is null)
        {
            return [];
        }

        return ChildrenOf(current, path, ancestors);
    }

    private IReadOnlyList<BevInspectedNode> ChildrenOf(object value, string path, List<object> ancestors)
    {
        var result = new List<BevInspectedNode>();
        ancestors.Add(value);
        try
        {
            if (value is IDictionary dictionary)
            {
                var i = 0;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (i++ >= _maxItems)
                    {
                        result.Add(new BevInspectedNode("…", null, $"{dictionary.Count - _maxItems} more items", BevNodeKind.Special, path, false));
                        break;
                    }

                    var key = FormatKey(entry.Key);
                    result.Add(DescribeCore(key, entry.Value, path + "[" + key + "]", ancestors));
                }

                return result;
            }

            if (value is IEnumerable enumerable and not string)
            {
                var i = 0;
                foreach (var item in enumerable)
                {
                    if (i >= _maxItems)
                    {
                        result.Add(new BevInspectedNode("…", null, "more items not shown", BevNodeKind.Special, path, false));
                        break;
                    }

                    result.Add(DescribeCore("[" + i + "]", item, path + "[" + i + "]", ancestors));
                    i++;
                }

                return result;
            }

            foreach (var member in GetMembers(value.GetType()))
            {
                var childPath = path.Length == 0 ? member.Name : path + "." + member.Name;
                if (_redactor.IsSensitive(member))
                {
                    result.Add(new BevInspectedNode(member.Name, TypeNames.Short(MemberType(member)), BevRedactor.RedactedValue, BevNodeKind.Redacted, childPath, false));
                    continue;
                }

                object? childValue;
                try
                {
                    childValue = GetValue(member, value);
                }
                catch (Exception ex)
                {
                    var inner = ex is TargetInvocationException { InnerException: { } ie } ? ie : ex;
                    result.Add(new BevInspectedNode(member.Name, TypeNames.Short(MemberType(member)), "«threw " + inner.GetType().Name + "»", BevNodeKind.Error, childPath, false));
                    continue;
                }

                result.Add(DescribeCore(member.Name, childValue, childPath, ancestors));
            }

            return result;
        }
        finally
        {
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    private BevInspectedNode DescribeCore(string name, object? value, string path, List<object>? ancestors)
    {
        if (value is null)
        {
            return new BevInspectedNode(name, null, "null", BevNodeKind.Null, path, false);
        }

        var type = value.GetType();
        var typeName = TypeNames.Short(type);

        if (ancestors is not null && !type.IsValueType && value is not string)
        {
            foreach (var ancestor in ancestors)
            {
                if (ReferenceEquals(ancestor, value))
                {
                    return new BevInspectedNode(name, typeName, "«circular reference»", BevNodeKind.Circular, path, false);
                }
            }
        }

        if (ancestors is not null && ancestors.Count >= _maxDepth)
        {
            return new BevInspectedNode(name, typeName, "«max depth»", BevNodeKind.Special, path, false);
        }

        if (TryFormatWithExtension(value, out var formatted))
        {
            return new BevInspectedNode(name, typeName, formatted!, BevNodeKind.Special, path, false);
        }

        if (value is string s)
        {
            return new BevInspectedNode(name, "string", Quote(s), BevNodeKind.String, path, false);
        }

        if (type.IsPrimitive || value is decimal or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Guid or Uri or Version)
        {
            return new BevInspectedNode(name, typeName, FormatPrimitive(value), BevNodeKind.Primitive, path, false);
        }

        if (type.IsEnum)
        {
            return new BevInspectedNode(name, typeName, value.ToString() ?? "", BevNodeKind.Enum, path, false);
        }

        if (TrySpecial(value, type, out var special))
        {
            return new BevInspectedNode(name, typeName, special!, BevNodeKind.Special, path, false);
        }

        if (value is IDictionary dictionary)
        {
            return new BevInspectedNode(name, typeName, $"{typeName} ({dictionary.Count} entries)", BevNodeKind.Dictionary, path, dictionary.Count > 0, dictionary.Count);
        }

        if (value is IEnumerable enumerable)
        {
            var count = TryCount(enumerable);
            var display = count is null ? typeName + " (enumerable)" : $"{typeName} ({count} items)";
            return new BevInspectedNode(name, typeName, display, BevNodeKind.Collection, path, count is null or > 0, count);
        }

        var summary = Summarize(value, type);
        return new BevInspectedNode(name, typeName, summary, BevNodeKind.Object, path, GetMembers(type).Length > 0);
    }

    private bool TryFormatWithExtension(object value, out string? formatted)
    {
        formatted = null;
        if (_formatters.Count == 0)
        {
            return false;
        }

        for (var t = value.GetType(); t is not null; t = t.BaseType)
        {
            if (_formatters.TryGetValue(t, out var formatter))
            {
                formatted = formatter(value);
                return formatted is not null;
            }
        }

        foreach (var iface in value.GetType().GetInterfaces())
        {
            if (_formatters.TryGetValue(iface, out var formatter))
            {
                formatted = formatter(value);
                return formatted is not null;
            }
        }

        return false;
    }

    private bool TrySpecial(object value, Type type, out string? display)
    {
        display = value switch
        {
            RenderFragment => "RenderFragment",
            Delegate d => "delegate " + (d.Method.DeclaringType is { } dt ? TypeNames.Short(dt) + "." : "") + d.Method.Name,
            EventCallback ec => ec.HasDelegate ? "EventCallback (bound)" : "EventCallback (empty)",
            Type t => "typeof(" + TypeNames.Short(t) + ")",
            Task task => "Task: " + task.Status,
            Exception ex => ex.GetType().Name + ": " + ex.Message,
            JsonElement je => je.ValueKind == JsonValueKind.String ? Quote(je.GetString() ?? "") : je.ToString(),
            ElementReference er => "ElementReference " + (string.IsNullOrEmpty(er.Id) ? "(unset)" : er.Id),
            IJSObjectReference => "IJSObjectReference",
            System.Linq.IQueryable q => "IQueryable<" + TypeNames.Short(q.ElementType) + "> (not enumerated: evaluating it would run the query)",
            IJSRuntime => "IJSRuntime",
            IServiceProvider => "IServiceProvider",
            CancellationToken ct => "CancellationToken (" + (ct.IsCancellationRequested ? "cancelled" : "active") + ")",
            CancellationTokenSource cts => "CancellationTokenSource (" + (cts.IsCancellationRequested ? "cancelled" : "active") + ")",
            Stream st => TypeNames.Short(st.GetType()) + (st.CanSeek ? $" ({st.Length} bytes)" : ""),
            _ => null,
        };

        if (display is null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>))
        {
            display = "EventCallback";
        }

        if (display is null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>))
        {
            display = "RenderFragment<T>";
        }

        return display is not null;
    }

    private string Summarize(object value, Type type)
    {
        if (type.IsValueType && type.GetMethod("ToString", Type.EmptyTypes)?.DeclaringType == type)
        {
            return SafeToString(value);
        }

        var toStringDeclaringType = type.GetMethod("ToString", Type.EmptyTypes)?.DeclaringType;
        if (toStringDeclaringType is not null && toStringDeclaringType != typeof(object) && toStringDeclaringType != typeof(ValueType) && !HasSensitiveMember(type))
        {
            // records and user ToString overrides print every member; show them only when short and clean.
            var str = SafeToString(value);
            if (str.Length <= 120 && !_redactor.IsSensitiveName(str))
            {
                return str;
            }
        }

        return TypeNames.Short(type);
    }

    /// <summary>True when any member of the type would be redacted, in which case its ToString must not be shown.</summary>
    private bool HasSensitiveMember(Type type)
    {
        foreach (var member in GetMembers(type))
        {
            if (_redactor.IsSensitive(member))
            {
                return true;
            }
        }

        return false;
    }

    private static string SafeToString(object value)
    {
        try
        {
            return value.ToString() ?? "";
        }
        catch (Exception ex)
        {
            return "«ToString threw " + ex.GetType().Name + "»";
        }
    }

    private string Quote(string s)
    {
        if (s.Length > _maxString)
        {
            s = s[.._maxString] + "… (+" + (s.Length - _maxString).ToString(CultureInfo.InvariantCulture) + " chars)";
        }

        return "\"" + s.Replace("\n", "\\n").Replace("\r", "") + "\"";
    }

    private static string FormatPrimitive(object value) => value switch
    {
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static string FormatKey(object? key) => key switch
    {
        null => "null",
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => key.ToString() ?? "?",
    };

    private static int? TryCount(IEnumerable enumerable) => enumerable switch
    {
        ICollection c => c.Count,
        _ => enumerable.GetType().GetProperty("Count")?.GetValue(enumerable) as int?,
    };

    private static MemberInfo[] GetMembers(Type type) => MemberCache.GetOrAdd(type, static t =>
    {
        var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && !p.IsDefined(typeof(BevToolsIgnoreAttribute), true))
            .Where(p => p.GetMethod?.IsPublic == true)
            .Cast<MemberInfo>();
        var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !f.IsDefined(typeof(BevToolsIgnoreAttribute), true))
            .Cast<MemberInfo>();
        return props.Concat(fields).ToArray();
    });

    private static Type MemberType(MemberInfo member) => member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;

    private static object? GetValue(MemberInfo member, object target) => member is PropertyInfo p ? p.GetValue(target) : ((FieldInfo)member).GetValue(target);

    private object? Resolve(object? root, string path, List<object> ancestors)
    {
        var current = root;
        if (string.IsNullOrEmpty(path))
        {
            return current;
        }

        foreach (var segment in ParsePath(path))
        {
            if (current is null)
            {
                return null;
            }

            ancestors.Add(current);
            if (segment.IsIndex)
            {
                current = Index(current, segment.Value);
            }
            else
            {
                var member = GetMembers(current.GetType()).FirstOrDefault(m => m.Name == segment.Value)
                    ?? throw new InvalidOperationException($"Member '{segment.Value}' not found on {current.GetType().Name}.");
                if (_redactor.IsSensitive(member))
                {
                    return null;
                }

                current = GetValue(member, current);
            }
        }

        return current;
    }

    private static object? Index(object target, string key)
    {
        if (target is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (FormatKey(entry.Key) == key)
                {
                    return entry.Value;
                }
            }

            return null;
        }

        if (target is IEnumerable enumerable && int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            var i = 0;
            foreach (var item in enumerable)
            {
                if (i++ == index)
                {
                    return item;
                }
            }
        }

        return null;
    }

    private readonly record struct PathSegment(string Value, bool IsIndex);

    private static IEnumerable<PathSegment> ParsePath(string path)
    {
        var i = 0;
        while (i < path.Length)
        {
            if (path[i] == '.')
            {
                i++;
                continue;
            }

            if (path[i] == '[')
            {
                var end = path.IndexOf(']', i);
                if (end < 0)
                {
                    end = path.Length;
                }

                yield return new PathSegment(path[(i + 1)..end], true);
                i = end + 1;
                continue;
            }

            var next = i;
            while (next < path.Length && path[next] != '.' && path[next] != '[')
            {
                next++;
            }

            yield return new PathSegment(path[i..next], false);
            i = next;
        }
    }

    /// <summary>
    /// Produces a bounded path → display map used for change detection (state diffs, component field diffs).
    /// Delegates, render fragments, services and streams are summarized, never traversed.
    /// </summary>
    public Dictionary<string, string> Flatten(object? root, int maxDepth = 3, int maxItems = 25, int maxEntries = 400)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        FlattenInto(result, "", root, 0, maxDepth, maxItems, maxEntries, [], null, FlattenDeadline());
        return result;
    }

    /// <summary>
    /// Adds one named value to <paramref name="result"/>: its display under <paramref name="name"/> and its members
    /// below it ("name.Member", "name[0]"). The value's own entry is always written; the budget only stops the walk
    /// below it. <paramref name="maxEntries"/> counts the whole dictionary and <paramref name="deadline"/> comes from
    /// the caller, so a loop over several values (a component's parameters) shares one bounded budget.
    /// </summary>
    internal void FlattenNamed(Dictionary<string, string> result, string name, object? value, MemberInfo? member, int maxDepth, int maxItems, int maxEntries, long deadline) =>
        FlattenInto(result, name, value, 0, maxDepth, maxItems, maxEntries, [], member, deadline);

    /// <summary>The deadline for one Flatten pass started now. Application getters can be arbitrarily slow; inspection must not be.</summary>
    internal static long FlattenDeadline() => Stopwatch.GetTimestamp() + (long)(Stopwatch.Frequency * FlattenBudgetSeconds);

    private const double FlattenBudgetSeconds = 0.05;

    private static bool OutOfBudget(Dictionary<string, string> result, int maxEntries, long deadline) =>
        result.Count >= maxEntries || Stopwatch.GetTimestamp() > deadline;

    private void FlattenInto(Dictionary<string, string> result, string path, object? value, int depth, int maxDepth, int maxItems, int maxEntries, List<object> ancestors, MemberInfo? member, long deadline)
    {
        // The budget is checked before each child, not here: the node itself is always written, and a getter is never
        // invoked once the budget is spent.
        var node = Describe(member?.Name ?? path, value, path, member, ancestors);
        if (path.Length > 0)
        {
            result[path] = node.Display;
        }

        if (value is null || depth >= maxDepth || !node.IsExpandable || node.Kind is BevNodeKind.Special)
        {
            return;
        }

        ancestors.Add(value);
        try
        {
            if (value is IDictionary dictionary)
            {
                var i = 0;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (i++ >= maxItems || OutOfBudget(result, maxEntries, deadline))
                    {
                        break;
                    }

                    var key = FormatKey(entry.Key);
                    FlattenInto(result, path + "[" + key + "]", entry.Value, depth + 1, maxDepth, maxItems, maxEntries, ancestors, null, deadline);
                }
            }
            else if (value is ICollection collection)
            {
                // Only materialized collections are walked. Enumerating an arbitrary IEnumerable would run deferred
                // LINQ, EF queries or generators — inspection must never execute application logic with side effects.
                var i = 0;
                foreach (var item in collection)
                {
                    if (i >= maxItems || OutOfBudget(result, maxEntries, deadline))
                    {
                        break;
                    }

                    FlattenInto(result, path + "[" + i + "]", item, depth + 1, maxDepth, maxItems, maxEntries, ancestors, null, deadline);
                    i++;
                }
            }
            else
            {
                foreach (var m in GetMembers(value.GetType()))
                {
                    if (OutOfBudget(result, maxEntries, deadline))
                    {
                        break;
                    }

                    var childPath = path.Length == 0 ? m.Name : path + "." + m.Name;
                    object? child;
                    try
                    {
                        child = _redactor.IsSensitive(m) ? BevRedactor.RedactedValue : GetValue(m, value);
                    }
                    catch
                    {
                        result[childPath] = "«threw»";
                        continue;
                    }

                    FlattenInto(result, childPath, child, depth + 1, maxDepth, maxItems, maxEntries, ancestors, m, deadline);
                }
            }
        }
        finally
        {
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    /// <summary>Computes changed, added and removed entries between two flattened snapshots.</summary>
    public static List<BevStateDiffEntry> Diff(IReadOnlyDictionary<string, string>? before, IReadOnlyDictionary<string, string> after)
    {
        var changes = new List<BevStateDiffEntry>();
        before ??= new Dictionary<string, string>();
        foreach (var (path, newValue) in after)
        {
            if (!before.TryGetValue(path, out var oldValue))
            {
                changes.Add(new BevStateDiffEntry(path, null, newValue));
            }
            else if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                changes.Add(new BevStateDiffEntry(path, oldValue, newValue));
            }
        }

        foreach (var (path, oldValue) in before)
        {
            if (!after.ContainsKey(path))
            {
                changes.Add(new BevStateDiffEntry(path, oldValue, null));
            }
        }

        return Collapse(changes);
    }

    /// <summary>
    /// Keeps only the most specific changed paths, so a changed list does not also report every element under it.
    /// Sorting first makes this O(n log n); the previous pairwise scan was quadratic and mutated the list it read.
    /// </summary>
    private static List<BevStateDiffEntry> Collapse(List<BevStateDiffEntry> changes)
    {
        if (changes.Count < 2)
        {
            return changes;
        }

        var sorted = changes.OrderBy(c => c.Path, StringComparer.Ordinal).ToList();
        var result = new List<BevStateDiffEntry>(sorted.Count);
        for (var i = 0; i < sorted.Count; i++)
        {
            var path = sorted[i].Path;
            var next = i + 1 < sorted.Count ? sorted[i + 1].Path : null;
            var hasChild = next is not null && next.Length > path.Length && next.StartsWith(path, StringComparison.Ordinal) && (next[path.Length] is '.' or '[');
            if (!hasChild)
            {
                result.Add(sorted[i]);
            }
        }

        return result;
    }
}
