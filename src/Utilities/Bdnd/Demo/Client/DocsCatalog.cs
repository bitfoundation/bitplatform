using Microsoft.AspNetCore.Components;

namespace Bit.Bdnd.Demo.Client;

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
    public const string Name = "Bdnd";

    public const string PackageId = "Bit.Bdnd";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bdnd";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bdnd is a declarative drag-and-drop library for Blazor inspired by React DnD: draggables, drop " +
        "targets, custom drag layers and drag handles on the browser's HTML5 drag-and-drop API, with the drag " +
        "state in C#. This site is itself a Blazor app built on it.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["dustbin", "sortable", "handle", "nesting", "files"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

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
    /// path below Demo/Client is the type's namespace past this assembly's root, one folder a segment -
    /// Pages.Dustbin.SingleTarget is Pages/Dustbin/SingleTarget.razor.
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
    // The groups are React DnD's own example groups, in its order, so a React DnD user finds each
    // example where they would look for it on the other side.
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("/getting-started", "Getting started",
                "Install the package, register the service, and put a first draggable and drop target inside a BdndProvider.",
                "install setup nuget package AddBitBdndServices quick start program.cs provider BdndProvider concepts items types monitor",
                typeof(Pages.GettingStarted)),
            new("/tutorial", "Chessboard tutorial",
                "React DnD's canonical tutorial: a knight that may only be dropped on the squares a knight can move to.",
                "tutorial chess chessboard knight CanDropPredicate legal moves highlight CanDrop IsOver",
                typeof(Pages.Tutorial)),
        ]),
        new("Dustbin",
        [
            new("/dustbin/single", "Single target",
                "The simplest setup: one drop target that accepts one item type, reacts to the hover and records the drop.",
                "dustbin single target Accept OnDrop IsOver basic first drop",
                typeof(Pages.Dustbin.SingleTarget)),
            new("/dustbin/iframe", "Within an iframe",
                "The same dustbin running in a document of its own, inside an iframe.",
                "iframe frame document window embedded",
                typeof(Pages.Dustbin.Iframe)),
            new("/dustbin/copy-or-move", "Copy or move",
                "A source that is removed once a drop took it, next to one that stays - OnEnd and DidDrop, with the matching DropEffect.",
                "copy move OnEnd DidDrop DropEffect remove palette keep take",
                typeof(Pages.Dustbin.CopyOrMove)),
            new("/dustbin/multiple", "Multiple targets",
                "Dustbins that accept different types: a box lights up only the targets that can take it.",
                "multiple targets AcceptTypes types CanDrop highlight dim",
                typeof(Pages.Dustbin.MultipleTargets)),
            new("/dustbin/stress-test", "Stress test",
                "Thirty sources and six targets of random types, re-rendering on every change of the drag state.",
                "stress test performance many sources targets random types shuffle",
                typeof(Pages.Dustbin.StressTest)),
        ]),
        new("Drag around",
        [
            new("/drag-around/naive", "Naive",
                "Boxes moved by the distance the pointer travelled, from Monitor.GetDifferenceFromInitialOffset(), with the browser's own drag image.",
                "drag around position move offset GetDifferenceFromInitialOffset absolute native preview",
                typeof(Pages.DragAround.Naive)),
            new("/drag-around/custom-layer", "Custom drag layer",
                "UseCustomDragLayer hides the native preview and a BdndDragLayer draws one of its own that follows the pointer.",
                "custom drag layer preview BdndDragLayer UseCustomDragLayer SourceClientOffset GetSourceClientOffset",
                typeof(Pages.DragAround.CustomLayer)),
        ]),
        new("Nesting",
        [
            new("/nesting/drag-sources", "Drag sources",
                "Draggables inside draggables: grabbing an inner box drags that box, not the ones around it.",
                "nested drag sources innermost inner outer dragstart",
                typeof(Pages.Nesting.DragSources)),
            new("/nesting/drop-targets", "Drop targets",
                "Drop targets inside drop targets: IsOver for every target under the pointer, IsOverShallow for the innermost one.",
                "nested drop targets IsOverShallow shallow IsOver innermost hover",
                typeof(Pages.Nesting.DropTargets)),
            new("/nesting/tree", "Tree",
                "A folder tree: nodes go before or after a node (its edges) or into it (its children list), never into their own subtree - by mouse, touch or keyboard.",
                "tree outline folders hierarchy nested drop targets Edges nest child parent subtree CanDropPredicate BdndList recursive",
                typeof(Pages.Nesting.Tree)),
        ]),
        new("Sortable",
        [
            new("/sortable/simple", "Simple",
                "Cards that are each a draggable and a drop target, reordered from OnHover as the dragged card passes their middle.",
                "sortable list reorder OnHover cards midpoint GetBoundingClientRectAsync",
                typeof(Pages.Sortable.Simple)),
            new("/sortable/cancel-on-drop-outside", "Cancel on drop outside",
                "The order is kept only when the card is dropped on the list; anywhere else it snaps back.",
                "cancel drop outside revert snap back OnEnd DidDrop sortable",
                typeof(Pages.Sortable.CancelOnDropOutside)),
            new("/sortable/stress-test", "Stress test",
                "A hundred-card sortable list, hit-testing every hover.",
                "sortable stress test performance long list hundred",
                typeof(Pages.Sortable.StressTest)),
            new("/sortable/indicators", "Drop indicators",
                "Two lists whose cards report the edge a drag is closest to: a line shows where the card lands, and BdndList moves it there - within a list or across.",
                "sortable drop indicator line Edges ClosestEdge data-bit-bdnd-edge BdndList InsertionIndex Move insertion before after reorder two lists cross list keyboard",
                typeof(Pages.Sortable.Indicators)),
            new("/sortable/grid", "Grid",
                "Tiles in a wrapping grid that land left or right of the tile they are dropped on; with the keyboard, Up and Down go a row up or down.",
                "sortable grid wrap wrapping tiles gallery photos Edges Horizontal left right keyboard rows arrows BdndList",
                typeof(Pages.Sortable.Grid)),
            new("/sortable/virtualized", "Virtualized list",
                "A thousand rows in Virtualize, dragged with the pointer or the keyboard: the list scrolls under the drag and the rows rendered as it scrolls take it.",
                "sortable virtualize virtualized virtualization long list thousand rows performance auto scroll AutoScroll keyboard PointerDrag",
                typeof(Pages.Sortable.Virtualized)),
            new("/sortable/multi-select", "Dragging several items",
                "Tick some cards and drag one: the whole selection travels as one item - a list - and lands in the archive together, with the mouse or the keyboard.",
                "multi select multiple selection several items many cards ItemFactory IsDraggingOverride list collection batch archive",
                typeof(Pages.Sortable.MultiSelect)),
        ]),
        new("Customize",
        [
            new("/customize/handles", "Handles & previews",
                "UseHandle and BdndDragHandle: a box that drags only from its grip, next to one that drags from anywhere.",
                "handle grip BdndDragHandle UseHandle drag handle interactive",
                typeof(Pages.Customize.Handles)),
            new("/customize/drop-effects", "Drop effects",
                "DropEffect sets the native effectAllowed, which is the cursor the browser shows over a target.",
                "drop effect DropEffect effectAllowed copy move link cursor",
                typeof(Pages.Customize.DropEffects)),
            new("/customize/pointer", "Touch and pointer drags",
                "Touch and pen drag through pointer events; PointerDrag=\"All\" drags with the mouse the same way - a copy following the pointer, Escape to cancel, and a tray that scrolls near its edges.",
                "touch mobile pen pointer PointerDrag TouchDelay DragThreshold AutoScroll auto scroll preview data-bit-bdnd-preview no-drag data-bit-bdnd-no-drag phone tablet",
                typeof(Pages.Customize.Pointer)),
        ]),
        new("Other cases",
        [
            new("/other/native-files", "Native files",
                "A drop target that accepts files dragged in from the operating system, with BdndNativeTypes.File and OnNativeDrop.",
                "native files file drop desktop os BdndNativeTypes OnNativeDrop upload",
                typeof(Pages.Other.NativeFiles)),
            new("/other/native-html", "Native HTML",
                "Selected text, links and HTML dropped from a page, read from the native DataTransfer.",
                "native html text url link selection BdndNativeTypes DataTransfer",
                typeof(Pages.Other.NativeHtml)),
            new("/other/page-builder", "Page builder",
                "A palette whose blocks are copied onto a page where the line shows, blocks moved within the page or into a bin that takes no palette block - with the mouse or the keyboard.",
                "page builder dashboard palette widgets blocks copy move bin trash canvas CanDropPredicate DropEffect insertion",
                typeof(Pages.Other.PageBuilder)),
        ]),
        new("Accessibility",
        [
            new("/accessibility/keyboard", "Keyboard and screen readers",
                "A kanban board dragged with the keyboard: Space picks a card up, the arrows move it, Space drops it - with live-region announcements and move buttons as the non-drag alternative.",
                "keyboard accessibility a11y screen reader announcements live region aria Label AllowKeyboard WCAG 2.5.7 kanban board focus data-bit-bdnd",
                typeof(Pages.Accessibility.Keyboard)),
        ]),
        new("Hosting",
        [
            new("/hosting/server-check", "Blazor Server check",
                "The one page of this site served with InteractiveServer: mouse, keyboard and touch drags, a drag layer, insertion edges and file contents, all over a SignalR circuit.",
                "blazor server interactive server signalr circuit render mode hosting latency web app",
                typeof(Pages.Hosting.ServerCheck)),
        ]),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing.",
                "mcp model context protocol ai agent tools resources prompts claude copilot server",
                typeof(Pages.McpServer)),
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
