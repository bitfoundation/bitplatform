using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using Bit.Butil.Demo.Client.Docs;
using Bit.Butil.Demo.Server.Dtos;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the
/// Bit.Butil assembly and pairing every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the
/// shipped library - a new wrapper shows up the moment it is written, with the exact signature it
/// actually has instead of one someone remembered to copy into a document. It matters more here
/// than in a smaller library: Butil is roughly seventy services over a thousand members, and the
/// members are precisely what an agent otherwise invents.
/// </para>
/// </summary>
public static class ButilApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // What the JSON around the text costs, for the cap Trim holds an answer to. The property
    // names, quotes, colons, commas and braces of one serialized member come to about eighty-five
    // characters whatever the member says, so a sixty-member type carries five kilobytes that a
    // count of the text alone cannot see; these round up from that, and the remainder is headroom
    // for the escaping that a summary with quotes or line breaks adds.
    private const int MemberJsonOverhead = 96;

    private const int DetailsJsonOverhead = 192;

    private static readonly Assembly _assembly = typeof(BitButil).Assembly;

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. _assembly.GetExportedTypes()
            .Where(t => t.IsNested is false && t.Name.Contains('<', StringComparison.Ordinal) is false)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)]);

    private static readonly ConcurrentDictionary<string, ButilApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    /// <summary>
    /// The version of the Bit.Butil assembly these answers are reflected out of - what the MCP
    /// server reports as its own version, because that is what an answer from it is true of. The
    /// informational version carries the source-revision suffix the build stamps on, which is
    /// exactly what identifies one build of a documentation server from another.
    /// </summary>
    public static string Version { get; } =
        _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? _assembly.GetName().Version?.ToString()
        ?? "unknown";

    /// <summary>
    /// The same version with the build metadata cut off - "1.0.0" rather than "1.0.0+eba7b0d97...".
    /// Used wherever the version is read as prose rather than matched as an identity: a forty-
    /// character commit hash mid-sentence costs a client's context window and tells a reader nothing
    /// they can act on, and the full string is still one initialize away in serverInfo.
    /// </summary>
    public static string DisplayVersion { get; } = Version.Split('+')[0];

    private static readonly Lazy<ButilApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(t => new ButilApiTypeDto
        {
            Name = FriendlyName(t),
            Kind = KindOf(t),
            IsInjectable = IsService(t),
            Summary = ButilXmlDocs.GetSummary(DocumentationId(t))
        })]);

    /// <summary>Every public type of Bit.Butil, with its summary.</summary>
    public static ButilApiTypeDto[] Types => _types.Value;

    /// <summary>The injectable services only - the classes marked [ButilService].</summary>
    public static ButilApiTypeDto[] Services => [.. Types.Where(t => t.IsInjectable)];

    private static readonly Lazy<ButilApiTypeDto[]> _listing = new(() =>
        [.. Types.Select(t => IsCallableSurface(t) ? t : t with { Summary = null })]);

    /// <summary>
    /// Every public type, as the listing hands them out: name and kind for all of them, and a
    /// summary for the ones the listing exists to be chosen from.
    /// <para>
    /// A caller reading this is picking something to call - a service to inject, or a static class
    /// to call through - and for those the summary is what separates two names. The rest of the
    /// surface is options records, event args, handles and enums, which nobody picks off a list:
    /// they are met in a signature and then looked up by the name that signature gave. Their
    /// summaries were two thirds of an answer that came to 45,000 characters, for a question every
    /// one of them answers better on its own.
    /// </para>
    /// </summary>
    public static ButilApiTypeDto[] TypeListing => _listing.Value;

    /// <summary>The types a caller reaches for by name: the injectable services and the static classes.</summary>
    private static bool IsCallableSurface(ButilApiTypeDto type) => type.IsInjectable || type.Kind == "Static class";

    /// <summary>
    /// The full reference of one type - its members with their signatures and documentation - or
    /// null when no public type goes by that name.
    /// </summary>
    public static ButilApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assembly does.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    /// <summary>
    /// The same reference, held to what one answer may cost a client's context window.
    /// <para>
    /// A handful of types are enormous - the extension classes carry sixty members, each with the
    /// XML remarks that explain its caveats - and a document tool truncates for exactly this reason.
    /// Cutting mid-member would be the wrong cut here: the members ARE the answer, and the last one
    /// is as likely to be the one asked about as the first. The remarks are what the size is, so
    /// they are what goes first, leaving every member with its signature, its defaults and its
    /// summary, and a sentence saying where the rest is.
    /// </para>
    /// <para>
    /// Every cut is measured rather than assumed to have been enough: a type whose summaries alone
    /// outrun the cap loses those too, and one that outruns it on signatures alone loses whole
    /// members off the end - a break between two members being the last cut available that leaves
    /// every member it keeps intact. The note says which cut was made, so a caller can tell a
    /// shortened reference from a whole one.
    /// </para>
    /// </summary>
    public static ButilApiTypeDetailsDto Trim(ButilApiTypeDetailsDto details, int maxLength)
    {
        if (Length(details) <= maxLength) return details;

        var trimmed = details with { Members = [.. details.Members.Select(m => m with { Remarks = null })] };
        var cut = "The per-member remarks are omitted";

        if (Fits(trimmed, cut, maxLength) is false)
        {
            trimmed = trimmed with { Members = [.. trimmed.Members.Select(m => m with { Summary = null })] };
            cut = "The per-member remarks and summaries are omitted";
        }

        if (Fits(trimmed, cut, maxLength) is false)
        {
            // The note is measured with both counts at their widest, so the sentence that finally
            // goes in can only be shorter than the room this reserved for it.
            var total = details.Members.Length;
            var room = maxLength - Length(trimmed with { Members = [], Remarks = Note(trimmed, MemberCut(total, total), maxLength) });
            var kept = 0;

            foreach (var member in trimmed.Members)
            {
                room -= Length(member);
                if (room < 0) break;

                kept++;
            }

            cut = MemberCut(kept, total);
            trimmed = trimmed with { Members = [.. trimmed.Members.Take(kept)] };
        }

        return trimmed with { Remarks = Note(trimmed, cut, maxLength) };
    }

    /// <summary>Whether the reference and the note that explains the cut both fit inside the cap.</summary>
    private static bool Fits(ButilApiTypeDetailsDto details, string cut, int maxLength) =>
        Length(details with { Remarks = Note(details, cut, maxLength) }) <= maxLength;

    /// <summary>
    /// What replaces the remarks of a shortened reference: which cut was made, and where the rest is.
    /// Every wording says "omitted" - that word is what a client, and the suite, reads to tell a
    /// shortened reference from a whole one.
    /// </summary>
    private static string Note(ButilApiTypeDetailsDto details, string cut, int maxLength) =>
        ($"{details.Remarks}\n\n[{cut} here: this type's full reference is longer than {maxLength:N0} characters. " +
         $"Ask for one member's caveats with SearchButil(query: \"{details.Name}.<member>\"), " +
         $"or read the page at {details.DocsUrl ?? "the documentation site"}.]").TrimStart();

    /// <summary>The wording of the last cut, which is the only one that returns fewer members than the type has.</summary>
    private static string MemberCut(int kept, int total) =>
        $"Only the first {kept:N0} of this type's {total:N0} members are listed, and their summaries and remarks are omitted";

    /// <summary>Roughly what the reference costs a client - its text, plus the JSON that carries it.</summary>
    private static int Length(ButilApiTypeDetailsDto details) =>
        DetailsJsonOverhead
      + details.Name.Length + details.FullName.Length + details.Kind.Length
      + (details.Inject?.Length ?? 0) + (details.DocsUrl?.Length ?? 0)
      + (details.Implements?.Sum(name => name.Length + 4) ?? 0)
      + (details.Summary?.Length ?? 0) + (details.Remarks?.Length ?? 0)
      + details.Members.Sum(Length);

    /// <summary>The same for one member, which is the part that repeats sixty times.</summary>
    private static int Length(ButilApiMemberDto member) =>
        MemberJsonOverhead
      + member.Name.Length + member.Kind.Length + (member.Type?.Length ?? 0) + (member.Signature?.Length ?? 0)
      + (member.Default?.Length ?? 0) + (member.Summary?.Length ?? 0) + (member.Remarks?.Length ?? 0);

    /// <summary>True when the class is registered by AddBitButilServices, i.e. injectable by its own name.</summary>
    public static bool IsService(Type type) => type.IsDefined(typeof(ButilServiceAttribute), inherit: false);

    /// <summary>The line to put in a component to get one, or null when the type is not a service.</summary>
    public static string? InjectLine(Type type)
    {
        if (IsService(type) is false) return null;

        var variable = char.ToLowerInvariant(type.Name[0]) + type.Name[1..];

        return $"@inject Bit.Butil.{type.Name} {variable}";
    }

    /// <summary>Resolves a type by its simple name, with or without a generic arity or argument list.</summary>
    public static Type? Find(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var name = typeName.Trim();

        var generic = name.IndexOf('<', StringComparison.Ordinal);
        if (generic > 0) name = name[..generic];

        return _publicTypes.Value.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(StripArity(t.Name), name, StringComparison.OrdinalIgnoreCase))
            ?? _publicTypes.Value.FirstOrDefault(t => string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase));
    }

    private static ButilApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var instance = TryCreateInstance(type);

        var members = new List<ButilApiMemberDto>();

        if (type.IsEnum)
        {
            members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                 .Select(field => new ButilApiMemberDto
                                 {
                                     Name = field.Name,
                                     Kind = "EnumValue",
                                     Type = FriendlyName(type.GetEnumUnderlyingType()),
                                     // Formatted straight off the raw constant: a [Flags] enum backed by
                                     // ulong has members that do not fit in an Int64.
                                     Default = Format(field.GetRawConstantValue()),
                                     Summary = ButilXmlDocs.GetSummary(DocumentationId(field)),
                                     Remarks = ButilXmlDocs.GetRemarks(DocumentationId(field))
                                 }));
        }
        else
        {
            members.AddRange(Properties(type, instance));
            members.AddRange(Fields(type));
            members.AddRange(Methods(type));
            members.AddRange(Events(type));
        }

        var page = DocsNav.AllLinks.FirstOrDefault(l => l.TypeNames().Contains(type.Name, StringComparer.OrdinalIgnoreCase));

        return new ButilApiTypeDetailsDto
        {
            Name = FriendlyName(type),
            FullName = type.FullName ?? type.Name,
            Kind = KindOf(type),
            Inject = InjectLine(type),
            Implements = [.. type.GetInterfaces()
                .Where(i => i.IsPublic && i.DeclaringType is null)
                .Select(FriendlyName)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)],
            Summary = ButilXmlDocs.GetSummary(DocumentationId(type)),
            Remarks = ButilXmlDocs.GetRemarks(DocumentationId(type)),
            DocsUrl = page is null ? null : $"/{page.Url}",
            Members = [.. members.OrderBy(m => KindOrder(m.Kind)).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)]
        };
    }

    /// <summary>
    /// The type and every base of it that Bit.Butil itself declares. What a wrapper inherits from
    /// its own base belongs in its reference; what it gets from System.Object does not.
    /// </summary>
    internal static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && current.Assembly == _assembly; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static IEnumerable<ButilApiMemberDto> Properties(Type type, object? instance)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (seen.Add(property.Name) is false) continue;

            yield return new ButilApiMemberDto
            {
                Name = property.Name,
                Kind = "Property",
                Type = FriendlyName(property.PropertyType),
                Default = DefaultValueOf(property, instance),
                Summary = ButilXmlDocs.GetSummary(DocumentationId(property)),
                Remarks = ButilXmlDocs.GetRemarks(DocumentationId(property))
            };
        }
    }

    private static IEnumerable<ButilApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new ButilApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                // The const-string catalogs (ButilEvents, ButilKeyCodes) are nothing but their
                // values - "KeyDown" is useless without "keydown" next to it.
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : null,
                Summary = ButilXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = ButilXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    private static IEnumerable<ButilApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;

            var signature = Signature(method);
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new ButilApiMemberDto
            {
                Name = method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Summary = ButilXmlDocs.GetSummary(DocumentationId(method)),
                Remarks = ButilXmlDocs.GetRemarks(DocumentationId(method))
            };
        }
    }

    private static IEnumerable<ButilApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (seen.Add(member.Name) is false) continue;

            yield return new ButilApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Summary = ButilXmlDocs.GetSummary(DocumentationId(member)),
                Remarks = ButilXmlDocs.GetRemarks(DocumentationId(member))
            };
        }
    }

    /// <summary>
    /// The value a property holds on a newly constructed instance. Most Butil classes take
    /// IJSRuntime, so there is no instance to read and the answer is simply null - an options
    /// record, which is what a caller fills in by hand, is the case this exists for.
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
            // Members that only make sense against a live browser (or throw by design) simply have
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
    /// is not the same thing as one that starts out null.
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
            // Butil's element and animation APIs are extension methods on ElementReference; the
            // "this" is how a reader tells them from a member of a service.
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
        if (type.IsInterface) return "Interface";
        if (typeof(Attribute).IsAssignableFrom(type)) return "Attribute";
        if (typeof(MulticastDelegate).IsAssignableFrom(type)) return "Delegate";
        // Before the static-class test: a service IS a class, and which it is is the thing a caller
        // needs to know first - a service is injected, everything else is constructed or returned.
        if (IsService(type)) return "Service";
        if (type.IsAbstract && type.IsSealed) return "Static class";
        if (type.IsValueType) return "Struct";
        if (type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null) return "Record";

        return "Class";
    }

    private static int KindOrder(string kind) => kind switch
    {
        "Method" => 0,
        "Property" => 1,
        "EnumValue" => 2,
        "Field" => 3,
        "Event" => 4,
        _ => 5
    };

    /// <summary>The C# spelling of a type: "int?", "byte[]", "ValueTask&lt;ButilSubscription&gt;".</summary>
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
        if (type == typeof(byte)) return "byte";
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
