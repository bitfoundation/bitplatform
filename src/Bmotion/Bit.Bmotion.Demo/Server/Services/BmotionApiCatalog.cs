using System.Text;
using System.Reflection;
using System.Globalization;
using System.Collections.Concurrent;
using Bit.Bmotion.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the
/// Bit.Bmotion assembly and pairing every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the shipped
/// library - a new component parameter shows up the moment it is written, with the default value it
/// actually has (read off a freshly constructed instance) instead of one someone remembered to copy
/// into a document. For an animation library that matters twice over, because the defaults
/// <i>are</i> the behaviour: whether Damping is 10 or 20 is the difference between the motion an
/// agent wrote and the motion it meant.
/// </para>
/// </summary>
public static class BmotionApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Assembly _assembly = typeof(Bm).Assembly;

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. _assembly.GetExportedTypes()
            .Where(type => type.IsNested is false && type.Name.Contains('<', StringComparison.Ordinal) is false)
            .OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)]);

    private static readonly ConcurrentDictionary<string, BmotionApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    private static readonly Lazy<BmotionApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(type => new BmotionApiTypeDto
        {
            Name = FriendlyName(type),
            Kind = KindOf(type),
            Summary = BmotionXmlDocs.GetSummary(DocumentationId(type))
        })]);

    /// <summary>Every public type of Bit.Bmotion, with its summary.</summary>
    public static BmotionApiTypeDto[] Types => _types.Value;

    /// <summary>
    /// The full reference of one type - its Blazor parameters, properties, methods, events or enum
    /// values - or null when no public type goes by that name.
    /// </summary>
    public static BmotionApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assembly does.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    private static BmotionApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var instance = TryCreateInstance(type);

        var members = new List<BmotionApiMemberDto>();

        if (type.IsEnum)
        {
            members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                 .Select(field => new BmotionApiMemberDto
                                 {
                                     Name = field.Name,
                                     Kind = "EnumValue",
                                     Type = FriendlyName(type.GetEnumUnderlyingType()),
                                     // Formatted straight off the raw constant: a [Flags] enum backed
                                     // by ulong has members that do not fit in an Int64.
                                     Default = Format(field.GetRawConstantValue()),
                                     Summary = BmotionXmlDocs.GetSummary(DocumentationId(field)),
                                     Remarks = BmotionXmlDocs.GetRemarks(DocumentationId(field))
                                 }));
        }
        else
        {
            members.AddRange(Properties(type, instance));
            members.AddRange(Fields(type));
            members.AddRange(Methods(type));
            members.AddRange(Events(type));
        }

        return new BmotionApiTypeDetailsDto
        {
            Name = FriendlyName(type),
            FullName = type.FullName ?? type.Name,
            Kind = KindOf(type),
            BaseType = type.BaseType is null || type.BaseType == typeof(object) || type.BaseType == typeof(ValueType)
                ? null
                : FriendlyName(type.BaseType),
            Implements = [.. type.GetInterfaces()
                .Where(contract => contract.IsPublic && contract.DeclaringType is null)
                .Select(FriendlyName)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)],
            Summary = BmotionXmlDocs.GetSummary(DocumentationId(type)),
            Remarks = BmotionXmlDocs.GetRemarks(DocumentationId(type)),
            Members = [.. members.OrderBy(member => KindOrder(member.Kind)).ThenBy(member => member.Name, StringComparer.OrdinalIgnoreCase)]
        };
    }

    /// <summary>Resolves a type by its simple name, with or without a generic arity or argument list.</summary>
    private static Type? Find(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var name = typeName.Trim();

        var generic = name.IndexOf('<', StringComparison.Ordinal);
        if (generic > 0) name = name[..generic];

        return _publicTypes.Value.FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(type => string.Equals(StripArity(type.Name), name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(type => string.Equals(type.FullName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The type and every base of it that Bit.Bmotion itself declares. A component's inherited
    /// parameters belong in its reference; the members it gets from ComponentBase do not.
    /// </summary>
    private static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && current.Assembly == _assembly; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static IEnumerable<BmotionApiMemberDto> Properties(Type type, object? instance)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (seen.Add(property.Name) is false) continue;

            var isParameter = property.IsDefined(typeof(ParameterAttribute), inherit: true);

            yield return new BmotionApiMemberDto
            {
                Name = property.Name,
                Kind = isParameter ? "Parameter" : "Property",
                Type = FriendlyName(property.PropertyType),
                Default = DefaultValueOf(property, instance),
                Required = property.IsDefined(typeof(EditorRequiredAttribute), inherit: true),
                Summary = BmotionXmlDocs.GetSummary(DocumentationId(property)),
                Remarks = BmotionXmlDocs.GetRemarks(DocumentationId(property))
            };
        }
    }

    private static IEnumerable<BmotionApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new BmotionApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : null,
                Summary = BmotionXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = BmotionXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    private static IEnumerable<BmotionApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;

            var signature = Signature(method);
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new BmotionApiMemberDto
            {
                Name = method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Summary = BmotionXmlDocs.GetSummary(DocumentationId(method)),
                Remarks = BmotionXmlDocs.GetRemarks(DocumentationId(method))
            };
        }
    }

    private static IEnumerable<BmotionApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (seen.Add(member.Name) is false) continue;

            yield return new BmotionApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Summary = BmotionXmlDocs.GetSummary(DocumentationId(member)),
                Remarks = BmotionXmlDocs.GetRemarks(DocumentationId(member))
            };
        }
    }

    /// <summary>
    /// The value a property holds on a newly constructed instance - the default a caller gets when
    /// the parameter is left unset.
    /// </summary>
    private static string? DefaultValueOf(PropertyInfo property, object? instance)
    {
        if (property.GetMethod is null || property.GetMethod.IsPublic is false) return null;
        if (property.GetMethod.IsStatic is false && instance is null) return null;

        try
        {
            return Format(property.GetValue(property.GetMethod.IsStatic ? null : instance));
        }
        catch (Exception)
        {
            // Members that only make sense on a mounted component (or throw by design) simply have
            // no observable default; that is not a reason to fail the whole type's reference.
            return null;
        }
    }

    private static object? TryCreateInstance(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsEnum) return null;
        if (type.IsValueType is false && type.GetConstructor(Type.EmptyTypes) is null) return null;

        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? Format(object? value) => value switch
    {
        null => null,
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        Enum enumValue => $"{enumValue.GetType().Name}.{enumValue}",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Describe(value)
    };

    /// <summary>
    /// A reference-typed default is worth reporting: a property that starts out holding an object is
    /// not the same thing as one that starts out null.
    /// </summary>
    private static string Describe(object value)
    {
        var type = value.GetType();
        var text = value.ToString();

        return string.IsNullOrEmpty(text) || text == type.FullName || text == type.Name
            ? $"new {FriendlyName(type)}()"
            : text;
    }

    private static string Signature(MethodInfo method)
    {
        var builder = new StringBuilder("(");

        var parameters = method.GetParameters();

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (i > 0) builder.Append(", ");
            if (i == 0 && method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)) builder.Append("this ");
            if (parameter.IsOut) builder.Append("out ");
            else if (parameter.ParameterType.IsByRef) builder.Append("ref ");

            builder.Append(FriendlyName(parameter.ParameterType)).Append(' ').Append(parameter.Name);

            if (parameter.HasDefaultValue) builder.Append(" = ").Append(Format(parameter.DefaultValue) ?? "null");
        }

        return builder.Append(')').ToString();
    }

    private static string KindOf(Type type)
    {
        if (type.IsEnum) return "Enum";
        // Before the IComponent test: an interface that extends IComponent is still an interface.
        if (type.IsInterface) return "Interface";
        if (typeof(IComponent).IsAssignableFrom(type)) return "Component";
        if (typeof(Attribute).IsAssignableFrom(type)) return "Attribute";
        if (typeof(MulticastDelegate).IsAssignableFrom(type)) return "Delegate";
        if (type.IsAbstract && type.IsSealed) return "Static class";
        if (type.IsValueType) return "Struct";
        if (type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null) return "Record";

        return "Class";
    }

    private static int KindOrder(string kind) => kind switch
    {
        "Parameter" => 0,
        "Property" => 1,
        "Field" => 2,
        "EnumValue" => 3,
        "Method" => 4,
        "Event" => 5,
        _ => 6
    };

    /// <summary>The C# spelling of a type: "double?", "string[]", "BmValue&lt;double&gt;".</summary>
    public static string FriendlyName(Type type)
    {
        if (type.IsByRef) return FriendlyName(type.GetElementType()!);
        if (type.IsArray) return $"{FriendlyName(type.GetElementType()!)}[]";

        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null) return $"{FriendlyName(nullable)}?";

        if (type == typeof(void)) return "void";
        if (type == typeof(object)) return "object";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(decimal)) return "decimal";

        if (type.IsGenericType is false) return type.Name;

        var arguments = string.Join(", ", type.GetGenericArguments().Select(FriendlyName));

        return $"{StripArity(type.Name)}<{arguments}>";
    }

    private static string StripArity(string name)
    {
        var arity = name.IndexOf('`', StringComparison.Ordinal);

        return arity < 0 ? name : name[..arity];
    }

    private static string DocumentationId(Type type) => $"T:{type.FullName}";

    private static string DocumentationId(PropertyInfo property) => $"P:{property.DeclaringType!.FullName}.{property.Name}";

    private static string DocumentationId(FieldInfo field) => $"F:{field.DeclaringType!.FullName}.{field.Name}";

    private static string DocumentationId(EventInfo member) => $"E:{member.DeclaringType!.FullName}.{member.Name}";

    private static string DocumentationId(MethodInfo method)
    {
        var id = new StringBuilder("M:").Append(method.DeclaringType!.FullName).Append('.').Append(method.Name);

        if (method.IsGenericMethodDefinition) id.Append("``").Append(method.GetGenericArguments().Length);

        var parameters = method.GetParameters();
        if (parameters.Length == 0) return id.ToString();

        id.Append('(')
          .Append(string.Join(',', parameters.Select(parameter => DocumentationTypeName(parameter.ParameterType))))
          .Append(')');

        return id.ToString();
    }

    /// <summary>Spells a type the way the C# compiler spells it inside a documentation id.</summary>
    private static string DocumentationTypeName(Type type)
    {
        if (type.IsByRef) return $"{DocumentationTypeName(type.GetElementType()!)}@";
        if (type.IsArray) return $"{DocumentationTypeName(type.GetElementType()!)}[]";
        if (type.IsGenericParameter) return type.DeclaringMethod is null ? $"`{type.GenericParameterPosition}" : $"``{type.GenericParameterPosition}";

        if (type.IsGenericType is false) return type.FullName ?? type.Name;

        var definition = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var arguments = string.Join(',', type.GetGenericArguments().Select(DocumentationTypeName));

        return $"{StripArity(definition)}{{{arguments}}}";
    }
}
