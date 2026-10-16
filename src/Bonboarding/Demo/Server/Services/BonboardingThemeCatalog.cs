using System.Text;
using System.Text.RegularExpressions;
using Bit.Bonboarding.Demo.Server.Dtos;

namespace Bit.Bonboarding.Demo.Server.Services;

/// <summary>
/// The CSS custom properties Bit.Bonboarding's default theme is built from, read out of the
/// stylesheet the library ships.
/// <para>
/// Restyling the tours means overriding these, and only these: the stylesheet routes every colour,
/// radius, font and duration through a variable on <c>.bit-bonboarding-root</c>, so an override needs
/// no selector of its own. Parsing the shipped file rather than keeping a table means the answer
/// cannot name a variable the library dropped, or miss one it added.
/// </para>
/// </summary>
public static partial class BonboardingThemeCatalog
{
    private const string RootSelector = ".bit-bonboarding-root";
    private const string DarkSelector = ".bit-bonboarding-root[data-bonboarding-theme=\"dark\"]";

    private static readonly Lazy<BonboardingThemeVariableDto[]> _variables = new(Build);

    /// <summary>
    /// The variables BonboardingRoot writes into the root's inline style for every step, from the
    /// resolved options. An inline declaration beats the stylesheet, so for these the option is the
    /// only thing that decides the value - the stylesheet's own value, dark one included, is a
    /// fallback that never shows while a tour is rendered.
    /// </summary>
    private static readonly Dictionary<string, string> _setInlineBy = new(StringComparer.Ordinal)
    {
        ["--bit-bonboarding-z"] = "ZIndex",
        ["--bit-bonboarding-duration"] = "AnimationDuration",
        ["--bit-bonboarding-overlay-opacity"] = "OverlayOpacity",
        ["--bit-bonboarding-arrow"] = "ArrowSize",
    };

    /// <summary>Every custom property of the default theme, in the order the stylesheet declares them.</summary>
    public static BonboardingThemeVariableDto[] Variables => _variables.Value;

    /// <summary>The variables as one Markdown table, grouped the way the stylesheet groups them.</summary>
    public static string Render()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bonboarding theme variables").AppendLine();
        builder.AppendLine($"Every value of the default theme is a CSS custom property on `{RootSelector}`, so the whole library is ")
               .AppendLine("restyled from your own stylesheet without overriding a single selector. Load your stylesheet after ")
               .AppendLine("`_content/Bit.Bonboarding/bit-bonboarding.css` so your values win:")
               .AppendLine()
               .AppendLine("```css")
               .AppendLine($"{RootSelector} {{ --bit-bonboarding-accent: #6d28d9; --bit-bonboarding-radius: 4px; }}")
               .AppendLine($"{DarkSelector} {{ --bit-bonboarding-accent: #a78bfa; }}")
               .AppendLine("```")
               .AppendLine()
               .AppendLine("The dark column applies when the resolved theme is `BonboardingTheme.Dark` (the root then carries ")
               .AppendLine("`data-bonboarding-theme=\"dark\"`); `System`, the default, keeps the light values. The variables ")
               .AppendLine("with an option in the last column are written into the root's inline style for every step from that ")
               .AppendLine("option, so overriding them in CSS has no effect - set the option (globally, per tour or per step) instead.")
               .AppendLine();

        foreach (var group in Variables.GroupBy(variable => variable.Group))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();
            builder.AppendLine("| Variable | Light (default) | Dark | Set from option |");
            builder.AppendLine("| --- | --- | --- | --- |");

            foreach (var variable in group)
            {
                var option = _setInlineBy.TryGetValue(variable.Name, out var name) ? $"`{name}`" : string.Empty;

                builder.AppendLine($"| `{variable.Name}` | `{Escape(variable.LightValue)}` | {(variable.DarkValue is null ? "same" : $"`{Escape(variable.DarkValue)}`")} | {option} |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static BonboardingThemeVariableDto[] Build()
    {
        var css = BonboardingSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);

        var light = ReadBlock(css, RootSelector);
        var dark = ReadBlock(css, DarkSelector).ToDictionary(variable => variable.Name, variable => variable.Value, StringComparer.Ordinal);

        return [.. light.Select(variable => new BonboardingThemeVariableDto
        {
            Name = variable.Name,
            Group = variable.Group,
            LightValue = variable.Value,
            DarkValue = dark.TryGetValue(variable.Name, out var value) && value != variable.Value ? value : null
        })];
    }

    /// <summary>
    /// The custom properties declared directly inside one rule, each with the group comment it sits
    /// under (<c>/* ---- Typography ---- */</c>). Only the rule whose selector is exactly
    /// <paramref name="selector"/> is read, so the variables a media query or a state selector
    /// re-declares do not leak into the defaults.
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

    [GeneratedRegex(@"^\s*/\*\s*-+\s*(?<name>.+?)\s*-+\s*\*/\s*$")]
    private static partial Regex GroupCommentRegex();

    [GeneratedRegex(@"^\s*(?<name>--bit-bonboarding-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);")]
    private static partial Regex DeclarationRegex();
}
