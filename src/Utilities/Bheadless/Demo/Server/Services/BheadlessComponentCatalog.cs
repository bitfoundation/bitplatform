using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Bit.Bheadless.Demo.Client.Core.Shared;

namespace Bit.Bheadless.Demo.Server.Services;

/// <summary>One <c>[Parameter]</c> of a part, as its table row shows it.</summary>
public sealed record ComponentParameter(string Name, string TypeName, string? Default, string? Summary, bool Required, Type DeclaringType, Type PropertyType);

/// <summary>One component of the library, read off its compiled type and its XML documentation.</summary>
public sealed record ComponentPart(
    Type Type,
    string Name,
    string TagName,
    string? Summary,
    string? Remarks,
    string? Element,
    bool HasCommonParameters,
    ComponentParameter[] Parameters,
    string[] Bindable,
    string[] Hooks);

/// <summary>A component family: the parts one demo page shows working together.</summary>
public sealed record ComponentFamily(
    string Key,
    string Label,
    string Area,
    string? Lead,
    ComponentPart[] Parts,
    DemoPage? CssPage,
    DemoPage? TailwindPage);

/// <summary>
/// The library's components grouped into families, with nothing written down for it: the families
/// and their areas are the demo site's <see cref="DemoCatalog"/>, the parts are the component types
/// of the compiled Bit.Bheadless assembly, and their prose is its XML documentation. So a
/// component added to the library and given a demo page appears here, in the search index and in the
/// catalog without anything else to update.
/// <para>
/// A part joins the family whose slug its name starts with (<c>BitBhMenuItem</c> joins <c>menu</c>, and
/// <c>BitBhMenubar</c> the longer <c>menubar</c>). A part that shares no name with any family - the
/// <c>BitBhTab</c>s, <c>BitBhLabel</c>, <c>BitBhMeter</c> - joins the family whose demo page uses it most,
/// counting double where the family's own root part names it in its documentation. A part no page
/// uses is a building block the others are made of.
/// </para>
/// </summary>
public static partial class BheadlessComponentCatalog
{
    public const string BuildingBlocksKey = "building-blocks";

    public static Assembly Library { get; } = typeof(BitBhComponentBase).Assembly;

    public static string Version { get; } =
        Library.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? Library.GetName().Version?.ToString()
        ?? "unknown";

    private sealed record Catalog(ComponentFamily[] Families,
                                  ComponentParameter[] CommonParameters,
                                  Type[] Types,
                                  Dictionary<Type, ComponentFamily> FamilyByPartType);

    private static readonly Lazy<Catalog> _catalog = new(Build, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Every family, in the demo catalog's order, the building blocks last.</summary>
    public static ComponentFamily[] Families => _catalog.Value.Families;

    /// <summary>Every part of every family.</summary>
    public static IEnumerable<ComponentPart> Parts => Families.SelectMany(family => family.Parts);

    /// <summary>The parameters every part deriving from BitBhComponentBase takes: As, Id, Ref and the attribute splat.</summary>
    public static ComponentParameter[] CommonParameters => _catalog.Value.CommonParameters;

    /// <summary>The library's public types that are not parts: enums, options, render contexts, services, the abstract bases.</summary>
    public static Type[] Types => _catalog.Value.Types;

    public static ComponentFamily FamilyOf(ComponentPart part) => _catalog.Value.FamilyByPartType[part.Type];

    /// <summary>
    /// Resolves a family (slug, label, or a piece of a label such as "Meter" of "Progress / Meter") or a
    /// part (with or without its BitBh prefix and generic arguments). A part answers with its family and
    /// the part itself.
    /// </summary>
    public static (ComponentFamily Family, ComponentPart? Part)? Find(string? name)
    {
        var key = BheadlessSuggest.Normalize(name);

        if (key.Length == 0) return null;

        // Typed with its BitBh prefix it is a part's name, even where a family is spelled the same.
        var typedAsPart = (name ?? string.Empty).TrimStart().StartsWith("BitBh", StringComparison.OrdinalIgnoreCase);

        var part = Parts.FirstOrDefault(part => BheadlessSuggest.Normalize(part.TagName) == key);

        if (typedAsPart && part is not null) return (FamilyOf(part), part);

        var family = Families.FirstOrDefault(family => FamilyKeys(family).Contains(key));

        if (family is not null) return (family, null);

        return part is null ? null : (FamilyOf(part), part);
    }

    /// <summary>Resolves a public library type that is not a part - an enum, an options class, a render context, a service.</summary>
    public static Type? FindType(string? name)
    {
        var key = BheadlessSuggest.Normalize(name);

        if (key.Length == 0) return null;

        var typed = (name ?? string.Empty).Trim();
        var generic = typed.IndexOf('<', StringComparison.Ordinal);
        if (generic > 0) typed = typed[..generic];

        return Types.FirstOrDefault(type => string.Equals(BareName(type), typed, StringComparison.OrdinalIgnoreCase))
            ?? Types.Where(type => BheadlessSuggest.Normalize(type.Name) == key)
                    .OrderBy(type => type.IsInterface) // the class a reader writes, before the interface it is injected as
                    .FirstOrDefault();
    }

    /// <summary>The names worth offering for one that resolved to nothing.</summary>
    public static string[] Similar(string? name)
        => BheadlessSuggest.Closest(name, Families.Where(family => family.CssPage is not null).Select(family => family.Key)
                                                         .Concat(Parts.Select(part => part.Name))
                                                         .Concat(Types.Select(type => BheadlessTypeNames.Of(type))));

    /// <summary>The words a family is asked for by: its slug, its label and each piece of a label like "Field / Input".</summary>
    public static HashSet<string> FamilyKeys(ComponentFamily family)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal)
        {
            BheadlessSuggest.Normalize(family.Key),
            BheadlessSuggest.Normalize(family.Label),
        };

        foreach (var piece in family.Label.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            keys.Add(BheadlessSuggest.Normalize(piece));
        }

        return keys;
    }

    /// <summary>A type's name without its arity marker: BitBhListbox for BitBhListbox`1.</summary>
    public static string BareName(Type type)
    {
        var arity = type.Name.IndexOf('`', StringComparison.Ordinal);

        return arity < 0 ? type.Name : type.Name[..arity];
    }

    private static Catalog Build()
    {
        var exported = Library.GetExportedTypes();

        var componentTypes = exported.Where(type => type.IsClass && type.IsAbstract is false
                                                 && typeof(IComponent).IsAssignableFrom(type)
                                                 && type.Name.StartsWith("BitBh", StringComparison.Ordinal))
                                     .ToArray();

        var parts = componentTypes.Select(BuildPart).ToDictionary(part => part.Type);

        var entries = DemoCatalog.Components;
        var cssPages = entries.ToDictionary(entry => entry.Slug, entry => BheadlessDemoSource.Css(entry.Slug));

        var assigned = new Dictionary<string, List<ComponentPart>>(StringComparer.Ordinal);
        foreach (var entry in entries) assigned[entry.Slug] = [];
        var buildingBlocks = new List<ComponentPart>();

        string SlugKey(DemoCatalog.Entry entry) => entry.Slug.Replace("-", string.Empty, StringComparison.Ordinal);

        // The parts named after a family: the longest slug that starts the name wins.
        var unmatched = new List<ComponentPart>();
        foreach (var part in parts.Values)
        {
            var stem = BheadlessSuggest.Normalize(part.TagName);

            var owner = entries.Where(entry => stem.StartsWith(SlugKey(entry), StringComparison.Ordinal))
                               .OrderByDescending(entry => SlugKey(entry).Length)
                               .FirstOrDefault();

            if (owner is null) unmatched.Add(part);
            else assigned[owner.Slug].Add(part);
        }

        // The rest by where the demo pages use them, counting double on the page of a family whose root
        // part names them in its documentation (BitBhField names BitBhTextarea, which the Fieldset page
        // happens to use more).
        foreach (var part in unmatched)
        {
            var fullName = part.Type.FullName!;

            var best = entries.Select((entry, index) =>
            {
                var usage = Usage(cssPages[entry.Slug], part.TagName);

                var namedByRoot = assigned[entry.Slug]
                    .Where(candidate => BheadlessSuggest.Normalize(candidate.TagName) == SlugKey(entry))
                    .Any(root => BheadlessXmlDocs.GetTypeReferences(BheadlessXmlDocs.IdOf(root.Type)).Contains(fullName));

                return (Entry: entry, Index: index, Score: usage + (namedByRoot ? usage + 1 : 0));
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Index)
            .FirstOrDefault();

            if (best.Entry is null) buildingBlocks.Add(part);
            else assigned[best.Entry.Slug].Add(part);
        }

        var families = entries.Select(entry =>
        {
            var cssPage = cssPages[entry.Slug];
            var key = SlugKey(entry);

            // The part named exactly after the family first, then the rest in the order the demo page
            // first writes them - which is, near enough, the order they nest in.
            var ordered = assigned[entry.Slug]
                .OrderBy(part => BheadlessSuggest.Normalize(part.TagName) == key ? 0 : 1)
                .ThenBy(part => FirstUse(cssPage, part.TagName))
                .ThenBy(part => part.Name, StringComparer.Ordinal)
                .ToArray();

            return new ComponentFamily(entry.Slug, entry.Label, DemoCatalog.AreaLabel(entry.Area),
                                       cssPage?.Lead ?? ordered.Select(part => FirstSentence(part.Summary)).FirstOrDefault(),
                                       ordered, cssPage, BheadlessDemoSource.Tailwind(entry.Slug));
        }).ToList();

        if (buildingBlocks.Count > 0)
        {
            families.Add(new ComponentFamily(BuildingBlocksKey, "Building blocks", "Building blocks",
                                             "Public parts no demo page uses on its own: the pieces other parts are built from.",
                                             [.. buildingBlocks.OrderBy(part => part.Name, StringComparer.Ordinal)], null, null));
        }

        var familyByPartType = families.SelectMany(family => family.Parts.Select(part => (part.Type, family)))
                                       .ToDictionary(entry => entry.Type, entry => entry.family);

        var commonParameters = ParametersOf(typeof(BitBhComponentBase), instance: null)
            .Where(parameter => parameter.DeclaringType == typeof(BitBhComponentBase))
            .ToArray();

        var types = exported.Where(type => parts.ContainsKey(type) is false
                                        && (type.Name.StartsWith("BitBh", StringComparison.Ordinal) || type.Name.StartsWith("IBitBh", StringComparison.Ordinal))
                                        && type.IsNested is false)
                            .OrderBy(type => type.Name, StringComparer.Ordinal)
                            .ToArray();

        return new Catalog([.. families], commonParameters, types, familyByPartType);
    }

    /// <summary>How many times a page's source writes the part's tag.</summary>
    private static int Usage(DemoPage? page, string tagName)
        => page is null ? 0 : Regex.Matches(page.Source, $@"<{tagName}[\s/>]").Count;

    private static int FirstUse(DemoPage? page, string tagName)
    {
        if (page is null) return int.MaxValue;

        var match = Regex.Match(page.Source, $@"<{tagName}[\s/>]");

        return match.Success ? match.Index : int.MaxValue;
    }

    public static string? FirstSentence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var paragraph = text.Split("\n\n", 2)[0].Replace('\n', ' ');
        var end = SentenceEndRegex().Match(paragraph);

        return (end.Success ? paragraph[..(end.Index + 1)] : paragraph).Trim();
    }

    /// <summary>Builds a part, or the parameter table of an abstract base asked for by name.</summary>
    public static ComponentPart BuildPart(Type type)
    {
        // An abstract base's defaults are what the first part built on it starts with.
        var instance = CreateInstance(type.IsAbstract
            ? Library.GetExportedTypes().Where(candidate => candidate.IsAbstract is false && Inherits(candidate, type))
                                        .OrderBy(candidate => candidate.Name, StringComparer.Ordinal)
                                        .FirstOrDefault() ?? type
            : type);

        var parameters = ParametersOf(type, instance)
            .Where(parameter => parameter.DeclaringType != typeof(BitBhComponentBase))
            .ToArray();

        var names = parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);

        var bindable = parameters.Where(parameter => names.Contains($"{parameter.Name}Changed"))
                                 .Select(parameter => $"@bind-{parameter.Name}")
                                 .ToArray();

        var summary = BheadlessXmlDocs.GetSummary(BheadlessXmlDocs.IdOf(type));
        var remarks = BheadlessXmlDocs.GetRemarks(BheadlessXmlDocs.IdOf(type));

        // The hooks its documentation names: on the type itself, and on its parameters.
        var hookText = string.Join('\n', new[] { summary, remarks }.Concat(parameters.Select(parameter => parameter.Summary)));

        var hooks = HookRegex().Matches(hookText)
                               .Select(match => match.Value.TrimEnd('-'))
                               .Distinct(StringComparer.Ordinal)
                               .ToArray();

        return new ComponentPart(type,
                                 BheadlessTypeNames.Of(type),
                                 BareName(type),
                                 summary,
                                 remarks,
                                 ElementOf(type, instance),
                                 typeof(BitBhComponentBase).IsAssignableFrom(type),
                                 parameters,
                                 bindable,
                                 hooks);
    }

    /// <summary>
    /// Every <c>[Parameter]</c> of a component type, its own first and then what it inherits, each in
    /// declaration order, with the default read off a constructed instance.
    /// </summary>
    private static IEnumerable<ComponentParameter> ParametersOf(Type type, object? instance)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => Attribute.IsDefined(property, typeof(ParameterAttribute), inherit: true))
            // A property hidden with 'new' is listed once per declaring type; the most derived one is the parameter.
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(property => Depth(property.DeclaringType!)).First())
            .OrderByDescending(property => Depth(property.DeclaringType!))
            .ThenBy(property => property.MetadataToken);

        foreach (var property in properties)
        {
            string? defaultValue = null;

            if (instance is not null && BheadlessTypeNames.IsDelegateLike(property.PropertyType) is false)
            {
                try
                {
                    var closed = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(candidate => candidate.Name == property.Name)
                                         .OrderByDescending(candidate => Depth(candidate.DeclaringType!))
                                         .First();

                    defaultValue = BheadlessTypeNames.Literal(closed.GetValue(instance), property.PropertyType);
                }
                catch (Exception)
                {
                    // A getter that needs a rendered component; the table shows a dash.
                }
            }

            var declaring = property.DeclaringType!;

            yield return new ComponentParameter(
                property.Name,
                BheadlessTypeNames.Of(property),
                defaultValue,
                BheadlessXmlDocs.GetPropertySummary(type, property),
                Attribute.IsDefined(property, typeof(EditorRequiredAttribute), inherit: true),
                declaring.IsGenericType ? declaring.GetGenericTypeDefinition() : declaring,
                property.PropertyType);
        }
    }

    /// <summary>Whether a type derives from another, an open generic one included.</summary>
    public static bool Inherits(Type type, Type ancestor)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if ((current.IsGenericType ? current.GetGenericTypeDefinition() : current) == ancestor) return true;
        }

        return false;
    }

    private static int Depth(Type type)
    {
        var depth = 0;
        for (var current = type; current is not null; current = current.BaseType) depth++;
        return depth;
    }

    /// <summary>
    /// A component constructed the way the renderer would before setting its parameters, so the values
    /// it starts with are what the table calls its defaults. A generic part is closed over string (or
    /// int, for a struct constraint); one that cannot be constructed simply has no defaults to show.
    /// </summary>
    private static object? CreateInstance(Type type)
    {
        try
        {
            var closed = type.IsGenericTypeDefinition
                ? type.MakeGenericType([.. type.GetGenericArguments().Select(argument =>
                      argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)
                          ? typeof(int)
                          : typeof(string))])
                : type;

            return Activator.CreateInstance(closed);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>The element a part renders when <c>As</c> is not set: its <c>DefaultTag</c>.</summary>
    private static string? ElementOf(Type type, object? instance)
    {
        if (instance is null) return null;

        try
        {
            var property = instance.GetType()
                                   .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                   .Where(candidate => candidate.Name == "DefaultTag" && candidate.PropertyType == typeof(string))
                                   .OrderByDescending(candidate => Depth(candidate.DeclaringType!))
                                   .FirstOrDefault();

            return property?.GetValue(instance) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [GeneratedRegex(@"(?<![\w-])(?:data|aria)-[a-z][a-z0-9-]*(?:=""[^""]*"")?|--bit-bh-[a-z0-9-]+")]
    private static partial Regex HookRegex();

    [GeneratedRegex(@"[.!?](?=\s|$)")]
    private static partial Regex SentenceEndRegex();
}
