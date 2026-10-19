using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Bmage.Demo.Server.Dtos;
using Bit.Bmage.Demo.Client.Shared;
using System.Text.RegularExpressions;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// Serves the two bodies of hand-written text the MCP tools hand out: the Bit.Bmage guides (the
/// README and the documents under docs/) and the source of this demo, whose pages are the worked
/// examples.
/// <para>
/// Both are embedded into this assembly by the .csproj rather than read from disk, so the tools
/// keep working from a published, single-folder deployment where the repository is nowhere to be
/// found.
/// </para>
/// </summary>
public static partial class BmageSourceCatalog
{
    private const string DocsPrefix = "BmageDocs/";
    private const string SourcePrefix = "BmageSource/";

    /// <summary>The name the README goes by among the documents. "overview" is accepted for it too.</summary>
    public const string ReadmeName = "readme";

    private static readonly Assembly _assembly = typeof(BmageSourceCatalog).Assembly;

    private static readonly Lazy<FrozenDictionary<string, string>> _documents = new(BuildDocuments);
    private static readonly Lazy<BmageDocumentDto[]> _documentList = new(BuildDocumentList);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BmageSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);

    /// <summary>The Bit.Bmage README, in full - empty when it was not embedded in this build.</summary>
    public static string Readme => GetDocument(ReadmeName) ?? string.Empty;

    /// <summary>Every guide, the README first and then the documents under docs/ in reading order.</summary>
    public static BmageDocumentDto[] Documents => _documentList.Value;

    /// <summary>Every embedded source file, described.</summary>
    public static BmageSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The order docs/README.md lists the guides in, which is the order they are meant to be read.
    /// A document that is not in it is listed after these rather than left out.
    /// </summary>
    private static readonly string[] _readingOrder =
    [
        ReadmeName, "getting-started", "architecture", "pipelines", "editor", "headless", "formats",
        "metadata-and-privacy", "performance", "security", "browser-support", "extending", "troubleshooting",
    ];

    /// <summary>
    /// A document by name - "pipelines", "docs/pipelines.md", "Pipelines" and "readme" all work - or
    /// null when there is no such document.
    /// </summary>
    public static string? GetDocument(string? name)
    {
        var key = NormalizeDocumentName(name);

        return key is null ? null : _documents.Value.GetValueOrDefault(key);
    }

    /// <summary>The canonical name of a document, or null when no document goes by it.</summary>
    public static string? ResolveDocumentName(string? name)
    {
        var key = NormalizeDocumentName(name);

        return key is not null && _documents.Value.ContainsKey(key) ? key : null;
    }

    /// <summary>
    /// The text under <paramref name="heading"/> in one document, including its sub-sections.
    /// Matching is case- and punctuation-insensitive, so "the optimiser" finds "## The optimiser".
    /// </summary>
    public static string? GetDocumentSection(string? name, string? heading)
    {
        var document = GetDocument(name);

        if (document is null || string.IsNullOrWhiteSpace(heading)) return null;

        var lines = document.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var normalized = NormalizeHeading(heading);

        int start = -1, level = 0;
        var fenced = false;

        for (int i = 0; i < lines.Length; i++)
        {
            if (IsCodeFence(lines[i]))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced) continue;

            if (TryReadHeading(lines[i], out var lineLevel, out var text) is false) continue;

            if (start < 0)
            {
                if (NormalizeHeading(text) != normalized && NormalizeHeading(WithoutAside(text)) != normalized) continue;

                start = i;
                level = lineLevel;
                continue;
            }

            // The section ends at the next heading of the same or a higher rank.
            if (lineLevel > level) continue;

            return string.Join('\n', lines[start..i]).TrimEnd();
        }

        return start < 0 ? null : string.Join('\n', lines[start..]).TrimEnd();
    }

    /// <summary>Every heading of one document at levels 2 and 3, in reading order, with its level.</summary>
    public static (string Heading, int Level)[] GetHeadings(string? name)
    {
        var document = GetDocument(name);

        if (document is null) return [];

        var headings = new List<(string, int)>();
        var fenced = false;

        foreach (var line in document.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (IsCodeFence(line))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced || TryReadHeading(line, out var level, out var text) is false || level is not (2 or 3)) continue;

            // The table of contents is a list of links to the sections below it: as an answer it is
            // strictly worse than the section it points at.
            if (NormalizeHeading(text) == "tableofcontents") continue;

            headings.Add((text, level));
        }

        return [.. headings];
    }

    /// <summary>The content of an embedded source file, or null when the path is unknown.</summary>
    public static string? GetSourceFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        return _sourceFiles.Value.GetValueOrDefault(Normalize(path));
    }

    private static string? NormalizeDocumentName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var key = Normalize(name).ToLowerInvariant();

        if (key.StartsWith("docs/", StringComparison.Ordinal)) key = key["docs/".Length..];
        if (key.EndsWith(".md", StringComparison.Ordinal)) key = key[..^3];

        key = key.Replace(' ', '-').Replace('_', '-');

        return key is "overview" or "readme" or "guide" or "index" ? ReadmeName : key;
    }

    private static string? ReadResource(string name)
    {
        using var stream = _assembly.GetManifestResourceStream(name);
        if (stream is null) return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    private static FrozenDictionary<string, string> BuildDocuments()
    {
        var documents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in _assembly.GetManifestResourceNames())
        {
            var normalized = Normalize(resource);

            if (normalized.StartsWith(DocsPrefix, StringComparison.OrdinalIgnoreCase) is false) continue;

            var content = ReadResource(resource);
            if (content is null) continue;

            var file = normalized[DocsPrefix.Length..];
            var key = file.Equals("README.md", StringComparison.OrdinalIgnoreCase) ? ReadmeName : NormalizeDocumentName(file)!;

            documents[key] = content;
        }

        return documents.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static BmageDocumentDto[] BuildDocumentList()
    {
        return [.. _documents.Value
            .Select(document => new BmageDocumentDto
            {
                Name = document.Key,
                Title = TitleOf(document.Value) ?? document.Key,
                Summary = SummaryOf(document.Value),
                Sections = [.. GetHeadings(document.Key).Where(heading => heading.Level == 2).Select(heading => heading.Heading)],
                Lines = CountLines(document.Value)
            })
            .OrderBy(document => Array.IndexOf(_readingOrder, document.Name) is var index and >= 0 ? index : int.MaxValue)
            .ThenBy(document => document.Name, StringComparer.Ordinal)];
    }

    /// <summary>The document's first heading.</summary>
    private static string? TitleOf(string content)
    {
        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (TryReadHeading(line, out var level, out var text) && level == 1) return text;
        }

        return null;
    }

    /// <summary>
    /// The first paragraph of prose under the title - every guide opens with one sentence saying
    /// what it covers, which is the description a listing wants.
    /// </summary>
    private static string? SummaryOf(string content)
    {
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var paragraph = new List<string>();
        var fenced = false;

        foreach (var line in lines.SkipWhile(line => line.StartsWith("# ", StringComparison.Ordinal) is false).Skip(1))
        {
            if (IsCodeFence(line))
            {
                fenced = fenced is false;
                if (paragraph.Count > 0) break;
                continue;
            }

            if (fenced) continue;

            var text = line.Trim();

            if (text.StartsWith('#') || text.StartsWith('|') || text.StartsWith('>') || text.StartsWith('-') || text.StartsWith('*'))
            {
                if (paragraph.Count > 0) break;
                continue;
            }

            if (text.Length == 0)
            {
                if (paragraph.Count > 0) break;
                continue;
            }

            paragraph.Add(text);
        }

        return paragraph.Count == 0 ? null : Summarize(string.Join(' ', paragraph));
    }

    private static FrozenDictionary<string, string> BuildSourceFiles()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in _assembly.GetManifestResourceNames())
        {
            var normalized = Normalize(resource);

            if (normalized.StartsWith(SourcePrefix, StringComparison.OrdinalIgnoreCase) is false) continue;

            var content = ReadResource(resource);
            if (content is null) continue;

            files[normalized[SourcePrefix.Length..]] = content;
        }

        return files.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static BmageSourceFileDto[] BuildSourceFileList()
    {
        // A demo page describes itself in the navigation, in a sentence written to say what the page
        // demonstrates. Scraping the file for a description instead reaches whatever comment comes
        // first, which for a page is usually a note about prerendering, not what the page is for.
        var pages = NavItem.All.ToDictionary(page => Normalize(page.SourcePath), StringComparer.OrdinalIgnoreCase);

        return [.. _sourceFiles.Value
            .Select(file =>
            {
                var page = pages.GetValueOrDefault(file.Key);

                return new BmageSourceFileDto
                {
                    Path = file.Key,
                    Kind = file.Key.Contains("/Pages/", StringComparison.OrdinalIgnoreCase) ? "Demo page"
                         : file.Key.StartsWith("Demo/Server/", StringComparison.OrdinalIgnoreCase) ? "Host"
                         : "Demo",
                    Description = page?.Description ?? DescribeSource(file.Key, file.Value),
                    Lines = CountLines(file.Value),
                    Title = page?.Title,
                    Slug = page?.Href,
                    Keywords = page?.Keywords
                };
            })
            .OrderBy(file => file.Kind, StringComparer.Ordinal)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// The number of lines in a file. A trailing newline ends the last line rather than starting
    /// another one, so a file that has one is not reported a line longer than it is.
    /// </summary>
    private static int CountLines(string content)
    {
        if (content.Length == 0) return 0;

        var newlines = content.Count(c => c == '\n');

        return content[^1] == '\n' ? newlines : newlines + 1;
    }

    /// <summary>
    /// A one-line description of a source file, taken from whatever the file itself already says:
    /// its leading razor/C# comment, its XML summary, or - for a page - its &lt;PageTitle&gt;.
    /// </summary>
    private static string? DescribeSource(string path, string content)
    {
        if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return "The WebAssembly client's project file: the Bit.Bmage.Editor reference, and RunAOTCompilation switched on for a Release publish.";
        }

        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        var summary = XmlSummaryRegex().Match(content);
        if (summary.Success)
        {
            // An XML summary is markup: <see cref="..."/> and friends are noise in a listing.
            var text = summary.Groups["text"].Value.Replace("///", " ", StringComparison.Ordinal);

            return Summarize(XmlTagRegex().Replace(text, string.Empty));
        }

        var title = PageTitleRegex().Match(content);
        if (title.Success) return Summarize(title.Groups["text"].Value);

        var lineComment = LeadingLineCommentRegex().Match(content);
        if (lineComment.Success) return Summarize(lineComment.Value.Replace("//", " ", StringComparison.Ordinal));

        // Nothing at the top of the file said what it is - the first commentary in it will do.
        var comment = RazorCommentRegex().Match(content);

        return comment.Success ? Summarize(comment.Groups["text"].Value) : null;
    }

    private static string? Summarize(string text)
    {
        text = WhitespaceRegex().Replace(text, " ").Trim();
        if (text.Length == 0) return null;

        // The first sentence is the description; the rest is the file's own commentary.
        var stop = text.IndexOf(". ", StringComparison.Ordinal);
        if (stop > 0) text = text[..(stop + 1)];

        return text.Length <= 220 ? text : $"{text[..217]}...";
    }

    /// <summary>
    /// A Markdown code fence. A '#' line inside one is a shell comment or a C# preprocessor
    /// directive, not a heading, and must not start or end a section.
    /// </summary>
    private static bool IsCodeFence(string line)
    {
        var text = line.TrimStart();

        return text.StartsWith("```", StringComparison.Ordinal) || text.StartsWith("~~~", StringComparison.Ordinal);
    }

    private static bool TryReadHeading(string line, out int level, out string text)
    {
        level = 0;
        text = string.Empty;

        while (level < line.Length && line[level] == '#') level++;

        if (level is 0 or > 6 || level >= line.Length || line[level] != ' ') return false;

        text = line[(level + 1)..].Trim();

        return text.Length > 0;
    }

    /// <summary>
    /// A heading without the parenthesised aside at its end, so "mcp server" finds
    /// "## MCP server (for AI agents)".
    /// </summary>
    private static string WithoutAside(string heading)
    {
        var aside = heading.LastIndexOf(" (", StringComparison.Ordinal);

        return aside > 0 && heading.EndsWith(')') ? heading[..aside] : heading;
    }

    /// <summary>
    /// Reduces a heading to its comparable core, so "the optimiser" finds "## The optimiser" -
    /// case and punctuation aside.
    /// </summary>
    internal static string NormalizeHeading(string heading)
    {
        var builder = new StringBuilder(heading.Length);

        foreach (var c in heading)
        {
            if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static string Normalize(string path) => path.Replace('\\', '/').Trim().Trim('/');

    [GeneratedRegex(@"^\s*@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex LeadingRazorCommentRegex();

    [GeneratedRegex(@"@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex RazorCommentRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex XmlTagRegex();

    [GeneratedRegex(@"///\s*<summary>(?<text>.*?)</summary>", RegexOptions.Singleline)]
    private static partial Regex XmlSummaryRegex();

    [GeneratedRegex(@"<PageTitle>(?<text>.*?)</PageTitle>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PageTitleRegex();

    [GeneratedRegex(@"^\s*(//[^\n]*\n)+")]
    private static partial Regex LeadingLineCommentRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
