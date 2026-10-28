using System.Collections.Frozen;
using System.Collections.Immutable;
using AngleSharp.Dom;

namespace Bit.BlazorHeadlessUI.Testing;

/// <summary>
/// WAI-ARIA 1.2 knowledge used by the audit: valid roles, implicit (HTML-AAM)
/// roles, which roles support which states / properties, value types, required
/// owned elements and required context roles. Every table is read-only.
/// </summary>
public static class BitBhAria
{
    /// <summary>Every non-abstract ARIA 1.2 role, plus the DPUB-ARIA 1.1 and Graphics-ARIA roles.</summary>
    public static readonly IReadOnlySet<string> ValidRoles = Set(
        "alert", "alertdialog", "application", "article", "banner", "blockquote", "button", "caption", "cell",
        "checkbox", "code", "columnheader", "combobox", "comment", "complementary", "contentinfo", "definition",
        "deletion", "dialog", "directory", "document", "emphasis", "feed", "figure", "form", "generic", "grid",
        "gridcell", "group", "heading", "img", "image", "insertion", "link", "list", "listbox", "listitem", "log", "main",
        "mark", "marquee", "math", "menu", "menubar", "menuitem", "menuitemcheckbox", "menuitemradio", "meter",
        "navigation", "none", "note", "option", "paragraph", "presentation", "progressbar", "radio", "radiogroup",
        "region", "row", "rowgroup", "rowheader", "scrollbar", "search", "searchbox", "separator", "slider",
        "spinbutton", "status", "strong", "subscript", "suggestion", "superscript", "switch", "tab", "table",
        "tablist", "tabpanel", "term", "textbox", "time", "timer", "toolbar", "tooltip", "tree", "treegrid",
        "treeitem",
        // Graphics-ARIA
        "graphics-document", "graphics-object", "graphics-symbol",
        // DPUB-ARIA 1.1
        "doc-abstract", "doc-acknowledgments", "doc-afterword", "doc-appendix", "doc-backlink", "doc-biblioentry",
        "doc-bibliography", "doc-biblioref", "doc-chapter", "doc-colophon", "doc-conclusion", "doc-cover", "doc-credit",
        "doc-credits", "doc-dedication", "doc-endnote", "doc-endnotes", "doc-epigraph", "doc-epilogue", "doc-errata",
        "doc-example", "doc-footnote", "doc-foreword", "doc-glossary", "doc-glossref", "doc-index", "doc-introduction",
        "doc-noteref", "doc-notice", "doc-pagebreak", "doc-pagefooter", "doc-pageheader", "doc-pagelist", "doc-part",
        "doc-preface", "doc-prologue", "doc-pullquote", "doc-qna", "doc-subtitle", "doc-tip", "doc-toc");

    /// <summary>Roles whose accessible name comes from their content.</summary>
    public static readonly IReadOnlySet<string> NameFromContent = Set(
        "button", "cell", "checkbox", "columnheader", "gridcell", "heading", "link", "menuitem", "menuitemcheckbox",
        "menuitemradio", "option", "radio", "row", "rowheader", "switch", "tab", "tooltip", "treeitem", "comment",
        "doc-backlink", "doc-biblioref", "doc-glossref", "doc-noteref");

    /// <summary>Roles that must have an accessible name (ARIA 1.2 "name required", plus the DPUB link roles).</summary>
    public static readonly IReadOnlySet<string> NameRequired = Set(
        "alertdialog", "application", "button", "checkbox", "columnheader", "combobox", "dialog", "grid", "heading", "img",
        "link", "listbox", "marquee", "menuitem", "menuitemcheckbox", "menuitemradio", "meter", "option", "progressbar",
        "radio", "radiogroup", "region", "rowheader", "searchbox", "slider", "spinbutton", "switch", "table", "tabpanel",
        "textbox", "tooltip", "tree", "treegrid", "treeitem",
        "doc-backlink", "doc-biblioref", "doc-glossref", "doc-noteref");

    /// <summary>Interactive widget roles (used by the nested-interactive and focus rules).</summary>
    public static readonly IReadOnlySet<string> Interactive = Set(
        "button", "checkbox", "combobox", "link", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "radio",
        "searchbox", "slider", "spinbutton", "switch", "tab", "textbox", "treeitem", "gridcell");

    /// <summary>Roles whose children are presentational (may not contain interactive content).</summary>
    public static readonly IReadOnlySet<string> ChildrenPresentational = Set(
        "button", "checkbox", "img", "menuitemcheckbox", "menuitemradio", "meter", "option", "progressbar", "radio",
        "scrollbar", "separator", "slider", "switch", "tab");

    /// <summary>
    /// The ARIA 1.2 global states and properties. Any of them on a presentational element makes
    /// browsers ignore the presentational role (presentational role conflict resolution).
    /// </summary>
    public static readonly IReadOnlySet<string> GlobalAttributes = Set(
        "aria-atomic", "aria-busy", "aria-controls", "aria-current", "aria-describedby", "aria-details", "aria-disabled",
        "aria-dropeffect", "aria-errormessage", "aria-flowto", "aria-grabbed", "aria-haspopup", "aria-hidden", "aria-invalid",
        "aria-keyshortcuts", "aria-label", "aria-labelledby", "aria-live", "aria-owns", "aria-relevant", "aria-roledescription");

    private static readonly string[] ActiveDescendantRoles = ["application", "combobox", "grid", "group", "listbox", "menu", "menubar", "radiogroup", "row", "searchbox", "spinbutton", "tablist", "textbox", "toolbar", "tree", "treegrid"];
    private static readonly string[] RangeRoles = ["meter", "progressbar", "scrollbar", "separator", "slider", "spinbutton"];

    /// <summary>States / properties that are only allowed on specific roles (global ones are not listed).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RoleSpecificAttributes = Map(new()
    {
        ["aria-activedescendant"] = ActiveDescendantRoles,
        ["aria-autocomplete"] = ["combobox", "searchbox", "textbox"],
        ["aria-checked"] = ["checkbox", "menuitemcheckbox", "menuitemradio", "option", "radio", "switch", "treeitem"],
        ["aria-colcount"] = ["grid", "table", "treegrid"],
        ["aria-colindex"] = ["cell", "columnheader", "gridcell", "row", "rowheader"],
        ["aria-colspan"] = ["cell", "columnheader", "gridcell", "rowheader"],
        ["aria-expanded"] = ["application", "button", "checkbox", "combobox", "gridcell", "link", "listbox", "menuitem", "menuitemcheckbox", "menuitemradio", "row", "rowheader", "tab", "treeitem", "columnheader", "switch"],
        ["aria-level"] = ["heading", "listitem", "row", "treeitem", "comment"],
        ["aria-modal"] = ["alertdialog", "dialog"],
        ["aria-multiline"] = ["searchbox", "textbox"],
        ["aria-multiselectable"] = ["grid", "listbox", "tablist", "tree", "treegrid"],
        ["aria-orientation"] = ["scrollbar", "select", "separator", "slider", "tablist", "toolbar", "listbox", "menu", "menubar", "radiogroup", "tree", "treegrid"],
        ["aria-placeholder"] = ["searchbox", "textbox"],
        ["aria-posinset"] = ["article", "listitem", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "radio", "row", "tab", "treeitem", "comment"],
        ["aria-pressed"] = ["button"],
        ["aria-readonly"] = ["checkbox", "combobox", "grid", "gridcell", "listbox", "radiogroup", "slider", "spinbutton", "textbox", "searchbox", "columnheader", "rowheader", "treegrid", "menuitemcheckbox", "menuitemradio", "switch"],
        ["aria-required"] = ["checkbox", "combobox", "gridcell", "listbox", "radiogroup", "spinbutton", "textbox", "searchbox", "tree", "treegrid", "columnheader", "rowheader", "switch"],
        ["aria-rowcount"] = ["grid", "table", "treegrid"],
        ["aria-rowindex"] = ["cell", "columnheader", "gridcell", "row", "rowheader"],
        ["aria-rowspan"] = ["cell", "columnheader", "gridcell", "rowheader"],
        ["aria-selected"] = ["gridcell", "option", "row", "tab", "columnheader", "rowheader", "treeitem"],
        ["aria-setsize"] = ["article", "listitem", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "radio", "row", "tab", "treeitem", "comment"],
        ["aria-sort"] = ["columnheader", "rowheader"],
        ["aria-valuemax"] = RangeRoles,
        ["aria-valuemin"] = RangeRoles,
        ["aria-valuenow"] = RangeRoles,
        ["aria-valuetext"] = RangeRoles,
    });

    /// <summary>Allowed values of enumerated / boolean attributes (compared ASCII case-insensitively).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> TokenValues = Map(new()
    {
        ["aria-atomic"] = ["true", "false"],
        ["aria-autocomplete"] = ["inline", "list", "both", "none"],
        ["aria-busy"] = ["true", "false"],
        ["aria-checked"] = ["true", "false", "mixed", "undefined"],
        ["aria-current"] = ["page", "step", "location", "date", "time", "true", "false"],
        ["aria-disabled"] = ["true", "false"],
        ["aria-expanded"] = ["true", "false", "undefined"],
        ["aria-haspopup"] = ["true", "false", "menu", "listbox", "tree", "grid", "dialog"],
        ["aria-hidden"] = ["true", "false", "undefined"],
        ["aria-invalid"] = ["true", "false", "grammar", "spelling"],
        ["aria-live"] = ["off", "polite", "assertive"],
        ["aria-modal"] = ["true", "false"],
        ["aria-multiline"] = ["true", "false"],
        ["aria-multiselectable"] = ["true", "false"],
        ["aria-orientation"] = ["horizontal", "vertical", "undefined"],
        ["aria-pressed"] = ["true", "false", "mixed", "undefined"],
        ["aria-readonly"] = ["true", "false"],
        ["aria-required"] = ["true", "false"],
        ["aria-selected"] = ["true", "false", "undefined"],
        ["aria-sort"] = ["ascending", "descending", "none", "other"],
    });

    /// <summary>Attributes whose value must be an integer.</summary>
    public static readonly IReadOnlySet<string> IntegerAttributes = Set(
        "aria-colcount", "aria-colindex", "aria-colspan", "aria-level", "aria-posinset", "aria-rowcount", "aria-rowindex", "aria-rowspan", "aria-setsize");

    /// <summary>Attributes whose value must be a number.</summary>
    public static readonly IReadOnlySet<string> NumberAttributes = Set("aria-valuemax", "aria-valuemin", "aria-valuenow");

    /// <summary>Attributes holding id references.</summary>
    public static readonly IReadOnlySet<string> IdRefAttributes = Set(
        "aria-activedescendant", "aria-controls", "aria-describedby", "aria-details", "aria-errormessage", "aria-flowto",
        "aria-labelledby", "aria-owns");

    /// <summary>Roles a container may own (directly, through generic wrappers).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RequiredOwned = Map(new()
    {
        ["grid"] = ["row", "rowgroup"],
        ["treegrid"] = ["row", "rowgroup"],
        ["table"] = ["row", "rowgroup", "caption"],
        ["rowgroup"] = ["row"],
        ["row"] = ["cell", "columnheader", "gridcell", "rowheader"],
        ["list"] = ["listitem"],
        ["listbox"] = ["option", "group"],
        ["menu"] = ["menuitem", "menuitemcheckbox", "menuitemradio", "group", "separator", "menu"],
        ["menubar"] = ["menuitem", "menuitemcheckbox", "menuitemradio", "group", "separator", "menu"],
        ["tablist"] = ["tab"],
        ["tree"] = ["treeitem", "group"],
        ["feed"] = ["article"],
    });

    /// <summary>The context roles an element of a role must live in.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RequiredContext = Map(new()
    {
        ["option"] = ["listbox", "group"],
        ["menuitem"] = ["menu", "menubar", "group"],
        ["menuitemcheckbox"] = ["menu", "menubar", "group"],
        ["menuitemradio"] = ["menu", "menubar", "group"],
        ["tab"] = ["tablist"],
        ["treeitem"] = ["tree", "group"],
        ["row"] = ["grid", "treegrid", "table", "rowgroup"],
        ["cell"] = ["row"],
        ["gridcell"] = ["row"],
        ["columnheader"] = ["row"],
        ["rowheader"] = ["row"],
        ["listitem"] = ["list", "directory"],
    });

    /// <summary>States / properties a role must have (a separator only when it is focusable).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RequiredAttributes = Map(new()
    {
        ["checkbox"] = ["aria-checked"],
        ["combobox"] = ["aria-expanded"],
        ["heading"] = ["aria-level"],
        ["menuitemcheckbox"] = ["aria-checked"],
        ["menuitemradio"] = ["aria-checked"],
        ["meter"] = ["aria-valuenow"],
        ["radio"] = ["aria-checked"],
        ["scrollbar"] = ["aria-controls", "aria-valuenow"],
        ["separator"] = ["aria-valuenow"],
        ["slider"] = ["aria-valuenow"],
        ["switch"] = ["aria-checked"],
    });

    /// <summary>
    /// Returns the element's role as browsers compute it: the first recognised token of <c>role</c>,
    /// else its implicit role (null for generic). Owned elements of a presentational list / table
    /// (<c>li</c>, <c>tr</c>, <c>td</c> …) inherit the presentational role, and a presentational role is
    /// ignored on focusable elements and elements with global ARIA attributes.
    /// </summary>
    public static string? GetRole(IElement element)
    {
        var role = ExplicitRole(element) ?? InheritedPresentation(element) ?? GetImplicitRole(element);
        if (role is not ("none" or "presentation") || !HasPresentationConflict(element)) return role;
        // Presentational role conflict resolution: the element keeps its native role.
        var implicitRole = GetImplicitRole(element);
        return implicitRole is "none" or "presentation" ? "img" : implicitRole;
    }

    /// <summary>The first recognised token of the element's <c>role</c> attribute (lowercase), or null.</summary>
    public static string? ExplicitRole(IElement element)
    {
        var value = element.GetAttribute("role");
        if (string.IsNullOrWhiteSpace(value)) return null;
        foreach (var token in value.Split(AsciiWhitespace, StringSplitOptions.RemoveEmptyEntries))
        {
            var role = token.ToLowerInvariant();
            if (ValidRoles.Contains(role)) return role == "image" ? "img" : role;
        }
        return null;
    }

    /// <summary>The HTML-AAM implicit role of an element (null when generic / none).</summary>
    public static string? GetImplicitRole(IElement element)
    {
        var tag = element.LocalName;
        switch (tag)
        {
            case "a" or "area": return element.HasAttribute("href") ? "link" : null;
            case "article": return "article";
            // complementary when scoped to body / main, or when named inside sectioning content.
            case "aside": return InSection(element, includeMain: false) && !HasNameAttribute(element) ? null : "complementary";
            case "button": return "button";
            case "datalist": return "listbox";
            case "details": return "group";
            case "dialog": return "dialog";
            case "fieldset": return "group";
            case "figure": return "figure";
            // banner / contentinfo only when scoped to body: inside main or sectioning content they're generic.
            case "footer": return InSection(element, includeMain: true) ? null : "contentinfo";
            case "header": return InSection(element, includeMain: true) ? null : "banner";
            // An unnamed form isn't exposed as a landmark.
            case "form": return HasNameAttribute(element) ? "form" : null;
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6": return "heading";
            case "hr": return "separator";
            case "img": return element.GetAttribute("alt") == "" ? "presentation" : "img";
            case "li": return "listitem";
            case "main": return "main";
            case "math": return "math";
            case "menu" or "ol" or "ul": return "list";
            case "meter": return "meter";
            case "nav": return "navigation";
            case "optgroup": return "group";
            case "option": return "option";
            case "output": return "status";
            case "progress": return "progressbar";
            case "search": return "search";
            case "section": return HasNameAttribute(element) ? "region" : null;
            case "select": return element.HasAttribute("multiple") || (int.TryParse(element.GetAttribute("size"), out var size) && size > 1) ? "listbox" : "combobox";
            // Only a details element's first summary child is its (button-like) disclosure control.
            case "summary": return IsDetailsSummary(element) ? "button" : null;
            case "table": return "table";
            case "tbody" or "thead" or "tfoot": return "rowgroup";
            case "td": return CellRole(element);
            case "textarea": return "textbox";
            case "th": return HeaderRole(element);
            case "tr": return "row";
            case "input":
                var hasList = element.HasAttribute("list");
                return (element.GetAttribute("type")?.Trim().ToLowerInvariant() ?? "text") switch
                {
                    "button" or "image" or "reset" or "submit" => "button",
                    "checkbox" => "checkbox",
                    "radio" => "radio",
                    "range" => "slider",
                    "number" => "spinbutton",
                    "search" => hasList ? "combobox" : "searchbox",
                    // HTML-AAM: these have no corresponding ARIA role.
                    "hidden" or "password" or "date" or "datetime-local" or "month" or "time" or "week" or "color" or "file" => null,
                    // email, tel, text, url and unknown types (which behave as text).
                    _ => hasList ? "combobox" : "textbox",
                };
            default: return null;
        }
    }

    /// <summary>Whether the element is a native form control whose state the browser exposes.</summary>
    public static bool IsNativeControl(IElement element)
        => element.LocalName is "input" or "select" or "textarea" or "button" or "meter" or "progress" or "option";

    /// <summary>
    /// Whether the element (or an ancestor) is hidden from assistive tech: not rendered
    /// (see <see cref="IsRendered"/>) or inside <c>aria-hidden="true"</c>.
    /// </summary>
    public static bool IsHidden(IElement element)
    {
        for (var e = element; e is not null; e = e.ParentElement)
            if (IsAriaHidden(e)) return true;
        return !IsRendered(element);
    }

    /// <summary>
    /// Whether the element is rendered, as far as static markup tells: it isn't inside a <c>hidden</c>,
    /// <c>display: none</c> or <c>visibility: hidden</c> (inline style) element, a <c>template</c>, <c>script</c>,
    /// <c>style</c> or <c>datalist</c>, a closed <c>dialog</c>, or the content of a closed <c>details</c> (its
    /// summary stays). <c>aria-hidden</c> doesn't affect rendering: such content still takes focus.
    /// </summary>
    public static bool IsRendered(IElement element)
    {
        string? visibility = null;
        for (var e = element; e is not null; e = e.ParentElement)
        {
            if (NotRenderedSelf(e)) return false;
            // The nearest inline visibility wins: a visible child of a hidden parent is visible.
            if (visibility is null && e.GetAttribute("style") is { } style) visibility = StyleValue(style, "visibility");
        }
        return visibility is not ("hidden" or "collapse");
    }

    /// <summary>Whether the element is inert (or inside an inert subtree).</summary>
    public static bool IsInert(IElement element) => element.Closest("[inert]") is not null;

    /// <summary>Whether the element can receive focus (natively or through a valid <c>tabindex</c>), ignoring rendering.</summary>
    public static bool IsFocusable(IElement element)
    {
        if (element.LocalName is "button" or "input" or "select" or "textarea" or "fieldset" && IsDisabled(element)) return false;
        if (element.LocalName == "input" && string.Equals(element.GetAttribute("type")?.Trim(), "hidden", StringComparison.OrdinalIgnoreCase)) return false;
        if (TryGetTabIndex(element, out _)) return true;
        return element.LocalName switch
        {
            "a" or "area" => element.HasAttribute("href"),
            "button" or "input" or "select" or "textarea" or "iframe" => true,
            "summary" => IsDetailsSummary(element),
            "audio" or "video" => element.HasAttribute("controls"),
            _ => element.GetAttribute("contenteditable")?.Trim().ToLowerInvariant() is "" or "true" or "plaintext-only",
        };
    }

    /// <summary>
    /// Whether the element is in the sequential (Tab) focus order: focusable, without a negative
    /// <c>tabindex</c>, rendered and not inert. Content hidden only with <c>aria-hidden</c> is included:
    /// browsers still Tab into it (focus-trap sentinels rely on that).
    /// </summary>
    public static bool IsTabbable(IElement element)
        => IsFocusable(element) && !(TryGetTabIndex(element, out var tabIndex) && tabIndex < 0) && IsRendered(element) && !IsInert(element);

    /// <summary>
    /// Reads <c>tabindex</c> with the HTML integer parsing rules; false when it is absent or
    /// invalid (an invalid value is ignored, it doesn't make the element focusable).
    /// </summary>
    internal static bool TryGetTabIndex(IElement element, out int tabIndex)
    {
        tabIndex = 0;
        var value = element.GetAttribute("tabindex").AsSpan().TrimStart(" \t\n\f\r");
        var sign = 1;
        if (value.Length > 0 && value[0] is '-' or '+')
        {
            sign = value[0] == '-' ? -1 : 1;
            value = value[1..];
        }
        var digits = 0;
        long number = 0;
        for (; digits < value.Length && char.IsAsciiDigit(value[digits]); digits++)
            number = Math.Min(number * 10 + (value[digits] - '0'), int.MaxValue);
        if (digits == 0) return false;
        tabIndex = (int)(sign * number);
        return true;
    }

    /// <summary>Whether <c>aria-hidden</c> is <c>true</c> on the element itself (ASCII case-insensitive).</summary>
    internal static bool IsAriaHidden(IElement element) => AttributeIs(element, "aria-hidden", "true");

    /// <summary>Whether a token attribute has <paramref name="value"/>, compared ASCII case-insensitively.</summary>
    internal static bool AttributeIs(IElement element, string name, string value)
        => string.Equals(element.GetAttribute(name)?.Trim(), value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the element is hidden from assistive tech by itself (not by an ancestor): used when walking
    /// down a subtree whose ancestors were already checked.
    /// </summary>
    internal static bool IsHiddenSelf(IElement element)
        => IsAriaHidden(element) || NotRenderedSelf(element)
            || element.GetAttribute("style") is { } style && StyleValue(style, "visibility") is "hidden" or "collapse";

    /// <summary>The value of a property in an inline <c>style</c> attribute (lowercase, without <c>!important</c>), or null.</summary>
    internal static string? StyleValue(string style, string property)
    {
        string? result = null;
        foreach (var declaration in style.Split(';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon < 0 || !declaration.AsSpan(0, colon).Trim().Equals(property, StringComparison.OrdinalIgnoreCase)) continue;
            var value = declaration[(colon + 1)..].Trim().ToLowerInvariant();
            if (value.EndsWith("!important", StringComparison.Ordinal)) value = value[..^"!important".Length].TrimEnd();
            result = value;
        }
        return result;
    }

    /// <summary>Whether a form control is disabled: by its own <c>disabled</c> attribute or by a disabled <c>fieldset</c> ancestor.</summary>
    internal static bool IsDisabled(IElement element)
    {
        if (element.HasAttribute("disabled") && element.LocalName is "button" or "input" or "select" or "textarea" or "fieldset" or "optgroup" or "option") return true;
        // Every disabled fieldset ancestor counts, unless the element is inside that fieldset's first legend child.
        var child = element;
        for (var e = element.ParentElement; e is not null; child = e, e = e.ParentElement)
        {
            if (e.LocalName != "fieldset" || !e.HasAttribute("disabled")) continue;
            if (!ReferenceEquals(child, e.Children.FirstOrDefault(c => c.LocalName == "legend"))) return true;
        }
        return false;
    }

    /// <summary>Whether the element is the first <c>summary</c> child of a <c>details</c> element.</summary>
    internal static bool IsDetailsSummary(IElement element)
        => element.LocalName == "summary" && element.ParentElement is { LocalName: "details" } details
           && ReferenceEquals(details.Children.FirstOrDefault(c => c.LocalName == "summary"), element);

    private static readonly char[] AsciiWhitespace = [' ', '\t', '\n', '\f', '\r'];

    private static bool NotRenderedSelf(IElement e)
    {
        if (e.HasAttribute("hidden") || e.LocalName is "template" or "script" or "style" or "datalist") return true;
        if (e.LocalName == "dialog" && !e.HasAttribute("open")) return true;
        if (e.LocalName == "input" && string.Equals(e.GetAttribute("type")?.Trim(), "hidden", StringComparison.OrdinalIgnoreCase)) return true;
        // A closed details element renders only its summary.
        if (e.ParentElement is { LocalName: "details" } details && !details.HasAttribute("open") && !IsDetailsSummary(e)) return true;
        return e.GetAttribute("style") is { } style && StyleValue(style, "display") == "none";
    }

    private static bool HasPresentationConflict(IElement element)
        => IsFocusable(element) || element.Attributes.Any(a => GlobalAttributes.Contains(a.Name) && !string.IsNullOrWhiteSpace(a.Value));

    /// <summary>The presentational role a list item / table part inherits from its presentational list or table, or null.</summary>
    private static string? InheritedPresentation(IElement element)
    {
        IElement? owner;
        switch (element.LocalName)
        {
            case "li":
                owner = element.ParentElement is { LocalName: "ul" or "ol" or "menu" } list ? list : null;
                break;
            case "thead" or "tbody" or "tfoot" or "tr" or "td" or "th" or "caption":
                owner = element.ParentElement;
                // Only through structural table elements that keep their native role.
                while (owner is { LocalName: "thead" or "tbody" or "tfoot" or "tr" } && ExplicitRole(owner) is null) owner = owner.ParentElement;
                if (owner?.LocalName != "table") owner = null;
                break;
            default:
                return null;
        }
        return owner is not null && GetRole(owner) is { } role && role is "none" or "presentation" ? role : null;
    }

    private static string? CellRole(IElement cell)
        => TableRole(cell) switch
        {
            "grid" or "treegrid" => "gridcell",
            // Parsed without its table (a fragment), a cell is still a cell.
            "table" or null => "cell",
            _ => null,
        };

    private static string? HeaderRole(IElement header)
    {
        var tableRole = TableRole(header);
        if (tableRole is not (null or "table" or "grid" or "treegrid")) return null;
        var scope = header.GetAttribute("scope")?.Trim().ToLowerInvariant();
        if (scope is "row" or "rowgroup") return "rowheader";
        if (scope is "col" or "colgroup") return "columnheader";
        // Without scope: a th in a header row (thead, or a row of th only) heads its column, one beside data cells heads its row.
        if (header.ParentElement is { LocalName: "tr" } row && row.ParentElement?.LocalName != "thead" && row.Children.Any(c => c.LocalName == "td"))
            return "rowheader";
        return "columnheader";
    }

    private static string? TableRole(IElement cell)
    {
        for (var e = cell.ParentElement; e is not null; e = e.ParentElement)
            if (e.LocalName == "table") return GetRole(e);
        return null;
    }

    /// <summary>Whether the element is inside sectioning content (article, aside, nav, section) or, optionally, main.</summary>
    private static bool InSection(IElement element, bool includeMain)
    {
        for (var e = element.ParentElement; e is not null; e = e.ParentElement)
        {
            if (e.LocalName is "article" or "aside" or "nav" or "section" || (includeMain && e.LocalName == "main")) return true;
            if (ExplicitRole(e) is "article" or "complementary" or "navigation" or "region" || (includeMain && ExplicitRole(e) == "main")) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether the element is named by an attribute (aria-label, aria-labelledby or title). Attribute-based on purpose:
    /// computing the full name here would recurse through role computation.
    /// </summary>
    private static bool HasNameAttribute(IElement element)
        => !string.IsNullOrWhiteSpace(element.GetAttribute("aria-label")) || !string.IsNullOrWhiteSpace(element.GetAttribute("aria-labelledby"))
           || !string.IsNullOrWhiteSpace(element.GetAttribute("title"));

    private static IReadOnlySet<string> Set(params string[] values) => values.ToFrozenSet(StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Map(Dictionary<string, string[]> table)
        => table.ToFrozenDictionary(e => e.Key, e => (IReadOnlyList<string>)ImmutableArray.Create(e.Value), StringComparer.Ordinal);
}
