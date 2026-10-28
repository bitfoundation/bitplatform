using Bit.Balette.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Balette.Demo.Client;

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
    public const string Name = "Balette";

    public const string PackageId = "Bit.Balette";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Balette";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Balette is an accessible, keyboard-first command palette for Blazor: fuzzy search, nested pages, modes, " +
        "async sources, recents and global shortcuts, with every decision made in C#. Press Ctrl+K anywhere on " +
        "this site - it runs on it.";

    public static readonly string[] Badges = ["MIT", "net10.0", "net11.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["shortcut", "prompt", "theme", "source", "fuzzy"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    /// <remarks>
    /// Off here: Ctrl+K is Balette's own toggle, and the palette this site demonstrates is the one it
    /// opens. The site's documentation search answers to "/" alone.
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
    /// path below Demo/Client is the type's namespace past this assembly's root, one folder a segment.
    /// </summary>
    public static string SourceUrlOf(Type page)
    {
        var root = typeof(SiteInfo).Namespace!;
        var relative = page.FullName!.StartsWith(root + ".", StringComparison.Ordinal)
            ? page.FullName[(root.Length + 1)..]
            : page.Name;

        return $"{RepositoryUrl}/blob/develop/{ProjectPath}/Demo/Client/{relative.Replace('.', '/')}.razor";
    }
}

/// <summary>
/// The single source of truth for the site's information architecture. The docs nav panel, the
/// header's search palette, the previous/next pager, the breadcrumb and the footer's link columns are
/// all driven from here, so adding a page means adding exactly one entry.
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("/getting-started", "Getting started",
                "Install the package, register the services, link the stylesheet, place the palette and register your first commands - on every hosting model.",
                "install setup nuget package AddBitBaletteServices quick start program.cs stylesheet render mode BaletteTrigger hosting webassembly server web app hybrid csp troubleshooting lazy load lazyassemblyloader blazorwebassemblylazyload GetBaletteService aot",
                typeof(GettingStarted)),
        ]),
        new("Commands",
        [
            new("/commands", "Registering commands",
                "The command model, the fluent builder, declarative markup, ids, groups, commands shown only when they apply, and settings that stay in sync.",
                "register unregister dispose BaletteCmd builder fluent declarative markup BaletteItem BaletteGroup BaletteCommands page-scoped IBaletteService toggle checked VisibleWhen Refresh Update With id icon priority",
                typeof(Commands)),
            new("/pages", "Nested pages & flows",
                "Pages, prompts, multi-select, confirmations, long-running and failing commands, recents, pins and the action panel - every step in one box.",
                "nested pages children lazy prompt multi-select destructive confirm toast error busy cancel CommandFailed retry recents pins history action panel BaletteCommandResult flow webhook reset",
                typeof(PagesAndFlows)),
        ]),
        new("Search",
        [
            new("/search", "Search & ranking",
                "The fuzzy matcher and the ranker run live over this app's registry, with every score, weight and the frecency shown - and the query operators, text folding and typos.",
                "fuzzy fzf matcher ranker score weights operators exact prefix suffix negate alternatives phrase diacritics folding normalize arabic persian digits fullwidth typo frecency priority smart case highlight BaletteQuery",
                typeof(SearchAndRanking)),
            new("/modes", "Modes & sources",
                "Typed prefixes that scope the palette, and async sources that are debounced, cancelled, paged, timed out and retried - with a live call log.",
                "modes prefix > # @ ? $ async sources IBaletteSource debounce cancel ApplyMatcher paging cursor timeout retry RegisterMode RegisterSource people issues documentation",
                typeof(Modes)),
        ]),
        new("Keyboard, theming & language",
        [
            new("/keyboard", "Keyboard & shortcuts",
                "Every key inside the palette, global chords, G-then-X sequences, keyboard layouts, conflicts, and the accessibility the native dialog brings.",
                "keyboard shortcuts hotkeys chord sequence global Mod VimBindings layout IME conflict accessibility aria combobox focus",
                typeof(Keyboard)),
            new("/theming", "Theming & templates",
                "Light and dark, design tokens and density changed live on this site's palette, every template, and unstyled mode - each one running.",
                "theme dark light css custom properties variables tokens accent density detail pane templates ItemTemplate unstyled data-balette contrast forced colors",
                typeof(Theming)),
            new("/localization", "Localization & RTL",
                "Every label and announcement of the palette in another language, plural forms, translated commands, and right-to-left layouts.",
                "localization localisation i18n translate strings BaletteStrings language rtl right to left persian arabic german plural",
                typeof(Localization)),
        ]),
        new("Performance",
        [
            new("/performance", "Large registry",
                "Register thousands of commands - or serve them from a static source - and measure, in your browser, what one keystroke costs.",
                "performance large registry benchmark bulk thousands virtualize MaxResults BaletteStaticSource measure milliseconds",
                typeof(Performance)),
        ]),
        new("Reference",
        [
            new("/options", "Options & security",
                "Every option with this site's value and the library's default, the parameters a single palette overrides, and what keeps text and links safe.",
                "options BaletteOptions configuration defaults parameters security csp xss links javascript AllowedUrlSchemes history privacy",
                typeof(OptionsAndSecurity)),
        ]),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing.",
                "mcp model context protocol ai agent tools resources prompts claude copilot server",
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
    /// page whose route takes a parameter is listed by its bare address, so a deeper path falls back to
    /// the entry it starts with.
    /// </summary>
    public static DocsPageInfo? FindByPath(string path)
        => AllPages.FirstOrDefault(p => IsSamePath(p.Url, path))
           ?? AllPages.FirstOrDefault(p => path.Trim('/').StartsWith(p.Url.Trim('/') + "/", StringComparison.OrdinalIgnoreCase));

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
}
