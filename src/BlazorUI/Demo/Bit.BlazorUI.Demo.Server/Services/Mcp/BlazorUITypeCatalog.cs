using System.Reflection;
using System.Collections.Frozen;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>One public type of the library, as the type tool talks about it.</summary>
public sealed record BlazorUIType(Type Clr, string Name, string Kind, BlazorUIPackage Package, string? Summary)
{
    /// <summary>Whether this type belongs to one component - its class-styles bag, its item class, its own enum.</summary>
    public bool OwnedByComponent { get; init; }
}

/// <summary>
/// Every public type of the five packages, so an agent can resolve any name a signature hands it -
/// the enums a parameter takes, the item and option classes a list is built from, the injectable
/// services, the static catalogs of constants.
/// <para>
/// The listing this serves is deliberately not "all 1100 of them": a type named after a component
/// is documented by that component's own answer, where it is read in context. What is left is the
/// library-wide surface, which is the part a signature can name without the component page having
/// mentioned it.
/// </para>
/// </summary>
public static class BlazorUITypeCatalog
{
    /// <summary>
    /// The types <c>AddBitBlazorUIServices</c> and <c>AddBitBlazorUIExtrasServices</c> put in DI.
    /// Named here rather than inferred, because "injectable" is a fact about the registration
    /// extensions and not about the shape of the class.
    /// </summary>
    private static readonly HashSet<string> _services = new(StringComparer.Ordinal)
    {
        "BitModalService", "BitProModalService", "BitMessageBoxService", "BitAccentColorService",
        "BitThemeManager", "BitThemeNotifications", "BitExternalThemeLoader", "BitPageVisibility", "BitExtraServices"
    };

    private static readonly Lazy<BlazorUIType[]> _types = new(Build, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<FrozenDictionary<string, BlazorUIType>> _byName = new(BuildIndex, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Every public type of the five packages.</summary>
    public static BlazorUIType[] Types => _types.Value;

    /// <summary>The types no component page owns - what a listing is worth showing.</summary>
    public static BlazorUIType[] LibraryWide => [.. Types.Where(t => t.OwnedByComponent is false)];

    /// <summary>
    /// The library-wide types a caller will actually meet, which is what the listing shows.
    /// <para>
    /// Every enum, struct, service, delegate and static catalog is here: those are the vocabulary
    /// of the API, and there is no other place to read them. Of the plain classes and the
    /// sub-components, only the ones some component's API names are - a type that appears in a
    /// parameter's type is a type a caller has to resolve, and one that appears nowhere is an
    /// implementation detail of a component that documents it in context. The rest are counted
    /// rather than listed by <see cref="Hidden"/>, and all of them still resolve by name.
    /// </para>
    /// </summary>
    public static BlazorUIType[] Listed => _listed.Value;

    /// <summary>How many library-wide types the listing leaves out - see <see cref="Listed"/>.</summary>
    public static int Hidden => LibraryWide.Length - Listed.Length;

    private static readonly Lazy<BlazorUIType[]> _listed = new(BuildListing, LazyThreadSafetyMode.PublicationOnly);

    private static BlazorUIType[] BuildListing()
    {
        // Every name written in the type of a parameter or member of any component: those are the
        // names a caller reads off a signature and then has to look up. Names rather than one
        // concatenated haystack, because a substring test answers yes for any name that happens to
        // sit inside another - "Link" is in "BitLink" and in "BitNavLinkItem", which listed an
        // Assets component nothing takes as a parameter, and "BitTheme" is in "BitThemeManager".
        var referenced = BlazorUIComponentCatalog.Components
            .SelectMany(c => c.Parameters.Concat(c.PublicMembers).Concat(c.OwnTypes.SelectMany(t => t.Members)))
            .SelectMany(m => Identifiers(m.Type))
            .ToHashSet(StringComparer.Ordinal);

        return [.. LibraryWide.Where(t => t.Kind is not ("class" or "component")
                                       || referenced.Contains(t.Name))];
    }

    /// <summary>
    /// The library types written in a run of type texts, resolved and deduplicated - what a reader
    /// of those signatures has to be able to look up. Names that resolve to nothing (the framework's
    /// own <c>IEnumerable</c>, <c>EventCallback</c>, a generic parameter) are left out.
    /// </summary>
    public static IEnumerable<BlazorUIType> Referenced(IEnumerable<string?> typeTexts)
    {
        return typeTexts.SelectMany(Identifiers)
                        .Distinct(StringComparer.Ordinal)
                        .Select(Find)
                        .OfType<BlazorUIType>()
                        .DistinctBy(t => t.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// The identifiers written in a type as the tables spell it - <c>IEnumerable&lt;BitNavItem&gt;?</c>
    /// is <c>IEnumerable</c> and <c>BitNavItem</c>. Anything that cannot be part of a C# name ends
    /// the one being read.
    /// </summary>
    private static IEnumerable<string> Identifiers(string? text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        var start = -1;

        for (var i = 0; i <= text.Length; i++)
        {
            var part = i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_');

            if (part && start < 0) start = i;
            else if (part is false && start >= 0)
            {
                yield return text[start..i];

                start = -1;
            }
        }
    }

    public static void Warm() => _ = _byName.Value;

    /// <summary>The type a name identifies, with or without the <c>Bit</c> prefix and ignoring generic arity.</summary>
    public static BlazorUIType? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var needle = name.Trim();

        var arity = needle.IndexOf('<', StringComparison.Ordinal);
        if (arity > 0) needle = needle[..arity];

        if (_byName.Value.TryGetValue(needle, out var type)) return type;

        if (_byName.Value.TryGetValue($"Bit{needle}", out type)) return type;

        // A dotted name is a nested static class: the token catalogs are trees, and BitCss.Var.Color
        // is how one of their branches is written both in C# and in a question about it.
        return needle.Contains('.', StringComparison.Ordinal) ? Nested(needle) : null;
    }

    /// <summary>The nested type a dotted name walks to, e.g. <c>BitCss.Var.Color.Primary</c>.</summary>
    private static BlazorUIType? Nested(string dotted)
    {
        var parts = dotted.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (_byName.Value.TryGetValue(parts[0], out var root) is false &&
            _byName.Value.TryGetValue($"Bit{parts[0]}", out root) is false) return null;

        var current = root.Clr;

        foreach (var part in parts.Skip(1))
        {
            current = current.GetNestedType(part, BindingFlags.Public);

            if (current is null) return null;
        }

        // Named from the root's own name rather than from what was typed, so that the path this
        // answer prints - and the nested paths under it - are paths that resolve again.
        var name = string.Join('.', new[] { root.Name }.Concat(parts.Skip(1)));

        return root with { Clr = current, Name = name, Kind = current.IsEnum ? "enum" : "static class", Summary = BlazorUIXmlDocs.GetSummary(BlazorUIXmlDocs.IdOf(current)) };
    }

    /// <summary>The names closest to one that resolved to nothing.</summary>
    public static string[] Similar(string name) => BlazorUISuggest.Closest(name, Types.Select(t => t.Name));

    private static FrozenDictionary<string, BlazorUIType> BuildIndex()
    {
        var index = new Dictionary<string, BlazorUIType>(StringComparer.OrdinalIgnoreCase);

        // The library-wide types claim their names first: BitDropdownItem and BitDropdownItem<TValue>
        // are two entries for one name, and the one a caller means is the one the docs talk about.
        foreach (var type in Types.OrderBy(t => t.OwnedByComponent).ThenBy(t => t.Clr.IsGenericTypeDefinition))
        {
            index.TryAdd(type.Name, type);
        }

        return index.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static BlazorUIType[] Build()
    {
        var componentNames = BlazorUIComponentCatalog.Components.Select(c => c.Name).ToArray();

        return [.. BlazorUIAssemblies.Packages
            .SelectMany(package => Exported(package.Assembly).Select(type => new BlazorUIType(
                type,
                Simple(type),
                Kind(type),
                package,
                BlazorUIXmlDocs.GetSummary(BlazorUIXmlDocs.IdOf(type)))
            {
                // A type named after a component is that component's, and is answered there in the
                // context that explains it. BitDropdownItem is BitDropdown's; BitColor is nobody's.
                OwnedByComponent = componentNames.Any(name => Simple(type).StartsWith(name, StringComparison.Ordinal))
            }))
            .OrderBy(t => t.Name, StringComparer.Ordinal)];
    }

    private static IEnumerable<Type> Exported(Assembly assembly)
    {
        Type[] types;

        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (Exception)
        {
            return [];
        }

        return types.Where(t => t.IsNested is false
                             && t.Name.StartsWith('<') is false
                             && t.Name.Contains("_Imports", StringComparison.Ordinal) is false
                             && t.Namespace?.StartsWith("Bit.BlazorUI", StringComparison.Ordinal) is true);
    }

    /// <summary>The name without the arity marker a generic type's reflected name carries.</summary>
    private static string Simple(Type type)
    {
        var arity = type.Name.IndexOf('`', StringComparison.Ordinal);

        return arity < 0 ? type.Name : type.Name[..arity];
    }

    private static string Kind(Type type)
    {
        if (type.IsEnum) return "enum";
        if (_services.Contains(Simple(type))) return "service";
        if (typeof(IComponent).IsAssignableFrom(type)) return "component";
        if (type.IsInterface) return "interface";
        if (typeof(Delegate).IsAssignableFrom(type)) return "delegate";
        if (type.IsValueType) return "struct";
        if (type.IsAbstract && type.IsSealed) return "static class";

        return "class";
    }
}
