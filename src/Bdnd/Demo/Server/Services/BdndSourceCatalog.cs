using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Bdnd.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bdnd.Demo.Server.Services;

/// <summary>
/// Serves the hand-written material the MCP tools hand out: the library's README (its reference
/// guide), the library's components and script module as they ship, and every source file of this
/// demo - the pages are working drag-and-drop examples, and the hosting files are the wiring of a real
/// Blazor Web App.
/// <para>
/// All of them are embedded into this assembly by the .csproj rather than read from disk, so the
/// MCP server keeps working from a published, single-folder deployment where the repository is
/// nowhere to be found.
/// </para>
/// </summary>
public static partial class BdndSourceCatalog
{
    private const string ReadmeResource = "BdndDocs/README.md";
    private const string SourcePrefix = "BdndSource/";

    /// <summary>The path the library's script module is served under, next to the demo's files.</summary>
    public const string ScriptPath = "Library/Scripts/bit-bdnd.ts";

    /// <summary>Where the demo's pages live.</summary>
    public const string PagesFolder = "Demo/Client/Pages/";

    /// <summary>Separates a parent heading from a child one in the key of a heading the README uses twice.</summary>
    public const string KeySeparator = " > ";

    private sealed record Section(BdndGuideSectionDto Dto, int Start, int End);

    private static readonly Assembly _assembly = typeof(BdndSourceCatalog).Assembly;

    private static readonly Lazy<string> _readme = new(() => ReadResource(ReadmeResource) ?? string.Empty);
    private static readonly Lazy<string[]> _readmeLines = new(() => Readme.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    private static readonly Lazy<Section[]> _sections = new(BuildSections);
    private static readonly Lazy<BdndGuideSectionDto[]> _guideSections = new(() => [.. _sections.Value.Select(section => section.Dto)]);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BdndSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);

    /// <summary>The library's README, in full.</summary>
    public static string Readme => _readme.Value;

    /// <summary>
    /// The guide's sections, as the index GetBdndGuideSection answers with when it is called without
    /// a heading. Plain text rather than a list of objects: an index is read, not parsed, and the
    /// same list as JSON costs twice the characters to say the same thing.
    /// </summary>
    public static string RenderGuideIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bdnd reference guide").AppendLine();
        builder.AppendLine("The library's README, section by section. Call `GetBdndGuideSection(heading: \"...\")` for one of them; ")
               .AppendLine("a section's line count is there so a heading that runs to a hundred lines is a deliberate choice rather than a surprise.")
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
    /// The files GetBdndSourceFile can hand out, as the index it answers with when it is called
    /// without a path.
    /// </summary>
    public static string RenderSourceIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bdnd source files").AppendLine();
        builder.AppendLine("Working code this server can hand out verbatim. Call `GetBdndSourceFile(path: \"...\")` for one. ")
               .AppendLine("A demo page says where the running demo serves it.")
               .AppendLine();

        foreach (var group in SourceFiles.GroupBy(file => file.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var file in group)
            {
                builder.Append($"- `{file.Path}` ({file.Lines} lines");
                if (file.Route is not null) builder.Append($", `{file.Route}`");
                builder.Append(')');
                if (file.Title is not null) builder.Append($" - **{file.Title}**");
                if (file.Description is not null) builder.Append($" - {file.Description}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Every level 2, 3 and 4 heading of the README, in reading order.</summary>
    public static BdndGuideSectionDto[] GuideSections => _guideSections.Value;

    /// <summary>Every embedded source file, keyed by the path the tools expose.</summary>
    public static BdndSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The README text under a heading, including its sub-sections. Matching is case- and
    /// punctuation-insensitive so "drop result" finds "Drop results" and "coming from react dnd"
    /// finds "Coming from React DnD"; a heading the README uses more than once is told apart by its
    /// key, "Parent > Heading", and otherwise answers with the first section of that name.
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

        // The singular as well: "drop result" is the section called "Drop results", and nobody asks in
        // the number the heading happens to use.
        return sections.FirstOrDefault(section => NormalizeHeading(section.Dto.Heading) == normalized)
            ?? sections.FirstOrDefault(section => NormalizeHeading(section.Dto.Key) == normalized)
            ?? sections.FirstOrDefault(section => Singular(NormalizeHeading(section.Dto.Heading)) == Singular(normalized));
    }

    private static string Singular(string normalized) => normalized.EndsWith('s') ? normalized[..^1] : normalized;

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

            sections.Add(new Section(new BdndGuideSectionDto
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

    private static BdndSourceFileDto[] BuildSourceFileList()
    {
        return [.. _sourceFiles.Value
            .Select(file => new BdndSourceFileDto
            {
                Path = file.Key,
                Kind = KindOf(file.Key, file.Value),
                Title = TitleOf(file.Value),
                Description = DescribeSource(file.Key, file.Value),
                Route = RouteOf(file.Value),
                Lines = CountLines(file.Value)
            })
            .OrderBy(file => KindOrder(file.Kind))
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// What a file is for: a page is one of the demo's working examples, the hosting files are the
    /// wiring every app needs once, a library file is shipped by the package itself, and the rest -
    /// the sortable card several pages render, the item types, the code viewer - is what the pages share.
    /// </summary>
    private static string KindOf(string path, string content)
    {
        if (path.StartsWith("Library/", StringComparison.OrdinalIgnoreCase)) return "Library";
        if (path.StartsWith(PagesFolder, StringComparison.OrdinalIgnoreCase) && RouteOf(content) is not null) return "Page";
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
        "Shared" => 1,
        "Hosting" => 2,
        "Library" => 3,
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
    /// The heading a demo page shows its reader - "Sortable - Simple", "Nesting - Drop Targets" - which
    /// is what a person calls the example, and so what the search index files it under.
    /// </summary>
    private static string? TitleOf(string content)
    {
        var match = DemoTitleRegex().Match(content);

        return match.Success ? Decode(XmlTagRegex().Replace(match.Groups["text"].Value, string.Empty)).Trim() : null;
    }

    /// <summary>
    /// A one-line description of a source file, taken from whatever the file itself already says:
    /// its leading razor/C#/TypeScript comment, the type summary it opens with, or - for a demo page -
    /// the introduction it shows the person reading it.
    /// </summary>
    private static string? DescribeSource(string path, string content)
    {
        // A library component's markup says nothing about itself - its summary is on the partial class
        // next to it, which is where IntelliSense reads it from too.
        if (path.StartsWith("Library/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) &&
            _sourceFiles.Value.TryGetValue($"{path}.cs", out var codeBehind))
        {
            content = codeBehind;
            path = $"{path}.cs";
        }

        // A byte order mark is not text, and it would stop every pattern anchored at the start of the file.
        content = content.TrimStart('\uFEFF');

        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        // A demo page opens by saying what it demonstrates, to the person reading it, in the paragraph
        // under its heading. That is the best description of it there is, far better than the first
        // comment somewhere in its code.
        var intro = DemoHeaderRegex().Match(content);
        if (intro.Success) return Summarize(Decode(XmlTagRegex().Replace(intro.Groups["text"].Value, string.Empty)));

        var blockComment = LeadingBlockCommentRegex().Match(content);
        if (blockComment.Success) return Summarize(blockComment.Groups["text"].Value);

        // A comment the file opens with describes the file; an XML summary further down describes
        // one type inside it (Program.cs ends with one about the partial Program class).
        var lineComment = LeadingLineCommentRegex().Match(content);
        if (lineComment.Success) return Summarize(lineComment.Value.Replace("//", " ", StringComparison.Ordinal));

        // In a .razor file the first XML summary belongs to a parameter in its @code block, which says
        // nothing about the component as a whole.
        var xmlSummary = path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ? Match.Empty : XmlSummaryRegex().Match(content);
        if (xmlSummary.Success)
        {
            // An XML summary is markup: a <see cref="..."/> is the name it points at, the other tags are
            // noise in a listing, and the text is entity-encoded ("&lt;DndProvider&gt;").
            var text = xmlSummary.Groups["text"].Value.Replace("///", " ", StringComparison.Ordinal);

            text = SeeCrefRegex().Replace(text, see => see.Groups["name"].Value);

            return Summarize(Decode(XmlTagRegex().Replace(text, string.Empty)));
        }

        var title = PageTitleRegex().Match(content);
        if (title.Success) return Summarize(Decode(title.Groups["text"].Value));

        // Nothing at the top of the file said what it is - the first commentary in it will do.
        var comment = RazorCommentRegex().Match(content);
        if (comment.Success) return Summarize(comment.Groups["text"].Value);

        // A component that says nothing about itself is still described by what it is made of - the
        // sortable card is a draggable and a drop target in one, and that is what a search should find.
        var used = BdndComponentRegex().Matches(content).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray();

        return used.Length > 0 ? $"Renders {string.Join(", ", used)}." : null;
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

        // A heading is read the way Markdown renders it: "BdndDraggable&lt;TItem&gt;" is the heading
        // "BdndDraggable<TItem>", and that is what a caller asks for and what a search hit shows.
        text = System.Net.WebUtility.HtmlDecode(line[(level + 1)..].Trim());

        return text.Length > 0;
    }

    /// <summary>Reduces a heading to its comparable core, so "drop targets nesting" finds nothing it should not and "the provider" finds "### The provider".</summary>
    private static string NormalizeHeading(string heading)
    {
        var builder = new StringBuilder(heading.Length);

        // The little words carry nothing a heading is told apart by, and they are the ones a caller
        // most often drops ("coming react dnd") or adds ("the custom drag layers").
        foreach (var word in heading.Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries))
        {
            if (_fillerWords.Contains(word)) continue;

            foreach (var c in word) builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static readonly HashSet<string> _fillerWords = new(StringComparer.OrdinalIgnoreCase) { "a", "an", "and", "the", "of", "from" };

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

    // The last segment of the cref, without its generic parameters: "BdndDraggable{TItem}" reads "BdndDraggable".
    [GeneratedRegex(@"<see(?:also)?\s+(?:cref|langword)=""(?:[^""]*\.)?(?<name>[^"".{]+)(?:\{[^""]*\})?""\s*/>")]
    private static partial Regex SeeCrefRegex();

    [GeneratedRegex(@"<(?<name>Bdnd[A-Z][A-Za-z]*)\b")]
    private static partial Regex BdndComponentRegex();

    [GeneratedRegex(@"<div class=""demo-header"">\s*<h3>.*?</h3>\s*<p[^>]*>(?<text>.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex DemoHeaderRegex();

    [GeneratedRegex(@"<div class=""demo-header"">\s*<h3>(?<text>.*?)</h3>", RegexOptions.Singleline)]
    private static partial Regex DemoTitleRegex();

    [GeneratedRegex(@"<PageTitle>(?<text>.*?)</PageTitle>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PageTitleRegex();

    [GeneratedRegex(@"^\s*@page\s+""(?<route>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex PageDirectiveRegex();

    [GeneratedRegex(@"^\s*(//[^\n]*\n)+")]
    private static partial Regex LeadingLineCommentRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
