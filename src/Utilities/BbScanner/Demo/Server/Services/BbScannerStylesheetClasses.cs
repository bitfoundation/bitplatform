using System.Text.RegularExpressions;

namespace Bit.BbScanner.Demo.Server.Services;

/// <summary>
/// The CSS classes and custom properties of the stylesheet Bit.BbScanner ships, read out of the file itself.
/// <para>
/// The stylesheet is the scanner's whole look - the viewfinder, the dimming mask, the reticle, the result
/// highlight, the status bar and the controls - and every colour, radius and dimension in it is a
/// <c>--bit-bbscanner-*</c> custom property declared at zero specificity. It has no tool of its own: its classes
/// and properties are entries of the search index, each pointing at the stylesheet, because "make the reticle
/// green" or "hide the status bar background" is answered by one of its rules. Parsing the shipped file means a
/// class or property the library drops is never named, and one it adds is never missing.
/// </para>
/// </summary>
public static partial class BbScannerStylesheetClasses
{
    public sealed record StyleRule(string Selector, string Description);

    public sealed record CustomProperty(string Name, string Default, string? Description);

    private static readonly Lazy<StyleRule[]> _classes = new(Build);
    private static readonly Lazy<CustomProperty[]> _properties = new(BuildProperties);

    /// <summary>Every selector of the stylesheet, once each, in the order the file first declares it.</summary>
    public static StyleRule[] Classes => _classes.Value;

    /// <summary>Every theming custom property, with the default the stylesheet gives it.</summary>
    public static CustomProperty[] Properties => _properties.Value;

    private static string Css => BbScannerSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static StyleRule[] Build()
    {
        var css = Css;

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

    /// <summary>
    /// The custom properties are declared once, in the zero specificity <c>:where(.bit-bbscanner)</c> block, which
    /// is what lets an application override any of them from any rule of its own.
    /// </summary>
    private static CustomProperty[] BuildProperties()
    {
        var block = DefaultsBlockRegex().Match(Css);
        if (block.Success is false) return [];

        var body = block.Groups["body"].Value;

        // The comment above the block explains all of it; every property gets that one sentence.
        var before = Css[..block.Index].TrimEnd();
        string? note = null;
        if (before.EndsWith("*/", StringComparison.Ordinal))
        {
            var open = before.LastIndexOf("/*", StringComparison.Ordinal);
            if (open >= 0) note = WhitespaceRegex().Replace(before[(open + 2)..^2], " ").Trim();
        }

        return [.. CustomPropertyRegex().Matches(body)
            .Select(match => new CustomProperty(match.Groups["name"].Value, match.Groups["value"].Value.Trim(), note))];
    }

    private static string Describe(List<string> comments, List<string> properties)
    {
        var prose = WhitespaceRegex().Replace(string.Join(' ', comments), " ").Trim();
        var declarations = string.Join("; ", properties);

        return prose.Length == 0 ? declarations : $"{prose} ({declarations})";
    }

    // Not preceded by '(': the selector inside :where(.bit-bbscanner) is the defaults block, which BuildProperties
    // reads, not a rule of its own.
    [GeneratedRegex(@"(?<!\()(?<selector>\.bit-bbscanner[^{};/]*?)\s*\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@":where\(\.bit-bbscanner\)\s*\{(?<body>[^{}]*)\}")]
    private static partial Regex DefaultsBlockRegex();

    [GeneratedRegex(@"(?<name>--bit-bbscanner-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);")]
    private static partial Regex CustomPropertyRegex();

    [GeneratedRegex(@"/\*(?<text>.*?)\*/", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"(?<name>-?[a-z][a-z-]*)\s*:\s*(?<value>[^;]+);?")]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
