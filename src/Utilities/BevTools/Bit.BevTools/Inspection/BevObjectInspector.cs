using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
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
/// Safe, bounded, lazy object inspection. Never mutates values, catches throwing getters, redacts sensitive members
/// and credential-shaped values, never walks configuration or key material, detects cycles along the expansion path,
/// caps strings and collections, and reads no more than <see cref="ValueAccess"/> allows.
/// </summary>
public sealed class BevObjectInspector
{
    private static readonly ConcurrentDictionary<Type, MemberInfo[]> MemberCache = new();
    private static readonly ConcurrentDictionary<Type, bool> IgnoredMemberCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> PairNameCache = new();
    private static readonly ConcurrentDictionary<Type, string?> ConfigurationTypeCache = new();
    private readonly BevRedactor _redactor;
    private readonly IReadOnlyDictionary<Type, BevToolsValueFormatter> _formatters;
    private readonly int _maxItems;
    private readonly int _maxString;
    private readonly int _maxDepth;

    public BevObjectInspector(BitBevToolsOptions options, BevRedactor redactor, IReadOnlyDictionary<Type, BevToolsValueFormatter>? formatters = null, BevValueAccess valueAccess = BevValueAccess.Full)
    {
        _redactor = redactor;
        _formatters = formatters ?? new Dictionary<Type, BevToolsValueFormatter>();
        _maxItems = Math.Max(1, options.MaxCollectionItems);
        _maxString = Math.Max(16, options.MaxStringLength);
        _maxDepth = Math.Max(1, options.MaxInspectionDepth);
        ValueAccess = valueAccess;
    }

    /// <summary>How much of an application value this inspector reads; see <see cref="BevValueAccess"/>.</summary>
    public BevValueAccess ValueAccess { get; }

    public BevInspectedNode Describe(string name, object? value, string path = "", MemberInfo? member = null) => Describe(name, value, path, member, null);

    /// <summary>
    /// Describes a value BevTools itself attached to a timeline event (a render cause, a batch number, a provider
    /// name), as opposed to an application value. Shown even when <see cref="ValueAccess"/> is
    /// <see cref="BevValueAccess.None"/>, but never more than scalars then: an extension's event can carry anything.
    /// </summary>
    public BevInspectedNode DescribeMetadata(string name, object? value) =>
        _redactor.IsSensitiveName(name)
            ? new BevInspectedNode(name, value?.GetType() is { } rt ? TypeNames.Short(rt) : null, BevRedactor.RedactedValue, BevNodeKind.Redacted, "", false)
            : DescribeCore(name, value, "", null, ValueAccess == BevValueAccess.None ? BevValueAccess.Shallow : ValueAccess);

    private BevInspectedNode Describe(string name, object? value, string path, MemberInfo? member, List<object>? ancestors)
    {
        // A sensitive name hides the value whatever its type: a token can be a byte[], a char[] or a custom struct
        // just as easily as a string, and over-redaction is the safe direction for a security control.
        var sensitive = member is not null ? _redactor.IsSensitive(member) : _redactor.IsSensitiveName(name);
        if (sensitive)
        {
            return new BevInspectedNode(name, value?.GetType() is { } rt ? TypeNames.Short(rt) : null, BevRedactor.RedactedValue, BevNodeKind.Redacted, path, false);
        }

        if (ValueAccess == BevValueAccess.None && value is not null)
        {
            // The type is shown, not the value: what kind of thing a parameter holds is structure, what it holds is data.
            return new BevInspectedNode(name, value is string ? "string" : TypeNames.Short(value.GetType()), BevRedactor.HiddenValue, BevNodeKind.Redacted, path, false);
        }

        return DescribeCore(name, value, path, ancestors, ValueAccess);
    }

    /// <summary>Resolves the children of the node at <paramref name="path"/> relative to <paramref name="root"/>.</summary>
    public IReadOnlyList<BevInspectedNode> Children(object? root, string path)
    {
        // Only Full access expands anything; refusing here as well keeps a caller holding an old path from walking in.
        if (ValueAccess != BevValueAccess.Full)
        {
            return [];
        }

        var ancestors = new List<object>();
        object? current;
        try
        {
            current = Resolve(root, path, ancestors);
        }
        catch (Exception ex)
        {
            return [new BevInspectedNode("error", null, "«" + ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message) + "»", BevNodeKind.Error, path, false)];
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

                    // Described by key, so a sensitive key ("Authorization", "password") hides its value the way a
                    // sensitive member does, and the way Flatten already hides it from diffs.
                    var key = FormatKey(entry.Key);
                    result.Add(Describe(key, entry.Value, path + "[" + key + "]", null, ancestors));
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

                    result.Add(DescribeCore("[" + i + "]", item, path + "[" + i + "]", ancestors, BevValueAccess.Full));
                    i++;
                }

                return result;
            }

            var pairNameIsSensitive = PairNameIsSensitive(value);
            foreach (var member in GetMembers(value.GetType()))
            {
                var childPath = path.Length == 0 ? member.Name : path + "." + member.Name;
                if (_redactor.IsSensitive(member) || (pairNameIsSensitive && IsPairValueMember(member)))
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

                result.Add(DescribeCore(member.Name, childValue, childPath, ancestors, BevValueAccess.Full));
            }

            return result;
        }
        finally
        {
            ancestors.RemoveAt(ancestors.Count - 1);
        }
    }

    private BevInspectedNode DescribeCore(string name, object? value, string path, List<object>? ancestors, BevValueAccess access)
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

        if (access == BevValueAccess.Full && TryFormatWithExtension(value, out var formatted))
        {
            return new BevInspectedNode(name, typeName, BevRedactor.RedactText(formatted!), BevNodeKind.Special, path, false);
        }

        if (value is string s)
        {
            // Credential-shaped text is redacted whatever its member is called; the rest of the text stays readable
            // ("Server=db;Password=«redacted»"), unless what is left still looks like a credential.
            var redacted = BevRedactor.RedactText(s);
            if (!string.Equals(redacted, s, StringComparison.Ordinal))
            {
                var display = redacted == BevRedactor.RedactedValue || BevRedactor.LooksLikeSecret(redacted) ? BevRedactor.RedactedValue : Quote(redacted);
                return new BevInspectedNode(name, "string", display, BevNodeKind.Redacted, path, false);
            }

            return new BevInspectedNode(name, "string", Quote(s), BevNodeKind.String, path, false);
        }

        if (value is Uri uri)
        {
            return new BevInspectedNode(name, typeName, _redactor.RedactUrl(uri.OriginalString), BevNodeKind.Primitive, path, false);
        }

        if (type.IsPrimitive || value is decimal or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Guid or Version)
        {
            return new BevInspectedNode(name, typeName, FormatPrimitive(value), BevNodeKind.Primitive, path, false);
        }

        if (type.IsEnum)
        {
            return new BevInspectedNode(name, typeName, value.ToString() ?? "", BevNodeKind.Enum, path, false);
        }

        if (value is JsonElement { ValueKind: JsonValueKind.Object or JsonValueKind.Array } json)
        {
            var size = json.ValueKind == JsonValueKind.Object ? json.EnumerateObject().Count() + " properties" : json.GetArrayLength() + " items";
            return new BevInspectedNode(name, typeName, access == BevValueAccess.Full ? JsonDisplay(json) : typeName + " (" + size + ")", BevNodeKind.Special, path, false);
        }

        if (TrySpecial(value, type, out var special))
        {
            return new BevInspectedNode(name, typeName, special!, BevNodeKind.Special, path, false);
        }

        if (value is IDictionary dictionary)
        {
            return new BevInspectedNode(name, typeName, $"{typeName} ({dictionary.Count} entries)", BevNodeKind.Dictionary, path, access == BevValueAccess.Full && dictionary.Count > 0, dictionary.Count);
        }

        if (value is IEnumerable enumerable)
        {
            var count = TryCount(enumerable);
            var display = count is null ? typeName + " (enumerable)" : $"{typeName} ({count} items)";
            return new BevInspectedNode(name, typeName, display, BevNodeKind.Collection, path, access == BevValueAccess.Full && count is null or > 0, count);
        }

        if (access != BevValueAccess.Full)
        {
            // Not even ToString: a record prints every member, which is exactly what Shallow promises not to show.
            return new BevInspectedNode(name, typeName, typeName, BevNodeKind.Object, path, false);
        }

        var summary = Summarize(value, type);
        return new BevInspectedNode(name, typeName, summary, BevNodeKind.Object, path, GetMembers(type).Length > 0);
    }

    /// <summary>
    /// True for a name/value pair whose name is sensitive: a <c>KeyValuePair</c> keyed "Authorization", a claim of type
    /// "access_token", a cookie named "session-token". The value member of such a pair is a secret even though the
    /// member is called "Value", which is how request headers, claims and settings hold their credentials.
    /// </summary>
    private bool PairNameIsSensitive(object value)
    {
        var nameProperty = PairNameCache.GetOrAdd(value.GetType(), static type =>
        {
            if (type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance) is null && type.GetProperty("Values", BindingFlags.Public | BindingFlags.Instance) is null)
            {
                return null;
            }

            foreach (var candidate in (string[])["Key", "Name", "Type"])
            {
                if (type.GetProperty(candidate, BindingFlags.Public | BindingFlags.Instance) is { CanRead: true } property
                    && property.GetIndexParameters().Length == 0 && property.GetMethod?.IsPublic == true)
                {
                    return property;
                }
            }

            return null;
        });

        if (nameProperty is null)
        {
            return false;
        }

        try
        {
            return nameProperty.GetValue(value) is string pairName && _redactor.IsSensitiveName(pairName);
        }
        catch
        {
            // A name that cannot be read cannot clear the value either.
            return true;
        }
    }

    private static bool IsPairValueMember(MemberInfo member) => member.Name is "Value" or "Values";

    /// <summary>
    /// The configuration, options and credential types BevTools never walks, with what is shown instead. Configuration
    /// is where an app keeps its connection strings and keys under names no pattern can predict ("Smtp:Pass",
    /// "Jwt:SigningKey"), so the safe answer is to never read it rather than to hope the names give it away.
    /// </summary>
    private static string? ConfigurationDisplay(Type type) => ConfigurationTypeCache.GetOrAdd(type, static t =>
    {
        if (typeof(Microsoft.Extensions.Configuration.IConfiguration).IsAssignableFrom(t))
        {
            return "IConfiguration (configuration values are never read)";
        }

        if (typeof(Microsoft.Extensions.Configuration.IConfigurationProvider).IsAssignableFrom(t)
            || typeof(Microsoft.Extensions.Configuration.IConfigurationSource).IsAssignableFrom(t))
        {
            return TypeNames.Short(t) + " (configuration values are never read)";
        }

        foreach (var iface in t.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() is var definition
                && (definition == typeof(Microsoft.Extensions.Options.IOptions<>) || definition == typeof(Microsoft.Extensions.Options.IOptionsMonitor<>)
                    || definition == typeof(Microsoft.Extensions.Options.IOptionsSnapshot<>)))
            {
                return TypeNames.Short(definition).Replace("<TOptions>", "") + "<" + TypeNames.Short(iface.GetGenericArguments()[0]) + "> (bound configuration is never read)";
            }
        }

        if (typeof(System.Net.NetworkCredential).IsAssignableFrom(t) || typeof(System.Security.SecureString).IsAssignableFrom(t)
            || typeof(System.Security.Cryptography.AsymmetricAlgorithm).IsAssignableFrom(t) || typeof(System.Security.Cryptography.SymmetricAlgorithm).IsAssignableFrom(t)
            || typeof(System.Security.Cryptography.X509Certificates.X509Certificate).IsAssignableFrom(t))
        {
            return TypeNames.Short(t) + " (credential, never read)";
        }

        return null;
    });

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
        display = ConfigurationDisplay(type) ?? value switch
        {
            RenderFragment => "RenderFragment",
            Delegate d => "delegate " + (d.Method.DeclaringType is { } dt ? TypeNames.Short(dt) + "." : "") + d.Method.Name,
            EventCallback ec => BevEventCallbacks.Describe(ec),
            Type t => "typeof(" + TypeNames.Short(t) + ")",
            Task task => "Task: " + task.Status,
            Exception ex => ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message),
            JsonElement je => je.ValueKind == JsonValueKind.String ? StringDisplay(je.GetString() ?? "") : je.ToString(),
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
            display = BevEventCallbacks.Describe(value);
        }

        if (display is null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>))
        {
            display = "RenderFragment<T>";
        }

        return display is not null;
    }

    private string Summarize(object value, Type type)
    {
        if (type.IsValueType && type.GetMethod("ToString", Type.EmptyTypes)?.DeclaringType == type && !PairNameIsSensitive(value) && !HasSensitiveMember(type))
        {
            var text = SafeToString(value);
            return BevRedactor.LooksLikeSecret(text) ? TypeNames.Short(type) + " (" + BevRedactor.RedactedValue + ")" : text;
        }

        if (PairNameIsSensitive(value))
        {
            // KeyValuePair<,>.ToString prints the value next to its key: "[Authorization, Bearer …]".
            return TypeNames.Short(type) + " (" + BevRedactor.RedactedValue + ")";
        }

        var toStringDeclaringType = type.GetMethod("ToString", Type.EmptyTypes)?.DeclaringType;
        if (toStringDeclaringType is not null && toStringDeclaringType != typeof(object) && toStringDeclaringType != typeof(ValueType) && !HasSensitiveMember(type))
        {
            // records and user ToString overrides print every member; show them only when short and clean.
            var str = SafeToString(value);
            if (str.Length <= 120 && !_redactor.IsSensitiveName(str) && !BevRedactor.LooksLikeSecret(str))
            {
                return str;
            }
        }

        return TypeNames.Short(type);
    }

    /// <summary>True when any member of the type would be redacted, in which case its ToString must not be shown.</summary>
    private bool HasSensitiveMember(Type type)
    {
        if (HasIgnoredMember(type))
        {
            return true;
        }

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

    private string StringDisplay(string s)
    {
        var redacted = BevRedactor.RedactText(s);
        if (string.Equals(redacted, s, StringComparison.Ordinal))
        {
            return Quote(s);
        }

        return redacted == BevRedactor.RedactedValue || BevRedactor.LooksLikeSecret(redacted) ? BevRedactor.RedactedValue : Quote(redacted);
    }

    private string JsonDisplay(JsonElement json)
    {
        var text = new StringBuilder();
        AppendJson(text, json, 0);
        return text.Length > _maxString ? text.ToString(0, _maxString) + "…" : text.ToString();
    }

    private void AppendJson(StringBuilder text, JsonElement json, int depth)
    {
        if (json.ValueKind is (JsonValueKind.Object or JsonValueKind.Array) && (depth >= _maxDepth || text.Length > _maxString))
        {
            text.Append('…');
            return;
        }

        var separator = "";
        switch (json.ValueKind)
        {
            case JsonValueKind.Object:
                text.Append('{');
                foreach (var property in json.EnumerateObject())
                {
                    if (text.Length > _maxString)
                    {
                        break;
                    }

                    text.Append(separator).Append('"').Append(property.Name).Append("\": ");
                    separator = ", ";
                    if (_redactor.IsSensitiveName(property.Name))
                    {
                        text.Append(BevRedactor.RedactedValue);
                    }
                    else
                    {
                        AppendJson(text, property.Value, depth + 1);
                    }
                }

                text.Append('}');
                break;
            case JsonValueKind.Array:
                text.Append('[');
                foreach (var item in json.EnumerateArray())
                {
                    if (text.Length > _maxString)
                    {
                        break;
                    }

                    text.Append(separator);
                    separator = ", ";
                    AppendJson(text, item, depth + 1);
                }

                text.Append(']');
                break;
            case JsonValueKind.String:
                text.Append(StringDisplay(json.GetString() ?? ""));
                break;
            default:
                text.Append(json.GetRawText());
                break;
        }
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
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && !Attribute.IsDefined(p, typeof(BevToolsIgnoreAttribute), true))
            .Where(p => p.GetMethod?.IsPublic == true)
            .Cast<MemberInfo>();
        var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !Attribute.IsDefined(f, typeof(BevToolsIgnoreAttribute), true))
            .Cast<MemberInfo>();
        return props.Concat(fields).ToArray();
    });

    private static bool HasIgnoredMember(Type type) => IgnoredMemberCache.GetOrAdd(type, static t =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => Attribute.IsDefined(p, typeof(BevToolsIgnoreAttribute), true))
        || t.GetFields(BindingFlags.Public | BindingFlags.Instance).Any(f => Attribute.IsDefined(f, typeof(BevToolsIgnoreAttribute), true)));

    private static Type MemberType(MemberInfo member) => member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;

    private static object? GetValue(MemberInfo member, object target) => member is PropertyInfo p ? p.GetValue(target) : ((FieldInfo)member).GetValue(target);

    /// <summary>
    /// Walks <paramref name="path"/> under the same rules <see cref="Describe(string, object?, string, MemberInfo?)"/>
    /// applies: a caller holding a crafted path (a frontend speaking the protocol) must not reach a value the panels
    /// show as not expandable, so configuration and key material end the walk wherever they appear, and so does the
    /// value of a pair whose name is sensitive.
    /// </summary>
    private object? Resolve(object? root, string path, List<object> ancestors)
    {
        var current = root;
        if (string.IsNullOrEmpty(path))
        {
            return NeverWalked(current) ? null : current;
        }

        foreach (var segment in ParsePath(path))
        {
            if (current is null || NeverWalked(current))
            {
                return null;
            }

            ancestors.Add(current);
            if (segment.IsIndex)
            {
                if (current is IDictionary && _redactor.IsSensitiveName(segment.Value))
                {
                    return null;
                }

                current = Index(current, segment.Value);
            }
            else
            {
                var member = GetMembers(current.GetType()).FirstOrDefault(m => m.Name == segment.Value)
                    ?? throw new InvalidOperationException($"Member '{segment.Value}' not found on {current.GetType().Name}.");
                if (_redactor.IsSensitive(member) || (IsPairValueMember(member) && PairNameIsSensitive(current)))
                {
                    return null;
                }

                current = GetValue(member, current);
            }
        }

        return NeverWalked(current) ? null : current;
    }

    private static bool NeverWalked(object? value) => value is not null && ConfigurationDisplay(value.GetType()) is not null;

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
                var pairNameIsSensitive = PairNameIsSensitive(value);
                foreach (var m in GetMembers(value.GetType()))
                {
                    if (OutOfBudget(result, maxEntries, deadline))
                    {
                        break;
                    }

                    var childPath = path.Length == 0 ? m.Name : path + "." + m.Name;
                    if (pairNameIsSensitive && IsPairValueMember(m))
                    {
                        result[childPath] = BevRedactor.RedactedValue;
                        continue;
                    }

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
