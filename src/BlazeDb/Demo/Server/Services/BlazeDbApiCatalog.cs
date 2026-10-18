using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using Bit.BlazeDb.Browser;
using Bit.BlazeDb.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the three
/// Bit.BlazeDb runtime assemblies and pairing every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the
/// shipped library - a new option shows up the moment it is written, with the default value it
/// actually has (read off a freshly constructed instance) instead of one someone remembered to
/// copy into a document. The source generator is not in this list: it has no public API, only the
/// attributes it reads (which live in Bit.BlazeDb), the members it generates (InspectBlazeDbTable)
/// and the diagnostics it reports (GetBlazeDbDiagnostics).
/// </para>
/// </summary>
public static class BlazeDbApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>The runtime assemblies whose public surface is the Bit.BlazeDb API, in package order.</summary>
    public static readonly Assembly[] Assemblies =
    [
        typeof(BlazeDbDatabase).Assembly,
        typeof(BlazeDbOpfsStorage).Assembly,
        typeof(BlazeDbDbContextOptionsExtensions).Assembly,
    ];

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(t => t.IsNested is false && t.Name.Contains('<', StringComparison.Ordinal) is false)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)]);

    private static readonly ConcurrentDictionary<string, BlazeDbApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    private static readonly Lazy<BlazeDbApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(t => new BlazeDbApiTypeDto
        {
            Name = FriendlyName(t),
            Kind = KindOf(t),
            Package = PackageOf(t),
            Summary = BlazeDbXmlDocs.GetSummary(DocumentationId(t))
        })]);

    /// <summary>Every public type of Bit.BlazeDb, with its summary.</summary>
    public static BlazeDbApiTypeDto[] Types => _types.Value;

    /// <summary>
    /// The index of the public types, grouped by package, as GetBlazeDbApi answers when it is called without a name.
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

        builder.AppendLine("# Bit.BlazeDb public API").AppendLine();
        builder.AppendLine($"{Types.Length} public types across {Assemblies.Length} packages. Call `GetBlazeDbApi(typeName: \"...\")` for one of them in full - ")
               .AppendLine("its properties, methods or enum values, with the default value each one really has. The generated members of a ")
               .AppendLine("[BlazeDbTable] type (`Table`, `Indexes`) are not here: `InspectBlazeDbTable` reports them for a given type.")
               .AppendLine();

        foreach (var package in Types.GroupBy(type => type.Package))
        {
            builder.AppendLine($"## {package.Key}").AppendLine();

            foreach (var type in package)
            {
                builder.Append($"- **{type.Name}** ({type.Kind})");
                if (FirstSentence(type.Summary) is string summary) builder.Append($" - {summary}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// The full reference of one type as Markdown, or null when no public type goes by that name.
    /// <para>
    /// Markdown rather than an object, and one renderer rather than two: the tool and the
    /// <c>blazedb://api/{typeName}</c> resource hand out the same text, so neither can drift from
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
        builder.Append($"`{details.FullName}` - package `{details.Package}`");

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
    /// The full reference of one type - its properties, methods, events or enum values - or null when
    /// no public type goes by that name.
    /// </summary>
    public static BlazeDbApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assembly does.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    private static BlazeDbApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var instance = TryCreateInstance(type);

        // A second instance, compared against the first, is what tells a real default from a value
        // made up per instance - a fresh Guid or a timestamp taken in a constructor, say - and
        // reporting one of those as "the default" would hand a caller a value nobody ever gets.
        var twin = TryCreateInstance(type);

        var members = new List<BlazeDbApiMemberDto>();

        if (type.IsEnum)
        {
            members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                 .Select(field => new BlazeDbApiMemberDto
                                 {
                                     Name = field.Name,
                                     Kind = "EnumValue",
                                     Type = FriendlyName(type.GetEnumUnderlyingType()),
                                     // Formatted straight off the raw constant: a [Flags] enum backed by
                                     // ulong has members that do not fit in an Int64.
                                     Default = Format(field.GetRawConstantValue()),
                                     Summary = BlazeDbXmlDocs.GetSummary(DocumentationId(field)),
                                     Remarks = BlazeDbXmlDocs.GetRemarks(DocumentationId(field))
                                 }));
        }
        else
        {
            members.AddRange(Properties(type, instance, twin));
            members.AddRange(Fields(type));
            members.AddRange(Methods(type));
            members.AddRange(Events(type));
        }

        return new BlazeDbApiTypeDetailsDto
        {
            Name = FriendlyName(type),
            FullName = type.FullName ?? type.Name,
            Kind = KindOf(type),
            Package = PackageOf(type),
            BaseType = type.BaseType is null || type.BaseType == typeof(object) || type.BaseType == typeof(ValueType)
                ? null
                : FriendlyName(type.BaseType),
            Implements = [.. type.GetInterfaces()
                .Where(i => i.IsPublic && i.DeclaringType is null)
                .Select(FriendlyName)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)],
            Summary = BlazeDbXmlDocs.GetSummary(DocumentationId(type)),
            Remarks = BlazeDbXmlDocs.GetRemarks(DocumentationId(type)),
            Members = [.. members.OrderBy(m => KindOrder(m.Kind)).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)]
        };
    }

    /// <summary>Resolves a type by its simple name, with or without a generic arity or argument list.</summary>
    private static Type? Find(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var name = typeName.Trim();

        // "BlazeDbTableDescriptor<TKey, TRow>" and "BlazeDbTableDescriptor" are two types (the generic
        // descriptor and its non-generic base), so an argument list is not decoration: its arity picks
        // between them.
        var generic = name.IndexOf('<', StringComparison.Ordinal);
        var arity = generic > 0 ? name[generic..].Count(c => c == ',') + 1 : 0;
        if (generic > 0) name = name[..generic];

        bool NameMatches(Type t) => string.Equals(StripArity(t.Name), name, StringComparison.OrdinalIgnoreCase);

        return _publicTypes.Value.FirstOrDefault(t => NameMatches(t) && t.GetGenericArguments().Length == arity)
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(NameMatches)
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The type and every base of it that Bit.BlazeDb itself declares. A member inherited from a
    /// BlazeDb base belongs in a type's reference; the members an exception gets from Exception do not.
    /// </summary>
    private static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && Assemblies.Contains(current.Assembly); current = current.BaseType)
        {
            yield return current;
        }
    }

    private static IEnumerable<BlazeDbApiMemberDto> Properties(Type type, object? instance, object? twin)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (seen.Add(property.Name) is false) continue;

            var isParameter = property.IsDefined(typeof(ParameterAttribute), inherit: true);

            yield return new BlazeDbApiMemberDto
            {
                Name = property.Name,
                Kind = isParameter ? "Parameter" : "Property",
                Type = FriendlyName(property.PropertyType),
                Default = DefaultValueOf(property, instance, twin),
                Required = property.IsDefined(typeof(EditorRequiredAttribute), inherit: true)
                           || property.IsDefined(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), inherit: false),
                Summary = Documentation(property, BlazeDbXmlDocs.GetSummary),
                Remarks = Documentation(property, BlazeDbXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BlazeDbApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new BlazeDbApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : null,
                Summary = BlazeDbXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = BlazeDbXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    private static IEnumerable<BlazeDbApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;
            // What the compiler writes for a record - Equals, GetHashCode, ToString, Deconstruct - is the
            // same on every record and documented nowhere; listing it on each of the library's many records
            // would bury the members someone wrote on purpose.
            if (method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)) continue;

            var signature = Signature(method);
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new BlazeDbApiMemberDto
            {
                Name = method.IsGenericMethodDefinition
                    ? $"{method.Name}<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name))}>"
                    : method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Summary = Documentation(method, BlazeDbXmlDocs.GetSummary),
                Remarks = Documentation(method, BlazeDbXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BlazeDbApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (seen.Add(member.Name) is false) continue;

            yield return new BlazeDbApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Summary = BlazeDbXmlDocs.GetSummary(DocumentationId(member)),
                Remarks = BlazeDbXmlDocs.GetRemarks(DocumentationId(member))
            };
        }
    }

    /// <summary>
    /// The documentation of a property, or - when it says no more than <c>&lt;inheritdoc /&gt;</c>, as the
    /// library's overrides and interface implementations do (a storage backend's ReadAsync, say) -
    /// that of the member it overrides or implements, which is what the compiler would show for it.
    /// </summary>
    private static string? Documentation(PropertyInfo property, Func<string, string?> read)
    {
        if (read(DocumentationId(property)) is string own) return own;

        var accessor = property.GetMethod ?? property.SetMethod;
        var root = accessor?.GetBaseDefinition();

        if (root is not null && root.DeclaringType != property.DeclaringType &&
            root.DeclaringType!.GetProperty(property.Name, Declared) is { } overridden &&
            read(DocumentationId(overridden)) is string inherited) return inherited;

        foreach (var contract in property.DeclaringType!.GetInterfaces())
        {
            if (contract.GetProperty(property.Name) is { } declared && read(DocumentationId(declared)) is string implemented) return implemented;
        }

        return null;
    }

    /// <summary>The documentation of a method, falling back to the one it overrides or implements.</summary>
    private static string? Documentation(MethodInfo method, Func<string, string?> read)
    {
        if (read(DocumentationId(method)) is string own) return own;

        var root = method.GetBaseDefinition();

        if (root.DeclaringType != method.DeclaringType && read(DocumentationId(root)) is string inherited) return inherited;

        var owner = method.DeclaringType!;
        if (owner.IsInterface) return null;

        foreach (var contract in owner.GetInterfaces())
        {
            InterfaceMapping map;

            try
            {
                map = owner.GetInterfaceMap(contract);
            }
            catch (Exception)
            {
                // A generic or otherwise unmappable interface simply has nothing to lend.
                continue;
            }

            var index = Array.IndexOf(map.TargetMethods, method);

            if (index >= 0 && read(DocumentationId(map.InterfaceMethods[index])) is string implemented) return implemented;
        }

        return null;
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
            // Members that only make sense on an open database, or in a browser (or throw by design),
            // simply have no observable default; that is not a reason to fail the whole type's reference.
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
        TimeSpan span => $"TimeSpan.Parse(\"{span}\")",
        Delegate => null,
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => Describe(value)
    };

    /// <summary>
    /// A reference-typed default is worth reporting: a property that starts out holding an object
    /// (BlazeDbDatabaseOptions.Tables does) is not the same thing as one that starts out null.
    /// </summary>
    private static string Describe(object value)
    {
        var type = value.GetType();

        // A default that is one of the type's own named values reads best by that name:
        // TimeSpan.Zero, EventCallback.Empty - rather than a dump of its fields.
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == type && field.IsInitOnly | field.IsLiteral && Equals(field.GetValue(null), value)) return $"{FriendlyName(type)}.{field.Name}";
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (property.PropertyType != type || property.GetIndexParameters().Length > 0) continue;

            try
            {
                if (Equals(property.GetValue(null), value)) return $"{FriendlyName(type)}.{property.Name}";
            }
            catch (Exception)
            {
                // A static property that throws simply names nothing.
            }
        }

        var text = value.ToString();

        // A ToString that is only the type's name says nothing about the value - and for a generic
        // type it is the CLR spelling ("EventCallback`1[...]") no C# reader would recognise.
        return string.IsNullOrEmpty(text) || text == type.FullName || text == type.Name || text.Contains('`', StringComparison.Ordinal)
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

    private static string PackageOf(Type type) => type.Assembly.GetName().Name ?? "Bit.BlazeDb";

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

    /// <summary>The C# spelling of a type: "int?", "string[]", "BlazeDbTable&lt;Guid, TodoItem&gt;".</summary>
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
