namespace Bit.BlazorHeadlessUI.Demo.Client.Core.Shared;

/// <summary>
/// Identifies which styling section a demo page belongs to.
/// Used to drive section-aware navigation and the section switcher.
/// </summary>
public enum DemoSection
{
    /// <summary>Hand-written CSS samples (lives at <c>/&lt;component&gt;</c>).</summary>
    Css,

    /// <summary>Tailwind CSS samples (lives at <c>/tw/&lt;component&gt;</c>).</summary>
    Tailwind,
}

/// <summary>
/// What a component family is for, which is how the MCP server's catalog groups the families.
/// </summary>
public enum DemoArea
{
    Overlays,
    Menus,
    Selection,
    HierarchiesAndData,
    Disclosure,
    FormControls,
    LayoutAndFeedback,
    AccessibilityUtilities,
}

/// <summary>
/// Static catalogue of demo components shared between both styling sections.
/// <para>
/// The one list of what the site demonstrates: the nav renders it, the E2E suite reads its slugs off
/// this file (the first argument of each entry, so it stays first), and the MCP server builds its
/// catalog, its component families and its search index from it.
/// </para>
/// </summary>
public static class DemoCatalog
{
    public sealed record Entry(string Slug, string Label, DemoArea Area);

    public static readonly IReadOnlyList<Entry> Components =
    [
        new("accessibility",   "Accessibility Utilities", DemoArea.AccessibilityUtilities),
        new("accordion",       "Accordion",               DemoArea.Disclosure),
        new("alert-dialog",    "Alert Dialog",            DemoArea.Overlays),
        new("aspect-ratio",    "Aspect Ratio",            DemoArea.LayoutAndFeedback),
        new("avatar",          "Avatar",                  DemoArea.LayoutAndFeedback),
        new("button",          "Button",                  DemoArea.LayoutAndFeedback),
        new("checkbox",        "Checkbox",                DemoArea.FormControls),
        new("close-button",    "CloseButton",             DemoArea.LayoutAndFeedback),
        new("collapsible",     "Collapsible",             DemoArea.Disclosure),
        new("combobox",        "Combobox",                DemoArea.Selection),
        new("command",         "Command Palette",         DemoArea.Selection),
        new("context-menu",    "Context Menu",            DemoArea.Menus),
        new("data-interactive", "DataInteractive",        DemoArea.AccessibilityUtilities),
        new("dialog",          "Dialog",                  DemoArea.Overlays),
        new("disclosure",      "Disclosure",              DemoArea.Disclosure),
        new("field",           "Field / Input",           DemoArea.FormControls),
        new("fieldset",        "Fieldset",                DemoArea.FormControls),
        new("focus-trap",      "FocusTrap",               DemoArea.AccessibilityUtilities),
        new("grid",            "Grid / TreeGrid",         DemoArea.HierarchiesAndData),
        new("hover-card",      "Hover Card",              DemoArea.Overlays),
        new("listbox",         "Listbox",                 DemoArea.Selection),
        new("menu",            "Menu",                    DemoArea.Menus),
        new("menubar",         "Menubar",                 DemoArea.Menus),
        new("popover",         "Popover",                 DemoArea.Overlays),
        new("portal",          "Portal",                  DemoArea.Overlays),
        new("progress",        "Progress / Meter",        DemoArea.LayoutAndFeedback),
        new("radiogroup",      "Radio Group",             DemoArea.FormControls),
        new("slider",          "Slider",                  DemoArea.FormControls),
        new("sortable",        "Sortable",                DemoArea.HierarchiesAndData),
        new("switch",          "Switch",                  DemoArea.FormControls),
        new("tabs",            "Tabs",                    DemoArea.Disclosure),
        new("toast",           "Toast",                   DemoArea.LayoutAndFeedback),
        new("toggle-group",    "Toggle / Toggle Group",   DemoArea.FormControls),
        new("toolbar",         "Toolbar",                 DemoArea.LayoutAndFeedback),
        new("tooltip",         "Tooltip",                 DemoArea.Overlays),
        new("transition",      "Transition",              DemoArea.AccessibilityUtilities),
        new("tree",            "Tree View",               DemoArea.HierarchiesAndData),
    ];

    public static string Href(DemoSection section, string slug) =>
        section == DemoSection.Tailwind ? $"tw/{slug}" : slug;

    public static string SectionRoot(DemoSection section) =>
        section == DemoSection.Tailwind ? "tw/" : "/";

    public static string AreaLabel(DemoArea area) => area switch
    {
        DemoArea.Overlays => "Overlays",
        DemoArea.Menus => "Menus",
        DemoArea.Selection => "Selection",
        DemoArea.HierarchiesAndData => "Hierarchies & data",
        DemoArea.Disclosure => "Disclosure",
        DemoArea.FormControls => "Form controls",
        DemoArea.LayoutAndFeedback => "Layout & feedback",
        DemoArea.AccessibilityUtilities => "Accessibility utilities",
        _ => area.ToString(),
    };
}
