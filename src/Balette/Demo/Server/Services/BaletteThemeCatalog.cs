using System.Text;
using System.Text.RegularExpressions;
using Bit.Balette.Demo.Server.Dtos;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// The CSS custom properties Bit.Balette's default theme is built from, read out of the stylesheet
/// the library ships, and the data-balette hooks its components render, read out of their markup.
/// <para>
/// Restyling the palette means overriding these variables and nothing else: every colour, size and
/// font the stylesheet uses goes through one on <c>:root</c>, so an override needs no selector of its
/// own and the trigger and shortcut hints outside the dialog follow it too. Parsing the shipped files
/// rather than keeping a table means the answer cannot name a variable or a hook the library dropped,
/// or miss one it added - and that it reports what the files actually do, including the variable the
/// component writes inline, where a stylesheet override never reaches.
/// </para>
/// </summary>
public static partial class BaletteThemeCatalog
{
    private const string RootSelector = ":root";

    /// <summary>The selector the dark values are declared under, next to the dialog's own <c>data-theme="dark"</c>.</summary>
    private const string DarkSelector = ":root[data-theme=\"dark\"]";

    private static readonly Lazy<BaletteThemeVariableDto[]> _variables = new(Build);
    private static readonly Lazy<BaletteMarkupHookDto[]> _hooks = new(BuildHooks);

    /// <summary>Every custom property of the default theme, in the order the stylesheet declares them.</summary>
    public static BaletteThemeVariableDto[] Variables => _variables.Value;

    /// <summary>Every element the shipped components mark with data-balette, once per value, in the order the markup first renders it.</summary>
    public static BaletteMarkupHookDto[] Hooks => _hooks.Value;

    /// <summary>The variables as Markdown tables grouped the way the stylesheet groups them, then the ways to override them, then the hooks.</summary>
    public static string Render()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Balette theme variables").AppendLine();
        builder.AppendLine($"Every value of the default theme is a CSS custom property on `{RootSelector}` in `_content/Bit.Balette/bit-balette.css` - ")
               .AppendLine("a stylesheet the host page links itself, it is not injected. Restyle the palette by overriding these, never by ")
               .AppendLine("overriding the library's selectors:")
               .AppendLine()
               .AppendLine("| Where | Reaches | Example |")
               .AppendLine("| --- | --- | --- |")
               .AppendLine("| `:root` in your stylesheet, linked after the library's | the whole app: the dialog, `BaletteTrigger` and every `BaletteShortcutHint` | `:root { --bit-balette-accent: #0f766e; }` |")
               .AppendLine("| `dialog[data-balette=\"dialog\"]` | the palette dialog alone | `dialog[data-balette=\"dialog\"] { --bit-balette-width: 760px; }` |")
               .AppendLine("| the `Style` parameter | that one palette, inline, ahead of every rule | `<Balette Style=\"--bit-balette-radius: 6px\" />` |")
               .AppendLine("| the `Theme` parameter | which column applies to that dialog: `auto` (the default, from `options.Theme`), `light` or `dark` | `<Balette Theme=\"dark\" />` |")
               .AppendLine()
               .AppendLine("The dark column applies under `prefers-color-scheme: dark` (unless `<html data-theme=\"light\">`), under ")
               .AppendLine("`<html data-theme=\"dark\">`, and on a dialog whose `Theme` is `dark`; a dialog whose `Theme` is `light` gets the light ")
               .AppendLine("column back on a dark page. To change a dark value, override it under the same selectors the stylesheet uses: ")
               .AppendLine($"`{DarkSelector}, dialog[data-balette=\"dialog\"][data-theme=\"dark\"]`, and inside `@media (prefers-color-scheme: dark)` on ")
               .AppendLine("`:root:not([data-theme=\"light\"])`. A value in the last column is set somewhere else as well, and where the component ")
               .AppendLine("writes it inline from an option, the option is the only thing that changes it for the elements under that element.")
               .AppendLine();

        foreach (var group in Variables.GroupBy(variable => variable.Group))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();
            builder.AppendLine("| Variable | Light (default) | Dark | Also set by |");
            builder.AppendLine("| --- | --- | --- | --- |");

            foreach (var variable in group)
            {
                var also = variable.AlsoSetBy is { Length: > 0 } ? string.Join("; ", variable.AlsoSetBy.Select(Escape)) : string.Empty;

                builder.AppendLine($"| `{variable.Name}` | `{Escape(variable.LightValue)}` | {(variable.DarkValue is null ? "same" : $"`{Escape(variable.DarkValue)}`")} | {also} |");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Unstyled: the data-balette hooks").AppendLine();
        builder.AppendLine("`Unstyled=\"true\"` drops the `bit-balette-dialog` class every dialog rule is scoped to, so none of them applies and ")
               .AppendLine("the markup is yours to style - through these attributes, which are also the stable hooks for tests. The rows ")
               .AppendLine("carry their state as attributes: style `[data-balette=\"item\"][data-active=\"true\"]`, not a class. An `ItemTemplate` ")
               .AppendLine("replaces what is inside a row; the row element itself, with these attributes, stays the palette's.")
               .AppendLine();
        builder.AppendLine("| `data-balette` | Element | Default class | State attributes | Rendered by |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var hook in Hooks)
        {
            var states = hook.States.Length == 0 ? string.Empty : string.Join(", ", hook.States.Select(state => $"`{state}`"));

            builder.AppendLine($"| `{hook.Value}` | `<{hook.Element}>` | {(hook.Class is null ? string.Empty : $"`{hook.Class}`")} | {states} | `{hook.Component}` |");
        }

        var unscoped = BaletteStylesheetClasses.Unscoped.Select(rule => $"`{rule.Selector}`").Distinct(StringComparer.Ordinal).ToArray();

        if (unscoped.Length > 0)
        {
            builder.AppendLine()
                   .AppendLine($"With the stylesheet still linked, the rules not scoped to the dialog keep applying under `Unstyled`: {string.Join(", ", unscoped)}. ")
                   .AppendLine("Leave the stylesheet out altogether to start from bare markup.");
        }

        builder.AppendLine()
               .AppendLine($"The rules that read these variables are in `{BaletteSourceCatalog.StylesheetPath}`, and the markup in ")
               .AppendLine($"`{BaletteSourceCatalog.ComponentMarkupFolder}Balette.razor` - both through `GetBaletteSourceFile`.");

        return builder.ToString();
    }

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static BaletteThemeVariableDto[] Build()
    {
        var css = BaletteSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);

        var light = ReadBlock(css, RootSelector);
        var dark = ReadDeclarations(FindRuleBody(css, DarkSelector) ?? string.Empty).ToDictionary(variable => variable.Name, variable => variable.Value, StringComparer.Ordinal);
        var elsewhere = OtherDeclarations(css);
        var inline = InlineDeclarations();

        return [.. light.Select(variable => new BaletteThemeVariableDto
        {
            Name = variable.Name,
            Group = variable.Group,
            LightValue = variable.Value,
            DarkValue = dark.TryGetValue(variable.Name, out var value) && value != variable.Value ? value : null,
            AlsoSetBy = [.. elsewhere.Where(other => other.Name == variable.Name).Select(other => $"`{other.Selector}`: `{other.Value}`")
                           .Concat(inline.Where(other => other.Name == variable.Name).Select(other => other.Condition is null
                               ? $"inline on `{other.Element}` from `BaletteOptions.{other.Option}`, so set the option"
                               : $"inline on `{other.Element}` from `BaletteOptions.{other.Option}` while `{other.Condition}` is on, so set the option then"))]
        }).Select(variable => variable.AlsoSetBy is { Length: 0 } ? variable with { AlsoSetBy = null } : variable)];
    }

    /// <summary>
    /// The custom properties declared directly inside the rule whose selector is exactly
    /// <paramref name="selector"/>, each with the group comment it sits under (<c>/* Layout */</c>).
    /// Only that rule is read, so the values a media query, a theme or a state selector re-declares do
    /// not leak into the defaults.
    /// </summary>
    private static List<(string Name, string Value, string Group)> ReadBlock(string css, string selector)
    {
        var result = new List<(string, string, string)>();

        var start = css.IndexOf($"\n{selector} {{", StringComparison.Ordinal);
        if (start < 0) return result;

        var open = css.IndexOf('{', start);
        var close = css.IndexOf("\n}", open, StringComparison.Ordinal);
        if (close < 0) return result;

        var group = "Other";

        foreach (var line in css[(open + 1)..close].Split('\n'))
        {
            var heading = GroupCommentRegex().Match(line);
            if (heading.Success)
            {
                group = heading.Groups["name"].Value.Trim();
                continue;
            }

            var declaration = DeclarationRegex().Match(line);
            if (declaration.Success) result.Add((declaration.Groups["name"].Value, declaration.Groups["value"].Value.Trim(), group));
        }

        return result;
    }

    /// <summary>
    /// The body of the top-level rule whose selector list includes <paramref name="selector"/> - the
    /// dark rule is written for two selectors at once, across two lines, so it is found by one of them
    /// rather than by the whole list.
    /// </summary>
    private static string? FindRuleBody(string css, string selector)
    {
        foreach (Match rule in TopLevelRuleRegex().Matches(css))
        {
            var selectors = rule.Groups["selector"].Value.Split(',').Select(part => part.Trim());

            if (selectors.Contains(selector, StringComparer.Ordinal)) return rule.Groups["body"].Value;
        }

        return null;
    }

    private static IEnumerable<(string Name, string Value)> ReadDeclarations(string body)
    {
        foreach (var line in body.Split('\n'))
        {
            var declaration = DeclarationRegex().Match(line);
            if (declaration.Success) yield return (declaration.Groups["name"].Value, declaration.Groups["value"].Value.Trim());
        }
    }

    /// <summary>
    /// Every theme variable a rule other than the theme rules redeclares - for a state of the dialog,
    /// say - which changes the value a caller sees without any theme being involved.
    /// </summary>
    private static List<(string Name, string Value, string Selector)> OtherDeclarations(string css)
    {
        var result = new List<(string, string, string)>();

        foreach (Match rule in TopLevelRuleRegex().Matches(css))
        {
            var selector = WhitespaceRegex().Replace(rule.Groups["selector"].Value, " ").Trim();

            // The theme rules themselves: the defaults, the dark values and the light values a dialog
            // forced to light gets back on a dark page.
            if (selector == RootSelector || selector.Contains("data-theme", StringComparison.Ordinal)) continue;

            foreach (Match declaration in InlineDeclarationRegex().Matches(rule.Groups["body"].Value))
            {
                result.Add((declaration.Groups["name"].Value, declaration.Groups["value"].Value.Trim(), selector));
            }
        }

        return result;
    }

    /// <summary>
    /// The theme variables a component writes into an element's inline style from an option, either
    /// straight in its markup (<c>style="--bit-balette-x:@(Options.X)"</c>) or through a property of the
    /// palette's code-behind that the markup binds (<c>style="@RowHeightStyle"</c>, which writes
    /// <c>--bit-balette-row-height</c> from <c>Options.RowHeight</c> while <c>Virtualize</c> is on). An inline
    /// declaration is inherited by everything under that element ahead of any stylesheet value, so for those
    /// elements - and while its condition holds - the option is the only thing that decides it.
    /// </summary>
    private static List<(string Name, string Option, string Element, string? Condition)> InlineDeclarations()
    {
        var result = new List<(string, string, string, string?)>();
        var computed = new Dictionary<string, Match>(StringComparer.Ordinal);
        foreach (Match match in ComputedStyleRegex().Matches(BaletteSourceCatalog.PaletteCodeBehind))
        {
            computed[match.Groups["property"].Value] = match;
        }

        foreach (var (_, markup) in BaletteSourceCatalog.ComponentMarkup)
        {
            foreach (Match tag in TagRegex().Matches(markup))
            {
                var attributes = tag.Groups["attributes"].Value;
                var hook = DataBaletteRegex().Match(attributes);
                var element = hook.Success ? $"[data-balette=\"{hook.Groups["value"].Value}\"]" : tag.Groups["tag"].Value;

                foreach (Match declaration in InlineOptionRegex().Matches(attributes))
                {
                    result.Add((declaration.Groups["name"].Value, declaration.Groups["option"].Value, element, null));
                }

                if (BoundStyleRegex().Match(attributes) is { Success: true } bound && computed.TryGetValue(bound.Groups["property"].Value, out var style))
                {
                    result.Add((style.Groups["name"].Value, style.Groups["option"].Value, element, style.Groups["condition"].Value));
                }
            }
        }

        return result;
    }

    private static BaletteMarkupHookDto[] BuildHooks()
    {
        var hooks = new List<BaletteMarkupHookDto>();

        foreach (var (component, markup) in BaletteSourceCatalog.ComponentMarkup.OrderBy(file => file.Component == "Balette" ? 0 : 1))
        {
            foreach (Match tag in TagRegex().Matches(markup))
            {
                var attributes = tag.Groups["attributes"].Value;
                var hook = DataBaletteRegex().Match(attributes);
                if (hook.Success is false) continue;

                var value = hook.Groups["value"].Value;
                if (hooks.Any(existing => existing.Value == value)) continue;

                hooks.Add(new BaletteMarkupHookDto
                {
                    Value = value,
                    Element = tag.Groups["tag"].Value,
                    Class = ClassRegex().Match(attributes) is { Success: true } css ? css.Value : null,
                    States = [.. StateAttributeRegex().Matches(attributes).Select(state => state.Groups["name"].Value).Where(name => name != "data-balette").Distinct(StringComparer.Ordinal)],
                    Component = component
                });
            }
        }

        return [.. hooks];
    }

    [GeneratedRegex(@"^\s*/\*\s*-*\s*(?<name>.+?)\s*-*\s*\*/\s*$")]
    private static partial Regex GroupCommentRegex();

    [GeneratedRegex(@"^\s*(?<name>--bit-balette-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);")]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"(?<name>--bit-balette-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);?")]
    private static partial Regex InlineDeclarationRegex();

    // A rule that starts a line: its selector list may span lines, and the media query's rules are
    // indented, so neither a nested rule nor the media query itself is taken for a top-level rule.
    [GeneratedRegex(@"(?m)^(?<selector>[^\s@}{/][^{}]*?)\{(?<body>[^{}]*)\}")]
    private static partial Regex TopLevelRuleRegex();

    // An element's opening tag. Attribute values are quoted, and a quoted value may hold a '>' (an
    // event handler's lambda), so quotes are consumed whole rather than scanned for the closing bracket.
    [GeneratedRegex(@"<(?<tag>[a-z][a-z0-9]*)\b(?<attributes>(?:""[^""]*""|[^>""])*)>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\bdata-balette=""(?<value>[a-z-]+)""")]
    private static partial Regex DataBaletteRegex();

    [GeneratedRegex(@"\b(?<name>data-[a-z-]+)=")]
    private static partial Regex StateAttributeRegex();

    [GeneratedRegex(@"\bbit-balette-[a-z0-9-]+")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"(?<name>--bit-balette-[a-z0-9-]+):@\(Options\.(?<option>\w+)")]
    private static partial Regex InlineOptionRegex();

    // style="@SomeProperty": the inline style is computed by a property of the code-behind.
    [GeneratedRegex(@"\bstyle=""@(?<property>[A-Z]\w*)""")]
    private static partial Regex BoundStyleRegex();

    // RowHeightStyle => EffectiveVirtualize ? $"--bit-balette-row-height:{Options.RowHeight...}px" : null
    [GeneratedRegex(@"(?<property>[A-Z]\w*)\s*=>\s*Effective(?<condition>\w+)\s*\?\s*\$""(?<name>--bit-balette-[a-z0-9-]+):\{Options\.(?<option>\w+)")]
    private static partial Regex ComputedStyleRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
