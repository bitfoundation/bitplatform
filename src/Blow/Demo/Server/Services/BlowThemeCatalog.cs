using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using Bit.Blow.Demo.Server.Dtos;

namespace Bit.Blow.Demo.Server.Services;

/// <summary>
/// The CSS custom properties Bit.Blow's theme is built from, read out of the stylesheet the library
/// ships: what each one is, its light and dark value, which rules paint with it, and which component
/// parameters default to it.
/// <para>
/// Restyling a flow means overriding these, and only these: every colour of the canvas, its nodes,
/// edges, handles and add-ons is routed through a <c>--bit-blow-*</c> property on <c>.bit-blow</c>,
/// so an override needs no selector of its own. Dark mode is two layers deep - the dark palette is a
/// second set of properties, <c>--bit-blow-dark-*</c>, that the colour-mode classes copy over the
/// first - which is exactly the kind of thing an agent restyling dark mode gets wrong by overriding
/// the light property and wondering why nothing changed. Parsing the shipped file rather than keeping
/// a table means the answer cannot name a property the library dropped, or miss one it added.
/// </para>
/// </summary>
public static partial class BlowThemeCatalog
{
    private const string RootSelector = ".bit-blow";
    private const string DarkSelector = ".bit-blow.bit-blow-dark";
    private const string SystemSelector = ".bit-blow.bit-blow-system";
    private const string DarkPrefix = "--bit-blow-dark-";

    public const string PaletteGroup = "Palette";
    public const string DarkPaletteGroup = "Dark palette";

    /// <summary>One rule of the stylesheet, as written: its selector, the comment above it, its declarations in order.</summary>
    internal sealed record CssRule(string Selector, string? Comment, (string Name, string Value, string? Comment)[] Declarations);

    private sealed record Theme(BlowThemePropertyDto[] Properties, string? DarkIntro, CssRule[] Rules);

    private static readonly Lazy<Theme> _theme = new(Build);

    /// <summary>Every custom property of the theme, palette first, in the order the stylesheet declares them.</summary>
    public static BlowThemePropertyDto[] Properties => _theme.Value.Properties;

    /// <summary>Every rule of the stylesheet, including the ones inside a media query.</summary>
    internal static CssRule[] Rules => _theme.Value.Rules;

    /// <summary>
    /// One property by its name - "--bit-blow-node-bg", "bit-blow-node-bg" or just "node-bg" - or null.
    /// </summary>
    public static BlowThemePropertyDto? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var text = name.Trim();
        if (text.StartsWith("var(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(')')) text = text[4..^1].Trim();

        text = text.TrimStart('-');
        if (text.StartsWith("bit-blow-", StringComparison.OrdinalIgnoreCase) is false) text = $"bit-blow-{text}";

        return Properties.FirstOrDefault(property => string.Equals(property.Name, $"--{text}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The theme as Markdown tables, as GetBlowTheme answers when it is called without a property.</summary>
    public static string RenderIndex()
    {
        var theme = _theme.Value;
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Blow theme").AppendLine();
        builder.AppendLine($"Every colour of the flow - canvas, nodes, edges, handles and the add-ons drawn over them - is a CSS custom ")
               .AppendLine($"property on `{RootSelector}`, read out of the shipped `bit-blow.css` below. Override them from your own ")
               .AppendLine("stylesheet, loaded after `_content/Bit.Blow/bit-blow.css`, without touching a single library selector:")
               .AppendLine()
               .AppendLine("```css")
               .AppendLine($"{RootSelector} {{ --bit-blow-node-selected: #7c3aed; --bit-blow-edge-selected: #7c3aed; }}")
               .AppendLine($"{RootSelector} {{ --bit-blow-dark-accent: #a78bfa; }}")
               .AppendLine("```")
               .AppendLine()
               .AppendLine("`BlowCanvas.ColorMode` picks the palette: `Light` (the default) uses the values below, `Dark` adds ")
               .AppendLine("`bit-blow-dark` to the canvas and `System` adds `bit-blow-system`, which turns dark under ")
               .AppendLine("`prefers-color-scheme: dark`. Dark mode does not have values of its own on the palette properties: it copies ")
               .AppendLine("the `--bit-blow-dark-*` properties over them - so to restyle dark mode, override the property named in the ")
               .AppendLine("Dark column, not the palette property. Call `GetBlowTheme(property: \"...\")` for one property in full: every ")
               .AppendLine("rule that paints with it, and the component parameters that default to it.")
               .AppendLine();

        builder.AppendLine($"## {PaletteGroup}").AppendLine();

        string? comment = null;
        var open = false;

        foreach (var property in Properties.Where(property => property.Group == PaletteGroup))
        {
            // The stylesheet files the add-ons' colours under a comment of their own; the table is split
            // where it is, so the comment sits above the rows it explains.
            if (property.Comment != comment)
            {
                if (open) builder.AppendLine();
                if (property.Comment is not null) builder.AppendLine($"> {property.Comment}").AppendLine();
                comment = property.Comment;
                open = false;
            }

            if (open is false)
            {
                builder.AppendLine("| Property | Light | Dark | Paints |")
                       .AppendLine("| --- | --- | --- | --- |");
                open = true;
            }

            var dark = property.DarkValue is null ? "same" : $"`{Escape(property.DarkValue)}` via `{property.DarkSource}`";

            builder.AppendLine($"| `{property.Name}` | `{Escape(property.Value)}` | {dark} | {Escape(Paints(property))} |");
        }

        builder.AppendLine().AppendLine($"## {DarkPaletteGroup}").AppendLine();
        if (theme.DarkIntro is not null) builder.AppendLine(theme.DarkIntro).AppendLine();

        builder.AppendLine($"Copied over the palette by `{DarkSelector}`, and by `{SystemSelector}` under `prefers-color-scheme: dark`.")
               .AppendLine()
               .AppendLine("| Property | Value | Copied over |")
               .AppendLine("| --- | --- | --- |");

        foreach (var property in Properties.Where(property => property.Group == DarkPaletteGroup))
        {
            var feeds = property.Feeds is { Length: > 0 } ? string.Join(", ", property.Feeds.Select(name => $"`{name}`")) : "nothing";

            builder.AppendLine($"| `{property.Name}` | `{Escape(property.Value)}` | {feeds} |");
        }

        return builder.ToString();
    }

    /// <summary>One property in full as Markdown, or null when the stylesheet declares no such property.</summary>
    public static string? Render(string name)
    {
        var property = Find(name);
        if (property is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# `{property.Name}`").AppendLine();

        if (property.Group == PaletteGroup)
        {
            builder.AppendLine($"A palette property, declared on `{RootSelector}` by the shipped stylesheet.").AppendLine()
                   .AppendLine($"- **Light** (and `ColorMode=\"BlowColorMode.Light\"`, the default) = `{property.Value}`")
                   .AppendLine(property.DarkValue is null
                       ? "- **Dark** = the same value: the dark colour mode leaves this property alone"
                       : $"- **Dark** = `{property.DarkValue}`, copied from `{property.DarkSource}` - override that property to change it in dark mode");
        }
        else
        {
            builder.AppendLine($"A dark-palette property, declared on `{RootSelector}`: nothing paints with it directly - in dark mode the ")
                   .AppendLine($"colour-mode classes copy it over the palette.").AppendLine()
                   .AppendLine($"- **Value** = `{property.Value}`")
                   .AppendLine($"- **Copied over** {(property.Feeds is { Length: > 0 } ? string.Join(", ", property.Feeds.Select(feed => $"`{feed}`")) : "nothing")}");
        }

        if (property.Comment is not null) builder.AppendLine($"- **From the stylesheet**: {property.Comment}");

        builder.AppendLine();

        var painted = property.Group == PaletteGroup
            ? property.PaintedBy
            : [.. (property.Feeds ?? []).SelectMany(feed => Find(feed)?.PaintedBy ?? [])
                                         .Distinct(StringComparer.Ordinal)];

        builder.AppendLine(property.Group == PaletteGroup ? "## Painted by" : "## Painted by, in dark mode").AppendLine();

        if (painted.Length == 0) builder.AppendLine("No rule of the stylesheet reads it - see the components below.");

        foreach (var rule in painted) builder.AppendLine($"- `{rule}`");

        builder.AppendLine();

        if (property.Components.Length > 0)
        {
            builder.AppendLine("## Components that paint with it in their own markup").AppendLine()
                   .AppendLine("These write the property into the markup they render, so overriding it restyles them; a colour parameter of ")
                   .AppendLine("theirs, where they have one, still wins for that one instance.")
                   .AppendLine();

            foreach (var component in property.Components) builder.AppendLine($"- `{component}`");

            builder.AppendLine();
        }

        if (property.Parameters.Length > 0)
        {
            builder.AppendLine("## Component parameters that default to it").AppendLine()
                   .AppendLine("Setting the parameter replaces the property for that one component; overriding the property restyles every one of them.")
                   .AppendLine();

            foreach (var parameter in property.Parameters) builder.AppendLine($"- `{parameter}`");

            builder.AppendLine();
        }

        builder.AppendLine("## Override").AppendLine()
               .AppendLine("```css");

        if (property.Group == PaletteGroup)
        {
            builder.AppendLine($"{RootSelector} {{ {property.Name}: <light value>; }}");
            if (property.DarkSource is not null) builder.AppendLine($"{RootSelector} {{ {property.DarkSource}: <dark value>; }}");
        }
        else
        {
            builder.AppendLine($"{RootSelector} {{ {property.Name}: <dark value>; }}");
        }

        builder.AppendLine("```").AppendLine()
               .AppendLine("Load the override after `_content/Bit.Blow/bit-blow.css`. To restyle one flow rather than every one on the page, ")
               .AppendLine("give that canvas a `Class` and write the override against `.bit-blow.your-class`.");

        return builder.ToString();
    }

    private static string Paints(BlowThemePropertyDto property)
    {
        var selectors = property.PaintedBy.Select(rule => rule[..rule.IndexOf(" {", StringComparison.Ordinal)])
                                          .Concat(property.Parameters)
                                          .Concat(property.Components)
                                          .Distinct(StringComparer.Ordinal)
                                          .ToArray();

        if (selectors.Length == 0) return "-";

        var shown = string.Join(", ", selectors.Take(3).Select(selector => $"`{selector}`"));

        return selectors.Length > 3 ? $"{shown} +{selectors.Length - 3} more" : shown;
    }

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static Theme Build()
    {
        var css = BlowSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);
        var rules = ParseRules(css);

        var declared = new List<(string Name, string Value, string? Comment)>();
        string? darkIntro = null;

        foreach (var rule in rules.Where(rule => rule.Selector == RootSelector))
        {
            var custom = rule.Declarations.Where(declaration => declaration.Name.StartsWith("--", StringComparison.Ordinal)).ToArray();
            if (custom.Length == 0) continue;

            // The comment above the dark palette explains the plumbing that copies it over the light
            // one, which is the one thing about dark mode nobody guesses. (The light palette's block
            // opens the file, so what sits above it is the file's title rather than an explanation.)
            if (custom.Any(declaration => declaration.Name.StartsWith(DarkPrefix, StringComparison.Ordinal))) darkIntro ??= rule.Comment;

            declared.AddRange(custom);
        }

        var values = declared.GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
                             .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

        // What the dark colour mode does to each palette property. Only the explicit class is read for
        // the values; the media-query copy is the same list by intent, and the oracle tests hold it to that.
        var darkCopies = rules.Where(rule => rule.Selector == DarkSelector)
                              .SelectMany(rule => rule.Declarations)
                              .Where(declaration => declaration.Name.StartsWith("--", StringComparison.Ordinal))
                              .GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
                              .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

        var parameters = ParametersReading();
        var components = ComponentsReading();

        var properties = new List<BlowThemePropertyDto>();

        foreach (var (name, value, comment) in declared.DistinctBy(declaration => declaration.Name, StringComparer.Ordinal))
        {
            var isDark = name.StartsWith(DarkPrefix, StringComparison.Ordinal);

            string? darkSource = null;
            string? darkValue = null;

            if (isDark is false && darkCopies.TryGetValue(name, out var copy))
            {
                darkSource = VariableIn(copy);
                darkValue = darkSource is not null && values.TryGetValue(darkSource, out var resolved) ? resolved : copy;
            }

            properties.Add(new BlowThemePropertyDto
            {
                Name = name,
                Group = isDark ? DarkPaletteGroup : PaletteGroup,
                Value = values[name],
                DarkValue = darkValue == values[name] ? null : darkValue,
                DarkSource = darkValue == values[name] ? null : darkSource,
                Feeds = isDark
                    ? [.. darkCopies.Where(pair => VariableIn(pair.Value) == name).Select(pair => pair.Key)]
                    : null,
                Comment = comment,
                PaintedBy = isDark ? [] : PaintedBy(rules, name),
                Parameters = parameters.GetValueOrDefault(name) ?? [],
                // A component already named through one of its parameters is not named a second time.
                Components = [.. (components.GetValueOrDefault(name) ?? [])
                    .Where(component => (parameters.GetValueOrDefault(name) ?? []).Any(parameter => parameter.StartsWith($"{component}.", StringComparison.Ordinal)) is false)]
            });
        }

        return new Theme([.. properties.OrderBy(property => property.Group == PaletteGroup ? 0 : 1)], darkIntro, [.. rules]);
    }

    /// <summary>
    /// The declarations that paint with a property, as "selector { declaration }". The colour-mode
    /// rules' own custom-property copies are plumbing rather than paint, so only ordinary
    /// declarations count.
    /// </summary>
    private static string[] PaintedBy(IEnumerable<CssRule> rules, string name)
    {
        return [.. rules.SelectMany(rule => rule.Declarations
                            .Where(declaration => declaration.Name.StartsWith("--", StringComparison.Ordinal) is false
                                                  && VariablesIn(declaration.Value).Contains(name, StringComparer.Ordinal))
                            .Select(declaration => $"{rule.Selector} {{ {declaration.Name}: {declaration.Value} }}"))
                        .Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// The component parameters whose default value is a <c>var(--bit-blow-*)</c>, read off the API
    /// reference - so a parameter that paints with the theme is found by the same reflection that
    /// documents it, rather than by a list kept next to it.
    /// </summary>
    private static Dictionary<string, string[]> ParametersReading()
    {
        var found = new List<(string Property, string Parameter)>();

        foreach (var type in BlowApiCatalog.Types.Where(type => type.Kind == "Component"))
        {
            var details = BlowApiCatalog.GetTypeDetails(type.Name);
            if (details is null) continue;

            foreach (var member in details.Members.Where(member => member.Kind == "Parameter" && member.Default is not null))
            {
                foreach (var variable in VariablesIn(member.Default!)) found.Add((variable, $"{type.Name}.{member.Name}"));
            }
        }

        return found.GroupBy(item => item.Property, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Select(item => item.Parameter).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
    }

    /// <summary>
    /// The components whose own markup paints with a property - the connection line BlowCanvas draws,
    /// the pattern of BlowBackground - found where the compiler put them: as string literals in the
    /// shipped assembly's IL. A razor file is compiled into the methods of its component, so a
    /// <c>ldstr</c> holding <c>var(--bit-blow-*)</c> in one of them is that component reading the
    /// property; nothing else could put the literal there, and nothing has to be kept in step with it.
    /// </summary>
    private static Dictionary<string, string[]> ComponentsReading()
    {
        const BindingFlags Everything = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        const byte LoadString = 0x72;
        const byte StringTable = 0x70;

        var found = new List<(string Property, string Component)>();
        var assembly = typeof(BlowCanvas).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            // Lambdas, local functions and state machines live in nested compiler-generated types; the
            // literal belongs to the public type they were written in.
            var owner = type;
            while (owner.DeclaringType is not null) owner = owner.DeclaringType;
            if (owner.IsPublic is false) continue;

            foreach (var method in type.GetMethods(Everything).Cast<MethodBase>().Concat(type.GetConstructors(Everything)))
            {
                byte[]? il;

                try
                {
                    il = method.GetMethodBody()?.GetILAsByteArray();
                }
                catch (Exception)
                {
                    // A method without a readable body has no literal to find.
                    continue;
                }

                if (il is null) continue;

                // A string token's top byte names the user-string heap, which is what tells an ldstr
                // from a byte of some other instruction's operand that happens to be 0x72.
                for (var i = 0; i + 4 < il.Length; i++)
                {
                    if (il[i] != LoadString || il[i + 4] != StringTable) continue;

                    string literal;

                    try
                    {
                        literal = type.Module.ResolveString(BitConverter.ToInt32(il, i + 1));
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    foreach (var variable in VariablesIn(literal)) found.Add((variable, BlowApiCatalog.FriendlyName(owner)));
                }
            }
        }

        return found.GroupBy(item => item.Property, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Select(item => item.Component).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
    }

    /// <summary>The single variable a value consists of (<c>var(--x)</c>), or null when it is anything else.</summary>
    private static string? VariableIn(string value)
    {
        var match = SingleVariableRegex().Match(value.Trim());

        return match.Success ? match.Groups["name"].Value : null;
    }

    private static IEnumerable<string> VariablesIn(string value) => VariableRegex().Matches(value).Select(match => match.Groups["name"].Value);

    /// <summary>
    /// Every rule of the stylesheet, top-level or inside a media query, with the comment right above
    /// it and, per declaration, the comment it sits under within the rule. A comment inside a block
    /// explains the declarations that follow it up to the next comment - which is how the stylesheet
    /// files the add-ons' colours under one explanation.
    /// </summary>
    internal static List<CssRule> ParseRules(string css)
    {
        var rules = new List<CssRule>();

        foreach (Match match in RuleRegex().Matches(css))
        {
            var head = match.Groups["head"].Value;

            // What precedes the selector, back to the previous rule, may be a comment about it - and a
            // media query's opening line, which is not part of the selector.
            string? comment = null;
            var end = head.LastIndexOf("*/", StringComparison.Ordinal);
            if (end >= 0)
            {
                var start = head.LastIndexOf("/*", end, StringComparison.Ordinal);
                if (start >= 0) comment = Clean(head[(start + 2)..end]);
                head = head[(end + 2)..];
            }

            var brace = head.LastIndexOf('{');
            if (brace >= 0) head = head[(brace + 1)..];

            var selector = WhitespaceRegex().Replace(head, " ").Trim();
            if (selector.Length == 0 || selector.StartsWith('@')) continue;

            var declarations = new List<(string, string, string?)>();
            string? current = null;

            foreach (Match token in TokenRegex().Matches(match.Groups["body"].Value))
            {
                if (token.Groups["comment"].Success)
                {
                    current = Clean(token.Groups["comment"].Value);
                    continue;
                }

                declarations.Add((token.Groups["name"].Value, WhitespaceRegex().Replace(token.Groups["value"].Value, " ").Trim(), current));
            }

            rules.Add(new CssRule(selector, comment, [.. declarations]));
        }

        return rules;
    }

    private static string Clean(string comment) => WhitespaceRegex().Replace(comment, " ").Trim();

    // A rule is whatever precedes a brace-free block, back to the end of the previous one. A media
    // query's own opening brace is inside the head, which is why the selector is read after it.
    [GeneratedRegex(@"(?<head>[^}]*?)\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"/\*(?<comment>.*?)\*/|(?<name>-{0,2}[a-zA-Z][a-zA-Z0-9-]*)\s*:\s*(?<value>[^;]+?)\s*(;|$)", RegexOptions.Singleline)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"var\(\s*(?<name>--bit-blow-[a-z0-9-]+)")]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"^var\(\s*(?<name>--bit-blow-[a-z0-9-]+)\s*\)$")]
    private static partial Regex SingleVariableRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
