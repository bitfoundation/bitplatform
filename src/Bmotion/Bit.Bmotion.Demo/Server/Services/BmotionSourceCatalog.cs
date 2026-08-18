using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Bmotion.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Serves the two bodies of hand-written text the MCP tools hand out: the Bit.Bmotion guide (the
/// library README) and the source of this demo, whose pages are the worked examples.
/// <para>
/// Both are embedded into this assembly by the .csproj rather than read from disk, so the tools
/// keep working from a published, single-folder deployment where the repository is nowhere to be
/// found.
/// </para>
/// </summary>
public static partial class BmotionSourceCatalog
{
    private const string ReadmeResource = "BmotionDocs/README.md";
    private const string SourcePrefix = "BmotionSource/";

    private static readonly Assembly _assembly = typeof(BmotionSourceCatalog).Assembly;

    private static readonly Lazy<string> _readme = new(() => ReadResource(ReadmeResource) ?? string.Empty);
    private static readonly Lazy<string[]> _readmeLines = new(() => Readme.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    private static readonly Lazy<BmotionGuideSectionDto[]> _guideSections = new(BuildGuideSections);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BmotionSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);

    /// <summary>The Bit.Bmotion guide, in full.</summary>
    public static string Readme => _readme.Value;

    /// <summary>Every heading of the guide, in reading order.</summary>
    public static BmotionGuideSectionDto[] GuideSections => _guideSections.Value;

    /// <summary>Every embedded source file, described.</summary>
    public static BmotionSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The guide text under <paramref name="heading"/>, including its sub-sections. Matching is
    /// case- and punctuation-insensitive, so "layout shared elements" finds
    /// "Layout &amp; shared elements".
    /// </summary>
    public static string? GetGuideSection(string heading)
    {
        if (string.IsNullOrWhiteSpace(heading)) return null;

        var lines = _readmeLines.Value;
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
                if (NormalizeHeading(text) != normalized) continue;

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

    /// <summary>The content of an embedded source file, or null when the path is unknown.</summary>
    public static string? GetSourceFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        return _sourceFiles.Value.GetValueOrDefault(Normalize(path));
    }

    private static string? ReadResource(string name)
    {
        using var stream = _assembly.GetManifestResourceStream(name);
        if (stream is null) return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    private static BmotionGuideSectionDto[] BuildGuideSections()
    {
        var lines = _readmeLines.Value;
        var headings = new List<(int Index, int Level, string Text)>();

        var fenced = false;

        for (int i = 0; i < lines.Length; i++)
        {
            if (IsCodeFence(lines[i]))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced) continue;

            // Every level is collected, not only the two that are reported: a section ends at the
            // next heading of the same or a higher rank, and a level 1 heading outranks both - so
            // leaving those out would run a section on into the chapter after it, as GetGuideSection
            // (which reads all of them) would not.
            if (TryReadHeading(lines[i], out var level, out var text))
            {
                headings.Add((i, level, text));
            }
        }

        var sections = new List<BmotionGuideSectionDto>(headings.Count);
        string? parent = null;

        for (int i = 0; i < headings.Count; i++)
        {
            var (index, level, text) = headings[i];

            if (level <= 2) parent = level == 2 ? text : null;

            if (level is not (2 or 3)) continue;

            // The section runs until the next heading of the same or a higher rank.
            var end = lines.Length;

            for (int j = i + 1; j < headings.Count; j++)
            {
                if (headings[j].Level > level) continue;

                end = headings[j].Index;
                break;
            }

            sections.Add(new BmotionGuideSectionDto
            {
                Heading = text,
                Level = level,
                Parent = level == 2 ? null : parent,
                Lines = end - index
            });
        }

        // The table of contents is a list of links to the sections below it: as an answer it is
        // strictly worse than the section it points at, and it outranks real content in search.
        return [.. sections.Where(section => NormalizeHeading(section.Heading) != "tableofcontents")];
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

    private static BmotionSourceFileDto[] BuildSourceFileList()
    {
        return [.. _sourceFiles.Value
            .Select(file => new BmotionSourceFileDto
            {
                Path = file.Key,
                Kind = file.Key.Contains("/Pages/", StringComparison.OrdinalIgnoreCase) ? "Demo page"
                     : file.Key.StartsWith("Demo/Server/", StringComparison.OrdinalIgnoreCase) ? "Host"
                     : "Demo",
                Description = DescribeSource(file.Value),
                Lines = CountLines(file.Value)
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
    private static string? DescribeSource(string content)
    {
        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        var summary = XmlSummaryRegex().Match(content);
        if (summary.Success)
        {
            // An XML summary is markup: <paramref name="Slug"/> and friends are noise in a listing.
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
    /// directive, not a heading, and must not start or end a guide section.
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
    /// Reduces a heading to its comparable core, so "Motion values" finds "## Motion values" and
    /// "layout shared elements" finds "## Layout &amp; shared elements".
    /// </summary>
    private static string NormalizeHeading(string heading)
    {
        var builder = new StringBuilder(heading.Length);

        foreach (var c in heading)
        {
            if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static string Normalize(string path) => path.Replace('\\', '/').Trim('/');

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
