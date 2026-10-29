using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using Bit.Borm.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components;

namespace Bit.Borm.Demo.Server.Services;

/// <summary>
/// Builds the API reference the MCP tools serve, by reflecting over the public surface of the
/// Bit.Borm assembly - and of Bit.Borm.FluentValidation, whose one extension method is part of the
/// same namespace - and pairing every type and member with its XML documentation.
/// <para>
/// Reflection rather than a hand-maintained table: the reference then cannot drift from the
/// shipped library - a new builder method shows up the moment it is written, with the default value
/// a property actually has (read off a freshly constructed instance) instead of one someone
/// remembered to copy into a document.
/// </para>
/// </summary>
public static class BormApiCatalog
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>How deep the arguments of a constructor are built themselves before a type is given up on.</summary>
    private const int MaxConstructionDepth = 2;

    /// <summary>
    /// The assemblies the reference covers: the library, and the FluentValidation integration that
    /// ships as a package of its own. Both put their types in the one <c>Bit.Borm</c> namespace, so a
    /// caller reading about <c>UseFluentValidation</c> has no way to tell they are separate - which is
    /// why every type also says which package it comes from.
    /// </summary>
    public static readonly Assembly[] Assemblies = [typeof(BormView).Assembly, typeof(BormFluentValidationIntegration).Assembly];

    private static readonly Lazy<Type[]> _publicTypes = new(() =>
        [.. Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(IsApi)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)]);

    /// <summary>
    /// Whether an exported type is part of the API. The Razor compiler turns the library's
    /// <c>_Imports.razor</c> into a public <c>_Imports</c> class nobody can use for anything, and
    /// listing it would put a build artefact among the types an agent is told to choose from.
    /// </summary>
    public static bool IsApi(Type type)
        => type.IsNested is false
           && type.Name.Contains('<', StringComparison.Ordinal) is false
           && type.Name != "_Imports";

    private static readonly ConcurrentDictionary<string, BormApiTypeDetailsDto> _typeDetails = new(StringComparer.Ordinal);

    private static readonly Lazy<BormApiTypeDto[]> _types = new(() =>
        [.. _publicTypes.Value.Select(t => new BormApiTypeDto
        {
            Name = FriendlyName(t),
            Kind = KindOf(t),
            Package = PackageOf(t),
            Summary = BormXmlDocs.GetSummary(DocumentationId(t))
        })]);

    /// <summary>Every public type of Bit.Borm and its FluentValidation integration, with its summary.</summary>
    public static BormApiTypeDto[] Types => _types.Value;

    /// <summary>
    /// The index of the public types, as GetBormApi answers when it is called without a name.
    /// <para>
    /// A summary is cut to its first sentence here: what an index is for is choosing what to read
    /// next, and the paragraph that follows the first sentence is the answer to the call the reader
    /// has not made yet.
    /// </para>
    /// </summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Borm public API").AppendLine();
        builder.AppendLine($"{Types.Length} public types, all in the `Bit.Borm` namespace. Call `GetBormApi(typeName: \"...\")` for one of them in full - ")
               .AppendLine("its parameters, properties, methods or enum values, with the default value each one really has. A type that ships in ")
               .AppendLine("a package of its own says so.")
               .AppendLine();

        foreach (var type in Types)
        {
            builder.Append($"- **{type.Name}** ({type.Kind}");
            if (type.Package != LibraryPackage) builder.Append($", package {type.Package}");
            builder.Append(')');
            if (FirstSentence(type.Summary) is string summary) builder.Append($" - {summary}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// The full reference of one type as Markdown, or null when no public type goes by that name.
    /// <para>
    /// Markdown rather than an object, and one renderer rather than two: the tool and the
    /// <c>borm://api/{typeName}</c> resource hand out the same text, so neither can drift from
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

        if (details.Package != LibraryPackage) builder.Append($" - package `{details.Package}` (`dotnet add package {details.Package}`)");
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

    private const string LibraryPackage = "Bit.Borm";

    /// <summary>The first sentence of a summary, for a listing that is read to choose by.</summary>
    private static string? FirstSentence(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return null;

        var text = summary.ReplaceLineEndings(" ").Trim();

        var stop = text.IndexOf(". ", StringComparison.Ordinal);

        // "e.g. " and "i.e. " end an abbreviation, not a sentence: cut there, a summary reads
        // "An asynchronous custom rule backed by a delegate (e.g." and stops making sense.
        while (stop > 0 && (EndsWith(text, stop, "e.g") || EndsWith(text, stop, "i.e")))
        {
            stop = text.IndexOf(". ", stop + 2, StringComparison.Ordinal);
        }

        return stop > 0 ? text[..(stop + 1)] : text;

        static bool EndsWith(string text, int end, string abbreviation)
            => end >= abbreviation.Length && string.CompareOrdinal(text, end - abbreviation.Length, abbreviation, 0, abbreviation.Length) == 0;
    }

    /// <summary>
    /// The full reference of one type - its Blazor parameters, properties, methods, events or enum
    /// values - or null when no public type goes by that name.
    /// </summary>
    public static BormApiTypeDetailsDto? GetTypeDetails(string typeName)
    {
        var type = Find(typeName);
        if (type is null) return null;

        // Every hit costs a full reflection walk plus an XML lookup per member, and the search index
        // asks for all of them at once; the answer only changes when the assembly does.
        return _typeDetails.GetOrAdd(type.FullName ?? type.Name, _ => BuildTypeDetails(type));
    }

    private static BormApiTypeDetailsDto BuildTypeDetails(Type type)
    {
        var (instance, supplied) = TryCreateInstance(type);

        // A second instance, compared against the first, is what tells a real default from a value
        // made up per instance - a counter or a cache minted in a constructor - and reporting one of
        // those as "the default" would hand a caller a value nobody ever gets.
        var (twin, _) = TryCreateInstance(type);

        try
        {
            var members = new List<BormApiMemberDto>();

            if (type.IsEnum)
            {
                members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                                     .Select(field => new BormApiMemberDto
                                     {
                                         Name = field.Name,
                                         Kind = "EnumValue",
                                         Type = FriendlyName(type.GetEnumUnderlyingType()),
                                         // Formatted straight off the raw constant, so a [Flags] value
                                         // reads as its number rather than as a list of names.
                                         Default = Format(field.GetRawConstantValue()),
                                         Summary = BormXmlDocs.GetSummary(DocumentationId(field)),
                                         Remarks = BormXmlDocs.GetRemarks(DocumentationId(field))
                                     }));
            }
            else
            {
                members.AddRange(Properties(type, instance, twin, supplied));
                members.AddRange(Fields(type));
                members.AddRange(Methods(type));
                members.AddRange(Events(type));
            }

            return new BormApiTypeDetailsDto
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
                Summary = BormXmlDocs.GetSummary(DocumentationId(type)),
                Remarks = BormXmlDocs.GetRemarks(DocumentationId(type)),
                Members = [.. members.OrderBy(m => KindOrder(m.Kind)).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)]
            };
        }
        finally
        {
            // BormState is disposable - it cancels the work it has in flight - and one built only to be
            // read is still one somebody has to dispose.
            (instance as IDisposable)?.Dispose();
            (twin as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Resolves a type by its simple name, with or without a generic arity or argument list.
    /// "BormBuilder" is the untyped builder and "BormBuilder&lt;TModel&gt;" the typed one: the
    /// argument list a caller wrote is what tells the two apart, so it is honoured before it is dropped.
    /// </summary>
    private static Type? Find(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var name = typeName.Trim();
        var arity = 0;

        var generic = name.IndexOf('<', StringComparison.Ordinal);
        if (generic > 0)
        {
            arity = name[generic..].Count(c => c == ',') + 1;
            name = name[..generic].Trim();
        }

        var types = _publicTypes.Value;

        if (arity > 0 && types.FirstOrDefault(t => string.Equals(t.Name, $"{name}`{arity}", StringComparison.OrdinalIgnoreCase)) is { } exact) return exact;

        return types.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? types.FirstOrDefault(t => string.Equals(StripArity(t.Name), name, StringComparison.OrdinalIgnoreCase))
            ?? types.FirstOrDefault(t => string.Equals(t.FullName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The type and every base of it that Bit.Borm itself declares. A component's inherited
    /// parameters belong in its reference; the members it gets from ComponentBase do not.
    /// </summary>
    private static IEnumerable<Type> Hierarchy(Type type)
    {
        for (var current = type; current is not null && Assemblies.Contains(current.Assembly); current = current.BaseType)
        {
            yield return current;
        }
    }

    private static IEnumerable<BormApiMemberDto> Properties(Type type, object? instance, object? twin, HashSet<string> supplied)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in Hierarchy(type).SelectMany(t => t.GetProperties(Declared)))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            if (seen.Add(property.Name) is false) continue;

            var isParameter = property.IsDefined(typeof(ParameterAttribute), inherit: true);

            // A value the constructor was handed is whatever the caller passes, not a default - a field
            // definition's Name is the name it was created with. So is anything worked out from it: on an
            // instance built from arguments, a property nobody can set (IsChoice, IsWizard) says only
            // what those placeholder arguments happened to make it.
            var derived = supplied.Contains(property.Name) || (supplied.Count > 0 && property.SetMethod?.IsPublic is not true);

            yield return new BormApiMemberDto
            {
                Name = property.Name,
                Kind = isParameter ? "Parameter" : "Property",
                Type = FriendlyName(property.PropertyType),
                Default = derived ? null : DefaultValueOf(property, instance, twin),
                Required = property.IsDefined(typeof(EditorRequiredAttribute), inherit: true),
                Summary = Documentation(property, BormXmlDocs.GetSummary),
                Remarks = Documentation(property, BormXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BormApiMemberDto> Fields(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in Hierarchy(type).SelectMany(t => t.GetFields(Declared)))
        {
            if (field.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (seen.Add(field.Name) is false) continue;

            yield return new BormApiMemberDto
            {
                Name = field.Name,
                Kind = "Field",
                Type = FriendlyName(field.FieldType),
                // BormMessageKeys is a table of constants: the key a provider is asked for IS the value.
                Default = field.IsLiteral ? Format(field.GetRawConstantValue()) : null,
                Summary = BormXmlDocs.GetSummary(DocumentationId(field)),
                Remarks = BormXmlDocs.GetRemarks(DocumentationId(field))
            };
        }
    }

    private static IEnumerable<BormApiMemberDto> Methods(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in Hierarchy(type).SelectMany(t => t.GetMethods(Declared)))
        {
            if (method.IsSpecialName) continue;                                     // property/event accessors and operators
            if (method.Name.Contains('<', StringComparison.Ordinal)) continue;
            if (method.DeclaringType == typeof(object)) continue;
            // What the compiler writes for a record - Equals, GetHashCode, ToString, Deconstruct - is the
            // same on every record and documented nowhere; listing it on each of the library's records
            // would bury the members someone wrote on purpose.
            if (method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)) continue;
            // An override of what every object has, which the type does not document, says nothing about the
            // type - and split into words it answers questions it has nothing to do with: to a search,
            // GetHashCode is "code", and the top hit for anything done from code.
            var summary = Documentation(method, BormXmlDocs.GetSummary);
            if (summary is null && method.Name is "Equals" or "GetHashCode" or "ToString") continue;
            // A delegate is its Invoke signature; BeginInvoke and EndInvoke are the runtime's plumbing
            // for the asynchronous pattern .NET no longer supports, on every delegate alike.
            if (type.IsSubclassOf(typeof(MulticastDelegate)) && method.Name is "BeginInvoke" or "EndInvoke") continue;

            var signature = Signature(method);
            if (seen.Add($"{method.Name}{signature}") is false) continue;

            yield return new BormApiMemberDto
            {
                Name = method.Name,
                Kind = "Method",
                Type = FriendlyName(method.ReturnType),
                Signature = signature,
                Summary = summary,
                Remarks = Documentation(method, BormXmlDocs.GetRemarks)
            };
        }
    }

    private static IEnumerable<BormApiMemberDto> Events(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in Hierarchy(type).SelectMany(t => t.GetEvents(Declared)))
        {
            if (seen.Add(member.Name) is false) continue;

            yield return new BormApiMemberDto
            {
                Name = member.Name,
                Kind = "Event",
                Type = FriendlyName(member.EventHandlerType ?? typeof(void)),
                Summary = BormXmlDocs.GetSummary(DocumentationId(member)),
                Remarks = BormXmlDocs.GetRemarks(DocumentationId(member))
            };
        }
    }

    /// <summary>
    /// The documentation of a property, or - when it says no more than <c>&lt;inheritdoc /&gt;</c>, as an
    /// override or an interface implementation often does (every rule's <c>Key</c> is documented once,
    /// on IBormValidationRule) - that of the member it overrides or implements, which is what the
    /// compiler would show for it.
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
            // Members that only make sense on a mounted component (or throw by design) simply have
            // no observable default; that is not a reason to fail the whole type's reference.
            return null;
        }
    }

    /// <summary>
    /// A new instance of a type to read its defaults off, and the names of the members its constructor
    /// was handed. Much of Borm's surface is constructed with arguments - a field definition takes its
    /// name and type, a state its definition and its data - and those carry most of the defaults worth
    /// knowing (<c>ShowLabel</c> is on, a state validates on change), so a type without a parameterless
    /// constructor is built from the plainest arguments that satisfy it rather than skipped.
    /// </summary>
    private static (object? Instance, HashSet<string> Supplied) TryCreateInstance(Type type)
    {
        var supplied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return (TryConstruct(type, 0, supplied), supplied);
    }

    private static object? TryConstruct(Type type, int depth, HashSet<string>? supplied)
    {
        if (type.IsAbstract || type.IsInterface || type.IsEnum || type.ContainsGenericParameters) return null;
        if (type.IsValueType) return Activator.CreateInstance(type);

        var constructor = type.GetConstructor(Type.EmptyTypes)
            ?? (depth < MaxConstructionDepth
                ? type.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault()
                : null);

        if (constructor is null) return null;

        try
        {
            var parameters = constructor.GetParameters();
            var arguments = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                arguments[i] = parameter.HasDefaultValue ? parameter.DefaultValue : ArgumentFor(parameter, depth);

                // A parameter left at its own default is the default; one the caller has to pass is not.
                if (parameter.HasDefaultValue is false && parameter.Name is not null) supplied?.Add(parameter.Name);
            }

            return constructor.Invoke(arguments);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// The plainest value a constructor parameter accepts: nothing for a nullable one, the empty
    /// string, a value type's default, or - for a class or an interface - an instance of the library's
    /// own simplest implementation of it (a state's IBormDataAccessor is a dictionary accessor).
    /// </summary>
    private static object? ArgumentFor(ParameterInfo parameter, int depth)
    {
        var type = parameter.ParameterType;

        if (type == typeof(string)) return string.Empty;
        if (type.IsValueType) return Activator.CreateInstance(type);
        if (new NullabilityInfoContext().Create(parameter).WriteState == NullabilityState.Nullable) return null;
        if (type == typeof(object)) return new object();

        var concrete = type.IsInterface || type.IsAbstract
            ? _publicTypes.Value.FirstOrDefault(candidate => candidate.IsClass && candidate.IsAbstract is false && type.IsAssignableFrom(candidate))
            : type;

        return concrete is null ? null : TryConstruct(concrete, depth + 1, supplied: null);
    }

    private static string? Format(object? value) => value switch
    {
        null => null,
        // An event parameter nobody has bound is not a value worth printing: "new EventCallback<...>()"
        // on every one of BormView's callbacks says only that it starts out unset, as they all do.
        _ when IsUnboundCallback(value) => null,
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        Enum enumValue => $"{enumValue.GetType().Name}.{enumValue}",
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        // A delegate default is behaviour, not a value: what it does is in the member's documentation.
        Delegate => "(built in)",
        // A collection default is its contents: an empty list of rules says the field starts with none,
        // and "new List<IBormValidationRule>()" would say only that it is a list.
        System.Collections.IDictionary dictionary => dictionary.Count == 0
            ? "{}"
            : $"{{ {string.Join(", ", dictionary.Keys.Cast<object>().Take(MaxListedItems).Select(key => $"{Format(key)} = {Format(dictionary[key]) ?? "null"}"))} }}",
        System.Collections.IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Take(MaxListedItems).Select(item => Format(item) ?? "null"))}]",
        _ => Describe(value)
    };

    private static bool IsUnboundCallback(object value)
    {
        var type = value.GetType();

        var isCallback = type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));

        return isCallback && type.GetProperty(nameof(EventCallback.HasDelegate))?.GetValue(value) is false;
    }

    /// <summary>The most items of a collection default that are spelled out.</summary>
    private const int MaxListedItems = 12;

    /// <summary>
    /// A reference-typed default is worth reporting: a property that starts out holding an object is
    /// not the same thing as one that starts out null.
    /// </summary>
    private static string Describe(object value)
    {
        var type = value.GetType();

        // A default that is one of the type's own named values reads best by that name:
        // BormDefaultMessageProvider.Instance rather than a dump of its fields.
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
            else if (parameter.IsDefined(typeof(ParamArrayAttribute), false)) builder.Append("params ");

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

    private static string PackageOf(Type type) => type.Assembly.GetName().Name ?? LibraryPackage;

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

    /// <summary>The C# spelling of a type: "int?", "string[]", "Func&lt;IBormDataReader, bool&gt;".</summary>
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
