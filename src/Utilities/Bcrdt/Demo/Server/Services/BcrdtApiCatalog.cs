using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using Bit.Bcrdt.Server;
using Bit.Bcrdt.SignalR;
using Bit.Bcrdt.Testing;
using Bit.Bcrdt.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components;

namespace Bit.Bcrdt.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the four
/// Bit.Bcrdt packages and pairing every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the
/// shipped library - a new option shows up the moment it is written, with the default value it
/// actually has (read off a freshly constructed instance) instead of one someone remembered to
/// copy into a document.
/// </para>
/// <para>
/// Unlike a component library's, this reference lists constructors and protected members as well.
/// A CRDT engine is used by constructing it - <c>new BcrdtSyncProvider(document, transport)</c> in a
/// worker, <c>new BcrdtUndoManager(text)</c> in a page - and extended by deriving from it:
/// <c>BcrdtComponentBase.Track</c>, <c>BcrdtHub.AuthorizeAsync</c> and
/// <c>BcrdtAbstractType.CreateInstance</c> are protected, and they are exactly what the guide tells
/// people to call or override.
/// </para>
/// </summary>
public static class BcrdtApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>The assemblies whose public surface is the Bit.Bcrdt API, in package order.</summary>
    public static readonly Assembly[] Assemblies =
    [
        typeof(BcrdtDocument).Assembly,
        typeof(BcrdtSignalRTransport).Assembly,
        typeof(BcrdtHub).Assembly,
        typeof(BcrdtSimulatedNetwork).Assembly,
    ];

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(t => t.IsNested is false && t.Name.Contains('<', StringComparison.Ordinal) is false && IsRazorImports(t) is false)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)]);

    /// <summary>
    /// The class the Razor compiler makes of a library's <c>_Imports.razor</c>. It is public because
    /// every compiled .razor file is, and it is nothing anyone writes against.
    /// </summary>
    public static bool IsRazorImports(Type type) => type.Name == "_Imports";

    private static readonly ConcurrentDictionary<string, BcrdtApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    private static readonly Lazy<BcrdtApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(t => new BcrdtApiTypeDto
        {
            Name = FriendlyName(t),
            Kind = KindOf(t),
            Package = PackageOf(t),
            Summary = BcrdtXmlDocs.GetSummary(DocumentationId(t))
        })]);

    /// <summary>Every public type of Bit.Bcrdt, with its summary.</summary>
    public static BcrdtApiTypeDto[] Types => _types.Value;

    /// <summary>
    /// The index of the public types, grouped by package, as GetBcrdtApi answers when it is called without a name.
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

        builder.AppendLine("# Bit.Bcrdt public API").AppendLine();
        builder.AppendLine($"{Types.Length} public types across {Assemblies.Length} packages. Call `GetBcrdtApi(typeName: \"...\")` for one of them in full - ")
               .AppendLine("its constructors, parameters, properties, methods, events or enum values, with the default value each one really has.")
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
    /// <c>bcrdt://api/{typeName}</c> resource hand out the same text, so neither can drift from
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
                if (member.Protected) builder.Append(" *(protected)*");
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

        // "(e.g. a console app ...)" is not where a sentence ends.
        var stop = text.IndexOf(". ", StringComparison.Ordinal);
        while (stop > 0 && (text[..stop].EndsWith("e.g", StringComparison.Ordinal) || text[..stop].EndsWith("i.e", StringComparison.Ordinal)))
        {
            stop = text.IndexOf(". ", stop + 2, StringComparison.Ordinal);
        }

        return stop > 0 ? text[..(stop + 1)] : text;
    }

    /// <summary>
    /// The full reference of one type - its constructors, parameters, properties, methods, events or
    /// enum values - or null when no public type goes by that name.
    /// </summary>
    public static BcrdtApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assemblies do.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    private static BcrdtApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var instance = TryCreateInstance(type);

        // A second instance, compared against the first, is what tells a real default from a value
        // made up per instance - a document's random client id, for one, differs between two
        // instances, and reporting it as "the default" would hand a caller a value nobody ever gets.
        var twin = TryCreateInstance(type);

        var members = new List<BcrdtApiMemberDto>();

        if (type.IsEnum)
        {
            members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                 .Select(field => new BcrdtApiMemberDto
                                 {
                                     Name = field.Name,
                                     Kind = "EnumValue",
                                     Type = FriendlyName(type.GetEnumUnderlyingType()),
                                     // Formatted straight off the raw constant: the wire discriminators are
                                     // bytes, and what an agent needs is the number on the wire.
                                     Default = Format(field.GetRawConstantValue()),
                                     Summary = BcrdtXmlDocs.GetSummary(DocumentationId(field)),
                                     Remarks = BcrdtXmlDocs.GetRemarks(DocumentationId(field))
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

        Release(instance);
        Release(twin);

        return new BcrdtApiTypeDetailsDto
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
            Summary = BcrdtXmlDocs.GetSummary(DocumentationId(type)),
            Remarks = BcrdtXmlDocs.GetRemarks(DocumentationId(type)),
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
    /// The type and every base of it that Bit.Bcrdt itself declares. BcrdtText's reference carries what
    /// it inherits from BcrdtAbstractType; the members an exception gets from Exception, or a component
    /// from ComponentBase, do not belong in it.
    /// </summary>
    private static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && Assemblies.Contains(current.Assembly); current = current.BaseType)
        {
            yield return current;
        }
    }

    /// <summary>
    /// Whether a member is part of the API of <paramref name="type"/>: public, or protected on the type
    /// that declares it, when that type can be derived from. Inherited, a protected member is the base's
    /// API rather than this type's - BcrdtComponentBase's Track belongs in its reference, not repeated in
    /// every component's - and a protected override of a framework method, a component's
    /// <c>OnParametersSet</c>, is its own lifecycle plumbing, not something a caller writes against.
    /// </summary>
    private static bool IsApi(Type type, MethodBase? accessor)
    {
        if (accessor is null) return false;
        if (accessor.IsPublic) return true;
        if (accessor.IsFamily is false && accessor.IsFamilyOrAssembly is false) return false;
        if (type.IsSealed || accessor.DeclaringType != type) return false;

        return accessor is not MethodInfo method
            || method.GetBaseDefinition() is not { DeclaringType: { } root }
            || Assemblies.Contains(root.Assembly);
    }

    private static IEnumerable<BcrdtApiMemberDto> Constructors(Type type)
    {
        if (type.IsAbstract && type.IsSealed) yield break;

        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (IsApi(type, constructor) is false) continue;

            var parameters = constructor.GetParameters();

            // The copy constructor a record gets for `with` is the compiler's, and nobody calls it.
            if (parameters.Length == 1 && parameters[0].ParameterType == type && KindOf(type) == "Record") continue;

            var summary = BcrdtXmlDocs.GetSummary(DocumentationId(constructor));

            // An undocumented constructor that takes nothing says only that the type can be created, which an
            // options class's own reference already makes plain - listed, it is a heading of noise on every one.
            if (parameters.Length == 0 && summary is null) continue;

            yield return new BcrdtApiMemberDto
            {
                Name = $"new {StripArity(type.Name)}",
                Kind = "Constructor",
                Signature = Signature(constructor),
                Protected = constructor.IsPublic is false,
                Summary = summary,
                Remarks = BcrdtXmlDocs.GetRemarks(DocumentationId(constructor))
            };
        }
    }

    private static IEnumerable<BcrdtApiMemberDto> Properties(Type type, object? instance, object? twin)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;

            var accessor = property.GetMethod is { } getter && IsApi(type, getter) ? getter : property.SetMethod;
            if (IsApi(type, accessor) is false) continue;
            if (seen.Add(property.Name) is false) continue;

            var isParameter = property.IsDefined(typeof(ParameterAttribute), inherit: true)
                              || property.IsDefined(typeof(CascadingParameterAttribute), inherit: true);

            yield return new BcrdtApiMemberDto
            {
                Name = property.Name,
                Kind = isParameter ? "Parameter" : "Property",
                Type = FriendlyName(property.PropertyType),
                Default = DefaultValueOf(property, instance, twin),
                Required = property.IsDefined(typeof(EditorRequiredAttribute), inherit: true)
                           || property.IsDefined(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), inherit: false),
                Protected = accessor!.IsPublic is false,
                Summary = Documentation(property, BcrdtXmlDocs.GetSummary),
                Remarks = Documentation(property, BcrdtXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BcrdtApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (field.IsPublic is false && ((field.IsFamily || field.IsFamilyOrAssembly) is false || type.IsSealed || field.DeclaringType != type)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new BcrdtApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : field.IsStatic && field.IsInitOnly ? Format(field.GetValue(null)) : null,
                Protected = field.IsPublic is false,
                Summary = BcrdtXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = BcrdtXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    private static IEnumerable<BcrdtApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;
            if (IsApi(type, method) is false) continue;
            // What the compiler writes for a record - Equals, GetHashCode, ToString, Deconstruct - is the
            // same on every record and documented nowhere; listing it on each of the library's many records
            // would bury the members someone wrote on purpose.
            if (method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)) continue;
            // An override of what every object has, which the type does not document, says nothing about the
            // type - and split into words it answers questions it has nothing to do with: to a search,
            // GetHashCode is "code", and the top hit for anything done from code.
            var summary = Documentation(method, BcrdtXmlDocs.GetSummary);
            if (summary is null && method.Name is "Equals" or "GetHashCode" or "ToString") continue;
            // BcrdtTextArea's caret callbacks are public only because JavaScript interop can call nothing
            // else. Listed next to its parameters they read as API, and an agent calling one from C#
            // bypasses the very checks the browser made before calling it.
            if (method.IsDefined(typeof(Microsoft.JSInterop.JSInvokableAttribute), inherit: false)) continue;

            var signature = Signature(method);
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new BcrdtApiMemberDto
            {
                Name = method.IsGenericMethodDefinition
                    ? $"{method.Name}<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name))}>"
                    : method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Protected = method.IsPublic is false,
                Summary = summary,
                Remarks = Documentation(method, BcrdtXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BcrdtApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (IsApi(type, member.AddMethod) is false) continue;
            if (seen.Add(member.Name) is false) continue;

            yield return new BcrdtApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Protected = member.AddMethod!.IsPublic is false,
                Summary = BcrdtXmlDocs.GetSummary(DocumentationId(member)),
                Remarks = BcrdtXmlDocs.GetRemarks(DocumentationId(member))
            };
        }
    }

    /// <summary>
    /// The documentation of a property, or - when it says no more than <c>&lt;inheritdoc /&gt;</c>, as
    /// the transports' members do (each one documents IBcrdtTransport once) - that of the member it
    /// overrides or implements, which is what the compiler would show for it.
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
    /// the option is left unset.
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
            // Members that only make sense on an attached type, an open document or a mounted component
            // (or throw by design) simply have no observable default; that is not a reason to fail the
            // whole type's reference.
            return null;
        }
    }

    private static object? TryCreateInstance(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsEnum || type.ContainsGenericParameters) return null;
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

    /// <summary>
    /// Disposes an instance made only to read its defaults. A document left undisposed holds nothing
    /// unmanaged, but a store or a transport made the same way might one day, and this costs nothing.
    /// </summary>
    private static void Release(object? instance)
    {
        try
        {
            (instance as IDisposable)?.Dispose();
        }
        catch (Exception)
        {
            // An instance that was never attached to anything may object to being disposed; it has
            // already told us what it had to.
        }
    }

    private static string? Format(object? value) => value switch
    {
        null => null,
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        Enum enumValue => $"{enumValue.GetType().Name}.{enumValue}",
        // Every timing option of the library is a TimeSpan, and "00:00:30" makes a reader count colons.
        TimeSpan span when span == TimeSpan.Zero => "TimeSpan.Zero",
        TimeSpan span when span.Ticks % TimeSpan.TicksPerSecond == 0 => $"TimeSpan.FromSeconds({span.Ticks / TimeSpan.TicksPerSecond})",
        TimeSpan span when span.Ticks % TimeSpan.TicksPerMillisecond == 0 => $"TimeSpan.FromMilliseconds({span.Ticks / TimeSpan.TicksPerMillisecond})",
        Delegate => null,
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => Describe(value)
    };

    /// <summary>
    /// A reference-typed default is worth reporting: a property that starts out holding an object
    /// (BcrdtCollaborationOptions.Sync does) is not the same thing as one that starts out null.
    /// </summary>
    private static string Describe(object value)
    {
        var type = value.GetType();

        // A default that is one of the type's own named values reads best by that name:
        // BcrdtRandomClientIdProvider.Instance, EventCallback.Empty - rather than a dump of its fields.
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

    private static string Signature(MethodBase method)
    {
        var builder = new StringBuilder("(");

        var parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (i > 0) builder.Append(", ");
            if (i == 0 && method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)) builder.Append("this ");
            if (parameter.IsDefined(typeof(ParamArrayAttribute), false)) builder.Append("params ");
            if (parameter.IsOut) builder.Append("out ");
            else if (parameter.ParameterType.IsByRef) builder.Append("ref ");

            builder.Append(FriendlyName(parameter.ParameterType)).Append(' ').Append(parameter.Name);

            if (parameter.HasDefaultValue) builder.Append(" = ").Append(Format(parameter.DefaultValue) ?? "null");
        }

        return builder.Append(')').ToString();
    }

    private static string PackageOf(Type type) => type.Assembly.GetName().Name ?? "Bit.Bcrdt";

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
        "Constructor" => 1,
        "Property" => 2,
        "Field" => 3,
        "EnumValue" => 4,
        "Method" => 5,
        "Event" => 6,
        _ => 7
    };

    /// <summary>The C# spelling of a type: "ulong?", "byte[]", "ReadOnlyMemory&lt;byte&gt;", "Action&lt;BcrdtUpdateEventArgs&gt;".</summary>
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
        if (type == typeof(char)) return "char";
        if (type == typeof(short)) return "short";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
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
        var id = new StringBuilder("M:").Append(method.DeclaringType!.FullName).Append('.')
                                        .Append(method is ConstructorInfo ? "#ctor" : method.Name);

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
