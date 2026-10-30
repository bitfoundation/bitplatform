using System.Globalization;
using System.Text;
using AngleSharp.Dom;

namespace Bit.BlazorHeadlessUI.Testing;

/// <summary>
/// Computes accessible names and descriptions following the W3C "Accessible Name
/// and Description Computation 1.2" algorithm and the HTML-AAM rules, as far as they can
/// be evaluated on static markup (no stylesheets: <c>::before</c> content, CSS visibility
/// and display come from inline styles and the element type only).
/// </summary>
public static class BitBhAccessibleName
{
    /// <summary>Returns the accessible name of <paramref name="element"/> (empty when it has none).</summary>
    public static string Compute(IElement element) => Compute(element, out _, out _);

    /// <summary>
    /// The accessible name, plus where it came from: <paramref name="fromTooltip"/> when the element's own
    /// <c>title</c> supplied it, <paramref name="fromDefault"/> when it is the browser's default label
    /// ("Submit" / "Reset").
    /// </summary>
    internal static string Compute(IElement element, out bool fromTooltip, out bool fromDefault)
    {
        var walk = new Walk(element, [], InLabelledBy: false, new Origin());
        var name = Normalize(Name(element, walk, Hop.Root));
        fromTooltip = walk.Origin.Tooltip;
        fromDefault = walk.Origin.Default;
        return name;
    }

    /// <summary>
    /// Returns the accessible description of <paramref name="element"/>: <c>aria-describedby</c>, else
    /// <c>aria-description</c>, else <c>title</c> when the title didn't already supply the name.
    /// </summary>
    public static string ComputeDescription(IElement element)
    {
        var ids = element.GetAttribute("aria-describedby");
        if (!string.IsNullOrWhiteSpace(ids))
        {
            var described = Normalize(string.Join(" ", ResolveIds(element, ids).Select(target => Name(target, Walk.Reference(element), Hop.Reference))));
            if (described.Length > 0) return described;
        }
        var description = element.GetAttribute("aria-description");
        if (!string.IsNullOrWhiteSpace(description)) return Normalize(description);
        var title = element.GetAttribute("title");
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        // A title that is the name would be announced twice.
        Compute(element, out var fromTooltip, out _);
        return fromTooltip ? string.Empty : Normalize(title);
    }

    private static IEnumerable<IElement> ResolveIds(IElement element, string ids)
    {
        foreach (var id in ids.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var target = FindById(element, id) ?? element.Owner?.GetElementById(id);
            if (target is not null) yield return target;
        }
    }

    private static IElement? FindById(IElement element, string id)
    {
        var root = element;
        while (root.ParentElement is not null) root = root.ParentElement;
        // Compared, not selected: an id may hold any character a selector would need escaped.
        return root.Id == id ? root : root.QuerySelectorAll("[id]").FirstOrDefault(e => e.Id == id);
    }

    /// <summary>Item roles whose nested sub-containers (child items) aren't part of their name, as browsers compute it.</summary>
    private static readonly HashSet<string> ItemRoles = new(StringComparer.Ordinal) { "treeitem", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "row" };

    private static readonly HashSet<string> NestedContainers = new(StringComparer.Ordinal) { "group", "tree", "menu", "listbox", "grid", "treegrid" };

    /// <summary>Elements that make their own box, so their text is a separate word (inline elements like span, b or a are not).</summary>
    private static readonly HashSet<string> SeparateBoxes = new(StringComparer.Ordinal)
    {
        "address", "article", "aside", "blockquote", "br", "caption", "dd", "details", "dialog", "div", "dl", "dt", "fieldset",
        "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup", "hr", "legend", "li",
        "main", "menu", "nav", "ol", "p", "pre", "search", "section", "summary", "table", "tbody", "td", "tfoot", "th", "thead",
        "tr", "ul",
        // Replaced and inline-block content.
        "audio", "button", "canvas", "iframe", "img", "input", "meter", "object", "progress", "select", "svg", "textarea", "video",
    };

    private static readonly string[] RangeRoles = ["slider", "spinbutton", "progressbar", "scrollbar", "meter"];

    /// <summary>How the current node was reached.</summary>
    private enum Hop
    {
        /// <summary>The element whose name is computed.</summary>
        Root,

        /// <summary>Referenced by aria-labelledby / aria-describedby, or a native text alternative element (label, legend, caption).</summary>
        Reference,

        /// <summary>A descendant visited for name from content.</summary>
        Descendant,
    }

    /// <summary>Where the root's name came from (set for the root only).</summary>
    private sealed class Origin
    {
        public bool Tooltip;
        public bool Default;
    }

    /// <summary>
    /// The state of one traversal: the visited nodes (cycle guard, shared with the label / legend / caption
    /// traversals it starts) and whether it follows an aria-labelledby (which is then not followed again:
    /// accname 2B — that is what stops labelledby cycles).
    /// </summary>
    private sealed record Walk(IElement Root, HashSet<IElement> Visited, bool InLabelledBy, Origin Origin)
    {
        /// <summary>A fresh traversal for an element referenced by aria-labelledby / aria-describedby (self-references allowed).</summary>
        public static Walk Reference(IElement root) => new(root, [], InLabelledBy: true, new Origin());
    }

    private static string Name(IElement element, Walk walk, Hop hop, bool insideItem = false)
    {
        if (!walk.Visited.Add(element)) return string.Empty;

        // 2A: hidden nodes contribute nothing. A traversal's starting node (the root, a referenced node, a label)
        // counts even when hidden, with its content: only nodes hidden below it are skipped. Checking each
        // descendant's own hiding is enough, as its ancestors up to the start were checked on the way down.
        if (hop == Hop.Descendant && BitBhAria.IsHiddenSelf(element)) return string.Empty;

        // 2B: aria-labelledby (not inside a labelledby traversal; each reference is computed on its own).
        var labelledBy = element.GetAttribute("aria-labelledby");
        if (!walk.InLabelledBy && !string.IsNullOrWhiteSpace(labelledBy))
        {
            var joined = string.Join(" ", ResolveIds(element, labelledBy).Select(target => Name(target, Walk.Reference(walk.Root), Hop.Reference))).Trim();
            if (joined.Length > 0) return joined;
        }

        var role = BitBhAria.GetRole(element);

        // 2C: a control embedded in another widget's label contributes its value.
        if (hop != Hop.Root && !ReferenceEquals(element, walk.Root) && EmbeddedValue(element, role, walk) is { } value) return value;

        // 2D: aria-label.
        var ariaLabel = element.GetAttribute("aria-label");
        if (!string.IsNullOrWhiteSpace(ariaLabel)) return ariaLabel;

        // 2E: native host-language label.
        if (role is not ("presentation" or "none"))
        {
            var native = NativeName(element, walk, hop);
            if (!string.IsNullOrWhiteSpace(native)) return native;
        }

        // 2F: name from content.
        if (hop != Hop.Root || (role is not null && BitBhAria.NameFromContent.Contains(role)) || element.LocalName is "label" or "legend" or "caption")
        {
            var content = Content(element, role, walk, insideItem);
            if (!string.IsNullOrWhiteSpace(content)) return content;
        }

        // 2I: tooltip attribute, then a text field's placeholder (an empty title doesn't hide the placeholder).
        var title = element.GetAttribute("title");
        if (!string.IsNullOrWhiteSpace(title))
        {
            if (hop == Hop.Root) walk.Origin.Tooltip = true;
            return title;
        }
        return element.LocalName is "input" or "textarea" ? element.GetAttribute("placeholder") ?? string.Empty : string.Empty;
    }

    private static string Content(IElement element, string? role, Walk walk, bool insideItem)
    {
        var sb = new StringBuilder();
        // A tree item's name is its own row: its nested group of child items is excluded.
        var item = insideItem || (role is not null && ItemRoles.Contains(role));
        foreach (var child in element.ChildNodes)
        {
            if (child is IText text) sb.Append(text.Data);
            else if (child is IElement childElement)
            {
                if (item && BitBhAria.GetRole(childElement) is { } childRole && NestedContainers.Contains(childRole)) continue;
                var childName = Name(childElement, walk, Hop.Descendant, item);
                if (IsSeparateBox(childElement)) sb.Append(' ').Append(childName).Append(' ');
                else sb.Append(childName);
            }
        }
        return sb.ToString();
    }

    private static bool IsSeparateBox(IElement element)
        => element.GetAttribute("style") is { } style && BitBhAria.StyleValue(style, "display") is { } display
            ? display is not ("inline" or "contents")
            : SeparateBoxes.Contains(element.LocalName);

    /// <summary>The value an embedded control contributes to a label (accname 2C), or null when it isn't one.</summary>
    private static string? EmbeddedValue(IElement element, string? role, Walk walk)
    {
        switch (role)
        {
            case "textbox" or "searchbox":
                return element.LocalName == "input" ? element.GetAttribute("value") ?? string.Empty : element.TextContent;
            case "combobox" or "listbox":
                if (element.LocalName == "select") return string.Join(" ", SelectedOptions(element).Select(o => Name(o, walk, Hop.Descendant)));
                if (element.LocalName is "input" or "textarea") return element.LocalName == "input" ? element.GetAttribute("value") ?? string.Empty : element.TextContent;
                // The chosen option: a listbox's selected options, a select-only combobox's displayed text.
                if (role == "combobox") return Content(element, role, walk, insideItem: false);
                return string.Join(" ", element.QuerySelectorAll("*")
                    .Where(o => BitBhAria.AttributeIs(o, "aria-selected", "true") && BitBhAria.GetRole(o) == "option")
                    .Select(o => Name(o, walk, Hop.Descendant)));
            case not null when RangeRoles.Contains(role):
                var valueText = element.GetAttribute("aria-valuetext");
                if (!string.IsNullOrWhiteSpace(valueText)) return valueText;
                var valueNow = element.GetAttribute("aria-valuenow");
                if (!string.IsNullOrWhiteSpace(valueNow)) return valueNow;
                return NativeValue(element);
            default:
                return null;
        }
    }

    private static string NativeValue(IElement element)
    {
        var value = element.GetAttribute("value");
        if (value is not null || element.LocalName != "input" || !string.Equals(element.GetAttribute("type")?.Trim(), "range", StringComparison.OrdinalIgnoreCase))
            return element.LocalName is "input" or "meter" or "progress" ? value ?? string.Empty : string.Empty;
        // A range input without a value sits halfway between its bounds (HTML default value).
        var min = double.TryParse(element.GetAttribute("min"), NumberStyles.Float, CultureInfo.InvariantCulture, out var mi) ? mi : 0;
        var max = double.TryParse(element.GetAttribute("max"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ma) ? ma : 100;
        return (max < min ? min : min + (max - min) / 2).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A select's selected options: those marked <c>selected</c>, else (single select) the first enabled one.</summary>
    private static IEnumerable<IElement> SelectedOptions(IElement select)
    {
        var options = select.QuerySelectorAll("option").ToList();
        var selected = options.Where(o => o.HasAttribute("selected")).ToList();
        var multiple = select.HasAttribute("multiple");
        if (multiple) return selected;
        // A single select shows one option: the last one marked selected, else the first enabled one.
        var shown = selected.LastOrDefault() ?? options.FirstOrDefault(o => !BitBhAria.IsDisabled(o));
        return shown is null ? [] : [shown];
    }

    private static string? NativeName(IElement element, Walk walk, Hop hop)
    {
        switch (element.LocalName)
        {
            case "input" or "select" or "textarea" or "meter" or "progress" or "output" or "button":
                var labels = Labels(element).Select(l => Name(l, walk, Hop.Reference)).ToList();
                var labelled = string.Join(" ", labels);
                if (!string.IsNullOrWhiteSpace(labelled)) return labelled;
                if (element.LocalName != "input") return null;
                var type = element.GetAttribute("type")?.Trim().ToLowerInvariant();
                if (type is "button" or "submit" or "reset")
                {
                    var value = element.GetAttribute("value");
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                    if (value is not null || type == "button") return null;
                    return Default(type == "submit" ? "Submit" : "Reset");
                }
                if (type == "image")
                {
                    // alt, value, title, then the browser's default label (as Chromium and Firefox compute it).
                    foreach (var attribute in new[] { "alt", "value", "title" })
                        if (element.GetAttribute(attribute) is { } text && !string.IsNullOrWhiteSpace(text)) return text;
                    return Default("Submit");
                }
                return null;
            case "img" or "area":
                return element.GetAttribute("alt");
            case "fieldset":
                var legend = element.Children.FirstOrDefault(c => c.LocalName == "legend");
                return legend is null ? null : Name(legend, walk, Hop.Reference);
            case "table":
                var caption = element.Children.FirstOrDefault(c => c.LocalName == "caption");
                return caption is null ? null : Name(caption, walk, Hop.Reference);
            case "option" or "optgroup":
                return element.GetAttribute("label");
            case "svg":
                return element.Children.FirstOrDefault(c => c.LocalName == "title")?.TextContent;
            default:
                return null;
        }

        string Default(string label)
        {
            if (hop == Hop.Root) walk.Origin.Default = true;
            return label;
        }
    }

    /// <summary>The labels of a control, in tree order: <c>label for</c> it, or a <c>label</c> without <c>for</c> whose first labelable descendant it is.</summary>
    private static IEnumerable<IElement> Labels(IElement control)
    {
        var root = control;
        while (root.ParentElement is not null) root = root.ParentElement;
        var candidates = root.QuerySelectorAll("label").AsEnumerable();
        if (root.LocalName == "label") candidates = candidates.Prepend(root);
        foreach (var label in candidates)
        {
            var target = label.GetAttribute("for");
            if (target is not null ? target == control.Id && !string.IsNullOrEmpty(target) : ReferenceEquals(FirstLabelable(label), control))
                yield return label;
        }
    }

    private static IElement? FirstLabelable(IElement label)
        => label.QuerySelectorAll("button, input, meter, output, progress, select, textarea")
            .FirstOrDefault(e => e.LocalName != "input" || !string.Equals(e.GetAttribute("type")?.Trim(), "hidden", StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
