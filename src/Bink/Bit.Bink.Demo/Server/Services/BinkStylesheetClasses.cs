using System.Text.RegularExpressions;

namespace Bit.Bink.Demo.Server.Services;

/// <summary>
/// The CSS classes of the stylesheet Bit.Bink ships, read out of the file itself.
/// <para>
/// The stylesheet is layout only - two stacked canvases, an input surface, the text editor and the
/// focus ring - and carries no theme variables, so it has no tool of its own: its classes are
/// entries of the search index, each pointing at the stylesheet, because "how do I change the
/// focus outline" or "why is the canvas not scrolling" is answered by one of its rules. Parsing the
/// shipped file means a class the library drops is never named, and one it adds is never missing.
/// </para>
/// </summary>
public static partial class BinkStylesheetClasses
{
    public sealed record StyleRule(string Selector, string Description);

    private static readonly Lazy<StyleRule[]> _classes = new(Build);

    /// <summary>Every selector of the stylesheet, once each, in the order the file first declares it.</summary>
    public static StyleRule[] Classes => _classes.Value;

    private static StyleRule[] Build()
    {
        var css = BinkSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);

        var rules = new List<(string Selector, List<string> Comments, List<string> Properties)>();

        foreach (Match match in RuleRegex().Matches(css))
        {
            var selector = WhitespaceRegex().Replace(match.Groups["selector"].Value, " ").Trim();
            var body = match.Groups["body"].Value;

            var rule = rules.FirstOrDefault(r => r.Selector == selector);
            if (rule.Selector is null)
            {
                rule = (selector, new List<string>(), new List<string>());
                rules.Add(rule);
            }

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
                var text = $"{declaration.Groups["name"].Value}: {declaration.Groups["value"].Value.Trim()}";
                if (rule.Properties.Contains(text) is false) rule.Properties.Add(text);
            }
        }

        return [.. rules.Select(rule => new StyleRule(rule.Selector, Describe(rule.Comments, rule.Properties)))];
    }

    private static string Describe(List<string> comments, List<string> properties)
    {
        var prose = WhitespaceRegex().Replace(string.Join(' ', comments), " ").Trim();
        var declarations = string.Join("; ", properties);

        return prose.Length == 0 ? declarations : $"{prose} ({declarations})";
    }

    [GeneratedRegex(@"(?<selector>\.bit-bink-[^{};/]*?)\s*\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"/\*(?<text>.*?)\*/", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"(?<name>-?[a-z][a-z-]*)\s*:\s*(?<value>[^;]+);?")]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
