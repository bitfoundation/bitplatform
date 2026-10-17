namespace Bit.Bonboarding.Demo.Client.Shared;

/// <summary>
/// One entry of the docs navigation. The list itself is <see cref="All"/>, and it is the site's
/// single index: the nav panel groups it, the landing page prints it, the breadcrumb reads a page's
/// group out of it and the pager walks it in order. A page therefore appears everywhere by having an
/// entry here, and none of those can disagree about what the site contains.
/// </summary>
/// <param name="Title">The label shown in the nav panel.</param>
/// <param name="Href">The route, relative to the app base.</param>
/// <param name="Category">The group this page is filed under; see <see cref="Groups"/>.</param>
/// <param name="Description">What the page demonstrates, in one sentence.</param>
/// <param name="Icon">The mark the landing page shows beside the page; one of <see cref="Icons"/>.</param>
public sealed record NavItem(
    string Title,
    string Href,
    string Category,
    string Description,
    string Icon)
{
    /// <summary>The route as a link target.</summary>
    public string Url => $"/{Href}";

    // The group names, written out once so a typo in an entry below cannot silently invent a new
    // group, and in the order the docs are meant to be read.
    private const string Guides = "Guides";
    private const string Customise = "Customise";
    private const string Ai = "AI";

    /// <summary>Every docs page, in the order they are meant to be read.</summary>
    public static readonly NavItem[] All =
    [
        new("Placement & spotlight", "placement", Guides,
            "Collision-aware positioning computed in C#: shift before flip, spotlight shapes and padding, and what the overlay does with a click.",
            Icons.Target),
        new("Dynamic targets", "dynamic", Guides,
            "Elements that render late, re-render or never appear, steps that wait for the user to act, and targets picked at run time.",
            Icons.Clock),
        new("Conditional & branching", "flows", Guides,
            "Steps gated on a condition, tours that fork on an answer, and validation before the tour moves on.",
            Icons.Branch),
        new("Multi-page tour", "workspace", Guides,
            "A tour that spans routes: it navigates for you, waits for the page to render, and holds its place if you wander off.",
            Icons.Pages),
        new("Theming & RTL", "theming", Customise,
            "The --bit-bonboarding-* custom properties, light and dark, progress styles, localised labels and right-to-left tours.",
            Icons.Palette),
        new("Headless UI", "headless", Customise,
            "Replace the card entirely and keep collision-aware placement, focus management and dialog semantics.",
            Icons.Code),
        new("Events & persistence", "events", Customise,
            "The event stream to forward to analytics, and the store that remembers how far each user got.",
            Icons.Pulse),
        new("MCP server", "mcp-server", Ai,
            "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing at it.",
            Icons.Sparkle),
    ];

    /// <summary>
    /// <see cref="All"/> in groups, in reading order. Both the order of the groups and the order
    /// within them come from the order of <see cref="All"/>, so re-filing a page is a one-line edit
    /// above and nothing here has to be kept in step with it.
    /// </summary>
    public static readonly (string Category, string Icon, NavItem[] Items)[] Groups =
    [
        .. All.GroupBy(item => item.Category)
              .Select(group => (group.Key, CategoryIcon(group.Key), group.ToArray()))
    ];

    /// <summary>
    /// The entry a route belongs to, or null when it is not a docs page. A route under an entry
    /// counts as that entry - workspace/settings is the second half of the multi-page tour, not a
    /// page of its own.
    /// </summary>
    public static NavItem? ByHref(string href)
        => Array.Find(All, item => string.Equals(item.Href, href, StringComparison.OrdinalIgnoreCase))
        ?? Array.Find(All, item => href.StartsWith($"{item.Href}/", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The pages either side of <paramref name="href"/> in reading order, for the pager at the foot
    /// of every page. Both are null for a route that is not itself an entry.
    /// </summary>
    public static (NavItem? Previous, NavItem? Next) Neighbours(string href)
    {
        var index = Array.FindIndex(All, item => string.Equals(item.Href, href, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return (null, null);

        return (index > 0 ? All[index - 1] : null,
                index < All.Length - 1 ? All[index + 1] : null);
    }

    // The one place a group is tied to a picture, so two pages in the same group cannot be given
    // two different ones.
    private static string CategoryIcon(string category) => category switch
    {
        Guides => Icons.Compass,
        Customise => Icons.Palette,
        Ai => Icons.Sparkle,
        _ => Icons.Compass,
    };
}
