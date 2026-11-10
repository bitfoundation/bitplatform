using Bit.Bheadless.Demo.Client.Core.Pages;
using Bit.Bheadless.Demo.Client.Core.Shared;
using Microsoft.AspNetCore.Components;
using Css = Bit.Bheadless.Demo.Client.Core.Pages.Css;
using Tw = Bit.Bheadless.Demo.Client.Core.Pages.Tailwind;

namespace Bit.Bheadless.Demo.Client.Core;

/// <summary>
/// One documentation page: <paramref name="Url"/> is its address, <paramref name="Keywords"/> feeds
/// the header's search palette, and <paramref name="PageType"/> is the component the address renders -
/// which is what the "Edit this page on GitHub" link under every page is built from.
/// <paramref name="Styling"/> is the section of the site the entry is listed in (see <see cref="DocsCatalog"/>).
/// </summary>
public record DocsPageInfo(string Url, string Title, string Description, string Keywords, Type PageType,
                           DemoSection Styling = DemoSection.Css);

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
/// <see cref="DemoArea"/>, so adding a component means adding its entry there and its two page types to
/// <see cref="PagesOf"/>.
/// </summary>
/// <remarks>
/// Every component has two pages - hand-written CSS at <c>/&lt;slug&gt;</c> and Tailwind at
/// <c>/tw/&lt;slug&gt;</c> - so the site is read in one of two sections, which the switcher at the top of
/// both pages moves between. <see cref="Sections"/> lists the CSS pages, and is what the search palette
/// and the footer link to; <see cref="TailwindSections"/> is the same reading order with every component
/// at its Tailwind address. A Tailwind address (<c>tw</c>, <c>tw/&lt;slug&gt;</c>) finds its entry in the
/// second, so the nav panel, the pager and the breadcrumb stay in the section the reader is browsing, and
/// the "edit" link opens the Tailwind page's own source.
/// </remarks>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections = BuildSections(DemoSection.Css);

    /// <summary><see cref="Sections"/> with every component at its Tailwind page (<c>/tw/&lt;slug&gt;</c>).</summary>
    public static readonly DocsSection[] TailwindSections = BuildSections(DemoSection.Tailwind);

    public static readonly DocsPageInfo[] AllPages = [.. Sections.SelectMany(s => s.Pages)];

    private static readonly DocsPageInfo[] AllTailwindPages = [.. TailwindSections.SelectMany(s => s.Pages)];

    /// <summary>
    /// The site's reading order in one styling section. Every entry carries its section - the ones both share
    /// (Getting started, MCP server) too - so an entry is in exactly one of the two lists, which is how the
    /// pager and the breadcrumb know which one to read.
    /// </summary>
    private static DocsSection[] BuildSections(DemoSection styling) =>
    [
        new("Introduction",
        [
            new("/getting-started", "Getting started",
                "Install the package, register the services and put a first accessible component on a page.",
                "install setup nuget package AddBitBheadlessServices quick start program.cs using render mode interactive prerender",
                typeof(GettingStarted), styling),
            new("/tw", "Styling with Tailwind",
                "The same components styled with Tailwind v4 utilities and custom data-* variants.",
                "tailwind utility classes custom-variant data-state data-active data-disabled variants styling css",
                typeof(Tw.Home), styling),
        ]),
        .. Enum.GetValues<DemoArea>().Select(area => new DocsSection(DemoCatalog.AreaLabel(area),
        [
            .. DemoCatalog.Components.Where(entry => entry.Area == area).Select(entry =>
                new DocsPageInfo($"/{DemoCatalog.Href(styling, entry.Slug)}", entry.Label, entry.Summary, entry.Keywords,
                                 PageOf(styling, entry.Slug), styling)),
        ])),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing.",
                "mcp model context protocol ai agent tools claude copilot server",
                typeof(McpServer), styling),
        ]),
    ];

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
    /// Tailwind address (<c>tw</c>, <c>tw/&lt;slug&gt;</c>) is looked up in <see cref="TailwindSections"/>.
    /// </summary>
    public static DocsPageInfo? FindByPath(string path)
    {
        var trimmed = path.Trim('/');
        var tailwind = IsSamePath("tw", trimmed) || trimmed.StartsWith("tw/", StringComparison.OrdinalIgnoreCase);

        return (tailwind ? AllTailwindPages : AllPages).FirstOrDefault(p => IsSamePath(p.Url, trimmed));
    }

    /// <summary>The nav panel's sections on a page: those of the styling section the page is listed in.</summary>
    public static DocsSection[] SectionsOf(DocsPageInfo? page)
        => page?.Styling == DemoSection.Tailwind ? TailwindSections : Sections;

    /// <summary>Previous/next pages in reading order for the bottom-of-page pager, within the page's styling section.</summary>
    public static (DocsPageInfo? Previous, DocsPageInfo? Next) NeighborsOf(DocsPageInfo page)
    {
        var pages = page.Styling == DemoSection.Tailwind ? AllTailwindPages : AllPages;
        var index = Array.IndexOf(pages, page);
        if (index < 0) return (null, null);
        return (index > 0 ? pages[index - 1] : null,
                index < pages.Length - 1 ? pages[index + 1] : null);
    }

    /// <summary>Section a page belongs to (the breadcrumb's middle step and the palette's group label).</summary>
    public static string SectionOf(DocsPageInfo page)
        => SectionsOf(page).First(s => s.Pages.Contains(page)).Title;

    private static Type PageOf(DemoSection styling, string slug)
    {
        var (css, tailwind) = PagesOf(slug);
        return styling == DemoSection.Tailwind ? tailwind : css;
    }

    /// <summary>
    /// A component's two pages. Named type by type rather than found by reflection, so the trimmer keeps
    /// every one and a renamed page is a build error rather than a missing edit link.
    /// </summary>
    private static (Type Css, Type Tailwind) PagesOf(string slug) => slug switch
    {
        "accessibility" => (typeof(Css.AccessibilityPage), typeof(Tw.AccessibilityPage)),
        "accordion" => (typeof(Css.AccordionPage), typeof(Tw.AccordionPage)),
        "alert-dialog" => (typeof(Css.AlertDialogPage), typeof(Tw.AlertDialogPage)),
        "angle-slider" => (typeof(Css.AngleSliderPage), typeof(Tw.AngleSliderPage)),
        "aspect-ratio" => (typeof(Css.AspectRatioPage), typeof(Tw.AspectRatioPage)),
        "avatar" => (typeof(Css.AvatarPage), typeof(Tw.AvatarPage)),
        "breadcrumbs" => (typeof(Css.BreadcrumbsPage), typeof(Tw.BreadcrumbsPage)),
        "button" => (typeof(Css.ButtonPage), typeof(Tw.ButtonPage)),
        "calendar" => (typeof(Css.CalendarPage), typeof(Tw.CalendarPage)),
        "carousel" => (typeof(Css.CarouselPage), typeof(Tw.CarouselPage)),
        "checkbox" => (typeof(Css.CheckboxPage), typeof(Tw.CheckboxPage)),
        "clipboard" => (typeof(Css.ClipboardPage), typeof(Tw.ClipboardPage)),
        "close-button" => (typeof(Css.CloseButtonPage), typeof(Tw.CloseButtonPage)),
        "collapsible" => (typeof(Css.CollapsiblePage), typeof(Tw.CollapsiblePage)),
        "color-picker" => (typeof(Css.ColorPickerPage), typeof(Tw.ColorPickerPage)),
        "combobox" => (typeof(Css.ComboboxPage), typeof(Tw.ComboboxPage)),
        "command" => (typeof(Css.CommandPage), typeof(Tw.CommandPage)),
        "context-menu" => (typeof(Css.ContextMenuPage), typeof(Tw.ContextMenuPage)),
        "data-interactive" => (typeof(Css.DataInteractivePage), typeof(Tw.DataInteractivePage)),
        "date-field" => (typeof(Css.DateFieldPage), typeof(Tw.DateFieldPage)),
        "date-picker" => (typeof(Css.DatePickerPage), typeof(Tw.DatePickerPage)),
        "dialog" => (typeof(Css.DialogPage), typeof(Tw.DialogPage)),
        "disclosure" => (typeof(Css.DisclosurePage), typeof(Tw.DisclosurePage)),
        "drawer" => (typeof(Css.DrawerPage), typeof(Tw.DrawerPage)),
        "editable" => (typeof(Css.EditablePage), typeof(Tw.EditablePage)),
        "field" => (typeof(Css.FieldPage), typeof(Tw.FieldPage)),
        "fieldset" => (typeof(Css.FieldsetPage), typeof(Tw.FieldsetPage)),
        "file-upload" => (typeof(Css.FileUploadPage), typeof(Tw.FileUploadPage)),
        "focus-trap" => (typeof(Css.FocusTrapPage), typeof(Tw.FocusTrapPage)),
        "grid" => (typeof(Css.GridPage), typeof(Tw.GridPage)),
        "hover-card" => (typeof(Css.HoverCardPage), typeof(Tw.HoverCardPage)),
        "listbox" => (typeof(Css.ListboxPage), typeof(Tw.ListboxPage)),
        "menu" => (typeof(Css.MenuPage), typeof(Tw.MenuPage)),
        "menubar" => (typeof(Css.MenubarPage), typeof(Tw.MenubarPage)),
        "navigation-menu" => (typeof(Css.NavigationMenuPage), typeof(Tw.NavigationMenuPage)),
        "number-field" => (typeof(Css.NumberFieldPage), typeof(Tw.NumberFieldPage)),
        "pagination" => (typeof(Css.PaginationPage), typeof(Tw.PaginationPage)),
        "password-field" => (typeof(Css.PasswordFieldPage), typeof(Tw.PasswordFieldPage)),
        "pin-input" => (typeof(Css.PinInputPage), typeof(Tw.PinInputPage)),
        "popover" => (typeof(Css.PopoverPage), typeof(Tw.PopoverPage)),
        "portal" => (typeof(Css.PortalPage), typeof(Tw.PortalPage)),
        "progress" => (typeof(Css.ProgressPage), typeof(Tw.ProgressPage)),
        "radiogroup" => (typeof(Css.RadioGroupPage), typeof(Tw.RadioGroupPage)),
        "rating" => (typeof(Css.RatingPage), typeof(Tw.RatingPage)),
        "scroll-area" => (typeof(Css.ScrollAreaPage), typeof(Tw.ScrollAreaPage)),
        "search-field" => (typeof(Css.SearchFieldPage), typeof(Tw.SearchFieldPage)),
        "slider" => (typeof(Css.SliderPage), typeof(Tw.SliderPage)),
        "sortable" => (typeof(Css.SortablePage), typeof(Tw.SortablePage)),
        "splitter" => (typeof(Css.SplitterPage), typeof(Tw.SplitterPage)),
        "stepper" => (typeof(Css.StepperPage), typeof(Tw.StepperPage)),
        "switch" => (typeof(Css.SwitchPage), typeof(Tw.SwitchPage)),
        "tabs" => (typeof(Css.TabsPage), typeof(Tw.TabsPage)),
        "tags-input" => (typeof(Css.TagsInputPage), typeof(Tw.TagsInputPage)),
        "toast" => (typeof(Css.ToastPage), typeof(Tw.ToastPage)),
        "toggle-group" => (typeof(Css.ToggleGroupPage), typeof(Tw.ToggleGroupPage)),
        "toolbar" => (typeof(Css.ToolbarPage), typeof(Tw.ToolbarPage)),
        "tooltip" => (typeof(Css.TooltipPage), typeof(Tw.TooltipPage)),
        "transition" => (typeof(Css.TransitionPage), typeof(Tw.TransitionPage)),
        "tree" => (typeof(Css.TreePage), typeof(Tw.TreePage)),
        _ => throw new InvalidOperationException($"DemoCatalog lists '{slug}', which has no pages here."),
    };
}
