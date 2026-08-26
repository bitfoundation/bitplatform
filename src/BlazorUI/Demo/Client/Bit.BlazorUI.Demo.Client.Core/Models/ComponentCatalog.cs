using Bit.BlazorUI.Demo.Client.Core.Shared;

namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// The one list of components the documentation site works from: the components gallery, the home
/// page's index and the prev/next pager at the foot of every demo page all read it.
/// <para>
/// It is DERIVED from <see cref="MainLayout.NavItems"/> rather than written out a second time. The
/// nav is already the authority on which components exist, what they are called and where they
/// live, and the old home page kept a hand-maintained copy of the same list - which is exactly the
/// kind of duplicate that drifts the moment a component is added. All this file adds is the part
/// the nav has no room for: a one-line summary and a category glyph.
/// </para>
/// </summary>
public static class ComponentCatalog
{
    /// <summary>Every documented component, in nav order.</summary>
    public static IReadOnlyList<ComponentCatalogItem> Items { get; }

    /// <summary>The same components grouped the way the nav groups them.</summary>
    public static IReadOnlyList<ComponentCatalogCategory> Categories { get; }

    private static readonly Dictionary<string, int> _indexByUrl;


    static ComponentCatalog()
    {
        var categories = new List<ComponentCatalogCategory>();

        foreach (var navItem in MainLayout.NavItems)
        {
            if (navItem.ChildItems is null || navItem.ChildItems.Count == 0) continue;

            AddCategory(categories, navItem.Text!, navItem.ChildItems);
        }

        Categories = categories;
        Items = [.. categories.SelectMany(c => c.Items)];

        // Position rather than the item itself, because the pager needs the neighbours too. Last
        // one wins is fine: no two nav entries share a Url, and a hypothetical duplicate would only
        // ever be the same page.
        _indexByUrl = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Items.Count; i++)
        {
            _indexByUrl[Items[i].Url] = i;
        }
    }


    /// <summary>
    /// The catalog entry for a demo page's route, or null when the route is not a component page
    /// (the prose pages, or a component whose nav entry was removed).
    /// </summary>
    public static ComponentCatalogItem? Find(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        return IndexOf(url) is int index ? Items[index] : null;
    }

    /// <summary>
    /// The entries either side of a page in the flattened catalog, which is what the pager at the
    /// foot of a demo page offers as "previous" and "next". Both ends are null-safe: the first page
    /// has no previous and the last has no next.
    /// </summary>
    public static (ComponentCatalogItem? Previous, ComponentCatalogItem? Next) Neighbors(string? url)
    {
        if (IndexOf(url) is not int index) return (null, null);

        return (index > 0 ? Items[index - 1] : null,
                index < Items.Count - 1 ? Items[index + 1] : null);
    }

    private static int? IndexOf(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        return _indexByUrl.TryGetValue(Normalize(url), out var index) ? index : null;
    }


    private static void AddCategory(List<ComponentCatalogCategory> categories, string name, IList<BitNavItem> navItems)
    {
        var items = new List<ComponentCatalogItem>();
        var nested = new List<BitNavItem>();

        foreach (var navItem in navItems)
        {
            // A group nested inside a group - "Pickers" under "Inputs" - is a category in its own
            // right in the gallery, where there is no room for two levels of nesting. It is held
            // back until this category has been added, so it lands after its parent rather than
            // before it: the nav lists Pickers last among the Inputs, and recursing here would put
            // the whole Pickers category ahead of the Inputs one.
            if (navItem.ChildItems?.Count > 0)
            {
                nested.Add(navItem);
                continue;
            }

            // The Theming group mixes the theming guide in with its two components. Only pages under
            // /components are components.
            if (navItem.Url?.StartsWith("/components/", StringComparison.OrdinalIgnoreCase) is not true) continue;

            var itemName = navItem.Text!;
            var summary = _summaries.GetValueOrDefault(itemName, string.Empty);

            items.Add(new ComponentCatalogItem
            {
                Name = itemName,
                Url = navItem.Url!,
                Category = name,
                Aliases = navItem.Description,
                Summary = summary,
                // Built once here rather than on every keystroke of the gallery's search box: the
                // catalog is ~110 items and the box filters on every character.
                SearchText = $"{itemName} {name} {navItem.Description} {navItem.Data} {summary}".ToLowerInvariant()
            });
        }

        if (items.Count > 0)
        {
            var (icon, blurb) = _categoryMeta.GetValueOrDefault(name, (BitIconName.Puzzle, string.Empty));

            categories.Add(new ComponentCatalogCategory
            {
                Name = name,
                IconName = icon,
                Summary = blurb,
                Items = items
            });
        }

        foreach (var navItem in nested)
        {
            AddCategory(categories, navItem.Text!, navItem.ChildItems);
        }
    }

    private static string Normalize(string url)
    {
        // The pager and the gallery compare routes, and a route can arrive with the origin attached
        // (NavigationManager.Uri), with a trailing slash, or with a fragment.
        var value = url.Trim();

        var hashIndex = value.IndexOf('#');
        if (hashIndex >= 0) value = value[..hashIndex];

        var queryIndex = value.IndexOf('?');
        if (queryIndex >= 0) value = value[..queryIndex];

        value = value.TrimEnd('/');

        var componentsIndex = value.IndexOf("/components/", StringComparison.OrdinalIgnoreCase);

        return componentsIndex >= 0 ? value[componentsIndex..] : value;
    }


    private static readonly Dictionary<string, (string Icon, string Summary)> _categoryMeta = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Buttons"] = (BitIconName.ButtonControl, "Trigger actions, open menus, and hold a pressed state."),
        ["Inputs"] = (BitIconName.TextField, "Collect and validate everything a form needs."),
        ["Pickers"] = (BitIconName.DateTime, "Drop-down controls for dates, times, and colors."),
        ["Layouts"] = (BitIconName.GridViewMedium, "Structure a page: headers, footers, grids, and stacks."),
        ["Lists"] = (BitIconName.CustomList, "Render collections as lists, carousels, and timelines."),
        ["Navs"] = (BitIconName.GlobalNavButton, "Move people around an app and show them where they are."),
        ["Notifications"] = (BitIconName.Ringer, "Tell people what happened, and how important it was."),
        ["Progress"] = (BitIconName.ProgressLoopOuter, "Show that work is happening, and how far along it is."),
        ["Surfaces"] = (BitIconName.Tiles, "Containers that hold content: cards, dialogs, panels, popovers."),
        ["Utilities"] = (BitIconName.Toolbox, "Small building blocks the other components are made of."),
        ["Extras"] = (BitIconName.Puzzle, "Larger components that ship in the separate Extras package."),
        ["Legacy"] = (BitIconName.History, "Previous implementations, kept unchanged for backward compatibility."),
        ["Theming"] = (BitIconName.Color, "Ready-made chrome for switching design system, scheme, and accent."),
    };

    // One line per component: what it is for, not what it can do. The demo page's own Description
    // carries the full paragraph; this is the version that has to fit on a card next to a hundred
    // others.
    private static readonly Dictionary<string, string> _summaries = new(StringComparer.OrdinalIgnoreCase)
    {
        // Buttons
        ["ActionButton"] = "An icon-first, transparent button for inline actions like New, Edit or Share.",
        ["Button"] = "The primary way to trigger an action, in fill, outline and text variants.",
        ["ButtonGroup"] = "Joins related buttons into one unit, optionally single- or multi-select.",
        ["MenuButton"] = "A button that opens a menu of related actions, with split and sticky modes.",
        ["ToggleButton"] = "A button that stays pressed and reports its state through aria-pressed.",

        // Inputs
        ["Calendar"] = "A full inline calendar for browsing and picking a date, with optional time.",
        ["Checkbox"] = "A yes/no box with a label, and an indeterminate state for partial selections.",
        ["ChoiceGroup"] = "Radio buttons: pick exactly one value from a set of mutually exclusive options.",
        ["Dropdown"] = "Single or multi select from a list, with search, chips, grouping and virtualization.",
        ["FileInput"] = "The selection half of file handling: browse, drag and drop, capture from camera.",
        ["FileUpload"] = "The transport half: chunked, resumable uploads with per-file progress.",
        ["NumberField"] = "A text field dedicated to numbers, with steppers, bounds and formatting.",
        ["OtpInput"] = "The row of boxes a one-time passcode is typed into, with paste and auto-advance.",
        ["Rating"] = "Turns an opinion into a row of stars: hover to preview, click to commit.",
        ["SearchBox"] = "An input for searching, with suggestions, debouncing and a clear button.",
        ["Slider"] = "Turns a number into a distance you can point at, single value or range.",
        ["TagsInput"] = "Turns free text into a list of short values confirmed with Enter.",
        ["TextField"] = "The way people enter and edit text, single line or multiline.",
        ["Toggle"] = "A switch for a setting that takes effect immediately.",

        // Pickers
        ["CircularTimePicker"] = "Picks a time from an analog clock dial the pointer snaps to.",
        ["ColorPicker"] = "The panel a color is chosen on: shade, hue, alpha and a live preview.",
        ["DatePicker"] = "A drop-down calendar for a single date, with a typeable text field.",
        ["DateRangePicker"] = "A drop-down calendar for a start and end date, with optional times.",
        ["TimePicker"] = "A drop-down for an hour and minute value, with a typeable text field.",

        // Layouts
        ["Footer"] = "The bar at the bottom of a site or an application.",
        ["Grid"] = "Divides the available width into equal columns and lays its items out on them.",
        ["Header"] = "The bar at the top of a site or an application.",
        ["Layout"] = "The base structure of an app: header, footer, nav panel and a scrolling middle.",
        ["Spacer"] = "Pushes flex siblings apart, absorbing whatever space is left over.",
        ["Stack"] = "A flexbox container that expresses direction, alignment and gap as parameters.",

        // Lists
        ["BasicList"] = "Renders a list of items into a scrolling region, one template per item.",
        ["Carousel"] = "A slideshow of items across sliding pages, with autoplay and indicators.",
        ["Swiper"] = "A touch slider: a swiping row of items with momentum and snapping.",
        ["Timeline"] = "Events in chronological order along a vertical or horizontal line.",

        // Navs
        ["Breadcrumb"] = "Shows where a page sits in the hierarchy, and the way back up.",
        ["DropMenu"] = "A button that opens a callout hosting any content: a list, a form, a filter.",
        ["Nav"] = "Links to the main areas of an app, and a tree view for hierarchical data.",
        ["NavBar"] = "A bar of navigation links to the main areas of an app, the way a mobile app puts its top-level destinations along the bottom of the screen.",
        ["Pagination"] = "Moves between pages of a long collection.",
        ["Pivot"] = "Tabs for switching between frequently accessed, distinct content categories.",

        // Notifications
        ["Badge"] = "A small marker on another element: a count, a dot, a status.",
        ["Message"] = "An inline bar for an error, a warning, or something worth knowing.",
        ["Persona"] = "A person: their avatar, name, secondary text and presence.",
        ["SnackBar"] = "A brief, dismissible notification that stacks in a corner of the screen.",
        ["Tag"] = "A compact chip for an attribute, a person or an asset - dismissible, selectable or clickable.",

        // Progress
        ["Loading"] = "Eighteen ready-made loading animations with one shared API.",
        ["Progress"] = "The completion status of an operation, determinate or indeterminate.",
        ["Shimmer"] = "A placeholder that stands in for content while it is being fetched.",

        // Surfaces
        ["Accordion"] = "Shows and hides one section of related content at a time.",
        ["Callout"] = "An anchored tip that teaches or guides without blocking the app.",
        ["Card"] = "A surface that wraps one subject, with a cover, a header, a body and a footer of its own.",
        ["Collapse"] = "Animates a block of content open and shut.",
        ["Dialog"] = "A temporary pop-up that takes focus and asks for a decision.",
        ["Modal"] = "A full overlay for content that has to be dealt with before anything else.",
        ["ModalService"] = "Opens modals from anywhere in the app, with any content.",
        ["Panel"] = "A sheet that slides in from an edge for supplementary content.",
        ["ScrollablePane"] = "A scrolling region with themed scrollbars and scroll helpers.",
        ["Splitter"] = "Divides a container into two sections the reader can resize.",
        ["Tooltip"] = "A short description that appears on hover or focus.",

        // Utilities
        ["CascadingValueProvider"] = "Cascades several values to child components without nesting providers.",
        ["Element"] = "Renders any HTML tag with the library's own styling parameters.",
        ["Icon"] = "Renders a Fabric glyph, or an icon from any other set you point it at.",
        ["Image"] = "An image with cover modes, loading states and a fallback.",
        ["Label"] = "Gives a name to a control or a group of controls.",
        ["Link"] = "Navigates elsewhere, inside the app or out of it.",
        ["MediaQuery"] = "Reports the library's breakpoints to your component as a parameter.",
        ["Overlay"] = "Dims everything behind a piece of UI to put the emphasis on it.",
        ["Params"] = "Cascades shared parameter objects so components inherit common defaults.",
        ["PullToRefresh"] = "Adds pull-down-to-refresh to a page or a scrolling element.",
        ["Separator"] = "Visually divides content into groups, with an optional label.",
        ["Sticky"] = "Pins an element in place while the rest of the page scrolls past.",
        ["SwipeTrap"] = "Traps swipe gestures on an element and reports them as events.",
        ["Text"] = "Applies the theme's typography ramp to a run of text.",

        // Extras
        ["AccordionList"] = "An accordion that builds its expandable items from a single collection.",
        ["AppShell"] = "The cross-platform application container: safe areas, scroll state, chrome.",
        ["Chart"] = "A native Blazor charting component rendered entirely with SVG.",
        ["DataGrid"] = "An information-rich grid with sorting, filtering, paging and virtualization.",
        ["ErrorBoundary"] = "Catches exceptions thrown by its children and renders a fallback.",
        ["Flag"] = "Renders the flag of a country from its code.",
        ["FullCalendar"] = "A scheduler with day, week, month, year, agenda and resource views.",
        ["InfiniteScrolling"] = "Loads the next page of a list as the reader reaches the bottom.",
        ["Map"] = "An interactive map with pluggable providers, markers, vectors and GeoJSON.",
        ["MarkdownEditor"] = "A native markdown editor with a toolbar, shortcuts and smart lists.",
        ["MarkdownViewer"] = "Renders Markdown to HTML entirely in C#, so it survives prerendering.",
        ["MessageBox"] = "A ready-made box for showing a message with a title and body.",
        ["NavPanel"] = "A vertical navigation panel with search, grouping and a collapsed rail.",
        ["PdfViewer"] = "A pure-C# PDF viewer: no pdf.js, works in every render mode.",
        ["PhoneInput"] = "A phone number field with a searchable country selector and flags.",
        ["ProModal"] = "The modal, extended: draggable, resizable, stackable, with a header slot.",
        ["ProModalService"] = "Opens ProModals from anywhere in the app, with any content.",
        ["ProPanel"] = "The panel, extended: modeless, full-screen, and scroll-aware modes.",
        ["RichTextEditor"] = "A native WYSIWYG editor written in C#, with a configurable toolbar.",
        ["TextShimmer"] = "A gradient band sweeping across text, for AI-style streaming states.",
        ["Virtualize"] = "Renders only the rows currently in view, for very long lists.",

        // Legacy
        ["ChartLegacy"] = "The original Chart.js based charting component.",
        ["DataGridLegacy"] = "The original data grid, previously named BitQuickGrid.",
        ["MarkdownEditorLegacy"] = "The original markdown editor.",
        ["MarkdownViewerLegacy"] = "The original marked.js based markdown viewer.",
        ["PdfReaderLegacy"] = "The original pdf.js based PDF reader.",
        ["RichTextEditorLegacy"] = "The original Quill based rich text editor.",

        // Theming
        ["AccentColorSwitcher"] = "Swatches that re-derive the whole palette from one brand color.",
        ["ThemeSwitcher"] = "Ready-made chrome for picking a design system and a light/dark scheme.",
    };
}
