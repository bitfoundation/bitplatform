using Bit.Bheadless.Demo.Client.Core.Pages;
using Bit.Bheadless.Demo.Client.Core.Pages.Css;
using Bit.Bheadless.Demo.Client.Core.Shared;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless.Demo.Client.Core;

/// <summary>
/// One documentation page: <paramref name="Url"/> is its address, <paramref name="Keywords"/> feeds
/// the header's search palette, and <paramref name="PageType"/> is the component the address renders -
/// which is what the "Edit this page on GitHub" link under every page is built from.
/// </summary>
public record DocsPageInfo(string Url, string Title, string Description, string Keywords, Type PageType);

public record DocsSection(string Title, DocsPageInfo[] Pages);

/// <summary>
/// A link in the header. <paramref name="MatchesDocs"/> marks the entry that stands for the docs as a
/// whole: it is the current section on every page no other entry claims.
/// </summary>
public record SiteNavItem(string Href, string Label, bool MatchesDocs = false);

/// <summary>
/// What the shared site chrome in Layout/ - the header, the docs shell, the palette and the footer -
/// says about this particular demo. Those components are the same file in every Utilities demo; this
/// class and <see cref="DocsCatalog"/> are what each demo fills in for itself.
/// </summary>
public static class SiteInfo
{
    /// <summary>The library's name, as the header's wordmark and the breadcrumb's root show it.</summary>
    public const string Name = "Bheadless";

    public const string PackageId = "Bit.Bheadless";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bheadless";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bheadless is a set of accessible, unstyled UI primitives for Blazor: keyboard interaction, focus " +
        "management, positioning, dismissal and WAI-ARIA semantics as plain Razor components, styled with your " +
        "own CSS or Tailwind. This site is itself a Blazor app built on it.";

    public static readonly string[] Badges = ["MIT", "net10.0", "net11.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["dialog", "menu", "combobox", "keyboard", "tailwind"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    /// <remarks>
    /// Off here: the command palette and the hotkey demos register mod+k with the library's own
    /// <c>BitBhCommandDialog</c> and <c>BitBhHotkey</c>.
    /// </remarks>
    public static readonly bool PaletteCtrlK = false;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/mcp-server", "MCP server"),
    ];

    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public static string ProjectUrl => $"{RepositoryUrl}/tree/develop/{ProjectPath}";

    public static string NuGetUrl => $"https://www.nuget.org/packages/{PackageId}";

    /// <summary>
    /// The page's source file on GitHub. Razor gives a component the namespace of its folder, so the
    /// path below the client project is the type's namespace past this assembly's root, one folder a segment.
    /// </summary>
    public static string SourceUrlOf(Type page)
    {
        var root = typeof(SiteInfo).Namespace!;
        var relative = page.FullName!.StartsWith(root + ".", StringComparison.Ordinal)
            ? page.FullName[(root.Length + 1)..]
            : page.Name;

        return $"{RepositoryUrl}/blob/develop/{ProjectPath}/Demo/Client/Bit.Bheadless.Demo.Client.Core/{relative.Replace('.', '/')}.razor";
    }
}

/// <summary>
/// The single source of truth for the site's information architecture. The docs nav panel, the
/// header's search palette, the previous/next pager, the breadcrumb and the footer's link columns are
/// all driven from here. The component pages come from <see cref="DemoCatalog"/>, one section per
/// <see cref="DemoArea"/>, so adding a component means adding its entry there and its page type to
/// <see cref="CssPageOf"/>.
/// </summary>
/// <remarks>
/// Every component has two pages - hand-written CSS at <c>/&lt;slug&gt;</c> and Tailwind at
/// <c>/tw/&lt;slug&gt;</c> - and is listed once, under its CSS address: <see cref="FindByPath"/> maps a
/// Tailwind address onto the same entry, and the switcher at the top of both pages moves between them.
/// </remarks>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("/getting-started", "Getting started",
                "Install the package, register the services and put a first accessible component on a page.",
                "install setup nuget package AddBitBheadlessServices quick start program.cs using render mode interactive prerender",
                typeof(GettingStarted)),
            new("/tw", "Styling with Tailwind",
                "The same components styled with Tailwind v4 utilities and custom data-* variants.",
                "tailwind utility classes custom-variant data-state data-active data-disabled variants styling css",
                typeof(Pages.Tailwind.Home)),
        ]),
        .. Enum.GetValues<DemoArea>().Select(area => new DocsSection(DemoCatalog.AreaLabel(area),
        [
            .. DemoCatalog.Components.Where(entry => entry.Area == area).Select(entry =>
                new DocsPageInfo($"/{entry.Slug}", entry.Label, entry.Summary, entry.Keywords, CssPageOf(entry.Slug))),
        ])),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing.",
                "mcp model context protocol ai agent tools claude copilot server",
                typeof(McpServer)),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = [.. Sections.SelectMany(s => s.Pages)];

    /// <summary>The current address relative to the base, without its query string or fragment.</summary>
    public static string PathOf(NavigationManager nav)
    {
        var path = nav.ToBaseRelativePath(nav.Uri);
        var cut = path.IndexOfAny(['?', '#']);
        return (cut >= 0 ? path[..cut] : path).Trim('/');
    }

    /// <summary>Whether a link's address and a base-relative path name the same page, ignoring case as the router does.</summary>
    public static bool IsSamePath(string href, string path)
        => string.Equals(href.Trim('/'), path.Trim('/'), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Finds the catalog entry for a base-relative path, or null when the page is not in the catalog. A
    /// component's Tailwind page (<c>tw/&lt;slug&gt;</c>) is the entry of its CSS page.
    /// </summary>
    public static DocsPageInfo? FindByPath(string path)
    {
        var trimmed = path.Trim('/');
        if (trimmed.StartsWith("tw/", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[3..];

        return AllPages.FirstOrDefault(p => IsSamePath(p.Url, trimmed));
    }

    /// <summary>Previous/next pages in reading order for the bottom-of-page pager.</summary>
    public static (DocsPageInfo? Previous, DocsPageInfo? Next) NeighborsOf(DocsPageInfo page)
    {
        var index = Array.IndexOf(AllPages, page);
        if (index < 0) return (null, null);
        return (index > 0 ? AllPages[index - 1] : null,
                index < AllPages.Length - 1 ? AllPages[index + 1] : null);
    }

    /// <summary>Section a page belongs to (the breadcrumb's middle step and the palette's group label).</summary>
    public static string SectionOf(DocsPageInfo page)
        => Sections.First(s => s.Pages.Contains(page)).Title;

    /// <summary>
    /// The CSS section's page of a component. Named type by type rather than found by reflection, so the
    /// trimmer keeps every one and a renamed page is a build error rather than a missing edit link.
    /// </summary>
    private static Type CssPageOf(string slug) => slug switch
    {
        "accessibility" => typeof(AccessibilityPage),
        "accordion" => typeof(AccordionPage),
        "alert-dialog" => typeof(AlertDialogPage),
        "aspect-ratio" => typeof(AspectRatioPage),
        "avatar" => typeof(AvatarPage),
        "button" => typeof(ButtonPage),
        "checkbox" => typeof(CheckboxPage),
        "close-button" => typeof(CloseButtonPage),
        "collapsible" => typeof(CollapsiblePage),
        "combobox" => typeof(ComboboxPage),
        "command" => typeof(CommandPage),
        "context-menu" => typeof(ContextMenuPage),
        "data-interactive" => typeof(DataInteractivePage),
        "dialog" => typeof(DialogPage),
        "disclosure" => typeof(DisclosurePage),
        "field" => typeof(FieldPage),
        "fieldset" => typeof(FieldsetPage),
        "focus-trap" => typeof(FocusTrapPage),
        "grid" => typeof(GridPage),
        "hover-card" => typeof(HoverCardPage),
        "listbox" => typeof(ListboxPage),
        "menu" => typeof(MenuPage),
        "menubar" => typeof(MenubarPage),
        "popover" => typeof(PopoverPage),
        "portal" => typeof(PortalPage),
        "progress" => typeof(ProgressPage),
        "radiogroup" => typeof(RadioGroupPage),
        "slider" => typeof(SliderPage),
        "sortable" => typeof(SortablePage),
        "switch" => typeof(SwitchPage),
        "tabs" => typeof(TabsPage),
        "toast" => typeof(ToastPage),
        "toggle-group" => typeof(ToggleGroupPage),
        "toolbar" => typeof(ToolbarPage),
        "tooltip" => typeof(TooltipPage),
        "transition" => typeof(TransitionPage),
        "tree" => typeof(TreePage),
        _ => throw new InvalidOperationException($"DemoCatalog lists '{slug}', which has no CSS page here."),
    };
}
