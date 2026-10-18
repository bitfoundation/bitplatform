using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Blow.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Blow.Demo.Server.Services;

/// <summary>
/// Serves the hand-written material the MCP tools hand out: the library's README (its reference
/// guide), its shipped stylesheet, and every source file of this demo - the pages and the examples
/// gallery are working flows, and the hosting files are the wiring of a real Blazor Web App.
/// <para>
/// All of them are embedded into this assembly by the .csproj rather than read from disk, so the
/// MCP server keeps working from a published, single-folder deployment where the repository is
/// nowhere to be found.
/// </para>
/// </summary>
public static partial class BlowSourceCatalog
{
    private const string ReadmeResource = "BlowDocs/README.md";
    private const string SourcePrefix = "BlowSource/";

    /// <summary>The path the shipped stylesheet is served under, next to the demo's files.</summary>
    public const string StylesheetPath = "Library/Styles/bit-blow.css";

    /// <summary>Where the pages of the examples gallery live.</summary>
    public const string ExamplesFolder = "Demo/Client/Pages/Examples/";

    /// <summary>Separates a parent heading from a child one in the key of a heading the README uses twice.</summary>
    public const string KeySeparator = " > ";

    private sealed record Section(BlowGuideSectionDto Dto, int Start, int End);

    private static readonly Assembly _assembly = typeof(BlowSourceCatalog).Assembly;

    private static readonly Lazy<string> _readme = new(() => ReadResource(ReadmeResource) ?? string.Empty);
    private static readonly Lazy<string[]> _readmeLines = new(() => Readme.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    private static readonly Lazy<Section[]> _sections = new(BuildSections);
    private static readonly Lazy<BlowGuideSectionDto[]> _guideSections = new(() => [.. _sections.Value.Select(section => section.Dto)]);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BlowSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);
    private static readonly Lazy<FrozenDictionary<string, BlowSourceFileDto>> _filesByRoute = new(BuildRouteIndex);

    /// <summary>The library's README, in full.</summary>
    public static string Readme => _readme.Value;

    /// <summary>The library's stylesheet as it is written (not minified), comments and all.</summary>
    public static string Stylesheet => GetSourceFile(StylesheetPath) ?? string.Empty;

    /// <summary>
    /// The guide's sections, as the index GetBlowGuideSection answers with when it is called without
    /// a heading. Plain text rather than a list of objects: an index is read, not parsed, and the
    /// same list as JSON costs twice the characters to say the same thing.
    /// </summary>
    public static string RenderGuideIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Blow reference guide").AppendLine();
        builder.AppendLine("The library's README, section by section. Call `GetBlowGuideSection(heading: \"...\")` for one of them; ")
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
    /// The files GetBlowSourceFile can hand out, as the index it answers with when it is called
    /// without a path.
    /// </summary>
    public static string RenderSourceIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Blow source files").AppendLine();
        builder.AppendLine("Working code this server can hand out verbatim. Call `GetBlowSourceFile(path: \"...\")` for one. ")
               .AppendLine("A routable page says where the running demo serves it.")
               .AppendLine();

        foreach (var group in SourceFiles.GroupBy(file => file.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var file in group)
            {
                builder.Append($"- `{file.Path}` ({file.Lines} lines");
                if (file.Route is not null) builder.Append($", `{file.Route}`");
                builder.Append(')');
                if (file.Description is not null) builder.Append($" - {file.Description}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Every level 2, 3 and 4 heading of the README, in reading order.</summary>
    public static BlowGuideSectionDto[] GuideSections => _guideSections.Value;

    /// <summary>Every embedded source file, keyed by the path the tools expose.</summary>
    public static BlowSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The page served at a route, e.g. "/examples/save-restore", or null when no embedded page
    /// declares it. The examples gallery names its entries by route, and this is how one of them is
    /// turned into the file that implements it.
    /// </summary>
    public static BlowSourceFileDto? FindByRoute(string route)
    {
        return _filesByRoute.Value.GetValueOrDefault($"/{route.Trim().Trim('/')}");
    }

    /// <summary>
    /// The README text under a heading, including its sub-sections. Matching is case- and
    /// punctuation-insensitive so "undo redo" finds "Undo and redo" and "saving undo clipboard"
    /// finds "Saving, undo and clipboard"; a heading the README uses more than once is told apart
    /// by its key, "Parent > Heading", and otherwise answers with the first section of that name.
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

            sections.Add(new Section(new BlowGuideSectionDto
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

    private static BlowSourceFileDto[] BuildSourceFileList()
    {
        return [.. _sourceFiles.Value
            .Select(file => new BlowSourceFileDto
            {
                Path = file.Key,
                Kind = KindOf(file.Key),
                Description = DescribeSource(file.Value),
                Route = RouteOf(file.Value),
                Lines = CountLines(file.Value)
            })
            .OrderBy(file => KindOrder(file.Kind))
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Routes are case-insensitive, as the router matches them: /Examples/Basic is the page that
    /// declares /examples/basic.
    /// </summary>
    private static FrozenDictionary<string, BlowSourceFileDto> BuildRouteIndex()
    {
        return SourceFiles.Where(file => file.Route is not null)
                          .GroupBy(file => file.Route!, StringComparer.OrdinalIgnoreCase)
                          .ToFrozenDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// What a file is for: an example is one entry of the gallery, a page is one of the hand-written
    /// tours of a feature, the hosting files are the wiring every app needs once, a library file is
    /// shipped by the package itself, and the rest is what the pages share.
    /// </summary>
    private static string KindOf(string path)
    {
        if (path.StartsWith(ExamplesFolder, StringComparison.OrdinalIgnoreCase)) return "Example";
        if (path.StartsWith("Demo/Client/Pages/", StringComparison.OrdinalIgnoreCase)) return "Page";
        if (path.StartsWith("Library/", StringComparison.OrdinalIgnoreCase)) return "Library";
        if (path.StartsWith("Demo/Server/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("Demo/Client/Extensions/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("Demo/Client/Layout/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("Demo/Client/Program.cs", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("Demo/Client/Routes.razor", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("Demo/Client/_Imports.razor", StringComparison.OrdinalIgnoreCase)) return "Hosting";

        return "Shared";
    }

    private static int KindOrder(string kind) => kind switch
    {
        "Page" => 0,
        "Example" => 1,
        "Shared" => 2,
        "Hosting" => 3,
        _ => 4
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

    private static string? RouteOf(string content)
    {
        var match = PageDirectiveRegex().Match(content);

        return match.Success ? match.Groups["route"].Value : null;
    }

    /// <summary>
    /// A one-line description of a source file, taken from whatever the file itself already says:
    /// its leading razor/C#/CSS comment, or - for a page - the summary it shows the person reading it.
    /// </summary>
    private static string? DescribeSource(string content)
    {
        // A byte order mark is not text, and it would stop every pattern anchored at the start of the file.
        content = content.TrimStart('﻿');

        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        // A demo page opens by saying what it demonstrates, to the person reading it - an example in
        // its scaffold's Summary, a hand-written page in the paragraph under its heading. That is the
        // best description of it there is, far better than the first comment somewhere in its code.
        var summary = ScaffoldSummaryRegex().Match(content);
        if (summary.Success) return Summarize(Decode(summary.Groups["text"].Value));

        var intro = DemoHeaderRegex().Match(content);
        if (intro.Success) return Summarize(Decode(XmlTagRegex().Replace(intro.Groups["text"].Value, string.Empty)));

        var blockComment = LeadingBlockCommentRegex().Match(content);
        if (blockComment.Success) return Summarize(blockComment.Groups["text"].Value);

        // A comment the file opens with describes the file; an XML summary further down describes
        // one type inside it (Program.cs ends with one about the partial Program class).
        var lineComment = LeadingLineCommentRegex().Match(content);
        if (lineComment.Success) return Summarize(lineComment.Value.Replace("//", " ", StringComparison.Ordinal));

        var xmlSummary = XmlSummaryRegex().Match(content);
        if (xmlSummary.Success)
        {
            // An XML summary is markup: <see cref="..."/> and friends are noise in a listing.
            var text = xmlSummary.Groups["text"].Value.Replace("///", " ", StringComparison.Ordinal);

            return Summarize(XmlTagRegex().Replace(text, string.Empty));
        }

        var title = PageTitleRegex().Match(content);
        if (title.Success) return Summarize(title.Groups["text"].Value);

        // Nothing at the top of the file said what it is - the first commentary in it will do.
        var comment = RazorCommentRegex().Match(content);

        return comment.Success ? Summarize(comment.Groups["text"].Value) : null;
    }

    /// <summary>The few entities the demo's markup writes, so a description reads as text rather than as HTML.</summary>
    private static string Decode(string text) => System.Net.WebUtility.HtmlDecode(text);

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

    /// <summary>Reduces a heading to its comparable core, so "undo redo" finds "### Undo and redo".</summary>
    private static string NormalizeHeading(string heading)
    {
        var builder = new StringBuilder(heading.Length);

        // The little words carry nothing a heading is told apart by, and they are the ones a caller
        // most often drops ("undo redo") or adds ("the custom nodes").
        foreach (var word in heading.Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries))
        {
            if (_fillerWords.Contains(word)) continue;

            foreach (var c in word) builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static readonly HashSet<string> _fillerWords = new(StringComparer.OrdinalIgnoreCase) { "a", "an", "and", "the", "of" };

    private static string Normalize(string path) => path.Replace('\\', '/').Trim('/');

    // The directives a component opens with come before the comment that describes it - but not
    // @page, since a page describes itself to its reader below its heading, which is the better line.
    [GeneratedRegex(@"^(?:\s*@(?:namespace|using|inject|implements|inherits|layout|attribute|typeparam)\b[^\n]*\n)*\s*@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex LeadingRazorCommentRegex();

    [GeneratedRegex(@"^\s*/\*(?<text>.*?)\*/", RegexOptions.Singleline)]
    private static partial Regex LeadingBlockCommentRegex();

    [GeneratedRegex(@"@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex RazorCommentRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex XmlTagRegex();

    [GeneratedRegex(@"///\s*<summary>(?<text>.*?)</summary>", RegexOptions.Singleline)]
    private static partial Regex XmlSummaryRegex();

    [GeneratedRegex(@"<ExampleScaffold\b[^>]*?\bSummary=""(?<text>[^""]*)""", RegexOptions.Singleline)]
    private static partial Regex ScaffoldSummaryRegex();

    [GeneratedRegex(@"<div class=""demo-header"">\s*<h3>.*?</h3>\s*<p[^>]*>(?<text>.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex DemoHeaderRegex();

    [GeneratedRegex(@"<PageTitle>(?<text>.*?)</PageTitle>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PageTitleRegex();

    [GeneratedRegex(@"^\s*@page\s+""(?<route>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex PageDirectiveRegex();

    [GeneratedRegex(@"^\s*(//[^\n]*\n)+")]
    private static partial Regex LeadingLineCommentRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
