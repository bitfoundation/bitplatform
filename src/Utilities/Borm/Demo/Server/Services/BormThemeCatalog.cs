using System.Text;
using System.Text.RegularExpressions;
using Bit.Borm.Demo.Server.Dtos;

namespace Bit.Borm.Demo.Server.Services;

/// <summary>
/// The CSS custom properties Bit.Borm's theme is built from, read out of the stylesheet the library
/// ships: what each one is, its light and dark value, which rules set it, and which rules paint with it.
/// <para>
/// Restyling a form means overriding these, and only these: every colour of a control, a message, a
/// repeater and a stepper - plus the corner radius, the gap and the form's width - is routed through
/// a <c>--bit-borm-*</c> property on <c>.bit-borm-form</c>. What is easy to get wrong is the dark theme:
/// it is not a second set of properties but the same ones declared again - under
/// <c>prefers-color-scheme: dark</c>, under <c>data-theme="dark"</c>, and once more under
/// <c>data-theme="light"</c> so an app's own toggle can beat the OS - and an override that does not
/// repeat itself under those selectors either loses to them or, worse, wins in one mode and leaves a
/// light surface under dark text in the other. Parsing the shipped file rather than keeping a table
/// means the answer cannot name a property the library dropped, or miss a rule it added.
/// </para>
/// </summary>
public static partial class BormThemeCatalog
{
    public const string RootSelector = ".bit-borm-form";

    private const string DarkMedia = "prefers-color-scheme: dark";
    private const string DarkAttribute = "data-theme=\"dark\"";

    /// <summary>One rule of the stylesheet, as written: the media query it sits in, its selector, the comment above it, its declarations in order.</summary>
    internal sealed record CssRule(string? Media, string Selector, string? Comment, (string Name, string Value, string? Comment)[] Declarations)
    {
        /// <summary>Where the rule applies, as a person would write it: "@media (...) selector", or the selector alone.</summary>
        public string Scope => Media is null ? Selector : $"{Media} {Selector}";
    }

    private sealed record Theme(BormThemePropertyDto[] Properties, CssRule[] Rules);

    private static readonly Lazy<Theme> _theme = new(Build);

    /// <summary>Every custom property of the theme, in the order the stylesheet declares them.</summary>
    public static BormThemePropertyDto[] Properties => _theme.Value.Properties;

    /// <summary>Every rule of the stylesheet, including the ones inside a media query.</summary>
    internal static CssRule[] Rules => _theme.Value.Rules;

    /// <summary>
    /// One property by its name - "--bit-borm-accent", "bit-borm-accent", "var(--bit-borm-accent)" or
    /// just "accent" - or null.
    /// </summary>
    public static BormThemePropertyDto? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var text = name.Trim();
        if (text.StartsWith("var(", StringComparison.OrdinalIgnoreCase) && text.EndsWith(')')) text = text[4..^1].Trim();

        text = text.TrimStart('-');
        if (text.StartsWith("bit-borm-", StringComparison.OrdinalIgnoreCase) is false) text = $"bit-borm-{text}";

        return Properties.FirstOrDefault(property => string.Equals(property.Name, $"--{text}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The theme as a Markdown table, as GetBormTheme answers when it is called without a property.</summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Borm theme").AppendLine();
        builder.AppendLine($"Every colour of a form - controls, messages, the error summary, repeaters, the stepper - plus its corner radius, ")
               .AppendLine($"field gap and maximum width is a CSS custom property declared on `{RootSelector}`, read out of the shipped ")
               .AppendLine("`bit-borm.css` below. Override them from your own stylesheet, loaded after `_content/Bit.Borm/bit-borm.css`, without ")
               .AppendLine("touching a single library selector. The form declares every one of them itself, so an override has to target the form - ")
               .AppendLine($"`{RootSelector}`, or `.my-page {RootSelector}` for one page - since a value set on an ancestor such as `:root` is ")
               .AppendLine("replaced by the form's own declaration before it can be inherited.")
               .AppendLine()
               .AppendLine("| Property | Light | Dark | Used for |")
               .AppendLine("| --- | --- | --- | --- |");

        foreach (var property in Properties)
        {
            var dark = property.DarkValue is null ? "same" : $"`{Escape(property.DarkValue)}`";

            builder.AppendLine($"| `{property.Name}` | `{Escape(property.Value)}` | {dark} | {Escape(property.Purpose ?? Paints(property))} |");
        }

        builder.AppendLine()
               .AppendLine("## Colour modes")
               .AppendLine()
               .AppendLine("The dark theme is the same properties declared again, by these rules of the stylesheet:")
               .AppendLine();

        foreach (var scope in ThemeScopes()) builder.AppendLine($"- `{scope}`");

        builder.AppendLine()
               .AppendLine("A form follows the OS preference until `data-theme=\"dark\"` or `data-theme=\"light\"` is put on it or on any ancestor, ")
               .AppendLine("which wins over the media query because its selector is more specific - so an app with its own theme toggle drives ")
               .AppendLine("the same tokens. The properties the Dark column calls \"same\" are set once and need one override on ")
               .AppendLine($"`{RootSelector}`. Any other one needs its override repeated under each of those selectors: an override on ")
               .AppendLine($"`{RootSelector}` alone also replaces the dark value of an OS-driven dark theme (it comes later, at the same ")
               .AppendLine("specificity), and loses to the `data-theme` rules. Call `GetBormTheme(property: \"...\")` for one property in full - ")
               .AppendLine("every rule that sets it or paints with it, and the override to write.");

        return builder.ToString();
    }

    /// <summary>One property in full as Markdown, or null when the stylesheet declares no such property.</summary>
    public static string? Render(string name)
    {
        var property = Find(name);
        if (property is null) return null;

        var builder = new StringBuilder();

        builder.AppendLine($"# `{property.Name}`").AppendLine();
        builder.AppendLine($"Declared on `{RootSelector}` by the shipped stylesheet.").AppendLine();

        if (property.Purpose is not null) builder.AppendLine($"- **Used for**: {property.Purpose}");

        builder.AppendLine($"- **Light** = `{property.Value}`")
               .AppendLine(property.DarkValue is null
                   ? "- **Dark** = the same value: the dark theme leaves this property alone"
                   : $"- **Dark** = `{property.DarkValue}`");

        if (property.DefaultsTo is not null) builder.AppendLine($"- **Defaults to** `{property.DefaultsTo}`, so it follows that property until it is overridden itself");
        if (property.Feeds.Length > 0)
        {
            builder.AppendLine($"- **Read by** {string.Join(", ", property.Feeds.Select(feed => $"`{feed}`"))}, " +
                               $"{(property.Feeds.Length == 1 ? "which follows" : "which follow")} it unless overridden as well");
        }
        if (property.Comment is not null) builder.AppendLine($"- **From the stylesheet**: {property.Comment}");

        builder.AppendLine();

        if (property.SetBy.Length > 0)
        {
            builder.AppendLine("## Set again by").AppendLine();

            foreach (var scope in property.SetBy) builder.AppendLine($"- `{scope}`");

            builder.AppendLine();
        }

        builder.AppendLine("## Painted by").AppendLine();

        if (property.PaintedBy.Length == 0) builder.AppendLine(property.Feeds.Length > 0 ? "No rule reads it directly - only the properties above." : "No rule of the stylesheet reads it.");

        foreach (var rule in property.PaintedBy) builder.AppendLine($"- `{rule}`");

        builder.AppendLine();

        builder.AppendLine("## Override").AppendLine()
               .AppendLine("```css")
               .AppendLine($"{RootSelector} {{ {property.Name}: {(property.SetBy.Length == 0 ? "<value>" : "<light value>")}; }}");

        // Every rule that sets the property again is a rule the override has to match, or it wins in
        // one colour mode and loses - or, worse, half wins - in another.
        foreach (var rule in Rules.Where(rule => IsRootRule(rule) is false && rule.Declarations.Any(declaration => declaration.Name == property.Name)))
        {
            var placeholder = IsDark(rule) ? "<dark value>" : "<light value>";
            var line = $"{rule.Selector} {{ {property.Name}: {placeholder}; }}";

            builder.AppendLine(rule.Media is null ? line : $"{rule.Media} {{ {line} }}");
        }

        builder.AppendLine("```").AppendLine()
               .AppendLine("Load the override after `_content/Bit.Borm/bit-borm.css`. To restyle one form rather than every one on the page, ")
               .AppendLine($"give that `BormView` a `Class` and write the override against `{RootSelector}.your-class`.");

        return builder.ToString();
    }

    /// <summary>The rules other than the root one that declare custom properties - the colour modes - as "@media (...) selector" or "selector".</summary>
    public static string[] ThemeScopes()
        => [.. Rules.Where(rule => IsRootRule(rule) is false && rule.Declarations.Any(declaration => declaration.Name.StartsWith("--bit-borm-", StringComparison.Ordinal)))
                    .Select(rule => rule.Scope)
                    .Distinct(StringComparer.Ordinal)];

    private static string Paints(BormThemePropertyDto property)
    {
        var selectors = property.PaintedBy.Select(rule => rule[..rule.IndexOf(" {", StringComparison.Ordinal)])
                                          .Distinct(StringComparer.Ordinal)
                                          .ToArray();

        if (selectors.Length == 0) return "-";

        var shown = string.Join(", ", selectors.Take(3).Select(selector => $"`{selector}`"));

        return selectors.Length > 3 ? $"{shown} +{selectors.Length - 3} more" : shown;
    }

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static bool IsRootRule(CssRule rule) => rule.Media is null && rule.Selector == RootSelector;

    /// <summary>A rule of the dark theme: the OS-driven one inside its media query, or the one an app's <c>data-theme="dark"</c> switches on.</summary>
    private static bool IsDark(CssRule rule)
        => rule.Selector.Contains(DarkAttribute, StringComparison.Ordinal)
           || (rule.Media?.Contains(DarkMedia, StringComparison.Ordinal) ?? false);

    private static Theme Build()
    {
        var rules = ParseRules(BormSourceCatalog.Stylesheet);

        var declared = rules.Where(IsRootRule)
                            .SelectMany(rule => rule.Declarations)
                            .Where(declaration => declaration.Name.StartsWith("--", StringComparison.Ordinal))
                            .ToArray();

        var values = declared.GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
                             .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

        // The value the dark theme gives each property. The rule an app's data-theme="dark" switches on is
        // read for it; the media-query copy is the same list by intent, and the oracle tests hold it to that.
        var dark = rules.Where(rule => rule.Media is null && rule.Selector.Contains(DarkAttribute, StringComparison.Ordinal))
                        .SelectMany(rule => rule.Declarations)
                        .Where(declaration => declaration.Name.StartsWith("--", StringComparison.Ordinal))
                        .GroupBy(declaration => declaration.Name, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

        var purposes = ReadPurposes();

        var properties = new List<BormThemePropertyDto>();

        foreach (var (name, _, comment) in declared.DistinctBy(declaration => declaration.Name, StringComparer.Ordinal))
        {
            var value = values[name];
            var darkValue = dark.GetValueOrDefault(name);

            properties.Add(new BormThemePropertyDto
            {
                Name = name,
                Value = value,
                DarkValue = darkValue is null || darkValue == value ? null : darkValue,
                SetBy = [.. rules.Where(rule => IsRootRule(rule) is false && rule.Declarations.Any(declaration => declaration.Name == name))
                                 .Select(rule => rule.Scope)
                                 .Distinct(StringComparer.Ordinal)],
                DefaultsTo = VariableIn(value),
                Feeds = [.. values.Where(pair => pair.Key != name && VariablesIn(pair.Value).Contains(name, StringComparer.Ordinal)).Select(pair => pair.Key)],
                Purpose = purposes.GetValueOrDefault(name),
                Comment = comment,
                PaintedBy = PaintedBy(rules, name)
            });
        }

        return new Theme([.. properties], [.. rules]);
    }

    /// <summary>
    /// What the guide's table of custom properties says each one is for. The stylesheet comments only
    /// the odd property, while the README's theming section gives every one of them a line - and it is
    /// the same text a person reading the guide sees, so the two answers cannot disagree.
    /// </summary>
    private static Dictionary<string, string> ReadPurposes()
    {
        return ReadmeThemeRowRegex().Matches(BormSourceCatalog.Readme)
                                    .GroupBy(match => match.Groups["name"].Value, StringComparer.Ordinal)
                                    .ToDictionary(group => group.Key, group => group.First().Groups["purpose"].Value.Trim(), StringComparer.Ordinal);
    }

    /// <summary>
    /// The declarations that paint with a property, as "selector { declaration }". The colour-mode
    /// rules' own custom-property declarations are plumbing rather than paint, so only ordinary
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

    /// <summary>The single variable a value consists of (<c>var(--x)</c>), or null when it is anything else.</summary>
    private static string? VariableIn(string value)
    {
        var match = SingleVariableRegex().Match(value.Trim());

        return match.Success ? match.Groups["name"].Value : null;
    }

    private static IEnumerable<string> VariablesIn(string value) => VariableRegex().Matches(value).Select(match => match.Groups["name"].Value);

    /// <summary>
    /// Every rule of the stylesheet, top-level or inside a media query, with the comment right above
    /// it and, per declaration, the comment it sits under within the rule.
    /// <para>
    /// A small reader rather than one regular expression: the dark theme's OS-driven half is a
    /// <c>.bit-borm-form</c> rule inside <c>@media (prefers-color-scheme: dark)</c>, and a pattern that
    /// sees only the innermost block reads it as a second root rule - which turns every light value
    /// into its dark one. Tracking which block a rule opens in is what keeps the two apart.
    /// </para>
    /// </summary>
    internal static List<CssRule> ParseRules(string css)
    {
        var rules = new List<CssRule>();

        css = css.Replace("\r\n", "\n", StringComparison.Ordinal);

        var media = new Stack<string?>();
        var prelude = new StringBuilder();
        string? pendingComment = null;

        for (var i = 0; i < css.Length; i++)
        {
            var c = css[i];

            if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
            {
                var end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0) break;

                pendingComment = Clean(css[(i + 2)..end]);
                i = end + 1;
                continue;
            }

            switch (c)
            {
                case '{':
                {
                    var head = WhitespaceRegex().Replace(prelude.ToString(), " ").Trim();
                    prelude.Clear();

                    if (head.StartsWith('@'))
                    {
                        // A media query (or any other grouping at-rule) holds rules of its own.
                        media.Push(head);
                        pendingComment = null;
                        continue;
                    }

                    var close = FindBlockEnd(css, i + 1);
                    var body = css[(i + 1)..close];

                    rules.Add(new CssRule(media.Count > 0 ? media.Peek() : null, head, pendingComment, ReadDeclarations(body)));

                    pendingComment = null;
                    i = close;
                    continue;
                }

                case '}':
                    if (media.Count > 0) media.Pop();
                    prelude.Clear();
                    pendingComment = null;
                    continue;

                default:
                    prelude.Append(c);
                    continue;
            }
        }

        return rules;
    }

    /// <summary>The closing brace of a declaration block, skipping the comments inside it.</summary>
    private static int FindBlockEnd(string css, int start)
    {
        for (var i = start; i < css.Length; i++)
        {
            if (css[i] == '/' && i + 1 < css.Length && css[i + 1] == '*')
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

    /// <summary>
    /// The declarations of one block. A comment inside a block explains the declarations that follow
    /// it up to the next comment - which is how the stylesheet says why <c>--bit-borm-max-width</c> is a token.
    /// </summary>
    private static (string Name, string Value, string? Comment)[] ReadDeclarations(string body)
    {
        var declarations = new List<(string, string, string?)>();
        string? current = null;

        foreach (Match token in TokenRegex().Matches(body))
        {
            if (token.Groups["comment"].Success)
            {
                current = Clean(token.Groups["comment"].Value);
                continue;
            }

            declarations.Add((token.Groups["name"].Value, WhitespaceRegex().Replace(token.Groups["value"].Value, " ").Trim(), current));
        }

        return [.. declarations];
    }

    private static string Clean(string comment) => WhitespaceRegex().Replace(comment, " ").Trim();

    [GeneratedRegex(@"/\*(?<comment>.*?)\*/|(?<name>-{0,2}[a-zA-Z][a-zA-Z0-9-]*)\s*:\s*(?<value>[^;]+?)\s*(;|$)", RegexOptions.Singleline)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"var\(\s*(?<name>--bit-borm-[a-z0-9-]+)")]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"^var\(\s*(?<name>--bit-borm-[a-z0-9-]+)\s*\)$")]
    private static partial Regex SingleVariableRegex();

    [GeneratedRegex(@"^\| `(?<name>--bit-borm-[a-z0-9-]+)` \| `[^`]+` \| `[^`]+` \| (?<purpose>.+?) \|\s*$", RegexOptions.Multiline)]
    private static partial Regex ReadmeThemeRowRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
