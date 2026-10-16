using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Blex.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// Serves the two bodies of hand-written source the MCP tools hand out: the library's README (its
/// reference guide) and the source files of this demo plus the standalone WebAssembly sample.
/// <para>
/// All of them are embedded into this assembly by the .csproj rather than read from disk, so the MCP
/// server keeps working from a published, single-folder deployment where the repository is nowhere to
/// be found.
/// </para>
/// </summary>
public static partial class BlexSourceCatalog
{
    private const string ReadmeResource = "BlexDocs/README.md";
    private const string SourcePrefix = "BlexSource/";

    private static readonly Assembly _assembly = typeof(BlexSourceCatalog).Assembly;

    private static readonly Lazy<string> _readme = new(() => ReadResource(ReadmeResource) ?? string.Empty);
    private static readonly Lazy<string[]> _readmeLines = new(() => Readme.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    private static readonly Lazy<BlexGuideSectionDto[]> _guideSections = new(BuildGuideSections);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BlexSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);

    /// <summary>The library's README, in full.</summary>
    public static string Readme => _readme.Value;

    /// <summary>The guide's sections, as the index GetBlexGuideSection answers with when it is called without a heading.</summary>
    public static string RenderGuideIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Blex reference guide").AppendLine();
        builder.AppendLine("The library's README, section by section. Call `GetBlexGuideSection(heading: \"...\")` for one of them; ")
               .AppendLine("a section's line count is there so a heading that runs to hundreds of lines is a deliberate choice rather than a surprise.")
               .AppendLine();

        foreach (var section in GuideSections)
        {
            builder.AppendLine($"{(section.Level == 2 ? "-" : "  -")} {section.Heading} ({section.Lines} lines)");
        }

        return builder.ToString();
    }

    /// <summary>The files GetBlexSourceFile can hand out, as the index it answers with when it is called without a path.</summary>
    public static string RenderSourceIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Blex source files").AppendLine();
        builder.AppendLine("Working code this server can hand out verbatim. Call `GetBlexSourceFile(path: \"...\")` for one.").AppendLine();

        foreach (var group in SourceFiles.GroupBy(file => file.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var file in group)
            {
                builder.Append($"- `{file.Path}` ({file.Lines} lines)");
                if (file.Description is not null) builder.Append($" - {file.Description}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Every level-2 and level-3 heading of the README, in reading order.</summary>
    public static BlexGuideSectionDto[] GuideSections => _guideSections.Value;

    /// <summary>Every embedded source file, keyed by the path the tools expose.</summary>
    public static BlexSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The README text under <paramref name="heading"/>, including its sub-sections. Matching is case-
    /// and punctuation-insensitive so "net maui" finds ".NET MAUI".
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

    private static BlexGuideSectionDto[] BuildGuideSections()
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

            if (TryReadHeading(lines[i], out var level, out var text) && level is 2 or 3)
            {
                headings.Add((i, level, text));
            }
        }

        var sections = new List<BlexGuideSectionDto>(headings.Count);
        string? parent = null;

        for (int i = 0; i < headings.Count; i++)
        {
            var (index, level, text) = headings[i];

            if (level == 2) parent = text;

            var end = lines.Length;
            for (int j = i + 1; j < headings.Count; j++)
            {
                if (headings[j].Level > level) continue;
                end = headings[j].Index;
                break;
            }

            sections.Add(new BlexGuideSectionDto
            {
                Heading = text,
                Level = level,
                Parent = level == 2 ? null : parent,
                Lines = end - index
            });
        }

        return [.. sections];
    }

    private static FrozenDictionary<string, string> BuildSourceFiles()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in _assembly.GetManifestResourceNames())
        {
            var normalized = Normalize(resource);

            if (normalized.StartsWith(SourcePrefix, StringComparison.OrdinalIgnoreCase) is false) continue;

            if (IsDocumentationPage(normalized)) continue;

            var content = ReadResource(resource);
            if (content is null) continue;

            files[normalized[SourcePrefix.Length..]] = content;
        }

        return files.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The documentation pages, which this catalog does not hand out: their Razor source is the page's
    /// prose with markup around it, and GetBlexDocsPage already serves the page itself - rendered, as the
    /// reader sees it. The page index is the single source of truth for which components are pages, so
    /// a page added to the site is excluded here without anyone remembering to.
    /// </summary>
    private static bool IsDocumentationPage(string resourceName)
    {
        var fileName = resourceName[(resourceName.LastIndexOf('/') + 1)..];

        return _documentationPageFiles.Value.Contains(fileName);
    }

    private static readonly Lazy<FrozenSet<string>> _documentationPageFiles = new(() =>
        DocsPageRenderer.Pages.Select(page => $"{page.PageType.Name}.razor").ToFrozenSet(StringComparer.OrdinalIgnoreCase));

    private static BlexSourceFileDto[] BuildSourceFileList()
    {
        return [.. _sourceFiles.Value
            .Select(file => new BlexSourceFileDto
            {
                Path = file.Key,
                Kind = file.Key.StartsWith("Demo/", StringComparison.OrdinalIgnoreCase) ? "Demo" : "Sample",
                Description = DescribeSource(file.Value),
                Lines = CountLines(file.Value)
            })
            .OrderBy(file => file.Kind, StringComparer.Ordinal)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)];
    }

    private static int CountLines(string content)
    {
        if (content.Length == 0) return 0;

        var newlines = content.Count(c => c == '\n');

        return content[^1] == '\n' ? newlines : newlines + 1;
    }

    /// <summary>A one-line description of a source file, taken from whatever the file itself already says.</summary>
    private static string? DescribeSource(string content)
    {
        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        var summary = XmlSummaryRegex().Match(content);
        if (summary.Success)
        {
            var text = summary.Groups["text"].Value.Replace("///", " ", StringComparison.Ordinal);

            return Summarize(XmlTagRegex().Replace(text, string.Empty));
        }

        var title = PageTitleRegex().Match(content);
        if (title.Success) return Summarize(title.Groups["text"].Value);

        var lineComment = LeadingLineCommentRegex().Match(content);
        if (lineComment.Success) return Summarize(lineComment.Value.Replace("//", " ", StringComparison.Ordinal));

        var comment = RazorCommentRegex().Match(content);

        return comment.Success ? Summarize(comment.Groups["text"].Value) : null;
    }

    private static string? Summarize(string text)
    {
        text = WhitespaceRegex().Replace(text, " ").Trim();
        if (text.Length == 0) return null;

        var stop = text.IndexOf(". ", StringComparison.Ordinal);
        if (stop > 0) text = text[..(stop + 1)];

        return text.Length <= 220 ? text : $"{text[..217]}...";
    }

    /// <summary>A '#' line inside a code fence is a shell comment or a preprocessor directive, not a heading.</summary>
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
