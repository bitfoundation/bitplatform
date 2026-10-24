using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Bit.Blex.Query;

/// <summary>
/// The data of an <see cref="BlexInfiniteQuery{TPage, TParam}"/>: the pages loaded so far, in order, and the page
/// param each was fetched with. A new instance every time a page is added or the pages are refetched.
/// </summary>
/// <typeparam name="TPage">One page, as the server returns it.</typeparam>
/// <typeparam name="TParam">What a page is fetched by: a page number, a cursor.</typeparam>
public sealed class BlexInfiniteData<TPage, TParam>
{
    /// <summary>An empty set of pages.</summary>
    public static BlexInfiniteData<TPage, TParam> Empty { get; } = new([], []);

    /// <summary>Creates the data from pages and the params they were fetched with; the two must line up.</summary>
    /// <remarks>
    /// The dependency marks this constructor as reached by reflection, which is how System.Text.Json reaches it
    /// when the data is handed from the prerender to the interactive render or across tabs. Without it a trimmed
    /// publish keeps the constructor (code calls it) but drops its parameter names, and System.Text.Json refuses
    /// a constructor whose parameters it cannot match by name ("ConstructorContainsNullParameterNames").
    /// </remarks>
    [JsonConstructor]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(BlexInfiniteData<,>))]
    public BlexInfiniteData(IReadOnlyList<TPage> pages, IReadOnlyList<TParam> pageParams)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(pageParams);
        if (pages.Count != pageParams.Count)
            throw new ArgumentException("Every page needs the param it was fetched with.", nameof(pageParams));

        Pages = pages;
        PageParams = pageParams;
    }

    /// <summary>The pages loaded so far, first to last.</summary>
    public IReadOnlyList<TPage> Pages { get; }

    /// <summary>The param each page in <see cref="Pages"/> was fetched with.</summary>
    public IReadOnlyList<TParam> PageParams { get; }
}
