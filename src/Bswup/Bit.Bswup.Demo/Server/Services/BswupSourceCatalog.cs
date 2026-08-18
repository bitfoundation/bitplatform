using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Bswup.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>
/// Serves the hand-written source the MCP tools hand out: the library's README (its reference
/// guide), the library's own TypeScript sources, every source file of this demo, and the minimal
/// samples.
/// <para>
/// All of them are embedded into this assembly by the .csproj rather than read from disk, so the
/// MCP server keeps working from a published, single-folder deployment where the repository is
/// nowhere to be found. The library sources matter more here than they would for a C# package:
/// Bswup ships as JavaScript, so its TypeScript is where the shipped behavior - the defaults, the
/// message names, the asset filters - is actually written down, and BswupScriptCatalog reads the
/// answers straight out of it.
/// </para>
/// </summary>
public static partial class BswupSourceCatalog
{
    private const string ReadmeResource = "BswupDocs/README.md";
    private const string SourcePrefix = "BswupSource/";

    private static readonly Assembly _assembly = typeof(BswupSourceCatalog).Assembly;

    private static readonly Lazy<string> _readme = new(() => ReadResource(ReadmeResource) ?? string.Empty);
    private static readonly Lazy<string[]> _readmeLines = new(() => Readme.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    private static readonly Lazy<BswupGuideSectionDto[]> _guideSections = new(BuildGuideSections);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BswupSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);

    /// <summary>The library's README, in full.</summary>
    public static string Readme => _readme.Value;

    /// <summary>Every heading of the README, in reading order.</summary>
    public static BswupGuideSectionDto[] GuideSections => _guideSections.Value;

    /// <summary>Every embedded source file, keyed by the path the tools expose.</summary>
    public static BswupSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The README text under <paramref name="heading"/>, including its sub-sections. Matching is
    /// case- and punctuation-insensitive so "javascript api" finds "JavaScript API".
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

    private static BswupGuideSectionDto[] BuildGuideSections()
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

        var sections = new List<BswupGuideSectionDto>(headings.Count);
        string? parent = null;

        for (int i = 0; i < headings.Count; i++)
        {
            var (index, level, text) = headings[i];

            if (level == 2) parent = text;

            // The section runs until the next heading of the same or a higher rank.
            var end = lines.Length;
            for (int j = i + 1; j < headings.Count; j++)
            {
                if (headings[j].Level > level) continue;
                end = headings[j].Index;
                break;
            }

            sections.Add(new BswupGuideSectionDto
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

            var content = ReadResource(resource);
            if (content is null) continue;

            files[normalized[SourcePrefix.Length..]] = content;
        }

        return files.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static BswupSourceFileDto[] BuildSourceFileList()
    {
        return [.. _sourceFiles.Value
            .Select(file => new BswupSourceFileDto
            {
                Path = file.Key,
                Kind = KindOf(file.Key),
                Description = DescribeSource(file.Value),
                Lines = CountLines(file.Value)
            })
            .OrderBy(file => file.Kind, StringComparer.Ordinal)
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)];
    }

    private static string KindOf(string path)
    {
        if (path.StartsWith("Library/", StringComparison.OrdinalIgnoreCase)) return "Library";
        if (path.StartsWith("Demo/", StringComparison.OrdinalIgnoreCase)) return "Demo";

        return "Sample";
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
    /// its leading razor/C#/JS comment, or - for a docs page - its &lt;PageTitle&gt;.
    /// </summary>
    private static string? DescribeSource(string content)
    {
        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        var summary = XmlSummaryRegex().Match(content);
        if (summary.Success)
        {
            // An XML summary is markup, and its tags fall into two kinds. <paramref name="Slug"/>
            // and <see cref="BswupProgress"/> ARE the word the sentence is built around, so deleting
            // them leaves "One documentation page: is its route" - they are replaced by what they
            // name. Everything else (<c>, <para>) only wraps text that is kept anyway.
            var text = summary.Groups["text"].Value.Replace("///", " ", StringComparison.Ordinal);

            text = XmlReferenceRegex().Replace(text, match => LastIdentifier(match.Groups["name"].Value));

            return Summarize(XmlTagRegex().Replace(text, string.Empty));
        }

        var title = PageTitleRegex().Match(content);
        if (title.Success) return Summarize(title.Groups["text"].Value);

        var lineComment = LeadingLineCommentRegex().Match(content);
        if (lineComment.Success) return Summarize(lineComment.Value.Replace("//", " ", StringComparison.Ordinal));

        // Nothing at the top of the file said what it is - the first commentary in it will do
        // (the service-worker files, for one, explain themselves right above their settings).
        var comment = RazorCommentRegex().Match(content);

        return comment.Success ? Summarize(comment.Groups["text"].Value) : null;
    }

    /// <summary>
    /// The word a doc-comment reference reads as. A cref is written as "T:Some.Namespace.Type" or
    /// "M:Type.Method(System.String)"; a paramref name is already just the identifier.
    /// </summary>
    private static string LastIdentifier(string name)
    {
        var colon = name.IndexOf(':', StringComparison.Ordinal);
        if (colon >= 0) name = name[(colon + 1)..];

        var parenthesis = name.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis >= 0) name = name[..parenthesis];

        var dot = name.LastIndexOf('.');

        return dot >= 0 && dot < name.Length - 1 ? name[(dot + 1)..] : name;
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

    /// <summary>Reduces a heading to its comparable core, so "JavaScript API" finds "## JavaScript API".</summary>
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

    /// <summary>An XML doc tag whose meaning is the identifier in its attribute, not its body.</summary>
    [GeneratedRegex(@"<(?:paramref|typeparamref|see|seealso)\s+(?:name|cref)\s*=\s*""(?<name>[^""]*)""\s*/?>")]
    private static partial Regex XmlReferenceRegex();

    [GeneratedRegex(@"///\s*<summary>(?<text>.*?)</summary>", RegexOptions.Singleline)]
    private static partial Regex XmlSummaryRegex();

    [GeneratedRegex(@"<PageTitle>(?<text>.*?)</PageTitle>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PageTitleRegex();

    [GeneratedRegex(@"^\s*(//[^\n]*\n)+")]
    private static partial Regex LeadingLineCommentRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
