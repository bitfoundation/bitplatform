using System.Reflection;
using System.Collections.Frozen;
using Microsoft.AspNetCore.Components;
using Bit.BlazorUI.Demo.Client.Core.Models;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>One row of a parameter or member table.</summary>
public sealed record ComponentMember(string Name, string Type, string? Default, string? Description);

/// <summary>A type that exists for one component only - its class-styles bag, its item class, its own enum.</summary>
public sealed record ComponentSubType(string Name, string? Description, IReadOnlyList<ComponentMember> Members, bool IsEnum);

/// <summary>
/// A base class a component takes the rest of its parameters from, named as that component closes
/// it - <c>BitInputBase&lt;string&gt;</c> on BitTextField, <c>BitInputBase&lt;TValue&gt;</c> on the
/// generic ones - with the parameters it brings and the name they are documented under.
/// </summary>
/// <param name="Name">The base as this component closes it, which is how its <c>TValue</c> reads on this component.</param>
/// <param name="Lookup">The name <c>GetBitBlazorUIComponent</c> documents it under.</param>
/// <param name="Parameters">The parameter names it brings, so a reader knows what is there without a second call.</param>
public sealed record ComponentBaseRef(string Name, string Lookup, IReadOnlyList<string> Parameters);

/// <summary>
/// Everything this server knows about one component, gathered from the three places that hold it:
/// the nav (what it is called and where it lives), the compiled type (which package it ships in and
/// what it is generic over) and its demo page (the parameter tables and the worked examples).
/// </summary>
public sealed record BlazorUIComponent
{
    public required string Name { get; init; }
    public required string ShortName { get; init; }
    public required string Category { get; init; }
    public required string Url { get; init; }
    public required BlazorUIPackage Package { get; init; }
    public string? Aliases { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Notes { get; init; }

    /// <summary>The generic parameter list, e.g. <c>&lt;TItem, TValue&gt;</c>, or null for a non-generic component.</summary>
    public string? TypeParameters { get; init; }

    public Type? ComponentType { get; init; }

    /// <summary>
    /// The component's own source on GitHub - the path its demo page links "view source" to.
    /// <para>
    /// One line, and the only route from a documented parameter to the code that implements it.
    /// An agent that has read the table and still cannot explain a behaviour has nowhere else to go.
    /// </para>
    /// </summary>
    public string? SourceUrl { get; init; }

    /// <summary>
    /// Whether this is a component or one of the handful of services the nav lists beside them
    /// (BitModalService and its siblings). A service has methods and events rather than parameters,
    /// so calling its table "Parameters" would be wrong in the one place a reader would not check.
    /// </summary>
    public bool IsComponent => ComponentType is null || typeof(IComponent).IsAssignableFrom(ComponentType);

    /// <summary>
    /// The base classes this component takes its remaining parameters from, most specific first.
    /// <para>
    /// Resolved from the compiled type rather than from a list: most components derive from
    /// <c>BitComponentBase</c>, the inputs add <c>BitInputBase&lt;TValue&gt;</c> - the value, the
    /// binding and the EditForm integration - and the text-entry ones add
    /// <c>BitTextInputBase&lt;TValue&gt;</c> on top of it, while a few of the larger Extras
    /// components are plain <c>ComponentBase</c> and carry none of them. Promising a reader a
    /// <c>Visibility</c> parameter a component has never had is worse than saying nothing.
    /// </para>
    /// </summary>
    public IReadOnlyList<ComponentBaseRef> Inherited { get; init; } = [];

    public IReadOnlyList<ComponentMember> Parameters { get; init; } = [];
    public IReadOnlyList<ComponentMember> PublicMembers { get; init; } = [];

    /// <summary>The classes and enums this component owns, in full - nothing else documents them.</summary>
    public IReadOnlyList<ComponentSubType> OwnTypes { get; init; } = [];

    /// <summary>
    /// The library-wide types this component's parameters take - <c>BitColor</c>, <c>BitVariant</c>,
    /// <c>BitIconInfo</c>. Named with their members but without their prose: the same handful
    /// appears on nearly every component, and repeating their descriptions 110 times is the
    /// redundancy this server exists to avoid. Each is documented in full by
    /// <c>GetBitBlazorUIType</c>.
    /// </summary>
    public IReadOnlyList<ComponentSubType> SharedTypes { get; init; } = [];

    public IReadOnlyList<DemoExampleSource> Examples { get; init; } = [];
}

/// <summary>
/// The catalog of documented components, derived rather than written down: the nav is the authority
/// on which components exist, the assemblies on what they are, and the demo pages on how they are
/// used. Adding a component to the nav is all it takes for it to appear here.
/// </summary>
public static class BlazorUIComponentCatalog
{
    /// <summary>
    /// Where a component's source lives. The demo page states the path relative to its package's
    /// Components folder; which package that is comes from the assembly the type was loaded from,
    /// rather than from which of the page's three GitHub attributes happened to be used.
    /// </summary>
    private const string SourceRoot = "https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI";

    /// <summary>
    /// The base types themselves, named apart from <see cref="Bases"/> because they are read while
    /// it is still being built: what a component's own parameters are is what its type declares
    /// less what one of these declares, and each of these is built the same way against the others.
    /// </summary>
    private static readonly Type[] _baseTypes = [typeof(BitComponentBase), typeof(BitInputBase<>), typeof(BitTextInputBase<>)];

    private static readonly Lazy<BlazorUIComponent[]> _components = new(Build, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<FrozenDictionary<string, BlazorUIComponent>> _byName = new(BuildIndex, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Every documented component, in nav order.</summary>
    public static BlazorUIComponent[] Components => _components.Value;

    /// <summary>
    /// The base classes whose parameters are documented once here rather than repeated on each of
    /// the components that inherit them - <c>BitComponentBase</c> on nearly all of them,
    /// <c>BitInputBase</c> on the inputs, <c>BitTextInputBase</c> on the text-entry ones. Ordered
    /// from the most general to the most specific, which is the order they are answered in reverse.
    /// </summary>
    public static BlazorUIComponent[] Bases { get; } = BuildBases();

    /// <summary>The parameters, members and enums every component inherits from <c>BitComponentBase</c>.</summary>
    public static BlazorUIComponent Base => Bases[0];

    /// <summary>Builds the catalog ahead of the first request - see <see cref="BlazorUISearchIndex.Warm"/>.</summary>
    public static void Warm() => _ = _byName.Value;

    /// <summary>
    /// The component a name identifies. Accepts what an agent actually has to hand: the type name,
    /// the name without the Bit prefix, the demo page's route, or one of the aliases the nav lists.
    /// </summary>
    public static BlazorUIComponent? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var needle = name.Trim().TrimStart('/').Replace("components/", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (_byName.Value.TryGetValue(needle, out var component)) return component;

        return _byName.Value.TryGetValue($"Bit{needle}", out component) ? component : null;
    }

    /// <summary>
    /// The names closest to one that resolved to nothing. The aliases are searched alongside the
    /// names and answered with the component they belong to, because "ComboBox" is a name someone
    /// arrives with and BitDropdown is what they were reaching for.
    /// </summary>
    public static string[] Similar(string name)
    {
        var byAlias = Components.SelectMany(c => (c.Aliases ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(alias => (Alias: alias, Component: c.Name)))
            .ToLookup(a => a.Alias, a => a.Component, StringComparer.OrdinalIgnoreCase);

        return [.. BlazorUISuggest.Closest(name, Components.Concat(Bases).Select(c => c.Name).Concat(byAlias.Select(g => g.Key)))
            .SelectMany(hit => byAlias.Contains(hit) ? byAlias[hit] : [hit])
            .Distinct(StringComparer.Ordinal)];
    }

    private static FrozenDictionary<string, BlazorUIComponent> BuildIndex()
    {
        var index = new Dictionary<string, BlazorUIComponent>(StringComparer.OrdinalIgnoreCase);

        foreach (var component in Components)
        {
            index.TryAdd(component.Name, component);
            index.TryAdd(component.ShortName, component);
            index.TryAdd(component.Url.TrimStart('/').Replace("components/", string.Empty, StringComparison.Ordinal), component);
        }

        // Aliases last and only where nothing claimed the key: "Accordion" is a component in its own
        // right AND an alias of AccordionList, and the component has to win.
        foreach (var component in Components)
        {
            foreach (var alias in (component.Aliases ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                index.TryAdd(alias, component);
                index.TryAdd($"Bit{alias}", component);
            }
        }

        foreach (var @base in Bases)
        {
            index.TryAdd(@base.Name, @base);
            index.TryAdd(@base.ShortName, @base);
        }

        return index.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static BlazorUIComponent[] Build()
    {
        var demoAssembly = typeof(ComponentCatalog).Assembly;

        return [.. ComponentCatalog.Items.Select(item =>
        {
            var name = $"Bit{item.Name}";
            var componentType = FindType(name);
            var demoType = demoAssembly.GetTypes().FirstOrDefault(t => t.Name == $"{name}Demo");
            var demo = demoType is null ? null : BlazorUIDemoSource.Get(demoType);
            var tables = demoType is null ? null : DemoTables.Read(demoType);

            var inherited = InheritedBases(componentType);
            var parameters = MergeParameters(tables?.Parameters, componentType);
            var (own, shared) = SplitSubTypes(name, tables);

            return new BlazorUIComponent
            {
                Name = name,
                ShortName = item.Name,
                Category = item.Category,
                Url = item.Url,
                Aliases = item.Aliases,
                Summary = item.Summary,
                Package = componentType is null ? BlazorUIAssemblies.Core : BlazorUIAssemblies.Of(componentType),
                Description = demo?.Description,
                Notes = demo?.Notes,
                TypeParameters = TypeParametersOf(componentType),
                ComponentType = componentType,
                SourceUrl = demo?.SourceUrl is null || componentType is null
                    ? null
                    : $"{SourceRoot}/{BlazorUIAssemblies.Of(componentType).PackageId}/Components/{demo.SourceUrl}",
                Inherited = inherited,
                Parameters = parameters,
                PublicMembers = MergeMembers(tables?.PublicMembers, componentType, parameters),
                OwnTypes = own,
                SharedTypes = shared,
                Examples = demo?.Examples ?? []
            };
        })];
    }

    /// <summary>
    /// The shared half of every component's answer, as components of their own.
    /// <para>
    /// Ten parameters that hold for every component, eleven more that hold for every input and five
    /// more for every text-entry one are worth three lookups and not three hundred repetitions, so
    /// this is what the per-component answers point at instead of restating them. Their tables are
    /// the ones the demo pages append to each component's own, read off <c>DemoPage</c> where they
    /// are written once for the site and for here alike.
    /// </para>
    /// </summary>
    private static BlazorUIComponent[] BuildBases() =>
    [
        BuildBase(typeof(BitComponentBase), "_componentBase",
                  "The base class every bit BlazorUI component derives from. Its parameters are available on all of them."),

        BuildBase(typeof(BitInputBase<>), "_inputBase",
                  "The base class of every input component: the value, the two-way binding through `@bind-Value`, and everything an EditForm needs to validate it."),

        BuildBase(typeof(BitTextInputBase<>), "_textInputBase",
                  "The base class of the inputs the user types into, on top of `BitInputBase`: the html input attributes and how often typing commits a value.")
    ];

    private static BlazorUIComponent BuildBase(Type type, string prefix, string summary)
    {
        var tables = DemoTables.Read(typeof(Bit.BlazorUI.Demo.Client.Core.Components.DemoPage), prefix);
        var name = Simple(type);

        return new BlazorUIComponent
        {
            Name = name,
            ShortName = name,
            Category = "Base",
            Url = "/components",
            Package = BlazorUIAssemblies.Of(type),
            Summary = summary,
            TypeParameters = TypeParametersOf(type),
            ComponentType = type,
            Parameters = MergeParameters(tables?.Parameters, type),
            PublicMembers = tables?.PublicMembers ?? [],
            OwnTypes = tables?.SubEnums ?? []
        };
    }

    /// <summary>
    /// The bases a component inherits, most specific first, each named as that component closes it:
    /// a reader of BitTextField is told about <c>BitInputBase&lt;string&gt;</c>, which is the type
    /// its <c>Value</c> actually has, rather than about a <c>TValue</c> they would have to resolve.
    /// </summary>
    private static ComponentBaseRef[] InheritedBases(Type? componentType)
    {
        if (componentType is null) return [];

        return [.. Bases.Where(b => b.ComponentType != componentType && Closes(componentType, b.ComponentType!) is not null)
                        .Select(b => new ComponentBaseRef(
                            BlazorUITypeNames.Of(Closes(componentType, b.ComponentType!)!),
                            b.Name,
                            [.. b.Parameters.Select(p => p.Name)]))
                        .Reverse()];
    }

    /// <summary>
    /// The base of <paramref name="type"/> that is <paramref name="baseType"/>, as it is closed
    /// there - <c>BitInputBase&lt;string&gt;</c> for BitTextField's <c>BitInputBase&lt;&gt;</c> -
    /// or null when it does not derive from it at all.
    /// </summary>
    private static Type? Closes(Type type, Type baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == baseType) return current;
        }

        return null;
    }

    /// <summary>
    /// A component's parameters: the hand-written table its demo page renders, plus every
    /// <c>[Parameter]</c> on the compiled type that the table does not name.
    /// <para>
    /// The tables are the better prose - reviewed, and kept in step with the component because the
    /// site renders them - but a parameter added to a component without its page being updated is
    /// invisible in them, and a parameter this server does not name is one an agent will not use
    /// and cannot be told about. So the type has the last word on WHICH parameters exist and the
    /// table on how each is described, which is what each of the two is actually authoritative for.
    /// </para>
    /// </summary>
    private static ComponentMember[] MergeParameters(IReadOnlyList<ComponentMember>? documented, Type? type)
    {
        var reflected = ReflectParameters(type);

        if (documented is null || documented.Count == 0) return reflected;

        var named = documented.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = reflected.Where(m => named.Contains(m.Name) is false).ToArray();

        // Sorted only when there is something to merge in, and then as a whole: the tables are
        // written in alphabetical order, so an addition belongs in that order rather than tacked on
        // the end where a reader scanning for it would not look.
        return missing.Length == 0
            ? [.. documented]
            : [.. documented.Concat(missing).OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// A component's public members - the methods and properties it is driven by from code rather
    /// than through its markup - merged the same way its parameters are, and for the same reason:
    /// a <c>FocusAsync</c> or a <c>Close</c> the page never got round to listing is one an agent
    /// will write a workaround for.
    /// </summary>
    private static ComponentMember[] MergeMembers(IReadOnlyList<ComponentMember>? documented, Type? type, IReadOnlyList<ComponentMember> parameters)
    {
        var reflected = ReflectMembers(type, parameters);

        if (documented is null || documented.Count == 0) return reflected;

        // Matched on the bare name: the tables write an overload out as "FocusAsync(bool
        // preventScroll)", and the reflected member it names is the same member.
        var named = documented.Select(m => m.Name.Split('(')[0]).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. documented, .. reflected.Where(m => named.Contains(m.Name.Split('(')[0]) is false)];
    }

    /// <summary>
    /// The members a component declares for its callers: its own public methods and read-only
    /// properties.
    /// <para>
    /// What is left out is what is public for a reason other than being called: the
    /// <c>[JSInvokable]</c> callbacks the library's own scripts invoke, the <c>Assign*</c> setters
    /// and the <c>HasNotBeenSet</c> probe the parameter source generator emits behind a two-way
    /// binding, the framework members a component overrides, and disposal.
    /// </para>
    /// </summary>
    private static ComponentMember[] ReflectMembers(Type? type, IReadOnlyList<ComponentMember> parameters)
    {
        if (type is null) return [];

        const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var generated = parameters.Select(p => $"Assign{p.Name}").Append(nameof(BitComponentBase.HasNotBeenSet)).ToHashSet(StringComparer.Ordinal);

        var methods = type.GetMethods(Declared)
            .Where(m => m.IsSpecialName is false && m.GetBaseDefinition() == m)
            .Where(m => m.IsDefined(typeof(Microsoft.JSInterop.JSInvokableAttribute)) is false)
            .Where(m => m.Name.StartsWith('_') is false && generated.Contains(m.Name) is false)
            .Where(m => m.Name is not ("Dispose" or "DisposeAsync" or "Equals" or "GetHashCode" or "ToString"))
            .Select(m => new ComponentMember(
                $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => $"{BlazorUITypeNames.Of(p.ParameterType)} {p.Name}"))})",
                BlazorUITypeNames.Of(m.ReturnType),
                null,
                BlazorUIXmlDocs.GetSummary($"M:{type.FullName}.{m.Name}")));

        var properties = type.GetProperties(Declared)
            .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
            .Where(p => p.IsDefined(typeof(ParameterAttribute)) is false && p.IsDefined(typeof(CascadingParameterAttribute)) is false)
            .Select(p => new ComponentMember(p.Name, BlazorUITypeNames.Of(p.PropertyType), null, BlazorUIXmlDocs.GetPropertySummary(type, p)));

        return [.. properties.Concat(methods).OrderBy(m => m.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The <c>[Parameter]</c> properties of a component, less the ones a base entry documents. The
    /// defaults come from an instance of the component, which is the only place a field
    /// initializer's value can be read from - and reading it runs nothing but that initializer.
    /// </summary>
    private static ComponentMember[] ReflectParameters(Type? type)
    {
        if (type is null) return [];

        // Every base but this one: a base's own answer is where its parameters belong, and a
        // component's is where everything it declares below them does.
        var documented = _baseTypes.Where(t => t != type).ToArray();

        var parameters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(ParameterAttribute)))
            .Where(p => documented.Contains(p.DeclaringType is { IsGenericType: true } declaring ? declaring.GetGenericTypeDefinition() : p.DeclaringType) is false)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        if (parameters.Length == 0) return [];

        var defaults = Defaults(type);

        return [.. parameters.Select(p => new ComponentMember(
            p.Name,
            BlazorUITypeNames.Of(p.PropertyType),
            defaults.GetValueOrDefault(p.Name),
            BlazorUIXmlDocs.GetPropertySummary(type, p)))];
    }

    /// <summary>
    /// What each parameter of a freshly constructed component holds, written the way it would be
    /// written in C#. An open generic cannot be constructed and a component that needs more than a
    /// constructor is skipped rather than allowed to fail the catalog, in which case the parameters
    /// are answered without their defaults rather than not at all.
    /// </summary>
    private static Dictionary<string, string> Defaults(Type type)
    {
        var defaults = new Dictionary<string, string>(StringComparer.Ordinal);

        if (type.IsGenericTypeDefinition || type.IsAbstract) return defaults;

        object instance;

        try
        {
            instance = Activator.CreateInstance(type)!;
        }
        catch (Exception)
        {
            return defaults;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.IsDefined(typeof(ParameterAttribute)) is false || property.CanRead is false) continue;

            try
            {
                if (Literal(property.GetValue(instance)) is string literal) defaults[property.Name] = literal;
            }
            catch (Exception)
            {
                // A getter that computes rather than stores has no default to report.
            }
        }

        return defaults;
    }

    /// <summary>A value as it would be typed in C#, or null when it is one nothing is gained by printing.</summary>
    private static string? Literal(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        string text => text.Length == 0 ? null : text,
        Enum member => $"{value.GetType().Name}.{member}",
        // A callback nobody has subscribed to, an empty bag: the row's own type says as much.
        _ => value.GetType().IsPrimitive ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) : null
    };

    /// <summary>
    /// Splits the types a demo page documents into the ones only this component uses and the ones
    /// the whole library shares. A type whose name opens with the component's own is its own; every
    /// other one - <c>BitColor</c>, <c>BitVariant</c>, <c>BitIconInfo</c> - belongs to the library
    /// and is documented once, by <c>GetBitBlazorUIType</c>.
    /// <para>
    /// The split is on the name alone and not on enum-ness: a shared class is as much a type a
    /// caller has to resolve as a shared enum is, and filtering the shared half down to the enums
    /// dropped it from both halves - <c>BitButton</c> never mentioned the <c>BitIconInfo</c> its
    /// own <c>Icon</c> parameter takes.
    /// </para>
    /// </summary>
    private static (ComponentSubType[] Own, ComponentSubType[] Shared) SplitSubTypes(string name, DemoTables? tables)
    {
        if (tables is null) return ([], []);

        var all = tables.SubClasses.Concat(tables.SubEnums).ToArray();

        var own = all.ToLookup(t => t.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));

        return ([.. own[true]], [.. own[false]]);
    }

    private static Type? FindType(string name)
    {
        foreach (var assembly in BlazorUIAssemblies.All)
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                if (type.Name == name) return type;

                // A generic type's reflected name carries its arity - BitDropdown is BitDropdown`2.
                var arity = type.Name.IndexOf('`', StringComparison.Ordinal);

                if (arity > 0 && type.Name.AsSpan(0, arity).SequenceEqual(name)) return type;
            }
        }

        return null;
    }

    private static string? TypeParametersOf(Type? type)
    {
        if (type is null || type.IsGenericTypeDefinition is false) return null;

        return $"<{string.Join(", ", type.GetGenericArguments().Select(a => a.Name))}>";
    }

    /// <summary>The name without the arity marker a generic type's reflected name carries.</summary>
    private static string Simple(Type type)
    {
        var arity = type.Name.IndexOf('`', StringComparison.Ordinal);

        return arity < 0 ? type.Name : type.Name[..arity];
    }
}
