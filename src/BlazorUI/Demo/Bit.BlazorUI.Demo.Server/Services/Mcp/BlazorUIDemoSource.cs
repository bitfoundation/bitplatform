using System.Net;
using System.Text;
using HtmlAgilityPack;
using System.Collections.Frozen;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>One example section of a demo page.</summary>
/// <param name="Title">The heading the section carries on the site, and the value the examples tool filters on.</param>
/// <param name="Prose">The paragraph above the live preview, when the section has one.</param>
/// <param name="RazorField">The name of the page field holding the Razor sample, resolved by reflection.</param>
/// <param name="CsharpField">The name of the page field holding the C# sample, when the section shows one.</param>
/// <param name="Tab">The pivot tab the section sits under, for the multi-API components that have tabs.</param>
public sealed record DemoExampleSource(string Title, string? Prose, string? RazorField, string? CsharpField, string? Tab)
{
    /// <summary>The type the sample fields live on - the tab's own component, not the page, when there are tabs.</summary>
    public Type? Owner { get; init; }
}

/// <summary>What the markup of one demo page says, beyond what its compiled type carries.</summary>
public sealed record DemoPageSource(string? Description, string? Notes, string? SourceUrl, string? DemoUrl, IReadOnlyList<DemoExampleSource> Examples);

/// <summary>
/// Reads the demo pages' own Razor markup - embedded into this assembly by the .csproj - for the
/// three things the compiled page type cannot answer: what each example section is called, what
/// the paragraph above it says, and which of the page's sample fields it shows.
/// <para>
/// The samples themselves and the parameter tables are reflected off the page type rather than
/// parsed, because they are C# and reflection cannot misread them. Only the ordering and the
/// naming live in the markup, and only that is read here.
/// </para>
/// </summary>
public static partial class BlazorUIDemoSource
{
    private const string ResourcePrefix = "BitBlazorUIDemoSource/";

    /// <summary>Elements that hold the paragraph above a preview rather than being part of it.</summary>
    private static readonly HashSet<string> _proseElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "div", "p", "span", "section", "b", "i", "em", "strong", "code", "ul", "ol", "br", "bittext"
    };

    /// <summary>The classes the demo pages mark a live preview with - where the prose stops.</summary>
    private static readonly string[] _previewClasses = ["example-content", "example-box"];

    private static readonly Lazy<FrozenDictionary<string, string>> _sources = new(Load, LazyThreadSafetyMode.PublicationOnly);
    private static readonly ConcurrentDictionary<string, DemoPageSource?> _parsed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The markup of the demo page for a component, parsed. Null when the component has no demo
    /// page, which the catalog reports as a component documented by its API alone.
    /// </summary>
    public static DemoPageSource? Get(Type demoPageType)
        => _parsed.GetOrAdd(demoPageType.Name, _ => Parse(demoPageType));

    /// <summary>The markup of one embedded page verbatim, for the search index to read prose out of.</summary>
    public static string? Raw(string fileName) => _sources.Value.GetValueOrDefault(fileName);

    private static DemoPageSource? Parse(Type demoPageType)
    {
        if (_sources.Value.TryGetValue($"{demoPageType.Name}.razor", out var markup) is false) return null;

        var document = new HtmlDocument();
        document.LoadHtml(markup);

        var page = document.DocumentNode.Descendants().FirstOrDefault(n => n.Name.Equals("demopage", StringComparison.OrdinalIgnoreCase));

        var examples = new List<DemoExampleSource>();

        // A page's sections either sit in its own markup or are composed out of components of their
        // own - one per pivot tab for a multi-API component, one per chart type for the legacy
        // chart. Both are collected, in the order the page renders them, so the answer for a
        // component is its whole page rather than whichever half happened to be inline.
        examples.AddRange(ReadExamples(document.DocumentNode, tab: null, demoPageType));

        var tabs = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var item in document.DocumentNode.Descendants().Where(n => n.Name.Equals("bitpivotitem", StringComparison.OrdinalIgnoreCase)))
        {
            if (Attribute(item, "HeaderText") is not string header) continue;

            foreach (Match match in TabComponentRegex().Matches(item.InnerHtml))
            {
                tabs.TryAdd(match.Groups[1].Value, header);
            }
        }

        foreach (var component in TabComponentRegex().Matches(markup).Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal))
        {
            var owner = demoPageType.Assembly.GetType($"{demoPageType.Namespace}.{component}");

            if (owner is null || _sources.Value.TryGetValue($"{component}.razor", out var section) is false) continue;

            var sectionDocument = new HtmlDocument();
            sectionDocument.LoadHtml(section);

            examples.AddRange(ReadExamples(sectionDocument.DocumentNode, tabs.GetValueOrDefault(component), owner));
        }

        return new DemoPageSource(
            Description: page is null ? null : Attribute(page, "Description"),
            Notes: page is null ? null : Attribute(page, "Notes") ?? TemplateText(page, "notestemplate"),
            SourceUrl: page is null ? null : Attribute(page, "GitHubUrl") ?? Attribute(page, "GitHubExtrasUrl") ?? Attribute(page, "GitHubLegacyUrl"),
            DemoUrl: page is null ? null : Attribute(page, "GitHubDemoUrl"),
            Examples: examples);
    }

    private static IEnumerable<DemoExampleSource> ReadExamples(HtmlNode root, string? tab, Type owner)
    {
        foreach (var node in root.Descendants().Where(n => n.Name.Equals("demoexample", StringComparison.OrdinalIgnoreCase)))
        {
            var title = Attribute(node, "Title");

            if (title is null) continue;

            yield return new DemoExampleSource(
                Title: title,
                Prose: Prose(node),
                RazorField: Field(node, "RazorCode"),
                CsharpField: Field(node, "CsharpCode"),
                Tab: tab)
            {
                Owner = owner
            };
        }
    }

    /// <summary>
    /// The paragraph a section opens with, when it has one.
    /// <para>
    /// A section is written as an explanation followed by the running component, so the prose is
    /// whatever comes before the first thing that is neither text nor a wrapper around it - a
    /// <c>Bit</c> component, a form control, or the container the page marks a preview with. Taking
    /// the text of the whole section instead would return every button label in the demo, and the
    /// labels are already in the Razor sample below it.
    /// </para>
    /// </summary>
    private static string? Prose(HtmlNode example)
    {
        var builder = new StringBuilder();

        foreach (var child in example.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                builder.Append(' ').Append(child.InnerText);
                continue;
            }

            if (child.NodeType != HtmlNodeType.Element) continue;

            if (_proseElements.Contains(child.Name) is false) break;

            var classes = child.GetAttributeValue("class", string.Empty);

            if (_previewClasses.Any(c => classes.Contains(c, StringComparison.OrdinalIgnoreCase))) break;

            builder.Append(' ').Append(child.InnerText);
        }

        var prose = Collapse(WebUtility.HtmlDecode(builder.ToString()));

        // Long enough to be a sentence, and with a space in it. Everything shorter that survived
        // the walk above is a stray label rather than an explanation, and a hit on it would read as
        // documentation that says nothing.
        return prose.Length >= 40 && prose.Contains(' ', StringComparison.Ordinal) ? prose : null;
    }

    /// <summary>The field a <c>RazorCode="@example1RazorCode"</c> attribute names.</summary>
    private static string? Field(HtmlNode node, string attribute)
    {
        var value = Attribute(node, attribute);

        return value is not null && value.StartsWith('@') ? value[1..] : null;
    }

    private static string? Attribute(HtmlNode node, string name)
    {
        var value = node.GetAttributeValue(name, null);

        return string.IsNullOrWhiteSpace(value) ? null : Collapse(WebUtility.HtmlDecode(value));
    }

    /// <summary>The text of a <c>&lt;NotesTemplate&gt;</c>, for the pages that write their notes as markup.</summary>
    private static string? TemplateText(HtmlNode page, string templateName)
    {
        var template = page.ChildNodes.FirstOrDefault(n => n.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));

        if (template is null) return null;

        var text = Collapse(WebUtility.HtmlDecode(template.InnerText));

        return text.Length == 0 ? null : text;
    }

    private static string Collapse(string text)
    {
        var builder = new StringBuilder(text.Length);
        var space = true;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (space is false) builder.Append(' ');
                space = true;
                continue;
            }

            builder.Append(c);
            space = false;
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// Every embedded demo page, keyed by its file name. The .csproj writes the folder into the
    /// resource name and MSBuild's separator for it differs by platform, while the file names are
    /// unique across the whole tree - so the trailing segment is the key and the folder is dropped.
    /// </summary>
    private static FrozenDictionary<string, string> Load()
    {
        var assembly = typeof(BlazorUIDemoSource).Assembly;
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.StartsWith(ResourcePrefix, StringComparison.Ordinal) is false) continue;

            using var stream = assembly.GetManifestResourceStream(name);

            if (stream is null) continue;

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            sources[name[(name.LastIndexOfAny(['/', '\\']) + 1)..]] = reader.ReadToEnd();
        }

        return sources.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The tab component a pivot item renders. Matched against the raw markup rather than the
    /// parsed tree because these names start with an underscore, which is not a character an HTML
    /// parser is obliged to accept at the start of a tag.
    /// </summary>
    [GeneratedRegex(@"<(_[A-Za-z0-9_]+)")]
    private static partial Regex TabComponentRegex();
}
