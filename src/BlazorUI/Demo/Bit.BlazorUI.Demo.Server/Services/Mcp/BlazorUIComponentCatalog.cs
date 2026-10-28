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

    /// <summary>
    /// The public CSS custom properties the component reads off its root, each with its default and
    /// what it changes. They are the theming surface a stylesheet reaches rather than a parameter
    /// markup sets, and the demo page's own table is the only place they are written down.
    /// </summary>
    public IReadOnlyList<ComponentMember> CssVariables { get; init; } = [];

    /// <summary>
    /// The parameters object this component accepts through <c>BitParams</c>, when it has one: its
    /// own parameter table again as nullables, which a cascade fills the unset ones of each
    /// instance from.
    /// <para>
    /// Read off the component's own <c>[CascadingParameter]</c> rather than from a list here, so a
    /// component that gains one is documented the moment it compiles - and left out of the
    /// parameter table itself, which is what markup sets rather than what an ancestor provides.
    /// </para>
    /// </summary>
    public Type? CascadingParams { get; init; }

    /// <summary>
    /// The component a service shows, for a service named after one - <c>BitModal</c> for
    /// <c>BitModalService</c> - which is where the service's <see cref="CascadingParams"/> come from:
    /// what it renders is that component, so a <c>BitParams</c> around its container reaches it.
    /// Null for a component, and for a service that is named after nothing it renders.
    /// </summary>
    public Type? ShownComponentType { get; init; }

    /// <summary>The classes and enums this component owns, in full - nothing else documents them.</summary>
    public IReadOnlyList<ComponentSubType> OwnTypes { get; init; } = [];

    /// <summary>
    /// The library-wide types this component's parameters take - <c>BitColor</c>, <c>BitVariant</c>,
    /// <c>BitIconInfo</c>. Named with their members but without their prose: the same handful
    /// appears on nearly every component, and repeating their descriptions 150 times is the
    /// redundancy this server exists to avoid. Each is documented in full by
    /// <c>GetBitBlazorUIType</c>.
    /// </summary>
    public IReadOnlyList<ComponentSubType> SharedTypes { get; init; } = [];

    /// <summary>
    /// The concrete components of a family documented on one page - the eighteen loaders on Loading -
    /// whose <see cref="ComponentType"/> is their abstract base, which is not a tag anyone can write.
    /// Read off the assembly once, while the catalog is built, and empty for every other component.
    /// </summary>
    public IReadOnlyList<string> FamilyMembers { get; init; } = [];

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

    private static readonly Lazy<FrozenDictionary<string, Type>> _typesByName = new(BuildTypeIndex, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// The link to a component's source: <paramref name="pagePath"/> is the path its demo page states,
    /// relative to the package's Components folder. A page whose source lives outside that folder (the
    /// Params utilities) climbs out of it with "../", which is resolved here segment by segment, so the
    /// link reads as the file's own path. Nothing else about the path is touched - Uri would resolve the
    /// segment too, but it would also percent-encode and normalize every other component's link - and a
    /// "../" never climbs above the package.
    /// </summary>
    private static string SourceUrlOf(string packageId, string pagePath)
    {
        List<string> segments = [packageId, "Components"];

        foreach (var segment in pagePath.Split('/'))
        {
            if (segment is "" or ".") continue;

            if (segment is "..")
            {
                if (segments.Count > 1)
                {
                    segments.RemoveAt(segments.Count - 1);
                }

                continue;
            }

            segments.Add(segment);
        }

        return $"{SourceRoot}/{string.Join('/', segments)}";
    }
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
            var componentType = FindComponentType(name);
            var demoType = demoAssembly.GetTypes().FirstOrDefault(t => t.Name == $"{name}Demo");
            var demo = demoType is null ? null : BlazorUIDemoSource.Get(demoType);
            var tables = demoType is null ? null : DemoTables.Read(demoType);

            var inherited = InheritedBases(componentType);
            var shownType = ShownComponentTypeOf(componentType);
            var parameters = MergeParameters(tables?.Parameters, componentType);

            // A service's page documents its methods in the table a component's parameters are in.
            if (componentType is not null && typeof(IComponent).IsAssignableFrom(componentType) is false) parameters = WithSignatures(parameters, componentType);
            var (own, shared) = SplitSubTypes(name, shownType, tables);

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
                    : SourceUrlOf(BlazorUIAssemblies.Of(componentType).PackageId, demo.SourceUrl),
                Inherited = inherited,
                Parameters = parameters,
                PublicMembers = MergeMembers(tables?.PublicMembers, componentType, parameters),
                CssVariables = tables?.CssVariables ?? [],
                CascadingParams = CascadingParamsOf(shownType ?? componentType),
                ShownComponentType = shownType,
                OwnTypes = [.. own.Select(type => type.IsEnum ? type : type with { Members = WithSignatures(type.Members, FindType(Bare(type.Name))) })],
                SharedTypes = shared,
                FamilyMembers = FamilyMembersOf(componentType),
                Examples = demo?.Examples ?? []
            };
        })];
    }

    /// <summary>
    /// The concrete components deriving from an abstract component type - see
    /// <see cref="BlazorUIComponent.FamilyMembers"/> - in ordinal order.
    /// </summary>
    private static string[] FamilyMembersOf(Type? componentType)
    {
        if (componentType is not { IsAbstract: true } family) return [];

        return [.. family.Assembly.GetExportedTypes()
                                  .Where(t => t.IsAbstract is false && family.IsAssignableFrom(t))
                                  .Select(t => t.Name)
                                  .Order(StringComparer.Ordinal)];
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
            PublicMembers = WithSignatures(tables?.PublicMembers ?? [], type),
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
    /// The parameters object a component takes from a <c>BitParams</c> ancestor - the
    /// <c>[CascadingParameter]</c> it declares of an <c>IBitComponentParams</c> type - or null for
    /// a component that takes none.
    /// <para>
    /// Derived rather than listed: every component that gains a <c>...Params</c> class gets the
    /// line that names it without this server being told, and a component whose cascade is
    /// something else entirely (a theme, a direction) is not mistaken for one that has it.
    /// </para>
    /// </summary>
    private static Type? CascadingParamsOf(Type? componentType)
    {
        if (componentType is null) return null;

        return componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.IsDefined(typeof(CascadingParameterAttribute)))
            .Select(p => p.PropertyType)
            .FirstOrDefault(t => typeof(IBitComponentParams).IsAssignableFrom(t));
    }

    /// <summary>
    /// The component a service named after one shows - see <see cref="BlazorUIComponent.ShownComponentType"/>.
    /// </summary>
    private static Type? ShownComponentTypeOf(Type? componentType)
    {
        if (componentType is null || typeof(IComponent).IsAssignableFrom(componentType)) return null;

        const string suffix = "Service";

        if (componentType.Name.EndsWith(suffix, StringComparison.Ordinal) is false) return null;

        var shown = FindType(componentType.Name[..^suffix.Length]);

        return shown is not null && typeof(IComponent).IsAssignableFrom(shown) ? shown : null;
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
        var named = documented.Select(m => Bare(m.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. WithSignatures(documented, type), .. reflected.Where(m => named.Contains(Bare(m.Name)) is false)];
    }

    /// <summary>
    /// A documented table with each method in it written as the signature it is called with.
    /// <para>
    /// The demo pages write a method's type as the delegate it would fit -
    /// <c>BitMap.PanTo</c> is <c>Func&lt;BitMapLatLng, bool, bool, ValueTask&gt;</c> - which names
    /// what it takes but not what the arguments are called or which of them may be left out, so it
    /// cannot say whether <c>PanTo(center)</c> compiles. The prose stays the page's; the name and the
    /// type are read off the method, which has the last word on how it is called.
    /// </para>
    /// <para>
    /// Which overload a row describes is read off the parameters it writes out - in its name
    /// (<c>FocusAsync(bool preventScroll)</c>), in its type (<c>Task (BitModalReference modal, object? result)</c>)
    /// or as the delegate it fits - and rows are paired with overloads by those types, best fit first,
    /// so two rows of as many parameters are two overloads rather than one twice, and each keeps its own
    /// prose. A row that writes out no parameters answers with every overload no other row has claimed,
    /// and an overload no row claims at all follows the rows of its name with the summary it ships with:
    /// the type has the last word on which overloads exist.
    /// </para>
    /// </summary>
    private static ComponentMember[] WithSignatures(IReadOnlyList<ComponentMember> documented, Type? type)
    {
        if (type is null || documented.Count == 0) return [.. documented];

        var names = documented.Select(row => Bare(row.Name)).ToHashSet(StringComparer.Ordinal);

        // Narrowed to the names the table documents before anything else is read off them: a component
        // inherits the whole of ComponentBase, and a collection type the whole of List<T>.
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => names.Contains(m.Name))
            .Where(m => m.IsSpecialName is false && m.DeclaringType != typeof(object))
            .Where(m => m.IsDefined(typeof(Microsoft.JSInterop.JSInvokableAttribute)) is false)
            .Where(m => m.GetCustomAttribute<System.ComponentModel.EditorBrowsableAttribute>()?.State is not System.ComponentModel.EditorBrowsableState.Never)
            // A method hidden with `new` is listed beside the one hiding it. Taking the same types is what
            // hides it - the parameter names may differ - and the one a call reaches is the most derived.
            .GroupBy(m => $"{m.Name}`{(m.IsGenericMethodDefinition ? m.GetGenericArguments().Length : 0)}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType))})", StringComparer.Ordinal)
            .Select(hiding => hiding.MaxBy(m => Depth(m.DeclaringType))!)
            .ToLookup(m => m.Name, StringComparer.Ordinal);

        var calls = documented.Select(CallOf).ToArray();
        var claimed = new MethodInfo?[documented.Count];
        var taken = new HashSet<MethodInfo>();

        var fits = Enumerable.Range(0, documented.Count)
            .Where(i => calls[i] is not null)
            .SelectMany(i => methods[Bare(documented[i].Name)].Select(method => (Row: i, Method: method, Fit: Fit(calls[i]!, method))))
            .Where(pair => pair.Fit >= 0)
            .OrderByDescending(pair => pair.Fit)
            .ThenBy(pair => pair.Row);

        foreach (var (row, method, _) in fits)
        {
            if (claimed[row] is not null || taken.Add(method) is false) continue;

            claimed[row] = method;
        }

        var last = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = 0; i < documented.Count; i++) last[Bare(documented[i].Name)] = i;

        var context = new NullabilityInfoContext();
        var signed = new List<ComponentMember>(documented.Count);

        for (var i = 0; i < documented.Count; i++)
        {
            var row = documented[i];
            var name = Bare(row.Name);

            if (claimed[i] is { } method) signed.Add(Signed(row, method));
            else if (calls[i] is null && Unclaimed(name) is { Length: > 0 } overloads) signed.AddRange(overloads.Select(m => Signed(row, m)));
            else signed.Add(row);

            if (last[name] == i)
            {
                signed.AddRange(Unclaimed(name).Select(m => new ComponentMember(
                    BlazorUIReflection.Signature(m, context),
                    BlazorUITypeNames.Of(m.ReturnParameter, context),
                    null,
                    BlazorUIXmlDocs.GetSummary(BlazorUIXmlDocs.IdOf(m)))));
            }
        }

        return [.. signed];

        // Taken as they are answered, so no overload is answered twice.
        MethodInfo[] Unclaimed(string name)
        {
            var overloads = methods[name].Where(m => taken.Contains(m) is false).ToArray();

            taken.UnionWith(overloads);

            return overloads;
        }

        ComponentMember Signed(ComponentMember row, MethodInfo method)
            => row with { Name = BlazorUIReflection.Signature(method, context), Type = BlazorUITypeNames.Of(method.ReturnParameter, context) };

        static int Depth(Type? declaring)
        {
            var depth = 0;

            for (; declaring is not null; declaring = declaring.BaseType) depth++;

            return depth;
        }
    }

    /// <summary>The parameters a table row writes out for a method, and the type arguments when it writes those too.</summary>
    private sealed record WrittenCall(int? TypeArguments, (string Type, string? Name)[] Parameters);

    /// <summary>
    /// The parameters a row writes out, which say which overload it describes: in its name
    /// (<c>FocusAsync(bool preventScroll)</c>), in its type written as a call (<c>Task Close(BitSnackBarItem item)</c>,
    /// <c>Task (BitModalReference modal)</c>, <c>() =&gt; Task</c>) or as the delegate it fits
    /// (<c>Func&lt;BitMapLatLng, bool, bool, ValueTask&gt;</c>). Null for a row that writes out none - its
    /// type is only what it returns - or that leaves some out (<c>Track(Task task, ...)</c>).
    /// </summary>
    private static WrittenCall? CallOf(ComponentMember row)
    {
        var name = Bare(row.Name);

        foreach (var text in (string[])[row.Name, row.Type])
        {
            if (ParameterList(text, out var head) is not { } list) continue;

            if (list.Contains("...", StringComparison.Ordinal)) return null;

            // What precedes the list is the method's own name when no space parts the two - Add<T>(...),
            // Task<T> Track<T>(...) - and it then says how many type arguments the call is written with.
            var token = head.Length == 0 || char.IsWhiteSpace(head[^1]) ? null : SplitTopLevel(head, ' ').Last();
            var open = token?.IndexOf('<', StringComparison.Ordinal) ?? -1;

            int? typeArguments = token is null || Bare(token) != name ? null
                               : open < 0 ? 0
                               : SplitTopLevel(token[(open + 1)..].TrimEnd('>')).Count();

            return new(typeArguments, [.. Parameters(list)]);
        }

        // A delegate's type arguments are what it takes, less the last of a Func, which is what it returns.
        var type = row.Type.Trim();

        if (type.StartsWith("Func<", StringComparison.Ordinal) && type.EndsWith('>'))
        {
            return new(null, [.. SplitTopLevel(type[5..^1]).SkipLast(1).Select(argument => (argument.Trim(), (string?)null))]);
        }

        if (type.StartsWith("Action<", StringComparison.Ordinal) && type.EndsWith('>'))
        {
            return new(null, [.. SplitTopLevel(type[7..^1]).Select(argument => (argument.Trim(), (string?)null))]);
        }

        return type == "Action" ? new(null, []) : null;
    }

    /// <summary>
    /// How well a row's written parameters fit an overload, or -1 when they cannot be its - more arguments
    /// than it takes, or fewer than it needs. A type that agrees counts most, then taking exactly that
    /// many, then the type arguments and the names agreeing too.
    /// </summary>
    private static int Fit(WrittenCall call, MethodInfo method)
    {
        var parameters = method.GetParameters();
        var variadic = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));
        var required = parameters.Count(p => p.IsOptional is false) - (variadic ? 1 : 0);

        if (call.Parameters.Length < required || (call.Parameters.Length > parameters.Length && variadic is false)) return -1;

        var fit = call.Parameters.Length == parameters.Length ? 4 : 0;

        if (call.TypeArguments == (method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0)) fit += 2;

        for (var i = 0; i < Math.Min(call.Parameters.Length, parameters.Length); i++)
        {
            var (type, name) = call.Parameters[i];
            var declared = parameters[i].ParameterType;

            if (Normalized(type) == Normalized(BlazorUITypeNames.Of(declared.IsByRef ? declared.GetElementType()! : declared))) fit += 8;
            if (name == parameters[i].Name) fit += 1;
        }

        return fit;

        // The tables write a nullable reference with its ? and reflection without one; either way it is one type.
        static string Normalized(string type) => new([.. type.Where(c => char.IsWhiteSpace(c) is false && c != '?')]);
    }

    /// <summary>
    /// A member's name without the parameter list or the type arguments a table or a signature writes after
    /// it - <c>PanTo</c> for a whole signature. The one spelling every part of this server matches on: the
    /// catalog pairing a row with its method, a <c>section</c> finding the row, and a search hit naming the
    /// <c>section</c> that finds it.
    /// </summary>
    public static string Bare(string name)
    {
        // A CSS variable is all punctuation and letters; it has no parameter list to cut off.
        if (name.StartsWith("--", StringComparison.Ordinal)) return name;

        return name.Split('(', '<')[0].Trim();
    }

    /// <summary>
    /// The types a name written as a call takes - <c>BitMapGeolocationOptions?</c> for
    /// <c>Locate(BitMapGeolocationOptions? options = null)</c> - and none for a name written alone. A
    /// method's row is typed by what it returns, so this is where the rest of the types it names are.
    /// </summary>
    public static IEnumerable<string> ParameterTypes(string name)
        => ParameterList(name, out _) is { } list ? Parameters(list).Select(p => p.Type) : [];

    /// <summary>
    /// Text split at each <paramref name="separator"/> that is outside a type's arguments, a parameter
    /// list and a quoted default, so <c>PanTo(BitMapLatLng center, bool animate = true), SetZoom</c> is two
    /// parts and <c>Func&lt;int, string&gt; map</c> is one.
    /// </summary>
    public static IEnumerable<string> SplitTopLevel(string text, char separator = ',')
    {
        var depth = 0;
        var start = 0;
        char? quote = null;

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];

            if (quote is not null)
            {
                if (character == '\\') i++;
                else if (character == quote) quote = null;

                continue;
            }

            switch (character)
            {
                case '"' or '\'': quote = character; break;
                case '<' or '(' or '[' or '{': depth++; break;
                case '>' or ')' or ']' or '}': depth--; break;
                default:
                    if (character == separator && depth == 0)
                    {
                        yield return text[start..i];

                        start = i + 1;
                    }

                    break;
            }
        }

        yield return text[start..];
    }

    /// <summary>
    /// The parameter list a name or a type written as a call carries - what is between the first parenthesis
    /// outside a type's own arguments and its match - with what precedes it; null when it carries none. A
    /// tuple the text opens with is the type of what is called rather than its parameters:
    /// <c>(double Min, double Max)? GetAxisRange(string axisId)</c> takes one argument.
    /// </summary>
    private static string? ParameterList(string text, out string head)
    {
        var depth = 0;

        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '<' or '[': depth++; break;
                case '>' or ']': depth--; break;
                case '(' when depth == 0:
                    var close = i + 1;

                    for (var nested = 0; close < text.Length; close++)
                    {
                        if (text[close] == '(') nested++;
                        else if (text[close] == ')' && nested == 0) break;
                        else if (text[close] == ')') nested--;
                    }

                    // A tuple type is followed by the name it types; a parameter list by nothing, or by the => of a lambda.
                    var after = close < text.Length ? text.AsSpan(close + 1).TrimStart("? ") : [];

                    if (after.Length > 0 && (char.IsLetter(after[0]) || after[0] is '_' or '@'))
                    {
                        i = close;
                        break;
                    }

                    head = text[..i];

                    return text[(i + 1)..Math.Min(close, text.Length)];
            }
        }

        head = text;

        return null;
    }

    /// <summary>
    /// The parameters of a written list, each as its type and its name when one is written: a table writes
    /// <c>string? body = ""</c> or <c>params object[] args</c>, a delegate's arguments are types alone.
    /// </summary>
    private static IEnumerable<(string Type, string? Name)> Parameters(string list)
    {
        if (string.IsNullOrWhiteSpace(list)) yield break;

        foreach (var parameter in SplitTopLevel(list))
        {
            // A type is never written with an '=', so the first one is where the default starts.
            var equals = parameter.IndexOf('=', StringComparison.Ordinal);
            var declaration = (equals < 0 ? parameter : parameter[..equals]).Trim();

            foreach (var modifier in (string[])["this ", "params ", "scoped ", "ref ", "out ", "in ", "readonly "])
            {
                if (declaration.StartsWith(modifier, StringComparison.Ordinal)) declaration = declaration[modifier.Length..].TrimStart();
            }

            var words = SplitTopLevel(declaration, ' ').Where(word => word.Length > 0).ToArray();

            yield return words.Length > 1 && IsIdentifier(words[^1])
                ? (string.Join(' ', words[..^1]), words[^1].TrimStart('@'))
                : (declaration, null);
        }

        static bool IsIdentifier(string word)
        {
            var name = word.TrimStart('@');

            return name.Length > 0 && char.IsDigit(name[0]) is false && name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }

    /// <summary>
    /// The members a component declares for its callers: its own public methods and read-only
    /// properties.
    /// <para>
    /// What is left out is what is public for a reason other than being called: the
    /// <c>[JSInvokable]</c> callbacks the library's own scripts invoke, the overloads hidden from the editor that
    /// are kept only for assemblies compiled against an earlier signature, the <c>Assign*</c> setters the
    /// parameter source generator emits behind a two-way binding and the <c>HasNotBeenSet</c> probe it
    /// emits on a component outside <c>BitComponentBase</c>, the framework members a component
    /// overrides, and disposal.
    /// </para>
    /// </summary>
    private static ComponentMember[] ReflectMembers(Type? type, IReadOnlyList<ComponentMember> parameters)
    {
        if (type is null) return [];

        const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var generated = parameters.Select(p => $"Assign{p.Name}").Append(nameof(BitComponentBase.HasNotBeenSet)).ToHashSet(StringComparer.Ordinal);
        var context = new NullabilityInfoContext();

        var methods = type.GetMethods(Declared)
            .Where(m => m.IsSpecialName is false && m.GetBaseDefinition() == m)
            .Where(m => m.IsDefined(typeof(Microsoft.JSInterop.JSInvokableAttribute)) is false)
            .Where(m => m.GetCustomAttribute<System.ComponentModel.EditorBrowsableAttribute>()?.State is not System.ComponentModel.EditorBrowsableState.Never)
            .Where(m => m.Name.StartsWith('_') is false && generated.Contains(m.Name) is false)
            .Where(m => m.Name is not ("Dispose" or "DisposeAsync" or "Equals" or "GetHashCode" or "ToString"))
            .Select(m => new ComponentMember(
                BlazorUIReflection.Signature(m, context),
                BlazorUITypeNames.Of(m.ReturnParameter, context),
                null,
                BlazorUIXmlDocs.GetSummary(BlazorUIXmlDocs.IdOf(m))));

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
            // An [Obsolete] parameter is kept only so old markup still binds; naming it would be
            // handing an agent the very name it should not write.
            .Where(p => p.IsDefined(typeof(ObsoleteAttribute)) is false)
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
    /// <para>
    /// A service's types are named after the component it shows rather than after the service -
    /// <c>BitModalReference</c>, <c>BitModalParameters</c> on the <c>BitModalService</c> page - and that
    /// page is the only one documenting them, so they are its own too.
    /// </para>
    /// </summary>
    private static (ComponentSubType[] Own, ComponentSubType[] Shared) SplitSubTypes(string name, Type? shownType, DemoTables? tables)
    {
        if (tables is null) return ([], []);

        var all = tables.SubClasses.Concat(tables.SubEnums).ToArray();

        var own = all.ToLookup(t => t.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase) ||
                                    (shownType is not null && t.Name.StartsWith(shownType.Name, StringComparison.OrdinalIgnoreCase)));

        return ([.. own[true]], [.. own[false]]);
    }

    /// <summary>
    /// The type whose API a nav entry documents. That is the type of the same name, except for a family of
    /// components documented on one page - the eighteen loaders on Loading - whose name belongs to the shell they
    /// all render through (<c>BitLoading</c>, a plain <c>ComponentBase</c> taking <c>This</c>): the family's
    /// parameters, its base and its <c>BitParams</c> cascade are declared on its abstract <c>...Base</c> instead.
    /// </summary>
    private static Type? FindComponentType(string name)
    {
        var type = FindType(name);

        if (type is null || typeof(BitComponentBase).IsAssignableFrom(type)) return type;

        return FindType($"{name}Base") is { } family && typeof(BitComponentBase).IsAssignableFrom(family) ? family : type;
    }

    private static Type? FindType(string name) => _typesByName.Value.GetValueOrDefault(name);

    /// <summary>
    /// Every exported type of the packages by its name less the arity marker a generic one carries -
    /// BitDropdown for BitDropdown`2 - the first in package order winning. Indexed once rather than
    /// scanned per lookup: the catalog looks up every component and every type its page documents.
    /// </summary>
    private static FrozenDictionary<string, Type> BuildTypeIndex()
    {
        var index = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var assembly in BlazorUIAssemblies.All)
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                index.TryAdd(Simple(type), type);
            }
        }

        return index.ToFrozenDictionary(StringComparer.Ordinal);
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
