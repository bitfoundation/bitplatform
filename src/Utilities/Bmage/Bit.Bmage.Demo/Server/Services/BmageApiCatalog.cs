using System.Text;
using System.Reflection;
using System.Globalization;
using System.Collections.Concurrent;
using Bit.Bmage.Editor.Components;
using Bit.Bmage.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the three
/// Bit.Bmage assemblies - the engine (Bit.Bmage.Core), the browser integration (Bit.Bmage) and the
/// editor (Bit.Bmage.Editor) - and pairing every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the shipped
/// library - a new pipeline method shows up the moment it is written, with its real parameter list
/// and defaults instead of ones someone remembered to copy into a document. That matters here
/// because Bmage's types are spread over a dozen namespaces across three packages: an agent that
/// guesses <c>using Bit.Bmage.Codecs</c> for <c>ImagePipeline</c>, or installs <c>Bit.Bmage.Core</c>
/// for <c>ImageLoader</c>, writes code that does not compile - so every type carries its namespace
/// and its package.
/// </para>
/// </summary>
public static class BmageApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>The three packages, engine first: the order their types are listed in.</summary>
    public static readonly Assembly[] Assemblies =
    [
        typeof(ImageBuffer).Assembly,
        typeof(BitBmage).Assembly,
        typeof(BmageEditor).Assembly,
    ];

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsNested is false && type.Name.Contains('<', StringComparison.Ordinal) is false)
            // Razor emits a public _Imports type per component library: an artefact of the build, not API.
            .Where(type => type.Name != "_Imports")
            .OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)]);

    private static readonly ConcurrentDictionary<string, BmageApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    private static readonly Lazy<BmageApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(type => new BmageApiTypeDto
        {
            Name = FriendlyName(type),
            Namespace = type.Namespace ?? string.Empty,
            Package = PackageOf(type),
            Kind = KindOf(type),
            Summary = BmageXmlDocs.GetSummary(DocumentationId(type))
        })]);

    private static readonly Lazy<BmageApiTypeDto[]> _typeSummaries = new(() =>
        [.. _types.Value.Select(type => type with { Summary = FirstSentence(type.Summary) })]);

    /// <summary>Every public type of Bit.Bmage, with its full summary.</summary>
    public static BmageApiTypeDto[] Types => _types.Value;

    /// <summary>
    /// Every public type with its summary cut to the first sentence: a list of a couple of hundred
    /// types is read to pick one, and the full prose behind each is what <see cref="GetTypeDetails"/>
    /// is for.
    /// </summary>
    public static BmageApiTypeDto[] TypeSummaries => _typeSummaries.Value;

    /// <summary>The endings that put a '.' before a space without ending a sentence.</summary>
    private static readonly string[] _abbreviations = ["e.g.", "i.e.", "etc.", "cf.", "vs."];

    /// <summary>
    /// The first sentence of a summary, for a listing. A summary that opens with a code sample has
    /// no sentence to take - the text before the sample is kept whole rather than cut at the first
    /// '.' inside the code.
    /// </summary>
    internal static string? FirstSentence(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return summary;

        var text = summary.Trim();

        var paragraph = text.IndexOf("\n\n", StringComparison.Ordinal);
        if (paragraph > 0) text = text[..paragraph].TrimEnd();

        for (var index = text.IndexOf(". ", StringComparison.Ordinal); index > 0;
             index = text.IndexOf(". ", index + 1, StringComparison.Ordinal))
        {
            var candidate = text[..(index + 1)];

            if (_abbreviations.Any(abbreviation => candidate.EndsWith(abbreviation, StringComparison.OrdinalIgnoreCase))) continue;

            // A boundary that leaves a single word behind is an initial or a fragment rather than a
            // sentence, and describes no type.
            if (candidate.Contains(' ') is false) continue;

            return candidate;
        }

        return text.EndsWith('.') || text.Length <= 240 ? text : $"{text[..237]}...";
    }

    /// <summary>
    /// The full reference of one type, or null when no public type goes by that name. Accepts the
    /// simple name ("ImagePipeline"), the full name ("Bit.Bmage.Pipeline.ImagePipeline") or a
    /// generic spelling ("Foo&lt;T&gt;").
    /// </summary>
    public static BmageApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assemblies do.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    /// <summary>
    /// One member of a type, written "Type.Member" - "ImagePipeline.Cover", "ImageExportOptions.Quality".
    /// Every overload of a method comes back. Null when the type or the member does not exist.
    /// </summary>
    public static BmageApiTypeDetailsDto? GetMemberDetails(string qualifiedName)
    {
        if (string.IsNullOrWhiteSpace(qualifiedName)) return null;

        var dot = qualifiedName.LastIndexOf('.');
        if (dot <= 0 || dot == qualifiedName.Length - 1) return null;

        var details = GetTypeDetails(qualifiedName[..dot]);
        if (details is null) return null;

        var member = qualifiedName[(dot + 1)..].Trim();
        var parenthesis = member.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis > 0) member = member[..parenthesis];

        var matches = details.Members.Where(entry => entry.Name.Equals(member, StringComparison.OrdinalIgnoreCase)).ToArray();

        return matches.Length == 0 ? null : details with { Members = matches };
    }

    private static BmageApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var instance = TryCreateInstance(type);

        var members = new List<BmageApiMemberDto>();

        if (type.IsEnum)
        {
            members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                 .Select(field => new BmageApiMemberDto
                                 {
                                     Name = field.Name,
                                     Kind = "EnumValue",
                                     Type = FriendlyName(type.GetEnumUnderlyingType()),
                                     Default = Format(field.GetRawConstantValue()),
                                     Summary = BmageXmlDocs.GetSummary(DocumentationId(field)),
                                     Remarks = BmageXmlDocs.GetRemarks(DocumentationId(field))
                                 }));
        }
        else
        {
            members.AddRange(Properties(type, instance));
            members.AddRange(Fields(type));
            members.AddRange(Constructors(type));
            members.AddRange(Methods(type));
            members.AddRange(Events(type));
        }

        // Nothing constructed here is kept, and some of it holds pooled pixel memory.
        (instance as IDisposable)?.Dispose();

        return new BmageApiTypeDetailsDto
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
                .Where(contract => contract.IsPublic && contract.DeclaringType is null)
                .Where(contract => contract.Name.StartsWith("IEquatable", StringComparison.Ordinal) is false)
                .Select(FriendlyName)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)],
            Summary = BmageXmlDocs.GetSummary(DocumentationId(type)),
            Remarks = BmageXmlDocs.GetRemarks(DocumentationId(type)),
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
    /// The type and every base of it that Bit.Bmage itself declares. An operation's inherited members
    /// belong in its reference; the members a component gets from ComponentBase do not.
    /// </summary>
    private static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && Assemblies.Contains(current.Assembly); current = current.BaseType)
        {
            yield return current;
        }
    }

    private static IEnumerable<BmageApiMemberDto> Properties(Type type, object? instance)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (property.Name == "EqualityContract") continue;
            if (seen.Add(property.Name) is false) continue;

            var isParameter = property.IsDefined(typeof(ParameterAttribute), inherit: true);

            yield return new BmageApiMemberDto
            {
                Name = property.Name,
                Kind = isParameter ? "Parameter" : "Property",
                Type = FriendlyName(property.PropertyType),
                Default = DefaultValueOf(property, instance),
                Required = property.IsDefined(typeof(EditorRequiredAttribute), inherit: true)
                        || property.IsDefined(typeof(System.Runtime.CompilerServices.RequiredMemberAttribute), inherit: true),
                Summary = BmageXmlDocs.GetSummary(DocumentationId(property)),
                Remarks = BmageXmlDocs.GetRemarks(DocumentationId(property))
            };
        }
    }

    private static IEnumerable<BmageApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new BmageApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : null,
                Summary = BmageXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = BmageXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    /// <summary>
    /// The public constructors. Much of Bmage is built with <c>new</c> - every operation, annotation
    /// and options record - so the argument list is part of how a type is used, not a detail of it.
    /// </summary>
    private static IEnumerable<BmageApiMemberDto> Constructors(Type type)
    {
        if (type.IsAbstract) yield break;

        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = constructor.GetParameters();

            // A record's copy constructor is how 'with' works, not something anyone calls.
            if (parameters.Length == 1 && parameters[0].ParameterType == type) continue;

            yield return new BmageApiMemberDto
            {
                Name = StripArity(type.Name),
                Kind = "Constructor",
                Signature = Signature(parameters, isExtension: false),
                Summary = BmageXmlDocs.GetSummary(DocumentationId(constructor))
            };
        }
    }

    private static IEnumerable<BmageApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;
            if (IsCompilerGenerated(method)) continue;

            var signature = Signature(method.GetParameters(), method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new BmageApiMemberDto
            {
                Name = method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Summary = BmageXmlDocs.GetSummary(DocumentationId(method)),
                Remarks = BmageXmlDocs.GetRemarks(DocumentationId(method))
            };
        }
    }

    /// <summary>
    /// The members a record, or a component's render pipeline, gets from the compiler. They are
    /// public, and listing them would bury the twenty members a reader came for under a dozen they
    /// would never call.
    /// </summary>
    private static bool IsCompilerGenerated(MethodInfo method)
    {
        if (method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)) return true;

        return method.Name is "Deconstruct" or "PrintMembers" or "<Clone>$" or "BuildRenderTree"
                           or "OnInitialized" or "OnInitializedAsync" or "OnParametersSet" or "OnParametersSetAsync"
                           or "OnAfterRender" or "OnAfterRenderAsync" or "ShouldRender" or "SetParametersAsync";
    }

    private static IEnumerable<BmageApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (seen.Add(member.Name) is false) continue;

            yield return new BmageApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Summary = BmageXmlDocs.GetSummary(DocumentationId(member)),
                Remarks = BmageXmlDocs.GetRemarks(DocumentationId(member))
            };
        }
    }

    /// <summary>
    /// The value a property holds on a newly constructed instance - the default a caller gets when
    /// it is left unset. For an options record that is the behaviour: Quality 0.85, Metadata Remove.
    /// </summary>
    private static string? DefaultValueOf(PropertyInfo property, object? instance)
    {
        if (property.GetMethod is null || property.GetMethod.IsPublic is false) return null;
        if (property.GetMethod.IsStatic is false && instance is null) return null;

        // A static property of the type's own type is a preset (ImageExportOptions.Png, Rgba32.White,
        // AspectRatio.Square), and printing it as "new ImageExportOptions()" would say nothing.
        if (property.GetMethod.IsStatic && property.PropertyType == property.DeclaringType) return null;

        try
        {
            return Format(property.GetValue(property.GetMethod.IsStatic ? null : instance));
        }
        catch (Exception)
        {
            // Members that only make sense on a live object (or throw by design) simply have no
            // observable default; that is not a reason to fail the whole type's reference.
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

    private static string? Format(object? value) => value switch
    {
        null => null,
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        Enum enumValue => $"{enumValue.GetType().Name}.{enumValue}",
        Rgba32 color => color == Rgba32.White ? "Rgba32.White" : color == Rgba32.Black ? "Rgba32.Black" : color == Rgba32.Transparent ? "Rgba32.Transparent" : $"Rgba32.ParseHex(\"{color.ToHex()}\")",
        float single => single.ToString("0.###", CultureInfo.InvariantCulture),
        double number => number.ToString("0.###", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        System.Collections.ICollection { Count: 0 } => "empty",
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

        if (string.IsNullOrEmpty(text) || text == type.FullName || text == type.Name) return $"new {FriendlyName(type)}()";

        // A record prints every member it has; as a default value that is a paragraph, not a value.
        return text.Length <= 120 ? text : $"new {FriendlyName(type)} {{ ... }}";
    }

    private static string Signature(ParameterInfo[] parameters, bool isExtension)
    {
        var builder = new StringBuilder("(");

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (i > 0) builder.Append(", ");
            if (i == 0 && isExtension) builder.Append("this ");
            if (parameter.IsOut) builder.Append("out ");
            else if (parameter.ParameterType.IsByRef) builder.Append("ref ");

            builder.Append(FriendlyName(parameter.ParameterType)).Append(' ').Append(parameter.Name);

            if (parameter.HasDefaultValue) builder.Append(" = ").Append(FormatDefault(parameter));
        }

        return builder.Append(')').ToString();
    }

    /// <summary>
    /// A parameter's default as C# would print it. A defaulted struct (Rgba32 background = default)
    /// reads as "default", which is what the source says, rather than as transparent black.
    /// </summary>
    private static string FormatDefault(ParameterInfo parameter)
    {
        var value = parameter.DefaultValue;

        if (value is null) return parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) is null ? "default" : "null";

        var enumType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        if (enumType.IsEnum && value is not Enum) value = Enum.ToObject(enumType, value);

        return Format(value) ?? "null";
    }

    private static string KindOf(Type type)
    {
        if (type.IsEnum) return "Enum";
        // Before the IComponent test: an interface that extends IComponent is still an interface.
        if (type.IsInterface) return "Interface";
        if (typeof(IComponent).IsAssignableFrom(type)) return "Component";
        if (typeof(Attribute).IsAssignableFrom(type)) return "Attribute";
        if (typeof(Exception).IsAssignableFrom(type)) return "Exception";
        if (typeof(MulticastDelegate).IsAssignableFrom(type)) return "Delegate";
        if (type.IsAbstract && type.IsSealed) return "Static class";
        if (type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null) return "Record";
        if (type.IsValueType) return "Struct";

        return "Class";
    }

    /// <summary>The NuGet package a type ships in - the one an agent has to add to use it.</summary>
    private static string PackageOf(Type type) => type.Assembly.GetName().Name ?? "Bit.Bmage";

    private static int KindOrder(string kind) => kind switch
    {
        "Parameter" => 0,
        "Property" => 1,
        "Field" => 2,
        "EnumValue" => 3,
        "Constructor" => 4,
        "Method" => 5,
        "Event" => 6,
        _ => 7
    };

    /// <summary>The C# spelling of a type: "double?", "string[]", "IReadOnlyList&lt;ImageFormat&gt;".</summary>
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
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(uint)) return "uint";
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

    private static string DocumentationId(ConstructorInfo constructor) => MethodId(constructor.DeclaringType!, "#ctor", constructor.GetParameters(), 0);

    private static string DocumentationId(MethodInfo method)
        => MethodId(method.DeclaringType!, method.Name, method.GetParameters(), method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0);

    private static string MethodId(Type declaringType, string name, ParameterInfo[] parameters, int genericArity)
    {
        var id = new StringBuilder("M:").Append(declaringType.FullName).Append('.').Append(name);

        if (genericArity > 0) id.Append("``").Append(genericArity);

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

        if (type.IsGenericType is false) return (type.FullName ?? type.Name).Replace('+', '.');

        var definition = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var arguments = string.Join(',', type.GetGenericArguments().Select(DocumentationTypeName));

        return $"{StripArity(definition).Replace('+', '.')}{{{arguments}}}";
    }
}
