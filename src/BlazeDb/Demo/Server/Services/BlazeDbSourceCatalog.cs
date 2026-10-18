using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.BlazeDb.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.BlazeDb.Demo.Server.Services;

/// <summary>
/// Serves the hand-written material the MCP tools hand out: the library's README (its reference
/// guide) and the source files of this demo - the pages each drive a real database, the data files
/// declare its tables, and the hosting files are the wiring of a real Blazor Web App.
/// <para>
/// All of them are embedded into this assembly by the .csproj rather than read from disk, so the
/// MCP server keeps working from a published, single-folder deployment where the repository is
/// nowhere to be found.
/// </para>
/// </summary>
public static partial class BlazeDbSourceCatalog
{
    private const string ReadmeResource = "BlazeDbDocs/README.md";
    private const string SourcePrefix = "BlazeDbSource/";

    /// <summary>Separates a parent heading from a child one in the key of a heading the README uses twice.</summary>
    public const string KeySeparator = " > ";

    private sealed record Section(BlazeDbGuideSectionDto Dto, int Start, int End);

    private static readonly Assembly _assembly = typeof(BlazeDbSourceCatalog).Assembly;

    private static readonly Lazy<string> _readme = new(() => ReadResource(ReadmeResource) ?? string.Empty);
    private static readonly Lazy<string[]> _readmeLines = new(() => Readme.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    private static readonly Lazy<Section[]> _sections = new(BuildSections);
    private static readonly Lazy<BlazeDbGuideSectionDto[]> _guideSections = new(() => [.. _sections.Value.Select(section => section.Dto)]);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BlazeDbSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);

    /// <summary>The library's README, in full.</summary>
    public static string Readme => _readme.Value;

    /// <summary>
    /// The guide's sections, as the index GetBlazeDbGuideSection answers with when it is called without
    /// a heading. Plain text rather than a list of objects: an index is read, not parsed, and the
    /// same list as JSON costs twice the characters to say the same thing.
    /// </summary>
    public static string RenderGuideIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.BlazeDb reference guide").AppendLine();
        builder.AppendLine("The library's README, section by section. Call `GetBlazeDbGuideSection(heading: \"...\")` for one of them; ")
               .AppendLine("a section's line count is there so a heading that runs to hundreds of lines is a deliberate choice rather than a surprise.")
               .AppendLine();

        foreach (var section in GuideSections)
        {
            var indent = new string(' ', (section.Level - 2) * 2);
            var key = section.Key == section.Heading ? null : $" - key `{section.Key}`";

            builder.AppendLine($"{indent}- {section.Heading} ({section.Lines} lines){key}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// The files GetBlazeDbSourceFile can hand out, as the index it answers with when it is called
    /// without a path.
    /// </summary>
    public static string RenderSourceIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.BlazeDb source files").AppendLine();
        builder.AppendLine("Working code this server can hand out verbatim. Call `GetBlazeDbSourceFile(path: \"...\")` for one.").AppendLine();

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

    /// <summary>Every level 2, 3 and 4 heading of the README, in reading order.</summary>
    public static BlazeDbGuideSectionDto[] GuideSections => _guideSections.Value;

    /// <summary>Every embedded source file, keyed by the path the tools expose.</summary>
    public static BlazeDbSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The README text under a heading, including its sub-sections. Matching is case- and
    /// punctuation-insensitive so "encryption rest" finds "Encryption at rest"; a heading the README
    /// uses more than once is told apart by its key, "Parent > Heading", and otherwise answers with
    /// the first section of that name.
    /// </summary>
    public static string? GetGuideSection(string heading)
    {
        var section = FindSection(heading);
        if (section is null) return null;

        return string.Join('\n', _readmeLines.Value[section.Start..section.End]).TrimEnd();
    }

    /// <summary>The content of an embedded source file, or null when the path is unknown.</summary>
    public static string? GetSourceFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        return _sourceFiles.Value.GetValueOrDefault(Normalize(path));
    }

    private static Section? FindSection(string? heading)
    {
        if (string.IsNullOrWhiteSpace(heading)) return null;

        var sections = _sections.Value;

        var separator = heading.LastIndexOf('>');
        if (separator > 0)
        {
            var parent = NormalizeHeading(heading[..separator]);
            var child = NormalizeHeading(heading[(separator + 1)..]);

            var qualified = sections.FirstOrDefault(section => section.Dto.Parent is not null
                                                               && NormalizeHeading(section.Dto.Parent) == parent
                                                               && NormalizeHeading(section.Dto.Heading) == child);
            if (qualified is not null) return qualified;
        }

        var normalized = NormalizeHeading(heading);

        return sections.FirstOrDefault(section => NormalizeHeading(section.Dto.Heading) == normalized)
            ?? sections.FirstOrDefault(section => NormalizeHeading(section.Dto.Key) == normalized);
    }

    private static string? ReadResource(string name)
    {
        using var stream = _assembly.GetManifestResourceStream(name);
        if (stream is null) return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    private static Section[] BuildSections()
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

            if (TryReadHeading(lines[i], out var level, out var text) && level is >= 2 and <= 4)
            {
                headings.Add((i, level, text));
            }
        }

        var duplicated = headings.GroupBy(heading => NormalizeHeading(heading.Text))
                                 .Where(group => group.Count() > 1)
                                 .Select(group => group.Key)
                                 .ToHashSet(StringComparer.Ordinal);

        var sections = new List<Section>(headings.Count);
        var parents = new string?[5];

        for (int i = 0; i < headings.Count; i++)
        {
            var (index, level, text) = headings[i];

            parents[level] = text;
            for (int deeper = level + 1; deeper < parents.Length; deeper++) parents[deeper] = null;

            string? parent = null;
            for (int upper = level - 1; upper >= 2 && parent is null; upper--) parent = parents[upper];

            // The section runs until the next heading of the same or a higher rank.
            var end = lines.Length;
            for (int j = i + 1; j < headings.Count; j++)
            {
                if (headings[j].Level > level) continue;
                end = headings[j].Index;
                break;
            }

            sections.Add(new Section(new BlazeDbGuideSectionDto
            {
                Heading = text,
                Key = parent is not null && duplicated.Contains(NormalizeHeading(text)) ? $"{parent}{KeySeparator}{text}" : text,
                Level = level,
                Parent = parent,
                Lines = end - index
            }, index, end));
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

    private static BlazeDbSourceFileDto[] BuildSourceFileList()
    {
        return [.. _sourceFiles.Value
            .Select(file => new BlazeDbSourceFileDto
            {
                Path = file.Key,
                Kind = KindOf(file.Key),
                Description = DescribeSource(file.Value),
                Lines = CountLines(file.Value)
            })
            .OrderBy(file => KindOrder(file.Kind))
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// What a file is for: a demo page drives a real database and is the worked example to copy,
    /// a data file declares the demo's tables and the services around them, and the hosting files are
    /// the wiring every app needs once.
    /// </summary>
    private static string KindOf(string path)
    {
        if (path.StartsWith("Demo/Client/Pages/", StringComparison.OrdinalIgnoreCase)) return "Page";
        if (path.StartsWith("Demo/Client/Data/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("Demo/Client/DiagnosticsCatalog.cs", StringComparison.OrdinalIgnoreCase)) return "Data";

        return "Hosting";
    }

    private static int KindOrder(string kind) => kind switch
    {
        "Page" => 0,
        "Data" => 1,
        "Hosting" => 2,
        _ => 3
    };

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
    /// its leading razor/C# comment, or - for a page - the lead of its header.
    /// </summary>
    private static string? DescribeSource(string content)
    {
        // A byte order mark is not text, and it would stop every pattern anchored at the start of the file.
        content = content.TrimStart('﻿');

        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        // A demo page opens by saying what it demonstrates, to the person reading it - which is the
        // best description of it there is, far better than the first comment somewhere in its code.
        var intro = PageLeadRegex().Match(content);
        if (intro.Success) return Summarize(System.Net.WebUtility.HtmlDecode(intro.Groups["text"].Value));

        var blockComment = LeadingBlockCommentRegex().Match(content);
        if (blockComment.Success) return Summarize(blockComment.Groups["text"].Value);

        // A comment the file opens with describes the file; an XML summary further down describes
        // one type inside it (Program.cs ends with one about the partial Program class).
        var lineComment = LeadingLineCommentRegex().Match(content);
        if (lineComment.Success) return Summarize(lineComment.Value.Replace("//", " ", StringComparison.Ordinal));

        var summary = XmlSummaryRegex().Match(content);
        if (summary.Success)
        {
            // An XML summary is markup: a <see cref="..."/> reads as the name it points at, and the rest
            // of the tags are noise in a listing.
            var text = summary.Groups["text"].Value.Replace("///", " ", StringComparison.Ordinal);
            text = CrefRegex().Replace(text, match => match.Groups["name"].Value);

            return Summarize(XmlTagRegex().Replace(text, string.Empty));
        }

        var title = PageTitleRegex().Match(content);
        if (title.Success) return Summarize(title.Groups["text"].Value);

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

    /// <summary>Reduces a heading to its comparable core, so "encryption rest" finds "## Encryption at rest".</summary>
    private static string NormalizeHeading(string heading)
    {
        var builder = new StringBuilder(heading.Length);

        // The little words carry nothing a heading is told apart by, and they are the ones a caller
        // most often drops ("encryption rest") or adds ("the threading model").
        foreach (var word in heading.Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries))
        {
            if (_fillerWords.Contains(word)) continue;

            foreach (var c in word) builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static readonly HashSet<string> _fillerWords = new(StringComparer.OrdinalIgnoreCase) { "a", "an", "and", "the", "of", "at" };

    private static string Normalize(string path) => path.Replace('\\', '/').Trim('/');

    [GeneratedRegex(@"^\s*@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex LeadingRazorCommentRegex();

    [GeneratedRegex(@"^\s*/\*(?<text>.*?)\*/", RegexOptions.Singleline)]
    private static partial Regex LeadingBlockCommentRegex();

    [GeneratedRegex(@"@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex RazorCommentRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex XmlTagRegex();

    [GeneratedRegex(@"<see(?:also)?\s+(?:cref|langword)=""(?:\w:)?(?:[\w.]+\.)?(?<name>[\w`]+)[^""]*""\s*/>")]
    private static partial Regex CrefRegex();

    [GeneratedRegex(@"///\s*<summary>(?<text>.*?)</summary>", RegexOptions.Singleline)]
    private static partial Regex XmlSummaryRegex();

    [GeneratedRegex(@"<PageHeader\b[^>]*?\bLead=""(?<text>[^""]*)""", RegexOptions.Singleline)]
    private static partial Regex PageLeadRegex();

    [GeneratedRegex(@"<PageTitle>(?<text>.*?)</PageTitle>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PageTitleRegex();

    [GeneratedRegex(@"^\s*(//[^\n]*\n)+")]
    private static partial Regex LeadingLineCommentRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
