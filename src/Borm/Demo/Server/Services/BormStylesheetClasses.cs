using System.Text.RegularExpressions;

namespace Bit.Borm.Demo.Server.Services;

/// <summary>
/// The CSS classes of the stylesheet Bit.Borm ships, read out of the file itself.
/// <para>
/// The colours live in custom properties, which <see cref="BormThemeCatalog"/> serves; the classes
/// are the other half - the BEM blocks and parts a form is made of (<c>bit-borm-field</c>,
/// <c>bit-borm-array__item</c>, <c>bit-borm-stepper__step</c>), the state modifiers a page can style
/// (<c>--invalid</c>, <c>is-active</c>), the focus rings and the forced-colours fallbacks. They have no
/// tool of their own: each is an entry of the search index pointing at the stylesheet, because "how do
/// I style an invalid field" or "why does my grid collapse on a phone" is answered by one of its
/// rules. Reading the rules the theme catalog already parsed means a class the library drops is never
/// named, and one it adds is never missing.
/// </para>
/// </summary>
public static partial class BormStylesheetClasses
{
    public sealed record StyleRule(string Selector, string Description);

    private static readonly Lazy<StyleRule[]> _classes = new(Build);

    /// <summary>Every selector of the stylesheet that names a Borm class, once each, in the order the file first declares it.</summary>
    public static StyleRule[] Classes => _classes.Value;

    private static StyleRule[] Build()
    {
        var rules = new List<(string Selector, List<string> Comments, List<string> Properties, bool DeclaresTheme)>();

        foreach (var rule in BormThemeCatalog.Rules.Where(rule => ClassRegex().IsMatch(rule.Selector)))
        {
            var index = rules.FindIndex(r => r.Selector == rule.Selector);
            if (index < 0)
            {
                rules.Add((rule.Selector, [], [], false));
                index = rules.Count - 1;
            }

            var entry = rules[index];

            // A comment right above the rule, or inside it, is the stylesheet saying why the rule exists.
            if (rule.Comment is not null && entry.Comments.Contains(rule.Comment) is false) entry.Comments.Add(rule.Comment);

            foreach (var (name, value, comment) in rule.Declarations)
            {
                if (comment is not null && entry.Comments.Contains(comment) is false) entry.Comments.Add(comment);

                // The custom properties are the theme, which GetBormTheme lays out property by property;
                // repeated here, the root rule's description would be fifteen colours long.
                if (name.StartsWith("--", StringComparison.Ordinal))
                {
                    entry.DeclaresTheme = true;
                    continue;
                }

                // A rule inside a media query applies only there, which is half of what it means: the
                // grid collapsing to one column is a rule of (max-width: 640px), not of every screen.
                var text = rule.Media is null ? $"{name}: {value}" : $"{name}: {value} under {rule.Media}";
                if (entry.Properties.Contains(text) is false) entry.Properties.Add(text);
            }

            rules[index] = entry;
        }

        return [.. rules.Select(rule => new StyleRule(rule.Selector, Describe(rule.Comments, rule.Properties, rule.DeclaresTheme)))];
    }

    private static string Describe(List<string> comments, List<string> properties, bool declaresTheme)
    {
        var prose = WhitespaceRegex().Replace(string.Join(' ', comments), " ").Trim();
        var declarations = string.Join("; ", properties);

        if (declaresTheme) prose = $"{prose} Declares the theme's --bit-borm-* custom properties (see GetBormTheme).".Trim();

        if (prose.Length == 0) return declarations;

        return declarations.Length == 0 ? prose : $"{prose} ({declarations})";
    }

    [GeneratedRegex(@"\.bit-borm-[a-z0-9_-]+")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
