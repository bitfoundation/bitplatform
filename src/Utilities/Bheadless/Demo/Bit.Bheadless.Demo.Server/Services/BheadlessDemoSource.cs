using System.Net;
using System.Reflection;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Bit.Bheadless.Demo.Client.Core;

namespace Bit.Bheadless.Demo.Server.Services;

/// <summary>One demo of a page: its heading, the paragraph explaining it, and the code its tabs show.</summary>
public sealed record DemoExample(string Title, string? Description, string? Razor, string? Css);

/// <summary>A demo page: its route, its heading and lead paragraph, and its examples in page order.</summary>
public sealed record DemoPage(string Route, string Heading, string? Lead, DemoExample[] Examples, string Source);

/// <summary>
/// The demo pages, as the MCP server reads them: the CSS section's page at <c>/&lt;slug&gt;</c> and the
/// Tailwind section's at <c>/tw/&lt;slug&gt;</c>.
/// <para>
/// The pages' .razor files are embedded into this assembly (see the csproj) - not into the client, which
/// would ship them to every WebAssembly visitor. Only the markup that names and orders the examples is
/// read from them: each DemoBlock's title and description, and which constants its tabs show. The code
/// itself is read off the compiled page type, where those constants live as literals reflection cannot
/// misread.
/// </para>
/// </summary>
public static partial class BheadlessDemoSource
{
    public const string ResourcePrefix = "BitBheadlessDemoSource/";

    private static readonly Lazy<FrozenDictionary<string, DemoPage>> _pages = new(Load, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Every embedded page by its route, e.g. <c>/accordion</c> or <c>/tw/accordion</c>.</summary>
    public static FrozenDictionary<string, DemoPage> Pages => _pages.Value;

    public static DemoPage? Css(string slug) => Pages.GetValueOrDefault($"/{slug}");

    public static DemoPage? Tailwind(string slug) => Pages.GetValueOrDefault($"/tw/{slug}");

    public const string TailwindInputResourceName = "BitBheadlessDemoTailwind/input.css";

    private static readonly Lazy<(string Name, string Declaration)[]> _tailwindVariants = new(LoadTailwindVariants, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// The <c>@custom-variant</c>s of the demo's Tailwind input (<c>data-state-open</c> and the like): the
    /// Tailwind examples use them, and copied without them their classes would match nothing.
    /// </summary>
    public static (string Name, string Declaration)[] TailwindVariants => _tailwindVariants.Value;

    private static (string Name, string Declaration)[] LoadTailwindVariants()
    {
        using var stream = typeof(BheadlessDemoSource).Assembly.GetManifestResourceStream(TailwindInputResourceName);

        if (stream is null) return [];

        using var reader = new StreamReader(stream);

        return [.. CustomVariantRegex().Matches(reader.ReadToEnd())
                                       .Select(match => (match.Groups["name"].Value, WhitespaceRegex().Replace(match.Value.Trim(), " ")))];
    }

    private static FrozenDictionary<string, DemoPage> Load()
    {
        var assembly = typeof(BheadlessDemoSource).Assembly;

        // The page types by route, for reading the snippet constants off them.
        var pageTypes = typeof(Routes).Assembly.GetTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .SelectMany(type => type.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>().Select(route => (route.Template, Type: type)))
            .GroupBy(entry => entry.Template, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Type, StringComparer.OrdinalIgnoreCase);

        var pages = new Dictionary<string, DemoPage>(StringComparer.OrdinalIgnoreCase);

        // On Windows %(RecursiveDir) puts a backslash in the logical name.
        foreach (var name in assembly.GetManifestResourceNames()
                                     .Where(name => name.Replace('\\', '/').StartsWith(ResourcePrefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null) continue;

            using var reader = new StreamReader(stream);
            var source = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);

            if (PageDirectiveRegex().Match(source) is not { Success: true } directive) continue;

            var route = directive.Groups["route"].Value;
            pageTypes.TryGetValue(route, out var pageType);

            pages[route] = Parse(route, source, pageType);
        }

        return pages.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static DemoPage Parse(string route, string source, Type? pageType)
    {
        var header = HeaderRegex().Match(source);
        var heading = header.Success ? ToMarkdown(header.Groups["heading"].Value) : route.Trim('/');
        var lead = header.Success && header.Groups["lead"].Success ? ToMarkdown(header.Groups["lead"].Value) : null;

        var examples = DemoBlockRegex().Matches(source).Select(block =>
        {
            var attributes = block.Groups["attributes"].Value;
            var body = block.Groups["body"].Value;

            var title = ToMarkdown(AttributeValue(attributes, "Title") ?? string.Empty);
            var description = DescriptionRegex().Match(body) is { Success: true } match ? ToMarkdown(match.Groups["text"].Value) : null;

            return new DemoExample(title, description,
                                   Constant(pageType, AttributeValue(attributes, "Code")),
                                   Constant(pageType, AttributeValue(attributes, "Css")));
        }).ToArray();

        return new DemoPage(route, heading, lead, examples, source);
    }

    private static string? AttributeValue(string attributes, string name)
    {
        var match = Regex.Match(attributes, $@"\b{name}=""(?<value>[^""]*)""");

        return match.Success ? match.Groups["value"].Value : null;
    }

    /// <summary>The value of a <c>Code="@_basicCode"</c> attribute: the page's constant of that name.</summary>
    private static string? Constant(Type? pageType, string? attributeValue)
    {
        if (pageType is null || attributeValue is null || attributeValue.StartsWith('@') is false) return null;

        var field = pageType.GetField(attributeValue[1..], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        var value = field switch
        {
            { IsLiteral: true } => field.GetRawConstantValue() as string,
            { IsStatic: true } => field.GetValue(null) as string,
            _ => null,
        };

        // DemoBlock shows the snippet trimmed of the blank lines a raw string literal starts with.
        return value?.Replace("\r\n", "\n", StringComparison.Ordinal).Trim('\n');
    }

    /// <summary>
    /// The prose of a page's markup as Markdown: <c>&lt;code&gt;</c> as code spans, emphasis kept, links
    /// as links, every other tag dropped and the entities decoded.
    /// </summary>
    public static string ToMarkdown(string markup)
    {
        var text = markup.Replace("@@", "@", StringComparison.Ordinal);

        text = CodeTagRegex().Replace(text, match => $"`{WebUtility.HtmlDecode(TagRegex().Replace(match.Groups["text"].Value, string.Empty))}`");
        text = StrongTagRegex().Replace(text, match => $"**{match.Groups["text"].Value}**");
        text = EmphasisTagRegex().Replace(text, match => $"_{match.Groups["text"].Value}_");
        text = LinkTagRegex().Replace(text, match => $"[{match.Groups["text"].Value}]({match.Groups["href"].Value})");
        text = TagRegex().Replace(text, string.Empty);
        text = WhitespaceRegex().Replace(WebUtility.HtmlDecode(text), " ");

        return text.Trim();
    }

    [GeneratedRegex(@"^@custom-variant\s+(?<name>[\w-]+)\s+\(.*?\);", RegexOptions.Multiline)]
    private static partial Regex CustomVariantRegex();

    [GeneratedRegex(@"^@page\s+""(?<route>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex PageDirectiveRegex();

    [GeneratedRegex(@"<h1[^>]*>(?<heading>.*?)</h1>\s*(?:<p[^>]*>(?<lead>.*?)</p>)?", RegexOptions.Singleline)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"<(?:Tw)?DemoBlock\b(?<attributes>[^>]*)>(?<body>.*?)</(?:Tw)?DemoBlock>", RegexOptions.Singleline)]
    private static partial Regex DemoBlockRegex();

    [GeneratedRegex(@"<Description>(?<text>.*?)</Description>", RegexOptions.Singleline)]
    private static partial Regex DescriptionRegex();

    [GeneratedRegex(@"<code\b[^>]*>(?<text>.*?)</code>", RegexOptions.Singleline)]
    private static partial Regex CodeTagRegex();

    [GeneratedRegex(@"<(?:strong|b)\b[^>]*>(?<text>.*?)</(?:strong|b)>", RegexOptions.Singleline)]
    private static partial Regex StrongTagRegex();

    [GeneratedRegex(@"<(?:em|i)\b[^>]*>(?<text>.*?)</(?:em|i)>", RegexOptions.Singleline)]
    private static partial Regex EmphasisTagRegex();

    [GeneratedRegex(@"<a\b[^>]*\bhref=""(?<href>[^""]*)""[^>]*>(?<text>.*?)</a>", RegexOptions.Singleline)]
    private static partial Regex LinkTagRegex();

    [GeneratedRegex(@"</?[A-Za-z][^>]*>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
