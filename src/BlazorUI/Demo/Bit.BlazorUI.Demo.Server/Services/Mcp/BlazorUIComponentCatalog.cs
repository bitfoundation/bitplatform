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
    /// Whether it derives from <c>BitComponentBase</c> and therefore carries its ten shared
    /// parameters. Most do; a few of the larger Extras components are plain
    /// <c>ComponentBase</c> and do not, and promising a reader a <c>Visibility</c> parameter that
    /// component has never had is worse than saying nothing.
    /// </summary>
    public bool InheritsBase => ComponentType is not null
                             && ComponentType != typeof(BitComponentBase)
                             && typeof(BitComponentBase).IsAssignableFrom(ComponentType);

    public IReadOnlyList<ComponentMember> Parameters { get; init; } = [];
    public IReadOnlyList<ComponentMember> PublicMembers { get; init; } = [];

    /// <summary>The classes and enums this component owns, in full - nothing else documents them.</summary>
    public IReadOnlyList<ComponentSubType> OwnTypes { get; init; } = [];

    /// <summary>
    /// The library-wide enums this component's parameters take. Named with their values but without
    /// their prose: the same handful of enums appears on nearly every component, and repeating their
    /// descriptions 110 times is the redundancy this server exists to avoid. Each is documented in
    /// full by <c>GetBitBlazorUIType</c>.
    /// </summary>
    public IReadOnlyList<ComponentSubType> SharedEnums { get; init; } = [];

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

    private static readonly Lazy<BlazorUIComponent[]> _components = new(Build, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<FrozenDictionary<string, BlazorUIComponent>> _byName = new(BuildIndex, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Every documented component, in nav order.</summary>
    public static BlazorUIComponent[] Components => _components.Value;

    /// <summary>The parameters, members and enums every component inherits from <c>BitComponentBase</c>.</summary>
    public static BlazorUIComponent Base { get; } = BuildBase();

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

        return [.. BlazorUISuggest.Closest(name, Components.Select(c => c.Name).Concat(byAlias.Select(g => g.Key)))
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

        index.TryAdd(Base.Name, Base);

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

            var parameters = tables?.Parameters ?? ReflectParameters(componentType);
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
                Parameters = parameters,
                PublicMembers = tables?.PublicMembers ?? [],
                OwnTypes = own,
                SharedEnums = shared,
                Examples = demo?.Examples ?? []
            };
        })];
    }

    /// <summary>
    /// The shared half of every component's answer, as a component of its own. Ten parameters and
    /// three enums that hold for all 110 of them are worth one lookup and not 110 repetitions, so
    /// this is what the per-component answers point at instead of restating them.
    /// </summary>
    private static BlazorUIComponent BuildBase()
    {
        var tables = DemoTables.Read(typeof(Bit.BlazorUI.Demo.Client.Core.Components.DemoPage), "_componentBase");

        return new BlazorUIComponent
        {
            Name = nameof(BitComponentBase),
            ShortName = nameof(BitComponentBase),
            Category = "Base",
            Url = "/components",
            Package = BlazorUIAssemblies.Core,
            Summary = "The base class every bit BlazorUI component derives from. Its parameters are available on all of them.",
            ComponentType = typeof(BitComponentBase),
            Parameters = tables?.Parameters ?? [],
            PublicMembers = tables?.PublicMembers ?? [],
            OwnTypes = tables?.SubEnums ?? []
        };
    }

    /// <summary>
    /// Splits the types a demo page documents into the ones only this component uses and the ones
    /// the whole library shares. A type whose name opens with the component's own is its own; every
    /// other one - <c>BitColor</c>, <c>BitVariant</c>, <c>BitSize</c> - belongs to the library and
    /// is documented once, by <c>GetBitBlazorUIType</c>.
    /// </summary>
    private static (ComponentSubType[] Own, ComponentSubType[] Shared) SplitSubTypes(string name, DemoTables? tables)
    {
        if (tables is null) return ([], []);

        var all = tables.SubClasses.Concat(tables.SubEnums).ToArray();

        return ([.. all.Where(t => t.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))],
                [.. all.Where(t => t.IsEnum && t.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase) is false)]);
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

    /// <summary>
    /// The parameters of a component that has no demo page to read them from, straight off the type.
    /// The hand-written tables are better prose where they exist; this is what keeps a component
    /// documented at all when they do not.
    /// </summary>
    private static ComponentMember[] ReflectParameters(Type? type)
    {
        if (type is null) return [];

        return [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(ParameterAttribute)) && p.DeclaringType != typeof(BitComponentBase))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => new ComponentMember(
                p.Name,
                BlazorUITypeNames.Of(p.PropertyType),
                null,
                BlazorUIXmlDocs.GetPropertySummary(type, p)))];
    }
}
