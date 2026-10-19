using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;
using Bit.Bsqlite.Storage.Opfs;
using Bit.Bsqlite.Demo.Server.Dtos;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.CacheStorage;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the five
/// Bit.Bsqlite packages - the core, the three storage backends and the testing kit - and pairing
/// every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the shipped
/// library - a new member shows up the moment it is written, with the default value it actually has
/// (read off a freshly constructed instance) instead of one someone remembered to copy into a
/// document. And every type carries its namespace and its package, because Bsqlite's are spread over
/// half a dozen namespaces - two of them Microsoft's, where the DI and EF Core entry points live - and
/// a wrong <c>using</c> is the commonest way generated code fails to compile.
/// </para>
/// </summary>
public static class BsqliteApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>The five packages, core first: the order their types are listed in.</summary>
    public static readonly Assembly[] Assemblies =
    [
        typeof(BsqliteConnection).Assembly,
        typeof(BsqliteOpfsStorageProvider).Assembly,
        typeof(BsqliteIndexedDbStorageProvider).Assembly,
        typeof(BsqliteCacheStorageProvider).Assembly,
        typeof(BsqliteInProcessTransport).Assembly,
    ];

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(t => t.IsNested is false && t.Name.Contains('<', StringComparison.Ordinal) is false)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.FullName, StringComparer.Ordinal)]);

    private static readonly ConcurrentDictionary<string, BsqliteApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    private static readonly Lazy<BsqliteApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(t => new BsqliteApiTypeDto
        {
            Name = FriendlyName(t),
            Namespace = t.Namespace ?? string.Empty,
            Package = PackageOf(t),
            Kind = KindOf(t),
            Summary = BsqliteXmlDocs.GetSummary(DocumentationId(t))
        })]);

    /// <summary>Every public type of the Bit.Bsqlite packages, with its summary.</summary>
    public static BsqliteApiTypeDto[] Types => _types.Value;

    /// <summary>
    /// The index of the public types, as GetBsqliteApi answers when it is called without a name -
    /// grouped by package and namespace, since which <c>using</c> a type needs is half of what a
    /// caller choosing from the list wants to know.
    /// <para>
    /// A summary is cut to its first sentence here: what an index is for is choosing what to read
    /// next, and the paragraph that follows the first sentence is the answer to the call the reader
    /// has not made yet.
    /// </para>
    /// </summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bsqlite public API").AppendLine();
        builder.AppendLine($"{Types.Length} public types in {Assemblies.Length} packages. Call `GetBsqliteApi(typeName: \"...\")` for one of them in full - ")
               .AppendLine("its constructors, properties, methods or enum values, with the default value each one really has.")
               .AppendLine();

        foreach (var package in Types.GroupBy(type => type.Package).OrderBy(group => PackageOrder(group.Key)))
        {
            builder.AppendLine($"## Package {package.Key}").AppendLine();

            foreach (var ns in package.GroupBy(type => type.Namespace).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                builder.AppendLine($"### `using {ns.Key};`").AppendLine();

                foreach (var type in ns)
                {
                    builder.Append($"- **{type.Name}** ({type.Kind})");
                    if (FirstSentence(type.Summary) is string summary) builder.Append($" - {summary}");
                    builder.AppendLine();
                }

                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// The full reference of one type as Markdown, or null when no public type goes by that name.
    /// <para>
    /// Markdown rather than an object, and one renderer rather than two: the tool and the
    /// <c>bsqlite://api/{typeName}</c> resource hand out the same text, so neither can drift from the
    /// other - and a reference a model reads costs less as prose than as JSON sent twice, once under an
    /// output schema and once as the text that mirrors it.
    /// </para>
    /// </summary>
    public static string? RenderType(string typeName)
    {
        var details = GetTypeDetails(typeName);
        if (details is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# {details.Name} ({details.Kind})").AppendLine();
        // One line: the fragments continue the FullName rather than standing on their own, so the line
        // is only ended once every part of it has been written.
        builder.Append($"`{details.FullName}` - package `{details.Package}` - `using {details.Namespace};`");

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
                builder.Append(member.Protected ? $"- protected **{member.Name}**{member.Signature}" : $"- **{member.Name}**{member.Signature}");
                if (member.Type is not null) builder.Append($" : `{member.Type}`");
                if (member.Default is not null) builder.Append($" = `{member.Default}`");
                if (member.Required) builder.Append(" **(required)**");
                if (member.Summary is not null) builder.Append($" - {member.Summary}");
                builder.AppendLine();

                // The caveats live in the remarks, which is exactly the half of the documentation an
                // agent writing against a member needs and the least likely to be remembered.
                if (member.Remarks is not null) builder.AppendLine($"  {member.Remarks.Replace("\n", "\n  ", StringComparison.Ordinal)}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>The first sentence of a summary, for a listing that is read to choose by.</summary>
    internal static string? FirstSentence(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return null;

        var text = summary.ReplaceLineEndings(" ").Trim();

        var stop = text.IndexOf(". ", StringComparison.Ordinal);

        return stop > 0 ? text[..(stop + 1)] : text;
    }

    /// <summary>
    /// The full reference of one type - its constructors, properties, methods, events or enum values -
    /// or null when no public type goes by that name.
    /// </summary>
    public static BsqliteApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assemblies do.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    private static BsqliteApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var instance = TryCreateInstance(type);

        // A second instance, compared against the first, is what tells a real default from a value made
        // up per instance - reporting one of those as "the default" would hand a caller a value nobody
        // ever gets.
        var twin = TryCreateInstance(type);

        var members = new List<BsqliteApiMemberDto>();

        if (type.IsEnum)
        {
            members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                 .Select(field => new BsqliteApiMemberDto
                                 {
                                     Name = field.Name,
                                     Kind = "EnumValue",
                                     Type = FriendlyName(type.GetEnumUnderlyingType()),
                                     // Formatted straight off the raw constant, so a [Flags] value reads as
                                     // the number it combines by.
                                     Default = Format(field.GetRawConstantValue()),
                                     Summary = BsqliteXmlDocs.GetSummary(DocumentationId(field)),
                                     Remarks = BsqliteXmlDocs.GetRemarks(DocumentationId(field))
                                 }));
        }
        else
        {
            members.AddRange(Constructors(type));
            members.AddRange(Properties(type, instance, twin));
            members.AddRange(Fields(type));
            members.AddRange(Methods(type));
            members.AddRange(Events(type));
        }

        return new BsqliteApiTypeDetailsDto
        {
            Name = FriendlyName(type),
            FullName = type.FullName ?? type.Name,
            Namespace = type.Namespace ?? string.Empty,
            Package = PackageOf(type),
            Kind = KindOf(type),
            BaseType = type.BaseType is null || type.BaseType == typeof(object) || type.BaseType == typeof(ValueType)
                ? null
                : FriendlyName(type.BaseType),
            Implements = [.. type.GetInterfaces()
                .Where(i => i.IsPublic && i.DeclaringType is null)
                .Select(FriendlyName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)],
            Summary = BsqliteXmlDocs.GetSummary(DocumentationId(type)),
            Remarks = BsqliteXmlDocs.GetRemarks(DocumentationId(type)),
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
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(StripArity(t.FullName ?? string.Empty), name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The type and every base of it that Bit.Bsqlite itself declares. The members a type gets from
    /// DbConnection or Exception belong to their documentation, not to this one.
    /// </summary>
    private static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && Assemblies.Contains(current.Assembly); current = current.BaseType)
        {
            yield return current;
        }
    }

    /// <summary>
    /// Whether a member is part of what a caller can use: anything public, and - on a type that can be
    /// derived from - anything protected, since that is the half of the contract a subclass writes. The
    /// conformance kit is nothing but that: its whole API is the protected factory a test class
    /// overrides. On a sealed type a protected member is an override of the base class's plumbing.
    /// </summary>
    private static bool IsVisible(MethodBase? member, Type owner)
    {
        if (member is null) return false;
        if (member.IsPublic) return true;

        return owner.IsSealed is false && (member.IsFamily || member.IsFamilyOrAssembly);
    }

    private static bool IsProtected(MethodBase? member) => member is not null && member.IsPublic is false;

    private static IEnumerable<BsqliteApiMemberDto> Constructors(Type type)
    {
        // A static class has none to speak of, and an abstract one is constructed by its subclass.
        if (type.IsAbstract && type.IsSealed) yield break;

        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (IsVisible(constructor, type) is false) continue;

            yield return new BsqliteApiMemberDto
            {
                Name = FriendlyName(type),
                Kind = "Constructor",
                Signature = Signature(constructor),
                Protected = IsProtected(constructor),
                Summary = BsqliteXmlDocs.GetSummary(DocumentationId(constructor)),
                Remarks = BsqliteXmlDocs.GetRemarks(DocumentationId(constructor))
            };
        }
    }

    private static IEnumerable<BsqliteApiMemberDto> Properties(Type type, object? instance, object? twin)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;

            // Either accessor makes the property usable: an init-only setter on a get-less property is
            // still an option a caller sets.
            var accessor = IsVisible(property.GetMethod, type) ? property.GetMethod : property.SetMethod;
            if (IsVisible(accessor, type) is false) continue;

            // The record's EqualityContract is compiler plumbing every record has.
            if (property.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) || property.Name == "EqualityContract") continue;
            if (seen.Add(property.Name) is false) continue;

            // A required member has no default: the compiler refuses an object initializer that leaves
            // it out, so whatever an instance made by reflection holds is a value nobody can get.
            var required = property.IsDefined(typeof(RequiredMemberAttribute), inherit: false);

            yield return new BsqliteApiMemberDto
            {
                Name = property.Name,
                Kind = "Property",
                Type = FriendlyName(property.PropertyType),
                Default = required ? null : DefaultValueOf(property, instance, twin),
                Required = required,
                Protected = IsProtected(accessor),
                Summary = Documentation(property, BsqliteXmlDocs.GetSummary),
                Remarks = Documentation(property, BsqliteXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BsqliteApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.IsPublic is false) continue;
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new BsqliteApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : field.IsInitOnly && field.IsStatic ? SafeFormat(() => field.GetValue(null)) : null,
                Summary = BsqliteXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = BsqliteXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    private static IEnumerable<BsqliteApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (IsVisible(method, type) is false) continue;
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;
            // An explicit interface implementation ("Microsoft.EntityFrameworkCore.IDbContextFactory<...>.CreateDbContextAsync")
            // is reached through the interface, whose own reference lists it.
            if (method.Name.Contains('.', StringComparison.Ordinal)) continue;
            // What the compiler writes for a record - Equals, GetHashCode, ToString, Deconstruct, PrintMembers -
            // is the same on every record and documented nowhere; listing it on each of the library's records
            // would bury the members someone wrote on purpose.
            if (method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)) continue;

            var signature = Signature(method);
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new BsqliteApiMemberDto
            {
                Name = method.IsGenericMethodDefinition ? $"{method.Name}<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name))}>" : method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Protected = IsProtected(method),
                Summary = Documentation(method, BsqliteXmlDocs.GetSummary),
                Remarks = Documentation(method, BsqliteXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BsqliteApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (IsVisible(member.AddMethod, type) is false) continue;
            if (seen.Add(member.Name) is false) continue;

            yield return new BsqliteApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Summary = BsqliteXmlDocs.GetSummary(DocumentationId(member)) ?? InterfaceDocumentation(member, BsqliteXmlDocs.GetSummary),
                Remarks = BsqliteXmlDocs.GetRemarks(DocumentationId(member)) ?? InterfaceDocumentation(member, BsqliteXmlDocs.GetRemarks)
            };
        }
    }

    /// <summary>
    /// The documentation of a property, or - when it says no more than <c>&lt;inheritdoc /&gt;</c>, as
    /// every provider's Name and Capabilities do - that of the member it overrides or implements, which
    /// is what the compiler would show for it.
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

    private static string? InterfaceDocumentation(EventInfo member, Func<string, string?> read)
    {
        foreach (var contract in member.DeclaringType!.GetInterfaces())
        {
            if (contract.GetEvent(member.Name) is { } declared && read(DocumentationId(declared)) is string implemented) return implemented;
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
    /// the property is left unset.
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
            // Members that only make sense on an open connection (or throw by design) simply have no
            // observable default; that is not a reason to fail the whole type's reference.
            return null;
        }
    }

    /// <summary>
    /// A fresh instance to read defaults off, or null when there is no safe way to make one.
    /// <para>
    /// Only types with a parameterless constructor are built, and never one that owns something - a
    /// connection, a command, a transport, anything disposable: the reference would have to dispose
    /// what it built just to read a default, and a type like that is configured through its
    /// constructor's parameters, which the reference lists with their defaults anyway.
    /// </para>
    /// </summary>
    private static object? TryCreateInstance(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsEnum || type.ContainsGenericParameters) return null;
        if (typeof(IDisposable).IsAssignableFrom(type) || typeof(IAsyncDisposable).IsAssignableFrom(type)) return null;
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

    private static string? SafeFormat(Func<object?> read)
    {
        try
        {
            return Format(read());
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
        Enum enumValue => FormatEnum(enumValue),
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => Describe(value)
    };

    /// <summary>A [Flags] combination reads as the C# that writes it: "BsqliteExecutionContexts.Window | BsqliteExecutionContexts.DedicatedWorker".</summary>
    private static string FormatEnum(Enum value)
    {
        var name = value.GetType().Name;

        return string.Join(" | ", value.ToString().Split(", ").Select(part => $"{name}.{part}"));
    }

    /// <summary>
    /// A reference-typed default is worth reporting: a property that starts out holding an object is
    /// not the same thing as one that starts out null.
    /// </summary>
    private static string Describe(object value)
    {
        var type = value.GetType();

        // A default that is one of the type's own named values reads best by that name
        // (BsqliteRuntimeLimits.Unrestricted) rather than as a dump of its fields.
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

        // An empty collection is the most common reference-typed default, and its ToString is only its
        // type name; say what it is instead.
        if (value is System.Collections.ICollection { Count: 0 }) return "empty";

        var text = value.ToString();

        // A ToString that is only the type's name says nothing about the value - and for a generic type
        // it is the CLR spelling ("List`1[...]") no C# reader would recognise.
        return string.IsNullOrEmpty(text) || text == type.FullName || text == type.Name || text.Contains('`', StringComparison.Ordinal)
            ? $"new {FriendlyName(type)}()"
            : text;
    }

    private static string Signature(MethodBase method)
    {
        var builder = new StringBuilder("(");

        var parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (i > 0) builder.Append(", ");
            if (i == 0 && method.IsDefined(typeof(ExtensionAttribute), false)) builder.Append("this ");
            if (parameter.IsOut) builder.Append("out ");
            else if (parameter.ParameterType.IsByRef) builder.Append("ref ");

            builder.Append(FriendlyName(parameter.ParameterType)).Append(' ').Append(parameter.Name);

            if (parameter.HasDefaultValue) builder.Append(" = ").Append(FormatParameterDefault(parameter));
        }

        return builder.Append(')').ToString();
    }

    /// <summary>A parameter's default as C# writes it: <c>default</c> for a CancellationToken, <c>null</c> for an absent reference.</summary>
    private static string FormatParameterDefault(ParameterInfo parameter)
    {
        if (parameter.ParameterType == typeof(CancellationToken)) return "default";

        return Format(parameter.DefaultValue) ?? (parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) is null ? "default" : "null");
    }

    private static string KindOf(Type type)
    {
        if (type.IsEnum) return "Enum";
        if (type.IsInterface) return "Interface";
        if (typeof(Exception).IsAssignableFrom(type)) return "Exception";
        if (typeof(Attribute).IsAssignableFrom(type)) return "Attribute";
        if (typeof(MulticastDelegate).IsAssignableFrom(type)) return "Delegate";
        if (type.IsAbstract && type.IsSealed) return "Static class";
        if (type.IsValueType) return "Struct";
        if (type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null) return "Record";
        if (type.IsAbstract) return "Abstract class";

        return "Class";
    }

    private static int KindOrder(string kind) => kind switch
    {
        "Constructor" => 0,
        "Property" => 1,
        "Field" => 2,
        "EnumValue" => 3,
        "Method" => 4,
        "Event" => 5,
        _ => 6
    };

    private static int PackageOrder(string package) => Array.FindIndex(Assemblies, assembly => assembly.GetName().Name == package);

    /// <summary>The package a type ships in - every Bsqlite assembly is its own package, named after it.</summary>
    public static string PackageOf(Type type) => type.Assembly.GetName().Name ?? string.Empty;

    /// <summary>The C# spelling of a type: "int?", "string[]", "IReadOnlyList&lt;BsqliteStorageCandidateOutcome&gt;".</summary>
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
        if (type == typeof(byte)) return "byte";
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

    private static string DocumentationId(MethodBase method)
    {
        var name = method is ConstructorInfo ? "#ctor" : method.Name;
        var id = new StringBuilder("M:").Append(method.DeclaringType!.FullName).Append('.').Append(name);

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
