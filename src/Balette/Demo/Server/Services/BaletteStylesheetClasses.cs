using System.Text.RegularExpressions;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// The rules of the stylesheet Bit.Balette ships, read out of the file itself.
/// <para>
/// The variables have a tool of their own; the rules that read them are entries of the search index,
/// each pointing at the stylesheet, because "how do I change the active row" or "why is the dialog
/// full screen on my phone" is answered by one of its rules. Parsing the shipped file means a rule the
/// library drops is never named, and one it adds is never missing.
/// </para>
/// <para>
/// A rule inside a media query is kept apart from the rule of the same selector outside it, with the
/// condition in its description: "top: 0" merged into the dialog's own rule would describe a dialog
/// that is never 12vh from the top, which is what it is everywhere but on a phone.
/// </para>
/// </summary>
public static partial class BaletteStylesheetClasses
{
    public sealed record StyleRule(string Selector, string? Media, string Description);

    private static readonly Lazy<StyleRule[]> _classes = new(Build);

    /// <summary>Every rule of the stylesheet that targets a bit-balette-* class, once per selector and media condition, in file order.</summary>
    public static StyleRule[] Classes => _classes.Value;

    /// <summary>
    /// The rules that apply whether or not the dialog carries <c>bit-balette-dialog</c> - the trigger,
    /// the shortcut hints and the utilities. <c>Unstyled</c> drops that one class, which every other rule
    /// is scoped to, so these are what still applies when the stylesheet is linked anyway.
    /// </summary>
    public static IEnumerable<StyleRule> Unscoped => Classes.Where(rule => rule.Selector.Split(',').Any(part => part.Trim().StartsWith(".bit-balette-dialog", StringComparison.Ordinal) is false));

    private static StyleRule[] Build()
    {
        var css = BaletteSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);
        var media = MediaBlocks(css);

        var rules = new List<(string Selector, string? Media, List<string> Comments, List<string> Properties)>();

        foreach (Match match in RuleRegex().Matches(css))
        {
            var selector = WhitespaceRegex().Replace(match.Groups["selector"].Value, " ").Trim();
            var body = match.Groups["body"].Value;
            var condition = media.FirstOrDefault(block => match.Index > block.Start && match.Index < block.End).Condition;

            var index = rules.FindIndex(r => r.Selector == selector && r.Media == condition);
            if (index < 0)
            {
                rules.Add((selector, condition, [], []));
                index = rules.Count - 1;
            }

            var rule = rules[index];

            // A comment right above the rule, or inside it, is the stylesheet saying why the rule exists.
            // A section banner ("/* ---------- List ---------- */") names a group of rules, not this one.
            var before = css[..match.Index].TrimEnd();
            if (before.EndsWith("*/", StringComparison.Ordinal))
            {
                var open = before.LastIndexOf("/*", StringComparison.Ordinal);
                var comment = open >= 0 ? before[(open + 2)..^2] : string.Empty;
                if (comment.Contains("---", StringComparison.Ordinal) is false && comment.TrimStart().StartsWith('!') is false) rule.Comments.Add(comment);
            }

            foreach (Match comment in CommentRegex().Matches(body)) rule.Comments.Add(comment.Groups["text"].Value);

            foreach (Match declaration in DeclarationRegex().Matches(CommentRegex().Replace(body, string.Empty)))
            {
                var text = $"{declaration.Groups["name"].Value}: {declaration.Groups["value"].Value.Trim()}";
                if (rule.Properties.Contains(text) is false) rule.Properties.Add(text);
            }
        }

        return [.. rules.Select(rule => new StyleRule(rule.Selector, rule.Media, Describe(rule.Media, rule.Comments, rule.Properties)))];
    }

    private static string Describe(string? media, List<string> comments, List<string> properties)
    {
        var prose = WhitespaceRegex().Replace(string.Join(' ', comments), " ").Trim();
        var declarations = string.Join("; ", properties);
        var description = prose.Length == 0 ? declarations : $"{prose} ({declarations})";

        return media is null ? description : $"Under {media}: {description}";
    }

    /// <summary>
    /// Where every <c>@media</c> block starts and ends, found by counting braces - a media query holds
    /// rules, so its block cannot be matched by a pattern that stops at the first closing brace.
    /// </summary>
    private static List<(int Start, int End, string Condition)> MediaBlocks(string css)
    {
        var blocks = new List<(int, int, string)>();

        foreach (Match match in MediaRegex().Matches(css))
        {
            var open = css.IndexOf('{', match.Index);
            if (open < 0) continue;

            var depth = 0;
            for (var i = open; i < css.Length; i++)
            {
                if (css[i] == '{') depth++;
                else if (css[i] == '}' && --depth == 0)
                {
                    blocks.Add((open, i, WhitespaceRegex().Replace(css[match.Index..open], " ").Trim()));
                    break;
                }
            }
        }

        return blocks;
    }

    [GeneratedRegex(@"(?<selector>[^{};/]*\.bit-balette-[^{};/]*?)\s*\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"@media[^{]*")]
    private static partial Regex MediaRegex();

    [GeneratedRegex(@"/\*(?<text>.*?)\*/", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"(?<name>-?[a-z][a-z-]*)\s*:\s*(?<value>[^;]+);?")]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
