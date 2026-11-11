using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Bit.BlazorUI;

/// <summary>
/// Decides which CLR types a model may bind to and describes them as JSON Schema, the way the
/// binder will read them.
/// </summary>
/// <remarks>
/// <para>
/// The two halves belong together because they must agree: a type the schema describes as
/// <c>"object"</c> with properties must be one the binder constructs, and a type the binder refuses
/// must never appear in the schema. Both walk object graphs to a bounded depth and treat a type
/// already on the path as a cycle, so a self-referencing record neither overflows the stack nor
/// produces an infinite schema.
/// </para>
/// <para>
/// Not bindable, at any depth: delegates, <see cref="RenderFragment"/>, <see cref="EventCallback"/>,
/// <see cref="Type"/>, and anything that would let a payload reach code rather than data. A record
/// with one such property is excluded whole, since binding half of it would still construct it.
/// </para>
/// <para>
/// Both walk the graph by reflection, over types nothing annotates (a parameter type, its property types, their
/// element types, ...), so every member that does is marked as unsafe to trim, and so are the registrations calling it.
/// </para>
/// </remarks>
internal static class BitAiGenerativeSchema
{
    /// <summary>How deep nested objects and collections are described before the rest is left open.</summary>
    internal const int MaxDepth = 6;

    private static readonly HashSet<Type> Scalars =
    [
        typeof(string), typeof(char), typeof(bool),
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan),
        typeof(Guid), typeof(Uri), typeof(JsonElement), typeof(JsonDocument), typeof(JsonNode),
        typeof(JsonObject), typeof(JsonArray), typeof(JsonValue), typeof(object),
    ];

    /// <summary>Whether a model may supply a value of this type, directly or inside a graph.</summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> when the type and everything reachable from it is plain data.</returns>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    public static bool IsBindable(Type type) => IsBindable(type, [], 0);

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static bool IsBindable(Type type, HashSet<Type> visiting, int depth)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (IsForbidden(type)) return false;
        if (Scalars.Contains(type) || type.IsEnum || type.IsPrimitive) return true;
        if (depth > MaxDepth || !visiting.Add(type)) return true;

        try
        {
            if (type.IsArray) return IsBindable(type.GetElementType()!, visiting, depth + 1);

            if (TryGetDictionaryTypes(type, out var key, out var value))
            {
                return IsBindable(key, visiting, depth + 1) && IsBindable(value, visiting, depth + 1);
            }

            if (TryGetElementType(type, out var element)) return IsBindable(element, visiting, depth + 1);

            foreach (var member in DataMembers(type))
            {
                if (!IsBindable(member.Type, visiting, depth + 1)) return false;
            }

            return true;
        }
        finally
        {
            visiting.Remove(type);
        }
    }

    /// <summary>Describes a type as JSON Schema.</summary>
    /// <param name="type">The type.</param>
    /// <param name="description">A description to attach, when there is one.</param>
    /// <returns>The schema node.</returns>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    public static Dictionary<string, object?> Describe(Type type, string? description) =>
        Describe(type, description, [], 0);

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static Dictionary<string, object?> Describe(Type type, string? description, HashSet<Type> visiting, int depth)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var node = new Dictionary<string, object?>(StringComparer.Ordinal);

        // Every type must be described the way the binder reads it: a model told "object" for a date
        // sends an object, and a model told "array" for a dictionary sends an array, and neither binds.
        if (underlying == typeof(string) || underlying == typeof(char))
        {
            node["type"] = "string";
        }
        else if (underlying == typeof(bool))
        {
            node["type"] = "boolean";
        }
        else if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short) || underlying == typeof(byte)
            || underlying == typeof(uint) || underlying == typeof(ulong) || underlying == typeof(ushort) || underlying == typeof(sbyte))
        {
            node["type"] = "integer";
        }
        else if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal))
        {
            node["type"] = "number";
        }
        else if (underlying.IsEnum)
        {
            node["type"] = "string";
            node["enum"] = Enum.GetNames(underlying);
        }
        else if (FormatOf(underlying) is { } format)
        {
            node["type"] = "string";
            node["format"] = format;
        }
        else if (underlying == typeof(object) || typeof(JsonNode).IsAssignableFrom(underlying)
            || underlying == typeof(JsonElement) || underlying == typeof(JsonDocument))
        {
            // Any JSON: the binder hands it over as it came.
        }
        else if (depth >= MaxDepth || visiting.Contains(underlying))
        {
            node["type"] = TryGetDictionaryTypes(underlying, out _, out _) ? "object"
                : underlying.IsArray || TryGetElementType(underlying, out _) ? "array"
                : "object";
        }
        else if (TryGetDictionaryTypes(underlying, out _, out var valueType))
        {
            visiting.Add(underlying);
            node["type"] = "object";
            node["additionalProperties"] = Describe(valueType, null, visiting, depth + 1);
            visiting.Remove(underlying);
        }
        else if (underlying.IsArray || TryGetElementType(underlying, out _))
        {
            var element = underlying.IsArray ? underlying.GetElementType()! : ElementTypeOf(underlying);
            visiting.Add(underlying);
            node["type"] = "array";
            node["items"] = element is null ? new Dictionary<string, object?>() : Describe(element, null, visiting, depth + 1);
            visiting.Remove(underlying);
        }
        else
        {
            visiting.Add(underlying);
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            var required = new List<string>();

            foreach (var member in DataMembers(underlying))
            {
                if (!IsBindable(member.Type)) continue;
                properties[member.Name] = Describe(member.Type, member.Description, visiting, depth + 1);
                if (member.IsRequired) required.Add(member.Name);
            }

            visiting.Remove(underlying);
            node["type"] = "object";
            node["properties"] = properties;
            node["additionalProperties"] = false;
            if (required.Count > 0) node["required"] = required;
        }

        description ??= underlying.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (description is not null) node["description"] = description;

        return node;
    }

    /// <summary>The public data members of a plain type: readable, writable, not ignored.</summary>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    internal static IEnumerable<(string Name, Type Type, string? Description, bool IsRequired)> DataMembers(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (property.GetMethod is null || !property.GetMethod.IsPublic) continue;
            if (property.SetMethod is null || !property.SetMethod.IsPublic)
            {
                // Init-only and get-only properties still bind through a matching constructor
                // parameter, which is how records work; skip only what nothing could ever set.
                if (!HasConstructorParameter(type, property.Name)) continue;
            }

            if (property.GetCustomAttribute<BitAiGenerativeIgnoreAttribute>() is not null) continue;
            if (property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is { Condition: System.Text.Json.Serialization.JsonIgnoreCondition.Always }) continue;

            yield return (
                property.Name,
                property.PropertyType,
                property.GetCustomAttribute<DescriptionAttribute>()?.Description,
                property.GetCustomAttribute<RequiredAttribute>() is not null);
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.IsInitOnly) continue;
            if (field.GetCustomAttribute<BitAiGenerativeIgnoreAttribute>() is not null) continue;

            yield return (
                field.Name,
                field.FieldType,
                field.GetCustomAttribute<DescriptionAttribute>()?.Description,
                field.GetCustomAttribute<RequiredAttribute>() is not null);
        }
    }

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static bool HasConstructorParameter(Type type, string name) =>
        type.GetConstructors().Any(c => c.GetParameters().Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)));

    private static bool IsForbidden(Type type)
    {
        if (typeof(Delegate).IsAssignableFrom(type)) return true;
        if (type == typeof(RenderFragment) || type == typeof(EventCallback)) return true;
        if (typeof(Type).IsAssignableFrom(type) || typeof(MemberInfo).IsAssignableFrom(type)) return true;
        if (typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(type)) return true;
        if (type == typeof(IntPtr) || type == typeof(UIntPtr) || type.IsPointer || type.IsByRef) return true;

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(RenderFragment<>) || definition == typeof(EventCallback<>)) return true;
        }

        return false;
    }

    private static string? FormatOf(Type type) => type switch
    {
        _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => "date-time",
        _ when type == typeof(DateOnly) => "date",
        _ when type == typeof(TimeOnly) => "time",
        _ when type == typeof(TimeSpan) => "duration",
        _ when type == typeof(Guid) => "uuid",
        _ when type == typeof(Uri) => "uri",
        _ => null,
    };

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static bool TryGetDictionaryTypes(Type type, out Type key, out Type value)
    {
        foreach (var candidate in Enumerable.Repeat(type, 1).Concat(type.GetInterfaces()))
        {
            if (!candidate.IsGenericType) continue;
            var definition = candidate.GetGenericTypeDefinition();
            if (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
            {
                var arguments = candidate.GetGenericArguments();
                key = arguments[0];
                value = arguments[1];
                return true;
            }
        }

        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            key = value = typeof(object);
            return true;
        }

        key = value = null!;
        return false;
    }

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static bool TryGetElementType(Type type, out Type element)
    {
        if (type == typeof(string))
        {
            element = null!;
            return false;
        }

        if (type.IsArray)
        {
            element = type.GetElementType()!;
            return true;
        }

        if (ElementTypeOf(type) is { } found)
        {
            element = found;
            return true;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            element = typeof(object);
            return true;
        }

        element = null!;
        return false;
    }

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static Type? ElementTypeOf(Type type)
    {
        if (type == typeof(string)) return null;

        foreach (var candidate in Enumerable.Repeat(type, 1).Concat(type.GetInterfaces()))
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return candidate.GetGenericArguments()[0];
            }
        }

        return typeof(IEnumerable).IsAssignableFrom(type) ? typeof(object) : null;
    }
}
