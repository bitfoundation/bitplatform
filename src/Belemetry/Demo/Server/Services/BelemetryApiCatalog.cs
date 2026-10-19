using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using System.Globalization;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Bit.Belemetry.Demo.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// The public API of the Bit.Belemetry assembly loaded in this server, reflected - every type, its members,
/// their types, the default a freshly constructed options object actually has, and the summary from the
/// XML documentation the build emits next to it.
/// <para>
/// Nothing here is written by hand, so the answer cannot drift from the library: an option that is renamed
/// is renamed here by the same build, and a default that changes shows its new value. The docs pages' API
/// tables and the MCP server's <c>GetBelemetryApiDetails</c> both answer from this one catalog.
/// </para>
/// </summary>
public static partial class BelemetryApiCatalog
{
    private static readonly Assembly _assembly = typeof(BelemetryOptions).Assembly;

    private static readonly Lazy<FrozenDictionary<string, Type>> _types = new(() =>
        _assembly.GetExportedTypes()
                 .Where(t => t.IsNested is false && t.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
                 .ToFrozenDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase));

    private static readonly Lazy<FrozenDictionary<string, ApiTypeInfo>> _infos = new(() =>
        _types.Value.Values.ToFrozenDictionary(t => t.Name, Describe, StringComparer.OrdinalIgnoreCase));

    /// <summary>Every public type, by name.</summary>
    public static IEnumerable<ApiTypeInfo> All => _infos.Value.Values.OrderBy(t => t.Name, StringComparer.Ordinal);

    /// <summary>The reflected type of that name (with or without the namespace; case-insensitive), or null.</summary>
    public static ApiTypeInfo? Find(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var name = typeName.Trim();
        if (name.StartsWith("Bit.Belemetry.", StringComparison.OrdinalIgnoreCase)) name = name["Bit.Belemetry.".Length..];

        // "BelemetryOptions.Consent" asks about a member; answering with its type is the useful reading.
        var dot = name.IndexOf('.', StringComparison.Ordinal);
        if (dot > 0 && _infos.Value.ContainsKey(name) is false) name = name[..dot];

        return _infos.Value.GetValueOrDefault(name);
    }

    /// <summary>The names closest to <paramref name="typeName"/>, for a "did you mean" answer.</summary>
    public static string[] Suggest(string? typeName, int count = 5)
    {
        var term = (typeName ?? string.Empty).Trim();

        return [.. _infos.Value.Keys
                   .Select(name => (name, score: Score(name, term)))
                   .OrderByDescending(x => x.score)
                   .ThenBy(x => x.name, StringComparer.Ordinal)
                   .Take(count)
                   .Select(x => x.name)];
    }

    private static int Score(string name, string term)
    {
        if (term.Length == 0) return 0;
        if (name.Contains(term, StringComparison.OrdinalIgnoreCase)) return 1000 - name.Length;

        // Shared words: "consent options" and "OtlpOptions" both find their Belemetry*Options type.
        var words = CamelWords().Split(term).SelectMany(w => w.Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries)).ToArray();

        return words.Count(w => name.Contains(w, StringComparison.OrdinalIgnoreCase)) * 100 - name.Length;
    }

    private static ApiTypeInfo Describe(Type type)
    {
        var members = new List<ApiMemberInfo>();

        if (type.IsEnum)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var value = Convert.ToInt64(field.GetRawConstantValue(), CultureInfo.InvariantCulture);
                members.Add(new(field.Name, "EnumValue", null, null, value.ToString(CultureInfo.InvariantCulture), Doc($"F:{type.FullName}.{field.Name}")));
            }

            return new(type.Name, "Enum", Doc($"T:{type.FullName}"), [.. members]);
        }

        var instance = CreateDefault(type);
        var isComponent = typeof(IComponent).IsAssignableFrom(type);
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var field in type.GetFields(flags).Where(f => f.IsLiteral || f.IsInitOnly))
        {
            var value = field.IsLiteral ? field.GetRawConstantValue() : field.IsStatic ? field.GetValue(null) : null;
            members.Add(new(field.Name, field.IsLiteral ? "Constant" : "Field", TypeName(field.FieldType), null, Format(value), Doc($"F:{type.FullName}.{field.Name}")));
        }

        foreach (var property in type.GetProperties(flags).OrderBy(p => p.MetadataToken))
        {
            if (property.GetIndexParameters().Length > 0) continue;

            var kind = isComponent && property.GetCustomAttribute<ParameterAttribute>() is not null ? "Parameter" : "Property";
            if (isComponent && kind == "Property") continue;

            members.Add(new(property.Name, kind, TypeName(property.PropertyType), null, DefaultOf(property, instance), Doc($"P:{type.FullName}.{property.Name}")));
        }

        foreach (var evt in type.GetEvents(flags))
        {
            members.Add(new(evt.Name, "Event", TypeName(evt.EventHandlerType!), null, null, Doc($"E:{type.FullName}.{evt.Name}")));
        }

        foreach (var method in type.GetMethods(flags).Where(m => m.IsSpecialName is false).OrderBy(m => m.MetadataToken))
        {
            // Members a record or a component declares without the author writing them say nothing about the API.
            if (method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;
            if (method.Name is "Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "PrintMembers" or "<Clone>$") continue;
            if (isComponent && method.DeclaringType != type) continue;
            if (isComponent && method.Name is "SetParametersAsync" or "Dispose" or "DisposeAsync") continue;

            members.Add(new(method.Name, "Method", TypeName(method.ReturnType), Signature(method), null, Doc(MethodDocId(type, method))));
        }

        return new(type.Name, KindOf(type, isComponent), Doc($"T:{type.FullName}"), [.. members]);
    }

    private static string KindOf(Type type, bool isComponent)
    {
        if (type.IsInterface) return "Interface";
        if (type.IsValueType) return "Struct";
        if (isComponent) return "Component";
        if (type.IsAbstract && type.IsSealed) return "Static class";
        if (type.GetMethod("<Clone>$") is not null) return "Record";
        return "Class";
    }

    private static object? CreateDefault(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters) return null;
        if (typeof(IComponent).IsAssignableFrom(type) is false && type.Name.EndsWith("Options", StringComparison.Ordinal) is false) return null;

        try
        {
            return type.GetConstructor(Type.EmptyTypes) is null ? null : Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    private static string? DefaultOf(PropertyInfo property, object? instance)
    {
        if (instance is null || property.GetMethod is null || property.GetMethod.IsStatic) return null;

        // A nested options object is described by its own table, not by a default.
        if (property.PropertyType.Name.EndsWith("Options", StringComparison.Ordinal)) return null;

        try
        {
            return Format(property.GetValue(instance));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>A value as C# would write it: <c>true</c>, <c>"x"</c>, <c>00:00:05</c>, <c>["a", "b"]</c>, <c>BelemetryFeatures.All</c>.</summary>
    public static string? Format(object? value) => value switch
    {
        null => null,
        bool b => b ? "true" : "false",
        string s => $"\"{s}\"",
        TimeSpan t => t.ToString("c", CultureInfo.InvariantCulture),
        Enum e => FormatEnum(e),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        IDictionary { Count: 0 } => "{}",
        IDictionary d => "{ " + string.Join(", ", d.Keys.Cast<object>().Select(k => $"{Format(k)}: {Format(d[k])}")) + " }",
        IEnumerable e => "[" + string.Join(", ", e.Cast<object?>().Select(Format)) + "]",
        _ => null,
    };

    private static string FormatEnum(Enum value)
    {
        var name = value.ToString();
        var type = value.GetType().Name;

        return string.Join(" | ", name.Split(", ").Select(part => $"{type}.{part}"));
    }

    private static string? Doc(string id) => BelemetryXmlDocs.GetSummary(id);

    private static string Signature(MethodInfo method)
    {
        var builder = new StringBuilder("(");
        var parameters = method.GetParameters();

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (i > 0) builder.Append(", ");
            if (i == 0 && method.IsDefined(typeof(ExtensionAttribute))) builder.Append("this ");

            builder.Append(TypeName(parameter.ParameterType, parameter)).Append(' ').Append(parameter.Name);

            if (parameter.HasDefaultValue) builder.Append(" = ").Append(Format(parameter.DefaultValue) ?? "null");
        }

        return builder.Append(')').ToString();
    }

    private static readonly NullabilityInfoContext _nullability = new();

    /// <summary>A type as C# writes it: <c>string?</c>, <c>List&lt;string&gt;</c>, <c>Action&lt;BelemetryOptions&gt;?</c>.</summary>
    public static string TypeName(Type type) => TypeName(type, null);

    private static string TypeName(Type type, ParameterInfo? parameter)
    {
        var nullable = false;

        if (parameter is not null && type.IsValueType is false)
        {
            try { nullable = _nullability.Create(parameter).ReadState == NullabilityState.Nullable; } catch { /* best effort */ }
        }

        return Simple(type) + (nullable ? "?" : string.Empty);
    }

    private static string Simple(Type type)
    {
        if (type.IsByRef) return Simple(type.GetElementType()!);
        if (type.IsArray) return Simple(type.GetElementType()!) + "[]";
        if (Nullable.GetUnderlyingType(type) is { } underlying) return Simple(underlying) + "?";

        var keyword = Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "bool",
            TypeCode.Int32 => "int",
            TypeCode.Int64 => "long",
            TypeCode.Double => "double",
            TypeCode.Single => "float",
            TypeCode.Decimal => "decimal",
            TypeCode.Byte => "byte",
            TypeCode.String => "string",
            _ => null,
        };

        if (keyword is not null && type.IsEnum is false) return keyword;
        if (type == typeof(object)) return "object";
        if (type == typeof(void)) return "void";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Simple))}>";
        }

        return type.Name;
    }

    /// <summary>The XML documentation id of a method: <c>M:Ns.Type.Name(Ns.Arg,System.Action{Ns.Other})</c>.</summary>
    private static string MethodDocId(Type type, MethodInfo method)
    {
        var parameters = method.GetParameters();
        var id = $"M:{type.FullName}.{method.Name}";

        return parameters.Length == 0 ? id : $"{id}({string.Join(",", parameters.Select(p => DocTypeName(p.ParameterType)))})";
    }

    private static string DocTypeName(Type type)
    {
        if (type.IsByRef) return DocTypeName(type.GetElementType()!) + "@";
        if (type.IsArray) return DocTypeName(type.GetElementType()!) + "[]";

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition().FullName!;
            definition = definition[..definition.IndexOf('`', StringComparison.Ordinal)];
            return $"{definition}{{{string.Join(",", type.GetGenericArguments().Select(DocTypeName))}}}";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex CamelWords();
}
