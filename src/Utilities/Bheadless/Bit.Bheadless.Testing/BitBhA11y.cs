using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AngleSharp.Dom;

namespace Bit.Bheadless.Testing;

/// <summary>Severity of an accessibility violation (mirrors axe-core impact levels).</summary>
public enum BitBhA11yImpact
{
    /// <summary>Best-practice issue.</summary>
    Minor,

    /// <summary>Degrades the experience for some users.</summary>
    Moderate,

    /// <summary>Severely degrades the experience.</summary>
    Serious,

    /// <summary>Blocks access for some users.</summary>
    Critical,
}

/// <summary>One accessibility problem found by <see cref="BitBhA11y.Audit(INode, BitBhA11yOptions?)"/>.</summary>
/// <param name="RuleId">The rule that failed (e.g. <c>aria-valid-attr-value</c>).</param>
/// <param name="Impact">The severity.</param>
/// <param name="Message">What is wrong and how to fix it.</param>
/// <param name="Selector">A CSS selector locating the element.</param>
/// <param name="Html">The element's opening tag.</param>
public sealed record BitBhA11yViolation(string RuleId, BitBhA11yImpact Impact, string Message, string Selector, string Html)
{
    /// <inheritdoc />
    public override string ToString() => $"[{Impact}] {RuleId}: {Message}\n    at {Selector}\n    {Html}";
}

/// <summary>Options for <see cref="BitBhA11y.Audit(INode, BitBhA11yOptions?)"/>.</summary>
public sealed class BitBhA11yOptions
{
    /// <summary>Rules to skip (by id).</summary>
    public ISet<string> DisabledRules { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Only report violations at or above this impact. Default: <see cref="BitBhA11yImpact.Minor"/>.</summary>
    public BitBhA11yImpact MinimumImpact { get; set; } = BitBhA11yImpact.Minor;

    /// <summary>
    /// Whether the markup is a fragment of a page. When true, ID references
    /// to elements outside the fragment are not reported — they may live elsewhere
    /// on the page. Default: false.
    /// </summary>
    public bool IsFragment { get; set; }

    /// <summary>Disables a rule (fluent).</summary>
    public BitBhA11yOptions Without(params string[] ruleIds)
    {
        foreach (var id in ruleIds) DisabledRules.Add(id);
        return this;
    }
}

/// <summary>The result of an accessibility audit.</summary>
public sealed class BitBhA11yReport
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    internal BitBhA11yReport(IReadOnlyList<BitBhA11yViolation> violations, int elementCount)
    {
        Violations = violations;
        ElementCount = elementCount;
    }

    /// <summary>The violations found, most severe first.</summary>
    public IReadOnlyList<BitBhA11yViolation> Violations { get; }

    /// <summary>The number of elements inspected.</summary>
    public int ElementCount { get; }

    /// <summary>Whether no violations were found.</summary>
    public bool Passed => Violations.Count == 0;

    /// <summary>A human-readable summary.</summary>
    public override string ToString()
        => Passed
            ? $"No accessibility violations in {ElementCount} elements."
            : $"{Violations.Count} accessibility violation(s) in {ElementCount} elements:\n\n" + string.Join("\n\n", Violations);

    /// <summary>A Markdown table (for CI job summaries).</summary>
    public string ToMarkdown(string? title = null)
    {
        var sb = new StringBuilder();
        if (title is not null) sb.AppendLine($"### {title}").AppendLine();
        if (Passed)
        {
            sb.AppendLine($"✅ No accessibility violations ({ElementCount} elements checked).");
            return sb.ToString();
        }
        sb.AppendLine("| Impact | Rule | Element | Problem |").AppendLine("| --- | --- | --- | --- |");
        foreach (var v in Violations)
            sb.AppendLine($"| {v.Impact} | `{v.RuleId}` | `{v.Selector.Replace("|", "\\|")}` | {v.Message.Replace("|", "\\|")} |");
        return sb.ToString();
    }

    /// <summary>The violations as JSON (for tooling): <c>{ passed, elements, violations: [{ rule, impact, message, selector, html }] }</c>.</summary>
    public string ToJson()
    {
        // Built as a JSON DOM rather than serialised by reflection: stable property order, trimming-safe.
        var violations = new JsonArray();
        foreach (var v in Violations)
        {
            violations.Add(new JsonObject
            {
                ["rule"] = v.RuleId,
                ["impact"] = v.Impact.ToString().ToLowerInvariant(),
                ["message"] = v.Message,
                ["selector"] = v.Selector,
                ["html"] = v.Html,
            });
        }
        return new JsonObject { ["passed"] = Passed, ["elements"] = ElementCount, ["violations"] = violations }.ToJsonString(Indented);
    }
}

/// <summary>Thrown by <c>ShouldBeAccessible</c> assertions.</summary>
public sealed class BitBhA11yException(BitBhA11yReport report) : Exception(report.ToString())
{
    /// <summary>The failing report.</summary>
    public BitBhA11yReport Report { get; } = report;
}

/// <summary>
/// A static WAI-ARIA conformance checker for rendered Razor component markup — the
/// checks that can be made reliably without a browser: valid roles and states,
/// allowed attributes, value types, ID references, required owned elements and
/// context roles, accessible names, focusable content hidden from AT, nested
/// interactive controls, positive tabindex and duplicate ids.
///
/// <para>Static checks complement — they don't replace — browser-based scanning
/// (axe-core) and manual testing with assistive technology.</para>
/// </summary>
public static class BitBhA11y
{
    /// <summary>All rule ids.</summary>
    public static readonly IReadOnlyList<string> Rules =
    [
        "duplicate-id", "aria-valid-role", "aria-valid-attr-value", "aria-allowed-attr", "aria-required-attr",
        "idref-exists", "aria-required-children", "aria-required-parent", "accessible-name", "aria-hidden-focus",
        "nested-interactive", "tabindex", "presentation-conflict", "activedescendant-target", "form-label",
    ];

    /// <summary>Audits <paramref name="root"/> and everything below it.</summary>
    public static BitBhA11yReport Audit(INode root, BitBhA11yOptions? options = null)
        => Audit(new[] { root }, options);

    /// <summary>Audits several root nodes (e.g. <c>cut.Nodes</c>).</summary>
    public static BitBhA11yReport Audit(IEnumerable<INode> roots, BitBhA11yOptions? options = null)
    {
        options ??= new BitBhA11yOptions();
        var elements = new List<IElement>();
        foreach (var root in roots)
        {
            if (root is IElement e) elements.Add(e);
            if (root is IParentNode parent) elements.AddRange(parent.QuerySelectorAll("*"));
        }
        elements = elements.Distinct().ToList();

        var ids = elements.Where(e => !string.IsNullOrEmpty(e.Id)).GroupBy(e => e.Id!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var context = new AuditContext(options, ids, OwnerMap(elements, ids));

        foreach (var (id, list) in ids)
        {
            if (list.Count < 2) continue;
            foreach (var el in list)
                context.Report("duplicate-id", BitBhA11yImpact.Serious, el, $"The id \"{id}\" is used by {list.Count} elements; ID references become ambiguous.");
        }

        // Once per element, however many aria-hidden ancestors it has.
        var hiddenFocusable = new HashSet<IElement>();
        foreach (var el in elements.Where(BitBhAria.IsAriaHidden))
        {
            foreach (var focusable in new[] { el }.Concat(el.QuerySelectorAll("*")).Where(BitBhAria.IsTabbable))
            {
                if (hiddenFocusable.Add(focusable))
                    context.Report("aria-hidden-focus", BitBhA11yImpact.Serious, focusable, "Focusable element inside aria-hidden=\"true\": keyboard users reach content screen readers can't perceive. Make it inert or remove it from the tab order.");
            }
        }

        foreach (var el in elements)
        {
            var role = BitBhAria.GetRole(el);
            CheckRoleAttribute(el, context);
            CheckAttributeValues(el, role, context);
            CheckIdRefs(el, context);
            if (BitBhAria.IsHidden(el)) continue;

            // Before the role check: a presentational role ignored for a conflict leaves a role-less element.
            CheckPresentation(el, context);
            CheckName(el, role, context);
            if (role is null) continue;
            CheckAllowedAttributes(el, role, context);
            CheckRequiredAttributes(el, role, context);
            CheckOwnedElements(el, role, context);
            CheckContext(el, role, context);
            CheckNestedInteractive(el, role, context);
            CheckActiveDescendant(el, context);
        }

        foreach (var el in elements)
        {
            if (BitBhAria.TryGetTabIndex(el, out var t) && t > 0)
                context.Report("tabindex", BitBhA11yImpact.Moderate, el, string.Create(CultureInfo.InvariantCulture, $"tabindex=\"{t}\" changes the natural focus order; use 0 or -1."));
        }

        var ordered = context.Violations.OrderByDescending(v => v.Impact).ThenBy(v => v.RuleId, StringComparer.Ordinal).ToList();
        return new BitBhA11yReport(ordered, elements.Count);
    }

    /// <summary>The audit's shared state: options, the id index, aria-owns relations and the violations found.</summary>
    private sealed class AuditContext(BitBhA11yOptions options, Dictionary<string, List<IElement>> ids, Dictionary<IElement, IElement> owners)
    {
        public BitBhA11yOptions Options => options;

        public Dictionary<string, List<IElement>> Ids => ids;

        public List<BitBhA11yViolation> Violations { get; } = [];

        public void Report(string rule, BitBhA11yImpact impact, IElement el, string message)
        {
            if (options.DisabledRules.Contains(rule) || impact < options.MinimumImpact) return;
            Violations.Add(new BitBhA11yViolation(rule, impact, message, Selector(el), OpeningTag(el)));
        }

        /// <summary>The element's accessibility parent: its aria-owns owner, else its DOM parent.</summary>
        public IElement? Parent(IElement element) => owners.TryGetValue(element, out var owner) ? owner : element.ParentElement;

        /// <summary>Whether another element than <paramref name="parent"/> claims <paramref name="element"/> with aria-owns.</summary>
        public bool OwnedElsewhere(IElement element, IElement parent) => owners.TryGetValue(element, out var owner) && !ReferenceEquals(owner, parent);

        /// <summary>The elements <paramref name="element"/> claims with aria-owns.</summary>
        public IEnumerable<IElement> AriaOwned(IElement element)
            => (element.GetAttribute("aria-owns") ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => ids.TryGetValue(id, out var list) ? list[0] : null)
                .OfType<IElement>()
                .Where(owned => owners.TryGetValue(owned, out var owner) && ReferenceEquals(owner, element));
    }

    /// <summary>aria-owns relations: owned element → owner (the first claim wins, as in browsers).</summary>
    private static Dictionary<IElement, IElement> OwnerMap(List<IElement> elements, Dictionary<string, List<IElement>> ids)
    {
        var owners = new Dictionary<IElement, IElement>();
        foreach (var owner in elements)
        {
            foreach (var id in (owner.GetAttribute("aria-owns") ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                // An element can't own itself or one of its (DOM or aria-owns) ancestors: that would be a cycle.
                if (ids.TryGetValue(id, out var list) && !owners.ContainsKey(list[0]) && !IsAncestorOrSelf(list[0], owner)) owners.Add(list[0], owner);
            }
        }
        return owners;

        bool IsAncestorOrSelf(IElement candidate, IElement element)
        {
            for (IElement? e = element; e is not null; e = owners.TryGetValue(e, out var o) ? o : e.ParentElement)
                if (ReferenceEquals(e, candidate)) return true;
            return false;
        }
    }

    // ── Rules ────────────────────────────────────────────────────────────────

    private static void CheckRoleAttribute(IElement el, AuditContext context)
    {
        var roleAttr = el.GetAttribute("role");
        if (roleAttr is null) return;
        if (string.IsNullOrWhiteSpace(roleAttr))
        {
            context.Report("aria-valid-role", BitBhA11yImpact.Minor, el, "Empty role attribute.");
            return;
        }
        if (BitBhAria.ExplicitRole(el) is null)
            context.Report("aria-valid-role", BitBhA11yImpact.Critical, el, $"role=\"{roleAttr}\" is not a valid WAI-ARIA role.");
    }

    private static void CheckAttributeValues(IElement el, string? role, AuditContext context)
    {
        foreach (var attr in el.Attributes)
        {
            var name = attr.Name;
            if (!name.StartsWith("aria-", StringComparison.Ordinal)) continue;
            var value = attr.Value.Trim();
            var typed = BitBhAria.TokenValues.ContainsKey(name) || BitBhAria.IntegerAttributes.Contains(name) || BitBhAria.NumberAttributes.Contains(name);
            if (value.Length == 0)
            {
                // An empty value means the attribute's default (usually "undefined"): noise, not breakage.
                if (typed || name is "aria-label" or "aria-labelledby" or "aria-describedby")
                    context.Report("aria-valid-attr-value", BitBhA11yImpact.Minor, el, $"{name} is empty, so it is ignored; remove it or give it a value.");
                continue;
            }
            // ARIA token values are ASCII case-insensitive ("True" is "true").
            if (BitBhAria.TokenValues.TryGetValue(name, out var tokens) && !tokens.Contains(value.ToLowerInvariant()))
                context.Report("aria-valid-attr-value", BitBhA11yImpact.Critical, el, $"{name}=\"{value}\" is invalid; expected one of: {string.Join(", ", tokens)}.");
            else if (BitBhAria.IntegerAttributes.Contains(name) && !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                context.Report("aria-valid-attr-value", BitBhA11yImpact.Critical, el, $"{name}=\"{value}\" must be an integer.");
            else if (BitBhAria.NumberAttributes.Contains(name) && !TryNumber(value, out _))
                context.Report("aria-valid-attr-value", BitBhA11yImpact.Critical, el, $"{name}=\"{value}\" must be a number.");
        }

        if (role is "slider" or "meter" or "progressbar" or "spinbutton" or "scrollbar" or "separator" && TryNumber(el.GetAttribute("aria-valuenow"), out var now))
        {
            // ARIA 1.2 defaults: 0 to 100, except spinbutton, which has no bounds unless given.
            double? min = TryNumber(el.GetAttribute("aria-valuemin"), out var mi) ? mi : role == "spinbutton" ? null : 0;
            double? max = TryNumber(el.GetAttribute("aria-valuemax"), out var ma) ? ma : role == "spinbutton" ? null : 100;
            if (now < min)
                context.Report("aria-valid-attr-value", BitBhA11yImpact.Serious, el, $"aria-valuenow ({Format(now)}) is below {Bound("aria-valuemin", min.Value)}.");
            else if (now > max)
                context.Report("aria-valid-attr-value", BitBhA11yImpact.Serious, el, $"aria-valuenow ({Format(now)}) is above {Bound("aria-valuemax", max.Value)}.");
        }

        string Bound(string name, double value) => el.HasAttribute(name) ? $"{name} ({Format(value)})" : $"the default {name} ({Format(value)})";
    }

    private static void CheckIdRefs(IElement el, AuditContext context)
    {
        foreach (var name in BitBhAria.IdRefAttributes.Append("for"))
        {
            var value = el.GetAttribute(name);
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (name == "for" && el.LocalName != "label") continue;
            // A collapsed control may point at popup content that isn't rendered yet (APG / axe).
            if (name == "aria-controls" && BitBhAria.AttributeIs(el, "aria-expanded", "false")) continue;
            foreach (var id in value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (context.Ids.ContainsKey(id)) continue;
                if (context.Options.IsFragment) continue;
                context.Report("idref-exists", name is "aria-activedescendant" or "aria-labelledby" or "for" ? BitBhA11yImpact.Serious : BitBhA11yImpact.Moderate, el,
                    $"{name} references \"{id}\", which doesn't exist.");
            }
        }
    }

    private static void CheckAllowedAttributes(IElement el, string role, AuditContext context)
    {
        foreach (var attr in el.Attributes)
        {
            if (!BitBhAria.RoleSpecificAttributes.TryGetValue(attr.Name, out var roles)) continue;
            if (roles.Contains(role)) continue;
            // Native controls may carry states the browser maps (e.g. aria-checked on input is redundant, not invalid).
            context.Report("aria-allowed-attr", BitBhA11yImpact.Serious, el, $"{attr.Name} is not supported on role \"{role}\"; assistive tech will ignore it.");
        }
    }

    private static void CheckRequiredAttributes(IElement el, string role, AuditContext context)
    {
        if (!BitBhAria.RequiredAttributes.TryGetValue(role, out var required)) return;
        if (BitBhAria.IsNativeControl(el) && BitBhAria.ExplicitRole(el) is null) return;
        if (role == "heading" && el.LocalName is "h1" or "h2" or "h3" or "h4" or "h5" or "h6") return;
        if (role is "checkbox" or "radio" or "switch" && el.LocalName == "input") return;
        if (role == "slider" && el.LocalName == "input") return;
        // Only a focusable separator (a splitter) carries a value.
        if (role == "separator" && !BitBhAria.IsFocusable(el)) return;
        foreach (var attr in required)
        {
            if (role == "scrollbar" && attr == "aria-controls") continue;
            // An empty value is the attribute's default, i.e. missing.
            if (string.IsNullOrWhiteSpace(el.GetAttribute(attr)))
                context.Report("aria-required-attr", BitBhA11yImpact.Critical, el, $"role \"{role}\" requires {attr}.");
        }
        if (role == "combobox" && BitBhAria.AttributeIs(el, "aria-expanded", "true") && !el.HasAttribute("aria-controls") && !el.HasAttribute("aria-owns"))
            context.Report("aria-required-attr", BitBhA11yImpact.Serious, el, "An expanded combobox needs aria-controls pointing at its popup.");
    }

    private static readonly HashSet<string> Transparent = new(StringComparer.Ordinal) { "presentation", "none", "generic" };

    private static bool IsTransparent(string? role) => role is null || Transparent.Contains(role);

    /// <summary>Owned elements: the first descendants with a non-generic role (through generic wrappers), plus aria-owns targets.</summary>
    private static IEnumerable<(IElement Element, string Role)> OwnedRoles(IElement el, AuditContext context, HashSet<IElement> visited)
    {
        if (!visited.Add(el)) yield break;
        var children = el.Children.Where(child => !context.OwnedElsewhere(child, el)).Concat(context.AriaOwned(el));
        foreach (var child in children)
        {
            if (BitBhAria.IsHidden(child)) continue;
            var childRole = BitBhAria.GetRole(child);
            if (IsTransparent(childRole))
            {
                foreach (var owned in OwnedRoles(child, context, visited)) yield return owned;
            }
            else
            {
                yield return (child, childRole!);
            }
        }
    }

    private static void CheckOwnedElements(IElement el, string role, AuditContext context)
    {
        var allowed = role == "group" ? GroupItems(el, context) : BitBhAria.RequiredOwned.GetValueOrDefault(role);
        if (allowed is null) return;
        foreach (var (owned, ownedRole) in OwnedRoles(el, context, []))
        {
            if (allowed.Contains(ownedRole)) continue;
            context.Report("aria-required-children", BitBhA11yImpact.Critical, owned,
                $"role \"{ownedRole}\" is not allowed directly inside role \"{role}\" (allowed: {string.Join(", ", allowed)}).");
        }
    }

    /// <summary>In a listbox, tree or menu, a group may only contain that container's items.</summary>
    private static IReadOnlyList<string>? GroupItems(IElement group, AuditContext context)
    {
        for (var e = context.Parent(group); e is not null; e = context.Parent(e))
        {
            var role = BitBhAria.GetRole(e);
            if (IsTransparent(role) || role is "group" or "treeitem") continue;
            return role switch
            {
                "listbox" => ["option"],
                "tree" => ["treeitem"],
                "menu" or "menubar" => ["menuitem", "menuitemcheckbox", "menuitemradio", "separator"],
                _ => null,
            };
        }
        return null;
    }

    private static void CheckContext(IElement el, string role, AuditContext context)
    {
        if (!BitBhAria.RequiredContext.TryGetValue(role, out var contexts)) return;
        if (role == "listitem" && el.LocalName == "li" && el.GetAttribute("role") is null)
        {
            var parent = context.Parent(el);
            if (parent?.LocalName is "ul" or "ol" or "menu" && parent.GetAttribute("role") is null) return;
        }
        if (role == "option" && el.LocalName == "option") return;

        for (var parent = context.Parent(el); parent is not null; parent = context.Parent(parent))
        {
            var parentRole = BitBhAria.GetRole(parent);
            if (IsTransparent(parentRole)) continue;
            if (contexts.Contains(parentRole!))
            {
                // A group only counts when it is itself in the right container.
                if (parentRole == "group" && role is "option" or "treeitem" or "menuitem" or "menuitemcheckbox" or "menuitemradio")
                {
                    var outer = context.Parent(parent);
                    while (outer is not null && (IsTransparent(BitBhAria.GetRole(outer)) || BitBhAria.GetRole(outer) is "group" or "treeitem"))
                        outer = context.Parent(outer);
                    var outerRole = outer is null ? null : BitBhAria.GetRole(outer);
                    var ok = role switch
                    {
                        "option" => outerRole is "listbox",
                        "treeitem" => outerRole is "tree",
                        _ => outerRole is "menu" or "menubar",
                    };
                    if (!ok) break;
                }
                return;
            }
            break;
        }
        context.Report("aria-required-parent", BitBhA11yImpact.Critical, el, $"role \"{role}\" must be contained by {string.Join(" / ", contexts)}.");
    }

    private static void CheckName(IElement el, string? role, AuditContext context)
    {
        // A text field needs a label whatever its role (password, date, file … have none).
        var field = el.LocalName is "select" or "textarea"
                    || (el.LocalName == "input" && el.GetAttribute("type")?.Trim().ToLowerInvariant() is not ("hidden" or "button" or "submit" or "reset" or "image"));
        var needsName = field || (role is not null && BitBhAria.NameRequired.Contains(role));
        if (!needsName) return;
        if (role == "img" && el.LocalName == "svg" && BitBhAria.IsAriaHidden(el)) return;
        if (role == "option" && el.LocalName == "option") return;
        if (role == "region" && el.LocalName == "section") return;
        // Hidden inputs and inputs inside a labelled group of radios are fine.
        if (el.LocalName == "input" && string.Equals(el.GetAttribute("type")?.Trim(), "hidden", StringComparison.OrdinalIgnoreCase)) return;

        var name = BitBhAccessibleName.Compute(el, out _, out var fromDefault);
        // An image button named only by the browser's default ("Submit") still lacks alt text.
        var imageButton = el.LocalName == "input" && BitBhAria.AttributeIs(el, "type", "image");
        if (name.Length > 0 && !(imageButton && fromDefault)) return;
        var rule = BitBhAria.IsNativeControl(el) && el.LocalName is "input" or "select" or "textarea" ? "form-label" : "accessible-name";
        context.Report(rule, role is "dialog" or "alertdialog" or "region" or "tabpanel" or "listbox" or "tree" or "grid" or "treegrid" or "table" or "radiogroup" or "meter" or "progressbar" or "heading" or "columnheader" or "rowheader" or "tooltip" ? BitBhA11yImpact.Serious : BitBhA11yImpact.Critical, el,
            (role is null ? $"<{el.LocalName}> has no accessible name." : $"Element with role \"{role}\" has no accessible name.")
            + (imageButton ? " Add alt text." : " Add text content, aria-label or aria-labelledby" + (rule == "form-label" ? ", or a <label>." : ".")));
    }

    private static void CheckNestedInteractive(IElement el, string role, AuditContext context)
    {
        if (!BitBhAria.ChildrenPresentational.Contains(role) && role is not "link") return;
        foreach (var descendant in el.QuerySelectorAll("*"))
        {
            if (BitBhAria.IsHidden(descendant) || BitBhAria.IsInert(descendant)) continue;
            var descendantRole = BitBhAria.GetRole(descendant);
            if (BitBhAria.IsTabbable(descendant) || (descendantRole is not null && BitBhAria.Interactive.Contains(descendantRole) && BitBhAria.IsFocusable(descendant)))
            {
                context.Report("nested-interactive", BitBhA11yImpact.Serious, descendant,
                    $"Interactive element nested inside role \"{role}\": screen readers don't announce it and keyboard behaviour is unpredictable.");
                return;
            }
        }
    }

    private static void CheckPresentation(IElement el, AuditContext context)
    {
        if (BitBhAria.ExplicitRole(el) is not ("presentation" or "none")) return;
        if (BitBhAria.IsFocusable(el))
            context.Report("presentation-conflict", BitBhA11yImpact.Minor, el, "A focusable element can't be presentational; the role is ignored.");
        else if (el.Attributes.Any(a => BitBhAria.GlobalAttributes.Contains(a.Name) && !string.IsNullOrWhiteSpace(a.Value)))
            context.Report("presentation-conflict", BitBhA11yImpact.Minor, el, "Global ARIA attributes on a presentational element cause the role to be ignored.");
    }

    private static void CheckActiveDescendant(IElement el, AuditContext context)
    {
        var id = el.GetAttribute("aria-activedescendant");
        if (string.IsNullOrWhiteSpace(id) || !context.Ids.TryGetValue(id, out var targets)) return;
        var target = targets[0];
        if (el.Contains(target)) return;
        foreach (var owner in new[] { "aria-controls", "aria-owns" })
        {
            var refs = el.GetAttribute(owner);
            if (refs is null) continue;
            foreach (var r in refs.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                if (context.Ids.TryGetValue(r, out var controlled) && controlled[0].Contains(target)) return;
        }
        context.Report("activedescendant-target", BitBhA11yImpact.Serious, el,
            $"aria-activedescendant points at \"{id}\", which is neither inside the element nor inside the element it controls.");
    }

    private static bool TryNumber(string? value, out double number)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);

    /// <summary>Numbers in messages are culture-invariant: the report reads the same on every machine.</summary>
    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

    // ── Formatting ───────────────────────────────────────────────────────────

    /// <summary>A short CSS selector locating <paramref name="element"/>.</summary>
    public static string Selector(IElement element)
    {
        if (!string.IsNullOrEmpty(element.Id)) return $"#{CssEscape(element.Id)}";
        var parts = new List<string>();
        for (var e = element; e is not null && parts.Count < 4; e = e.ParentElement)
        {
            if (!string.IsNullOrEmpty(e.Id))
            {
                parts.Insert(0, $"#{CssEscape(e.Id)}");
                break;
            }
            var part = e.LocalName;
            var role = e.GetAttribute("role");
            if (role is not null) part += $"[role=\"{role.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"]";
            else if (e.ClassList.Length > 0) part += "." + string.Join('.', e.ClassList.Take(2).Select(CssEscape));
            parts.Insert(0, part);
        }
        return string.Join(" > ", parts);
    }

    /// <summary>Escapes a CSS identifier (CSSOM "serialize an identifier", like <c>CSS.escape()</c>).</summary>
    internal static string CssEscape(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\0') sb.Append('�');
            else if (c is >= '\x01' and <= '\x1F' or '\x7F' || (i == 0 && char.IsAsciiDigit(c)) || (i == 1 && char.IsAsciiDigit(c) && value[0] == '-'))
                sb.Append('\\').Append(((int)c).ToString("x", CultureInfo.InvariantCulture)).Append(' ');
            else if (i == 0 && c == '-' && value.Length == 1) sb.Append("\\-");
            else if (c >= '\x80' || c is '-' or '_' || char.IsAsciiLetterOrDigit(c)) sb.Append(c);
            else sb.Append('\\').Append(c);
        }
        return sb.ToString();
    }

    private static string OpeningTag(IElement element)
    {
        var html = element.OuterHtml;
        var end = html.IndexOf('>');
        var tag = end > 0 ? html[..(end + 1)] : html;
        return tag.Length > 200 ? tag[..200] + "…" : tag;
    }
}
