using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using Bit.Bonboarding.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the
/// Bit.Bonboarding assembly and pairing every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the
/// shipped library - a new parameter shows up the moment it is written, with the default value it
/// actually has (read off a freshly constructed instance) instead of one someone remembered to
/// copy into a document.
/// </para>
/// </summary>
public static class BonboardingApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Assembly _assembly = typeof(BonboardingRoot).Assembly;

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. _assembly.GetExportedTypes()
            .Where(t => t.IsNested is false && t.Name.Contains('<', StringComparison.Ordinal) is false)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)]);

    private static readonly ConcurrentDictionary<string, BonboardingApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    private static readonly Lazy<BonboardingApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(t => new BonboardingApiTypeDto
        {
            Name = FriendlyName(t),
            Kind = KindOf(t),
            Summary = BonboardingXmlDocs.GetSummary(DocumentationId(t))
        })]);

    /// <summary>Every public type of Bit.Bonboarding, with its summary.</summary>
    public static BonboardingApiTypeDto[] Types => _types.Value;

    /// <summary>
    /// The index of the public types, as GetBonboardingApi answers when it is called without a name.
    /// <para>
    /// A summary is cut to its first sentence here: what an index is for is choosing what to read
    /// next, and the paragraph that follows the first sentence is the answer to the call the reader
    /// has not made yet. Whole summaries made this list four times longer than the reference of the
    /// one type anybody wanted.
    /// </para>
    /// </summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bonboarding public API").AppendLine();
        builder.AppendLine($"{Types.Length} public types. Call `GetBonboardingApi(typeName: \"...\")` for one of them in full - ")
               .AppendLine("its parameters, properties, methods or enum values, with the default value each one really has.")
               .AppendLine();

        foreach (var type in Types)
        {
            builder.Append($"- **{type.Name}** ({type.Kind})");
            if (FirstSentence(type.Summary) is string summary) builder.Append($" - {summary}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// The full reference of one type as Markdown, or null when no public type goes by that name.
    /// <para>
    /// Markdown rather than an object, and one renderer rather than two: the tool and the
    /// <c>bonboarding://api/{typeName}</c> resource hand out the same text, so neither can drift from
    /// the other - and a reference a model reads costs less as prose than as JSON sent twice, once
    /// under an output schema and once as the text that mirrors it.
    /// </para>
    /// </summary>
    public static string? RenderType(string typeName)
    {
        var details = GetTypeDetails(typeName);
        if (details is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# {details.Name} ({details.Kind})").AppendLine();
        // One line: the fragments continue the FullName rather than standing on their own, so the
        // line is only ended once every part of it has been written.
        builder.Append($"`{details.FullName}`");

        if (details.BaseType is not null) builder.Append($" - inherits `{details.BaseType}`");
        if (details.Implements?.Length > 0) builder.Append($" - implements {string.Join(", ", details.Implements.Select(i => $"`{i}`"))}");

        builder.AppendLine().AppendLine();

        if (details.Summary is not null) builder.AppendLine(details.Summary).AppendLine();
        if (details.Remarks is not null) builder.AppendLine(details.Remarks).AppendLine();

        foreach (var group in details.Members.GroupBy(member => member.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var member in group)
            {
                builder.Append($"- **{member.Name}**{member.Signature}");
                if (member.Type is not null) builder.Append($" : `{member.Type}`");
                if (member.Default is not null) builder.Append($" = `{member.Default}`");
                if (member.Required) builder.Append(" **(required)**");
                if (member.Summary is not null) builder.Append($" - {member.Summary}");
                builder.AppendLine();

                // The caveats live in the remarks, which is exactly the half of the documentation
                // an agent writing against a member needs and the least likely to be remembered.
                if (member.Remarks is not null) builder.AppendLine($"  {member.Remarks.Replace("\n", "\n  ", StringComparison.Ordinal)}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>The first sentence of a summary, for a listing that is read to choose by.</summary>
    private static string? FirstSentence(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return null;

        var text = summary.ReplaceLineEndings(" ").Trim();

        var stop = text.IndexOf(". ", StringComparison.Ordinal);

        return stop > 0 ? text[..(stop + 1)] : text;
    }

    /// <summary>
    /// The full reference of one type - its Blazor parameters, properties, methods, events or enum
    /// values - or null when no public type goes by that name.
    /// </summary>
    public static BonboardingApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assembly does.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    private static BonboardingApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var instance = TryCreateInstance(type);

        // A second instance, compared against the first, is what tells a real default from a value
        // made up per instance - BonboardingStepDefinition.Id, for one, generates a fresh id each time,
        // and reporting one of those as "the default" would hand a caller a value nobody ever gets.
        var twin = TryCreateInstance(type);

        var members = new List<BonboardingApiMemberDto>();

        if (type.IsEnum)
        {
            members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                 .Select(field => new BonboardingApiMemberDto
                                 {
                                     Name = field.Name,
                                     Kind = "EnumValue",
                                     Type = FriendlyName(type.GetEnumUnderlyingType()),
                                     // Formatted straight off the raw constant: a [Flags] enum backed by
                                     // ulong has members that do not fit in an Int64.
                                     Default = Format(field.GetRawConstantValue()),
                                     Summary = BonboardingXmlDocs.GetSummary(DocumentationId(field)),
                                     Remarks = BonboardingXmlDocs.GetRemarks(DocumentationId(field))
                                 }));
        }
        else
        {
            members.AddRange(Properties(type, instance, twin));
            members.AddRange(Fields(type));
            members.AddRange(Methods(type));
            members.AddRange(Events(type));
        }

        return new BonboardingApiTypeDetailsDto
        {
            Name = FriendlyName(type),
            FullName = type.FullName ?? type.Name,
            Kind = KindOf(type),
            BaseType = type.BaseType is null || type.BaseType == typeof(object) || type.BaseType == typeof(ValueType)
                ? null
                : FriendlyName(type.BaseType),
            Implements = [.. type.GetInterfaces()
                .Where(i => i.IsPublic && i.DeclaringType is null)
                .Select(FriendlyName)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)],
            Summary = BonboardingXmlDocs.GetSummary(DocumentationId(type)),
            Remarks = BonboardingXmlDocs.GetRemarks(DocumentationId(type)),
            Members = [.. members.OrderBy(m => KindOrder(m.Kind)).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)]
        };
    }

    /// <summary>Resolves a type by its simple name, with or without a generic arity or argument list.</summary>
    private static Type? Find(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var name = typeName.Trim();

        var generic = name.IndexOf('<', StringComparison.Ordinal);
        if (generic > 0) name = name[..generic];

        return _publicTypes.Value.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(StripArity(t.Name), name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The type and every base of it that Bit.Bonboarding itself declares. A component's inherited
    /// parameters belong in its reference; the members it gets from ComponentBase do not.
    /// </summary>
    private static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && current.Assembly == _assembly; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static IEnumerable<BonboardingApiMemberDto> Properties(Type type, object? instance, object? twin)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (seen.Add(property.Name) is false) continue;

            var isParameter = property.IsDefined(typeof(ParameterAttribute), inherit: true);

            yield return new BonboardingApiMemberDto
            {
                Name = property.Name,
                Kind = isParameter ? "Parameter" : "Property",
                Type = FriendlyName(property.PropertyType),
                Default = DefaultValueOf(property, instance, twin),
                Required = property.IsDefined(typeof(EditorRequiredAttribute), inherit: true),
                Summary = BonboardingXmlDocs.GetSummary(DocumentationId(property)),
                Remarks = BonboardingXmlDocs.GetRemarks(DocumentationId(property))
            };
        }
    }

    private static IEnumerable<BonboardingApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new BonboardingApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : null,
                Summary = BonboardingXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = BonboardingXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    private static IEnumerable<BonboardingApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;

            var signature = Signature(method);
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new BonboardingApiMemberDto
            {
                Name = method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Summary = BonboardingXmlDocs.GetSummary(DocumentationId(method)),
                Remarks = BonboardingXmlDocs.GetRemarks(DocumentationId(method))
            };
        }
    }

    private static IEnumerable<BonboardingApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (seen.Add(member.Name) is false) continue;

            yield return new BonboardingApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Summary = BonboardingXmlDocs.GetSummary(DocumentationId(member)),
                Remarks = BonboardingXmlDocs.GetRemarks(DocumentationId(member))
            };
        }
    }

    /// <summary>
    /// The value a property holds on a newly constructed instance - the default a caller gets when
    /// the parameter is left unset.
    /// </summary>
    private static string? DefaultValueOf(PropertyInfo property, object? instance, object? twin)
    {
        if (property.GetMethod is null || property.GetMethod.IsPublic is false) return null;
        if (property.GetMethod.IsStatic is false && instance is null) return null;

        try
        {
            if (property.GetMethod.IsStatic) return Format(property.GetValue(null));

            var value = Format(property.GetValue(instance));

            return twin is null || value == Format(property.GetValue(twin)) ? value : null;
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
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => Describe(value)
    };

    /// <summary>
    /// A reference-typed default is worth reporting: a property that starts out holding an object
    /// (BonboardingOptions.Labels does) is not the same thing as one that starts out null.
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

    /// <summary>The C# spelling of a type: "int?", "string[]", "Func&lt;BonboardingStepContext, ValueTask&gt;".</summary>
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
          .Append(string.Join(',', parameters.Select(p => DocumentationTypeName(p.ParameterType))))
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
