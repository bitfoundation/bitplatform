using System.Text.RegularExpressions;

namespace Bit.Blow.Demo.Server.Services;

/// <summary>
/// The CSS classes of the stylesheet Bit.Blow ships, read out of the file itself.
/// <para>
/// The colours live in custom properties, which <see cref="BlowThemeCatalog"/> serves; the classes
/// are the other half - the positioning that makes a node a node and a handle sit on its side, the
/// focus rings, the state classes a custom template can style (<c>selected</c>, <c>dragging</c>,
/// <c>animated</c>). They have no tool of their own: each is an entry of the search index pointing
/// at the stylesheet, because "how do I style a selected node" or "why does the handle sit in the
/// middle" is answered by one of its rules. Parsing the shipped file means a class the library drops
/// is never named, and one it adds is never missing.
/// </para>
/// </summary>
public static partial class BlowStylesheetClasses
{
    public sealed record StyleRule(string Selector, string Description);

    private static readonly Lazy<StyleRule[]> _classes = new(Build);

    /// <summary>Every selector of the stylesheet, once each, in the order the file first declares it.</summary>
    public static StyleRule[] Classes => _classes.Value;

    private static StyleRule[] Build()
    {
        var css = BlowSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);

        var rules = new List<(string Selector, List<string> Comments, List<string> Properties, bool DeclaresTheme)>();

        foreach (Match match in RuleRegex().Matches(css))
        {
            var selector = WhitespaceRegex().Replace(match.Groups["selector"].Value, " ").Trim();
            var body = match.Groups["body"].Value;

            var index = rules.FindIndex(r => r.Selector == selector);
            if (index < 0)
            {
                rules.Add((selector, [], [], false));
                index = rules.Count - 1;
            }

            var rule = rules[index];

            // A comment right above the rule, or inside it, is the stylesheet saying why the rule exists.
            var before = css[..match.Index].TrimEnd();
            if (before.EndsWith("*/", StringComparison.Ordinal))
            {
                var open = before.LastIndexOf("/*", StringComparison.Ordinal);
                if (open >= 0) rule.Comments.Add(before[(open + 2)..^2]);
            }

            foreach (Match comment in CommentRegex().Matches(body)) rule.Comments.Add(comment.Groups["text"].Value);

            foreach (Match declaration in DeclarationRegex().Matches(CommentRegex().Replace(body, string.Empty)))
            {
                var name = declaration.Groups["name"].Value;

                // The custom properties are the theme, which GetBlowTheme lays out property by property;
                // repeated here, the root rule's description would be fifty colours long.
                if (name.StartsWith("--", StringComparison.Ordinal))
                {
                    rule.DeclaresTheme = true;
                    continue;
                }

                var text = $"{name}: {declaration.Groups["value"].Value.Trim()}";
                if (rule.Properties.Contains(text) is false) rule.Properties.Add(text);
            }

            rules[index] = rule;
        }

        return [.. rules.Select(rule => new StyleRule(rule.Selector, Describe(rule.Comments, rule.Properties, rule.DeclaresTheme)))];
    }

    private static string Describe(List<string> comments, List<string> properties, bool declaresTheme)
    {
        var prose = WhitespaceRegex().Replace(string.Join(' ', comments), " ").Trim();
        var declarations = string.Join("; ", properties);

        if (declaresTheme) prose = $"{prose} Declares the theme's --bit-blow-* custom properties (see GetBlowTheme).".Trim();

        if (prose.Length == 0) return declarations;

        return declarations.Length == 0 ? prose : $"{prose} ({declarations})";
    }

    // The root class is matched as well as the prefixed ones: `.bit-blow` itself carries the theme,
    // the size the canvas fills, and the colour-mode classes.
    [GeneratedRegex(@"(?<selector>\.bit-blow(?![a-z0-9])[^{};/]*?|\.bit-blow-[^{};/]*?)\s*\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"/\*(?<text>.*?)\*/", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"(?<name>-{0,2}[a-z][a-z0-9-]*)\s*:\s*(?<value>[^;]+);?")]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
