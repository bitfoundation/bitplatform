using System.Text;
using System.Text.RegularExpressions;
using Bit.Bortable.Demo.Server.Dtos;

namespace Bit.Bortable.Demo.Server.Services;

/// <summary>
/// The CSS custom properties Bit.Bortable's drag states are painted with, read out of the stylesheet
/// the library ships: what each one is, its default, which rules paint with it, and which component
/// parameters name the classes those rules style.
/// <para>
/// The stylesheet has two kinds of property, and telling them apart is the whole point of reading
/// it rather than a table. Three are inputs, declared on <c>:root</c>: the accent (an "r, g, b"
/// triplet) and two opacities. The five colours derived from the accent are declared nowhere - each
/// state rule reads its own one with a fallback, <c>var(--bit-bortable-chosen-bg, rgba(var(--bit-bortable-accent-rgb), 0.12))</c>,
/// so the colour is worked out on the item itself, from whatever accent it inherits. Declared on
/// <c>:root</c> instead, they would be resolved there once, and an accent set on one list would never
/// reach its items. So an agent asking "what is --bit-bortable-chosen-bg set to" has to be told it is
/// set to nothing, and what the rule falls back to - which is only knowable from the rule.
/// </para>
/// <para>
/// The classes are the other half: the container, the item wrapper and the drag states, served by
/// <see cref="BortableStylesheetClasses"/> and rendered through the same tool, since "how do I style
/// the dragged item" is answered by a class and the properties its rule reads together.
/// </para>
/// </summary>
public static partial class BortableThemeCatalog
{
    private const string RootSelector = ":root";
    private const string Prefix = "--bit-bortable-";

    public const string DeclaredGroup = "Declared on :root";
    public const string OverrideGroup = "Optional override";

    /// <summary>One rule of the stylesheet, as written: its selector, the comment above it, its declarations in order.</summary>
    internal sealed record CssRule(string Selector, string? Comment, (string Name, string Value, string? Comment)[] Declarations)
    {
        /// <summary>The rule as one line, "selector { name: value; ... }".</summary>
        public override string ToString() => $"{Selector} {{ {string.Join("; ", Declarations.Select(d => $"{d.Name}: {d.Value}"))} }}";
    }

    private sealed record Theme(BortableThemePropertyDto[] Properties, string? OverrideIntro, CssRule[] Rules);

    private static readonly Lazy<Theme> _theme = new(Build);

    /// <summary>Every custom property of the stylesheet: the declared ones first, then the optional overrides, each in file order.</summary>
    public static BortableThemePropertyDto[] Properties => _theme.Value.Properties;

    /// <summary>Every rule of the stylesheet, including the ones inside a media query.</summary>
    internal static CssRule[] Rules => _theme.Value.Rules;

    /// <summary>
    /// One property by its name - "--bit-bortable-ghost-opacity", "bit-bortable-ghost-opacity",
    /// "ghost-opacity" or "var(--bit-bortable-ghost-opacity)" - or null.
    /// </summary>
    public static BortableThemePropertyDto? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var text = name.Trim();
        if (text.StartsWith("var(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(')')) text = text[4..^1].Trim();

        text = text.TrimStart('-');
        if (text.StartsWith("bit-bortable-", StringComparison.OrdinalIgnoreCase) is false) text = $"bit-bortable-{text}";

        return Properties.FirstOrDefault(property => string.Equals(property.Name, $"--{text}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The theme as Markdown tables, as GetBortableTheme answers when it is called without a name.</summary>
    public static string RenderIndex()
    {
        var theme = _theme.Value;
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bortable theme").AppendLine();
        builder.AppendLine("`_content/Bit.Bortable/bit-bortable.css` styles the drag states only - the look of the list and its items is ")
               .AppendLine("your own CSS, through `Class`, `Style`, `ItemClass` and `ItemClassSelector`. The states' colours and opacities are ")
               .AppendLine("the CSS custom properties below, read out of the shipped file. Override them on `:root`, on one list's `Class`, ")
               .AppendLine("or inline through its `Style`, from a stylesheet loaded after the library's:")
               .AppendLine()
               .AppendLine("```css")
               .AppendLine(":root { --bit-bortable-accent-rgb: 109, 40, 217; }")
               .AppendLine(".my-board { --bit-bortable-ghost-opacity: 0.25; --bit-bortable-selected-bg: #fde68a; }")
               .AppendLine("```")
               .AppendLine()
               .AppendLine("Call `GetBortableTheme(name: \"...\")` for one property in full - every rule that paints with it and the ")
               .AppendLine("parameters that name the classes those rules style - or for one of the classes at the bottom.")
               .AppendLine();

        builder.AppendLine($"## {DeclaredGroup}").AppendLine()
               .AppendLine("The inputs: the stylesheet declares these, so each has a value of its own to override.").AppendLine()
               .AppendLine("| Property | Default | Paints |")
               .AppendLine("| --- | --- | --- |");

        foreach (var property in Properties.Where(property => property.Group == DeclaredGroup))
        {
            builder.AppendLine($"| `{property.Name}` | `{Escape(property.Value)}` | {Escape(Paints(property))} |");
        }

        builder.AppendLine().AppendLine($"## {OverrideGroup}s").AppendLine();

        if (theme.OverrideIntro is not null) builder.AppendLine($"> {theme.OverrideIntro}").AppendLine();

        builder.AppendLine("Declared nowhere: the rule that paints with each one reads it with a fallback, and the fallback is its default. ")
               .AppendLine("Set one to replace that colour outright; leave it unset and it follows the accent.")
               .AppendLine()
               .AppendLine("| Property | Default (the fallback) | Paints |")
               .AppendLine("| --- | --- | --- |");

        foreach (var property in Properties.Where(property => property.Group == OverrideGroup))
        {
            builder.AppendLine($"| `{property.Name}` | `{Escape(property.Value)}` | {Escape(Paints(property))} |");
        }

        builder.AppendLine().AppendLine("## Drag-state classes").AppendLine()
               .AppendLine("| Class | Set on | Replaced by | Styled with |")
               .AppendLine("| --- | --- | --- | --- |");

        foreach (var cssClass in BortableStylesheetClasses.Classes)
        {
            var parameter = cssClass.Parameter is null ? "-" : $"`{cssClass.Parameter}`";

            builder.AppendLine($"| `{cssClass.Name}` | {Escape(cssClass.SetOn ?? "-")} | {parameter} | {Escape(StyledWith(cssClass))} |");
        }

        var replaceable = BortableStylesheetClasses.Classes.Where(cssClass => cssClass.Parameter is not null).Select(cssClass => $"`{cssClass.Parameter}`").ToArray();

        if (replaceable.Length > 0)
        {
            builder.AppendLine()
                   .AppendLine($"{string.Join(", ", replaceable)} replace the class of their state on one list: that list renders its own class ")
                   .AppendLine("there instead, and the stylesheet's rule for the state no longer applies to it. The other classes are always ")
                   .AppendLine("rendered under these names.");
        }

        return builder.ToString();
    }

    /// <summary>
    /// One property - or one of the stylesheet's classes - in full as Markdown, or null when the
    /// stylesheet has neither by that name. A property is looked for first: "ghost-opacity" is a
    /// property, "ghost" is the class <c>bit-bortable-ghost</c>.
    /// </summary>
    public static string? Render(string name)
    {
        var property = name.TrimStart().StartsWith('.') ? null : Find(name);

        if (property is null) return BortableStylesheetClasses.Render(name);

        var builder = new StringBuilder();

        builder.AppendLine($"# `{property.Name}`").AppendLine();

        if (property.Group == DeclaredGroup)
        {
            builder.AppendLine($"An input, declared on `{RootSelector}` by the shipped stylesheet.").AppendLine()
                   .AppendLine($"- **Default** = `{property.Value}`");
        }
        else
        {
            builder.AppendLine("An optional override: the stylesheet declares it nowhere. The rule that paints with it reads it with a ")
                   .AppendLine("fallback, resolved on the item itself - so leaving it unset follows the accent the item inherits, and setting ")
                   .AppendLine("it replaces the colour outright.").AppendLine()
                   .AppendLine($"- **Default** (the fallback) = `{property.Value}`");
        }

        if (property.Reads.Length > 0) builder.AppendLine($"- **Reads** {string.Join(", ", property.Reads.Select(read => $"`{read}`"))}");
        if (property.ReadBy.Length > 0) builder.AppendLine($"- **Read by the defaults of** {string.Join(", ", property.ReadBy.Select(read => $"`{read}`"))}");
        if (property.Comment is not null) builder.AppendLine($"- **From the stylesheet**: {property.Comment}");

        builder.AppendLine().AppendLine("## Painted by").AppendLine();

        if (property.PaintedBy.Length == 0) builder.AppendLine("No rule of the stylesheet reads it.");

        foreach (var (rule, through) in PaintingRules(property.Name))
        {
            builder.AppendLine(through is null ? $"- `{rule}`" : $"- `{rule}` - through the fallback of `{through}`, while that one is unset");
        }

        builder.AppendLine();

        if (property.Parameters.Length > 0)
        {
            builder.AppendLine("## Parameters that name the classes these rules style").AppendLine()
                   .AppendLine("A list that sets one of them to a class of its own is no longer painted by these rules; style its class instead.")
                   .AppendLine();

            foreach (var parameter in property.Parameters) builder.AppendLine($"- `{parameter}`");

            builder.AppendLine();
        }

        builder.AppendLine("## Override").AppendLine()
               .AppendLine("```css")
               .AppendLine($"{RootSelector} {{ {property.Name}: {(property.Name == $"{Prefix}accent-rgb" ? "109, 40, 217" : "<value>")}; }}")
               .AppendLine($".my-board {{ {property.Name}: <value>; }} /* one list, given Class=\"my-board\" */")
               .AppendLine("```").AppendLine();

        builder.AppendLine(property.Group == DeclaredGroup && property.ReadBy.Length > 0
            ? "Load the override after `_content/Bit.Bortable/bit-bortable.css`. Because the colours that read it are fallbacks resolved on each item, an override on one list reaches that list's items - the chosen item, the swap target and the selection follow it."
            : "Load the override after `_content/Bit.Bortable/bit-bortable.css`, or set it inline through a list's `Style`.");

        return builder.ToString();
    }

    /// <summary>The rules that paint with a property, each with the property whose fallback it is read through, if any.</summary>
    private static IEnumerable<(string Rule, string? Through)> PaintingRules(string name)
    {
        foreach (var rule in Rules)
        {
            foreach (var declaration in rule.Declarations.Where(declaration => IsCustom(declaration.Name) is false))
            {
                if (VariablesIn(declaration.Value).Contains(name, StringComparer.Ordinal) is false) continue;

                yield return ($"{rule.Selector} {{ {declaration.Name}: {declaration.Value} }}", FallbackOwner(declaration.Value, name));
            }
        }
    }

    /// <summary>
    /// The variable whose fallback reads <paramref name="name"/> in a value, or null when the value
    /// reads it directly: in <c>var(--a, rgba(var(--b), 0.12))</c>, <c>--b</c> is read through <c>--a</c>.
    /// </summary>
    private static string? FallbackOwner(string value, string name)
    {
        foreach (var (variable, fallback) in References(value))
        {
            if (variable == name) return null;
            if (fallback is not null && VariablesIn(fallback).Contains(name, StringComparer.Ordinal)) return variable;
        }

        return null;
    }

    private static string Paints(BortableThemePropertyDto property)
    {
        var selectors = property.PaintedBy.Select(rule => rule[..rule.IndexOf(" {", StringComparison.Ordinal)])
                                          .Distinct(StringComparer.Ordinal)
                                          .ToArray();

        if (selectors.Length == 0) return "-";

        var shown = string.Join(", ", selectors.Take(3).Select(selector => $"`{selector}`"));

        return selectors.Length > 3 ? $"{shown} +{selectors.Length - 3} more" : shown;
    }

    /// <summary>
    /// What a class looks like, in a table cell: the declarations of the rule that is the class itself,
    /// and the selectors of the rules it is only part of.
    /// </summary>
    private static string StyledWith(BortableStateClassDto cssClass)
    {
        var parts = new List<string>();

        foreach (var rule in Rules.Where(rule => BortableStylesheetClasses.ClassesIn(rule.Selector).Contains(cssClass.Name, StringComparer.Ordinal)))
        {
            parts.Add(rule.Selector == $".{cssClass.Name}"
                ? $"`{string.Join("; ", rule.Declarations.Select(d => $"{d.Name}: {d.Value}"))}`"
                : $"`{rule.Selector}`");
        }

        if (parts.Count == 0) return "-";

        return parts.Count > 3 ? $"{string.Join(", ", parts.Take(3))} +{parts.Count - 3} more" : string.Join(", ", parts);
    }

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static bool IsCustom(string name) => name.StartsWith("--", StringComparison.Ordinal);

    private static Theme Build()
    {
        var css = BortableSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);
        var rules = ParseRules(css);

        // The inputs: what :root declares.
        var root = rules.Where(rule => rule.Selector == RootSelector).ToArray();
        var declared = root.SelectMany(rule => rule.Declarations)
                           .Where(declaration => declaration.Name.StartsWith(Prefix, StringComparison.Ordinal))
                           .GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
                           .Select(group => group.Last())
                           .ToArray();

        // The comment above :root is where the stylesheet explains why the derived colours are not
        // declared there - the one thing about overriding them nobody guesses.
        var overrideIntro = root.Select(rule => rule.Comment).FirstOrDefault(comment => comment is not null);

        var declaredNames = declared.Select(declaration => declaration.Name).ToHashSet(StringComparer.Ordinal);

        // The optional overrides: every property a rule reads that :root does not declare, with the
        // fallback it is read with and the rule that reads it.
        var overrides = new List<(string Name, string Fallback, CssRule Rule)>();

        foreach (var rule in rules)
        {
            foreach (var declaration in rule.Declarations.Where(declaration => IsCustom(declaration.Name) is false))
            {
                foreach (var (variable, fallback) in References(declaration.Value))
                {
                    if (variable.StartsWith(Prefix, StringComparison.Ordinal) is false || declaredNames.Contains(variable)) continue;
                    if (overrides.Any(item => item.Name == variable)) continue;

                    overrides.Add((variable, fallback ?? string.Empty, rule));
                }
            }
        }

        var parameterByClass = BortableStylesheetClasses.ParameterByClass;

        var properties = new List<BortableThemePropertyDto>();

        properties.AddRange(declared.Select(declaration => Describe(declaration.Name, DeclaredGroup, declaration.Value, declaration.Comment)));
        properties.AddRange(overrides.Select(item => Describe(item.Name, OverrideGroup, item.Fallback, item.Rule.Comment)));

        // Who reads whom is only known once every default is.
        var readBy = properties.ToDictionary(
            property => property.Name,
            property => properties.Where(other => other.Reads.Contains(property.Name, StringComparer.Ordinal)).Select(other => other.Name).ToArray(),
            StringComparer.Ordinal);

        return new Theme([.. properties.Select(property => property with { ReadBy = readBy[property.Name] })], overrideIntro, [.. rules]);

        BortableThemePropertyDto Describe(string name, string group, string value, string? comment)
        {
            var paintedBy = PaintedBy(rules, name);

            return new BortableThemePropertyDto
            {
                Name = name,
                Group = group,
                Value = value,
                Reads = [.. VariablesIn(value).Distinct(StringComparer.Ordinal)],
                ReadBy = [],
                Comment = comment,
                PaintedBy = paintedBy,
                Parameters = [.. paintedBy.SelectMany(rule => BortableStylesheetClasses.ClassesIn(rule[..rule.IndexOf(" {", StringComparison.Ordinal)]))
                                          .Distinct(StringComparer.Ordinal)
                                          .Where(parameterByClass.ContainsKey)
                                          .Select(cssClass => $"{BortableStylesheetClasses.ComponentName}.{parameterByClass[cssClass]}")]
            };
        }
    }

    /// <summary>
    /// The declarations that paint with a property, directly or through another one's fallback, as
    /// "selector { declaration }". The custom-property declarations of <c>:root</c> are the inputs
    /// themselves rather than paint, so only ordinary declarations count.
    /// </summary>
    private static string[] PaintedBy(IEnumerable<CssRule> rules, string name)
    {
        return [.. rules.SelectMany(rule => rule.Declarations
                            .Where(declaration => IsCustom(declaration.Name) is false
                                                  && VariablesIn(declaration.Value).Contains(name, StringComparer.Ordinal))
                            .Select(declaration => $"{rule.Selector} {{ {declaration.Name}: {declaration.Value} }}"))
                        .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Every <c>--bit-bortable-*</c> property a value reads, fallbacks included, in order.</summary>
    internal static IEnumerable<string> VariablesIn(string value)
        => References(value).Select(reference => reference.Name).Where(name => name.StartsWith(Prefix, StringComparison.Ordinal));

    /// <summary>
    /// Every <c>var()</c> in a value with its fallback, nested ones included: a regex cannot match the
    /// parentheses of <c>var(--a, rgba(var(--b), 0.12))</c>, so they are counted.
    /// </summary>
    internal static IEnumerable<(string Name, string? Fallback)> References(string value)
    {
        var index = 0;

        while ((index = value.IndexOf("var(", index, StringComparison.Ordinal)) >= 0)
        {
            var start = index + 4;
            var depth = 1;
            var comma = -1;
            var end = start;

            for (; end < value.Length && depth > 0; end++)
            {
                if (value[end] == '(') depth++;
                else if (value[end] == ')') depth--;
                else if (value[end] == ',' && depth == 1 && comma < 0) comma = end;
            }

            // An unbalanced var( is not CSS; what there is of it is read as far as it goes.
            var close = depth == 0 ? end - 1 : value.Length;

            yield return comma < 0
                ? (value[start..close].Trim(), null)
                : (value[start..comma].Trim(), value[(comma + 1)..close].Trim());

            // From inside, so the var() in a fallback is found as well.
            index = start;
        }
    }

    /// <summary>
    /// Every rule of the stylesheet, top-level or inside a media query, with the comment right above it
    /// and, per declaration, the comment right above that declaration.
    /// <para>
    /// A scanner rather than a regex, because the stylesheet's header comment holds an example rule -
    /// <c>:root { --bit-bortable-accent-rgb: ... }</c> - whose braces a regex would read as a rule of the
    /// file. A comment inside a block describes the one declaration it sits on: the <c>:root</c> block's
    /// comment on the accent says nothing about the two opacities that follow it.
    /// </para>
    /// </summary>
    internal static List<CssRule> ParseRules(string css)
    {
        var rules = new List<CssRule>();
        var head = new StringBuilder();
        string? comment = null;

        for (var i = 0; i < css.Length;)
        {
            if (IsCommentStart(css, i))
            {
                var end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) break;

                // A comment between rules is about the rule that follows; a later one replaces an earlier
                // one, so the file's header is not mistaken for the first rule's description.
                if (string.IsNullOrWhiteSpace(head.ToString())) comment = Clean(css[(i + 2)..end].TrimStart('!'));

                i = end + 2;
                continue;
            }

            switch (css[i])
            {
                case '{':
                    var selector = Clean(head.ToString());
                    head.Clear();

                    // An at-rule's block (a media query) holds rules of its own; they are read as they come.
                    if (selector.StartsWith('@'))
                    {
                        comment = null;
                        i++;
                        continue;
                    }

                    var close = BodyEnd(css, i + 1);

                    rules.Add(new CssRule(selector, comment, Declarations(css[(i + 1)..close])));

                    comment = null;
                    i = close + 1;
                    continue;

                case '}':
                    // The end of an at-rule's block.
                    head.Clear();
                    comment = null;
                    i++;
                    continue;

                default:
                    head.Append(css[i]);
                    i++;
                    continue;
            }
        }

        return rules;
    }

    private static bool IsCommentStart(string css, int i) => css[i] == '/' && i + 1 < css.Length && css[i + 1] == '*';

    /// <summary>The closing brace of a rule's body, skipping the comments inside it.</summary>
    private static int BodyEnd(string css, int start)
    {
        for (var i = start; i < css.Length; i++)
        {
            if (IsCommentStart(css, i))
            {
                var end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) return css.Length;
                i = end + 1;
                continue;
            }

            if (css[i] == '}') return i;
        }

        return css.Length;
    }

    private static (string Name, string Value, string? Comment)[] Declarations(string body)
    {
        var declarations = new List<(string, string, string?)>();
        string? pending = null;

        foreach (Match token in TokenRegex().Matches(body))
        {
            if (token.Groups["comment"].Success)
            {
                pending = Clean(token.Groups["comment"].Value);
                continue;
            }

            declarations.Add((token.Groups["name"].Value, Clean(token.Groups["value"].Value), pending));
            pending = null;
        }

        return [.. declarations];
    }

    private static string Clean(string text) => WhitespaceRegex().Replace(text, " ").Trim();

    [GeneratedRegex(@"/\*(?<comment>.*?)\*/|(?<name>-{0,2}[a-zA-Z][a-zA-Z0-9-]*)\s*:\s*(?<value>[^;]+?)\s*(;|$)", RegexOptions.Singleline)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
