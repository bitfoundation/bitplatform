namespace Bit.Bheadless.Demo.Client.Core.Shared;

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
/// What a component family is for, which is how the docs navigation and the MCP server's catalog group the
/// families.
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
/// The one list of what the site demonstrates: the docs navigation and the search palette are built from it
/// (DocsCatalog.cs), the E2E suite reads its slugs off this file (the first argument of each entry, so it stays
/// first), and the MCP server builds its catalog, its component families and its search index from it.
/// </para>
/// </summary>
public static class DemoCatalog
{
    /// <summary>
    /// One component family: its address (<c>/&lt;slug&gt;</c>, and <c>/tw/&lt;slug&gt;</c> in the Tailwind section),
    /// the name the navigation shows, its area, the one sentence the navigation and the search palette describe it
    /// with, and the extra words the palette finds it by.
    /// </summary>
    public sealed record Entry(string Slug, string Label, DemoArea Area, string Summary, string Keywords);

    public static readonly IReadOnlyList<Entry> Components =
    [
        new("accessibility", "Accessibility Utilities", DemoArea.AccessibilityUtilities,
            "Visually hidden text, separators, live regions, announcements, hotkeys, focus scopes, reading direction and roving focus.",
            "BitBhVisuallyHidden BitBhSeparator BitBhLiveRegion IBitBhAnnouncer BitBhHotkey BitBhFocusScope BitBhDirectionProvider BitBhRovingFocusGroup announce screen reader shortcut rtl"),
        new("accordion", "Accordion", DemoArea.Disclosure,
            "A vertically stacked set of collapsible sections, single or multiple open at a time.",
            "BitBhAccordion BitBhAccordionItem BitBhAccordionTrigger BitBhAccordionContent expand collapse faq"),
        new("alert-dialog", "Alert Dialog", DemoArea.Overlays,
            "A modal dialog that interrupts with an important message and expects a deliberate response.",
            "BitBhAlertDialog BitBhAlertDialogAction BitBhAlertDialogCancel confirm destructive modal"),
        new("aspect-ratio", "Aspect Ratio", DemoArea.LayoutAndFeedback,
            "Displays content within a desired ratio, regardless of its intrinsic size.",
            "BitBhAspectRatio ratio image video embed 16:9"),
        new("avatar", "Avatar", DemoArea.LayoutAndFeedback,
            "An image with a graceful fallback, for representing a user or an entity.",
            "BitBhAvatar BitBhAvatarImage BitBhAvatarFallback initials profile picture"),
        new("button", "Button", DemoArea.LayoutAndFeedback,
            "A polymorphic, accessible button with loading and disabled states.",
            "BitBhButton As polymorphic loading disabled data-loading"),
        new("checkbox", "Checkbox", DemoArea.FormControls,
            "A tri-state checkbox with full keyboard support, groups and select-all.",
            "BitBhCheckbox BitBhCheckboxGroup BitBhCheckboxIndicator indeterminate tri-state select all"),
        new("close-button", "CloseButton", DemoArea.LayoutAndFeedback,
            "A button pre-wired to close the nearest Dialog, Popover or Disclosure.",
            "BitBhCloseButton dismiss close dialog popover disclosure"),
        new("collapsible", "Collapsible", DemoArea.Disclosure,
            "An interactive component which expands and collapses a panel.",
            "BitBhCollapsible BitBhCollapsibleTrigger BitBhCollapsibleContent expand collapse"),
        new("combobox", "Combobox", DemoArea.Selection,
            "A typeable autocomplete with filtering you control, single or multiple, virtualized.",
            "BitBhCombobox BitBhComboboxInput BitBhComboboxOptions BitBhComboboxOption autocomplete typeahead search filter multi"),
        new("command", "Command Palette", DemoArea.Selection,
            "A cmdk-style command menu: fuzzy search, groups, nested pages and a Ctrl+K palette.",
            "BitBhCommand BitBhCommandDialog BitBhCommandInput BitBhCommandItem cmdk command palette fuzzy ctrl+k mod+k"),
        new("context-menu", "Context Menu", DemoArea.Menus,
            "A menu at the pointer, opened with a right click, with typeahead and collision handling.",
            "BitBhContextMenu BitBhContextMenuTrigger BitBhContextMenuContent BitBhContextMenuItem right click"),
        new("data-interactive", "DataInteractive", DemoArea.AccessibilityUtilities,
            "Forwards data-hover, data-active and data-focus to its child, for styling interaction without pseudo-classes.",
            "BitBhDataInteractive data-hover data-active data-focus hover pressed"),
        new("dialog", "Dialog", DemoArea.Overlays,
            "A modal with a focus trap, scroll lock and an inert background.",
            "BitBhDialog BitBhDialogPanel BitBhDialogTitle BitBhDialogBackdrop modal inert focus trap scroll lock"),
        new("disclosure", "Disclosure", DemoArea.Disclosure,
            "A single show/hide region toggled by a button - the simplest collapsible.",
            "BitBhDisclosure BitBhDisclosureButton BitBhDisclosurePanel show hide toggle"),
        new("field", "Field / Input", DemoArea.FormControls,
            "Form controls with their labels, descriptions and validation state wired up for you.",
            "BitBhField BitBhLabel BitBhDescription BitBhErrorMessage BitBhInput BitBhSelect BitBhTextarea EditForm validation form"),
        new("fieldset", "Fieldset", DemoArea.FormControls,
            "Groups form controls with an accessible, easy-to-style fieldset and legend.",
            "BitBhFieldset BitBhLegend group form disabled"),
        new("focus-trap", "FocusTrap", DemoArea.AccessibilityUtilities,
            "Traps keyboard focus inside a container - the building block of modal patterns.",
            "BitBhFocusTrap BitBhFocusScope focus trap restore tab"),
        new("grid", "Grid / TreeGrid", DemoArea.HierarchiesAndData,
            "A two-dimensional widget with roving cell focus, row selection, sorting and expandable rows.",
            "BitBhGrid BitBhGridRow BitBhGridCell BitBhGridRowToggle data grid treegrid table spreadsheet cells"),
        new("hover-card", "Hover Card", DemoArea.Overlays,
            "Lets sighted users preview the content behind a link.",
            "BitBhHoverCard BitBhHoverCardTrigger BitBhHoverCardContent preview link"),
        new("listbox", "Listbox", DemoArea.Selection,
            "A custom select with single or multiple selection, keyboard navigation and typeahead.",
            "BitBhListbox BitBhListboxButton BitBhListboxOptions BitBhListboxOption select dropdown multi typeahead"),
        new("menu", "Menu", DemoArea.Menus,
            "A dropdown menu with keyboard navigation, typeahead, submenus and checkable items.",
            "BitBhMenu BitBhMenuButton BitBhMenuItems BitBhMenuItem dropdown submenu"),
        new("menubar", "Menubar", DemoArea.Menus,
            "A desktop-style application menu bar with submenus, checkable items and shortcuts.",
            "BitBhMenubar BitBhMenuSub BitBhMenuCheckboxItem BitBhMenuRadioGroup application menu bar"),
        new("popover", "Popover", DemoArea.Overlays,
            "A non-modal floating panel with focus management, Escape to close and group coordination.",
            "BitBhPopover BitBhPopoverButton BitBhPopoverPanel BitBhPopoverGroup floating panel anchor"),
        new("portal", "Portal", DemoArea.Overlays,
            "Renders its content into another part of the DOM, out of overflow and stacking contexts.",
            "BitBhPortal BitBhPortalOutlet BitBhPortalGroup teleport overflow z-index"),
        new("progress", "Progress / Meter", DemoArea.LayoutAndFeedback,
            "A progress bar for task completion, or a meter for a measurement within a known range.",
            "BitBhProgress BitBhProgressIndicator BitBhMeter loading percentage gauge"),
        new("radiogroup", "Radio Group", DemoArea.FormControls,
            "A single-select group with roving tabindex and full keyboard support.",
            "BitBhRadioGroup BitBhRadio radio options roving"),
        new("slider", "Slider", DemoArea.FormControls,
            "Picks a value - or a range, with several thumbs - by pointer or keyboard.",
            "BitBhSlider BitBhSliderTrack BitBhSliderRange BitBhSliderThumb range vertical"),
        new("sortable", "Sortable", DemoArea.HierarchiesAndData,
            "Reorders a list with a pointer or the keyboard, with live screen-reader announcements.",
            "BitBhSortable BitBhSortableHandle reorder drag drop"),
        new("switch", "Switch", DemoArea.FormControls,
            "An on/off toggle with full keyboard support.",
            "BitBhSwitch BitBhSwitchThumb toggle on off"),
        new("tabs", "Tabs", DemoArea.Disclosure,
            "A tab group with roving tabindex and full keyboard navigation.",
            "BitBhTabGroup BitBhTabList BitBhTab BitBhTabPanels BitBhTabPanel tablist"),
        new("toast", "Toast", DemoArea.LayoutAndFeedback,
            "Short, non-blocking notifications - queued from anywhere, announced, dismissed automatically.",
            "BitBhToaster BitBhToastViewport BitBhToast notification snackbar"),
        new("toggle-group", "Toggle / Toggle Group", DemoArea.FormControls,
            "Two-state buttons on their own, or grouped for formatting, alignment and view switchers.",
            "BitBhToggle BitBhToggleGroup BitBhToggleGroupItem pressed aria-pressed"),
        new("toolbar", "Toolbar", DemoArea.LayoutAndFeedback,
            "A row of buttons, toggles and links that is a single tab stop with arrow-key navigation.",
            "BitBhToolbar BitBhToolbarButton BitBhToolbarLink BitBhToolbarSeparator"),
        new("tooltip", "Tooltip", DemoArea.Overlays,
            "A small popup that describes a control on hover or focus.",
            "BitBhTooltip BitBhTooltipTrigger BitBhTooltipContent BitBhTooltipProvider hint"),
        new("transition", "Transition", DemoArea.AccessibilityUtilities,
            "Class-based enter and leave animations with their lifecycle managed for you.",
            "BitBhTransition animation enter leave fade data-transition"),
        new("tree", "Tree View", DemoArea.HierarchiesAndData,
            "A hierarchical list with expand and collapse, single, multiple and checkbox selection and lazy loading.",
            "BitBhTree BitBhTreeItem BitBhTreeGroup BitBhTreeItemToggle file explorer hierarchy nested"),
    ];

    public static string Href(DemoSection section, string slug) =>
        section == DemoSection.Tailwind ? $"tw/{slug}" : slug;

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
