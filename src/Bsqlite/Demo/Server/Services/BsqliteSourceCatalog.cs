using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Bsqlite.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Serves the hand-written material the MCP tools hand out: the library's README, the guides under
/// docs/, every package's own README, and every source file of this demo - the pages run real queries
/// against a database in the browser, the model is the one both sides share, and the hosting files
/// are the wiring of a real Blazor Web App.
/// <para>
/// All of them are embedded into this assembly by the .csproj rather than read from disk, so the MCP
/// server keeps working from a published, single-folder deployment where the repository is nowhere to
/// be found.
/// </para>
/// </summary>
public static partial class BsqliteSourceCatalog
{
    private const string DocsPrefix = "BsqliteDocs/";
    private const string SourcePrefix = "BsqliteSource/";
    private const string GuidePrefix = "docs/";
    private const string PackagePrefix = "packages/";

    /// <summary>The name the repository README goes by.</summary>
    public const string ReadmeName = "readme";

    /// <summary>Separates a parent heading from a child one in the key of a heading a document uses twice.</summary>
    public const string KeySeparator = " > ";

    private sealed record Section(BsqliteDocumentSectionDto Dto, int Start, int End);

    private sealed record Document(BsqliteDocumentDto Dto, string Text, string[] Lines, Section[] Sections);

    private static readonly Assembly _assembly = typeof(BsqliteSourceCatalog).Assembly;

    private static readonly Lazy<Document[]> _documents = new(BuildDocuments);
    private static readonly Lazy<BsqliteDocumentDto[]> _documentList = new(() => [.. _documents.Value.Select(document => document.Dto)]);
    private static readonly Lazy<BsqliteDocumentSectionDto[]> _sections = new(() => [.. _documents.Value.SelectMany(document => document.Sections.Select(section => section.Dto))]);
    private static readonly Lazy<FrozenDictionary<string, string>> _sourceFiles = new(BuildSourceFiles);
    private static readonly Lazy<BsqliteSourceFileDto[]> _sourceFileList = new(BuildSourceFileList);

    /// <summary>The repository README, in full.</summary>
    public static string Readme => GetDocument(ReadmeName) ?? string.Empty;

    /// <summary>Every document, in reading order: the README, the guides as it links them, then the packages.</summary>
    public static BsqliteDocumentDto[] Documents => _documentList.Value;

    /// <summary>Every level 2, 3 and 4 heading of every document, in reading order.</summary>
    public static BsqliteDocumentSectionDto[] Sections => _sections.Value;

    /// <summary>Every embedded source file, keyed by the path the tools expose.</summary>
    public static BsqliteSourceFileDto[] SourceFiles => _sourceFileList.Value;

    /// <summary>
    /// The documents and their sections, as the index GetBsqliteDocument answers with when it is called
    /// without a name. Plain text rather than a list of objects: an index is read, not parsed, and the
    /// same list as JSON costs twice the characters to say the same thing.
    /// </summary>
    public static string RenderDocumentIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bsqlite documents").AppendLine();
        builder.AppendLine("The README, the guides under docs/ and every package's own README. Call ")
               .AppendLine("`GetBsqliteDocument(name: \"...\")` for one of them whole, or add `section: \"...\"` for one of its sections; ")
               .AppendLine("a section's line count is there so a heading that runs to a hundred lines is a deliberate choice rather than a surprise.")
               .AppendLine();

        foreach (var group in _documents.Value.GroupBy(document => document.Dto.Kind))
        {
            builder.AppendLine($"## {KindTitle(group.Key)}").AppendLine();

            foreach (var document in group)
            {
                builder.Append($"- **{document.Dto.Name}** - {document.Dto.Title} ({document.Dto.Lines} lines)");
                if (document.Dto.Summary is not null) builder.Append($" - {document.Dto.Summary}");
                builder.AppendLine();

                foreach (var section in document.Sections)
                {
                    var indent = new string(' ', (section.Dto.Level - 1) * 2);
                    var key = section.Dto.Key == section.Dto.Heading ? null : $" - key `{section.Dto.Key}`";

                    builder.AppendLine($"{indent}- {section.Dto.Heading} ({section.Dto.Lines} lines){key}");
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// The files GetBsqliteSourceFile can hand out, as the index it answers with when it is called
    /// without a path.
    /// </summary>
    public static string RenderSourceIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bsqlite source files").AppendLine();
        builder.AppendLine("Working code this server can hand out verbatim. Call `GetBsqliteSourceFile(path: \"...\")` for one.").AppendLine();

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

    /// <summary>
    /// The document a caller means, or null. Case and punctuation are ignored, an extension or a docs/
    /// prefix is dropped, and a package README can be named without the package's common prefix:
    /// "storage.md", "README", "Testing" and "opfs" all find their document.
    /// </summary>
    public static string? ResolveDocumentName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var text = name.Trim().Replace('\\', '/');

        if (text.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) text = text[..^3];

        var slash = text.LastIndexOf('/');
        if (slash >= 0) text = text[(slash + 1)..];

        var wanted = Squash(text);
        if (wanted.Length == 0) return null;

        var documents = _documents.Value;

        return (documents.FirstOrDefault(document => Squash(document.Dto.Name) == wanted)
                ?? documents.FirstOrDefault(document => document.Dto.Kind == "Package" && Squash(document.Dto.Name) == $"bitbsqlite{wanted}")
                ?? documents.FirstOrDefault(document => document.Dto.Kind == "Package" && Squash(document.Dto.Name) == $"bitbsqlitestorage{wanted}")
                ?? documents.FirstOrDefault(document => Squash(document.Dto.Title) == wanted))?.Dto.Name;
    }

    /// <summary>One document in full, or null when no document goes by that name.</summary>
    public static string? GetDocument(string? name) => FindDocument(name)?.Text;

    /// <summary>The headings of one document, or an empty list when it does not exist.</summary>
    public static BsqliteDocumentSectionDto[] GetSections(string? name)
        => FindDocument(name) is { } document ? [.. document.Sections.Select(section => section.Dto)] : [];

    /// <summary>
    /// The text under a heading of one document, including its sub-sections. Matching is case- and
    /// punctuation-insensitive so "first party backends" finds "First-party backends"; a heading the
    /// document uses more than once is told apart by its key, "Parent > Heading".
    /// </summary>
    public static string? GetDocumentSection(string? name, string? heading)
    {
        var document = FindDocument(name);
        if (document is null) return null;

        var section = FindSection(document, heading);
        if (section is null) return null;

        return string.Join('\n', document.Lines[section.Start..section.End]).TrimEnd();
    }

    /// <summary>
    /// The text of a document above its first section - its title, its opening prose, the code it leads
    /// with - or all of it when it has no sections. Null when the document does not exist.
    /// </summary>
    public static string? GetPreamble(string? name)
    {
        var document = FindDocument(name);
        if (document is null) return null;

        var end = document.Sections.Length == 0 ? document.Lines.Length : document.Sections[0].Start;

        return string.Join('\n', document.Lines[..end]).Trim();
    }

    /// <summary>The content of an embedded source file, or null when the path is unknown.</summary>
    public static string? GetSourceFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        return _sourceFiles.Value.GetValueOrDefault(Normalize(path));
    }

    private static Document? FindDocument(string? name)
    {
        var resolved = ResolveDocumentName(name);

        return resolved is null ? null : _documents.Value.First(document => document.Dto.Name == resolved);
    }

    private static Section? FindSection(Document document, string? heading)
    {
        if (string.IsNullOrWhiteSpace(heading)) return null;

        var sections = document.Sections;

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

    private static Document[] BuildDocuments()
    {
        var documents = new List<Document>();

        foreach (var resource in _assembly.GetManifestResourceNames())
        {
            var normalized = Normalize(resource);

            if (normalized.StartsWith(DocsPrefix, StringComparison.OrdinalIgnoreCase) is false) continue;

            var text = ReadResource(resource);
            if (text is null) continue;

            var relative = normalized[DocsPrefix.Length..];
            var stem = Path.GetFileNameWithoutExtension(relative);

            var (name, kind) = relative.StartsWith(GuidePrefix, StringComparison.OrdinalIgnoreCase) ? (stem, "Guide")
                             : relative.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase) ? (stem, "Package")
                             : (ReadmeName, "Readme");

            documents.Add(BuildDocument(name, kind, text));
        }

        // The README's own list of guides is their reading order - getting started before storage before
        // the limits - and the packages follow the order the API reference lists them in.
        var readme = documents.FirstOrDefault(document => document.Dto.Kind == "Readme")?.Text ?? string.Empty;

        return [.. documents
            .OrderBy(document => document.Dto.Kind switch { "Readme" => 0, "Guide" => 1, _ => 2 })
            .ThenBy(document => document.Dto.Kind switch
            {
                "Guide" => GuideOrder(readme, document.Dto.Name),
                "Package" => PackageOrder(document.Dto.Name),
                _ => 0
            })
            .ThenBy(document => document.Dto.Name, StringComparer.OrdinalIgnoreCase)];
    }

    private static int GuideOrder(string readme, string name)
    {
        var index = readme.IndexOf($"docs/{name}.md", StringComparison.OrdinalIgnoreCase);

        return index < 0 ? int.MaxValue : index;
    }

    private static int PackageOrder(string name)
    {
        var index = Array.FindIndex(BsqliteApiCatalog.Assemblies, assembly => string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase));

        return index < 0 ? int.MaxValue : index;
    }

    private static Document BuildDocument(string name, string kind, string text)
    {
        text = text.TrimStart('﻿');

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var sections = BuildSections(name, lines);

        var title = lines.Select(line => TryReadHeading(line, out var level, out var heading) && level == 1 ? heading : null)
                         .FirstOrDefault(heading => heading is not null) ?? name;

        return new Document(new BsqliteDocumentDto
        {
            Name = name,
            Title = title,
            Kind = kind,
            Summary = Summary(lines),
            Lines = CountLines(text),
            Sections = [.. sections.Select(section => section.Dto.Key)]
        }, text, lines, sections);
    }

    /// <summary>
    /// The first sentence of the document's opening paragraph - the prose under its title, skipping the
    /// code, quotes and tables some documents open with.
    /// </summary>
    private static string? Summary(string[] lines)
    {
        var paragraph = new List<string>();
        var fenced = false;
        var titled = false;

        foreach (var line in lines)
        {
            if (IsCodeFence(line))
            {
                fenced = fenced is false;
                if (paragraph.Count > 0) break;
                continue;
            }

            if (fenced) continue;

            // A guide that goes straight into its first section is summarised by that section's opening
            // paragraph: what comes first is what the document is about.
            if (TryReadHeading(line, out _, out _))
            {
                if (paragraph.Count > 0) break;

                titled = true;
                continue;
            }

            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                if (paragraph.Count > 0) break;
                continue;
            }

            if (titled is false || trimmed.StartsWith('>') || trimmed.StartsWith('|') || trimmed.StartsWith('-')) continue;

            paragraph.Add(trimmed);
        }

        if (paragraph.Count == 0) return null;

        var text = MarkdownLinkRegex().Replace(string.Join(' ', paragraph), "${text}");

        return Summarize(text);
    }

    private static Section[] BuildSections(string document, string[] lines)
    {
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

            sections.Add(new Section(new BsqliteDocumentSectionDto
            {
                Document = document,
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

    private static BsqliteSourceFileDto[] BuildSourceFileList()
    {
        return [.. _sourceFiles.Value
            .Select(file => new BsqliteSourceFileDto
            {
                Path = file.Key,
                Kind = KindOf(file.Key),
                Description = DescribeSource(file.Key, file.Value),
                Lines = CountLines(file.Value)
            })
            .OrderBy(file => KindOrder(file.Kind))
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// What a file is for: a demo page is a working example to copy, the hosting files are the wiring
    /// every app needs once, the model is the EF Core model both sides share, and the rest is the
    /// components and helpers the pages share.
    /// </summary>
    private static string KindOf(string path)
    {
        if (path.StartsWith("Demo/Client/Pages/", StringComparison.OrdinalIgnoreCase)) return "Page";
        if (path.StartsWith("Demo/Data/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("Demo/Domain/", StringComparison.OrdinalIgnoreCase)) return "Model";
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
        "Model" => 2,
        "Hosting" => 3,
        _ => 4
    };

    private static string KindTitle(string kind) => kind switch
    {
        "Readme" => "The README",
        "Guide" => "Guides",
        "Package" => "Package READMEs",
        _ => kind
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
    /// A one-line description of a source file, taken from whatever the file itself already says: its
    /// leading razor/C# comment, its page header, the summary of the type it declares, or - for a page -
    /// its &lt;PageTitle&gt;.
    /// </summary>
    private static string? DescribeSource(string path, string content)
    {
        // A byte order mark is not text, and it would stop every pattern anchored at the start of the file.
        content = content.TrimStart('﻿');

        var leadingComment = LeadingRazorCommentRegex().Match(content);
        if (leadingComment.Success) return Summarize(leadingComment.Groups["text"].Value);

        // A demo page opens with a PageHeader that says what it demonstrates, to the person reading it -
        // which is the best description of it there is, far better than the first comment in its code.
        var header = PageHeaderRegex().Match(content);
        if (header.Success)
        {
            var intro = Summarize(XmlTagRegex().Replace(header.Groups["text"].Value, string.Empty));

            return intro is null ? header.Groups["title"].Value : $"{header.Groups["title"].Value}: {intro}";
        }

        // A comment the file opens with describes the file; an XML summary further down describes one
        // type inside it (Program.cs ends with one about the partial Program class).
        var lineComment = LeadingLineCommentRegex().Match(content);
        if (lineComment.Success) return Summarize(lineComment.Value.Replace("//", " ", StringComparison.Ordinal));

        if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
        {
            // In a component the XML summaries document its parameters, not the component: the comment that
            // follows its directives, or its title, says what it is.
            var comment = RazorCommentRegex().Match(content);
            if (comment.Success) return Summarize(comment.Groups["text"].Value);

            var title = PageTitleRegex().Match(content);
            if (title.Success) return Summarize(title.Groups["text"].Value);
        }
        else
        {
            // The summary of the type the file declares, not the first one in it: a file that opens with an
            // enum's values would otherwise be described by the first of them.
            var summary = TypeSummaryRegex().Match(content);
            if (summary.Success) return Summarize(Unmark(summary.Groups["text"].Value));
        }

        // Nothing said what the file is, but the first thing its code explains usually does.
        var codeComment = LineCommentRegex().Match(content);
        if (codeComment.Success) return Summarize(codeComment.Value.Replace("//", " ", StringComparison.Ordinal));

        // A model file says nothing about itself, but its declaration is a summary of it all the same.
        var declaration = TypeDeclarationRegex().Match(content);

        return declaration.Success ? Summarize($"Declares `{declaration.Groups["text"].Value.Trim()}`.") : null;
    }

    /// <summary>
    /// An XML summary as prose: a <c>&lt;see cref="..."/&gt;</c> becomes the name it refers to, and every
    /// other tag goes - they are markup, and noise in a listing.
    /// </summary>
    private static string Unmark(string summary)
    {
        var text = summary.Replace("///", " ", StringComparison.Ordinal);

        text = SeeCrefRegex().Replace(text, match => match.Groups["name"].Value.Split('.')[^1]);

        return XmlTagRegex().Replace(text, string.Empty);
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
    /// A Markdown code fence. A '#' line inside one is a shell comment or a C# preprocessor directive,
    /// not a heading, and must not start or end a section.
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

    /// <summary>Reduces a heading to its comparable core, so "first party backends" finds "## First-party backends".</summary>
    private static string NormalizeHeading(string heading)
    {
        var builder = new StringBuilder(heading.Length);

        // The little words carry nothing a heading is told apart by, and they are the ones a caller most
        // often drops ("moving off besql") or adds ("the live queries").
        foreach (var word in heading.Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries))
        {
            if (_fillerWords.Contains(word)) continue;

            foreach (var c in word) builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static readonly HashSet<string> _fillerWords = new(StringComparer.OrdinalIgnoreCase) { "a", "an", "and", "the", "of" };

    /// <summary>A name with nothing but its letters and digits, lower-cased: "Bit.Bsqlite.Testing" is "bitbsqlitetesting".</summary>
    private static string Squash(string text) => new([.. text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

    private static string Normalize(string path) => path.Replace('\\', '/').Trim('/');

    [GeneratedRegex(@"^\s*@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex LeadingRazorCommentRegex();

    [GeneratedRegex(@"@\*(?<text>.*?)\*@", RegexOptions.Singleline)]
    private static partial Regex RazorCommentRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex XmlTagRegex();

    [GeneratedRegex(@"///\s*<summary>(?<text>(?:(?!</summary>).)*)</summary>[^\n]*\n(?:[ \t]*(?:///|\[)[^\n]*\n)*[ \t]*(?:public|internal)\b[^\n{]*\b(?:class|record|struct|enum|interface)\b", RegexOptions.Singleline)]
    private static partial Regex TypeSummaryRegex();

    [GeneratedRegex(@"<see\s+cref=""(?:[A-Z]:)?(?<name>[^""]+)""\s*/>")]
    private static partial Regex SeeCrefRegex();

    [GeneratedRegex(@"(?:^[ \t]*//(?!/)[^\n]*\n)+", RegexOptions.Multiline)]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex(@"<PageHeader\b[^>]*\bTitle=""(?<title>[^""]*)""[^>]*>(?<text>.*?)</PageHeader>", RegexOptions.Singleline)]
    private static partial Regex PageHeaderRegex();

    [GeneratedRegex(@"<PageTitle>(?<text>.*?)</PageTitle>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PageTitleRegex();

    [GeneratedRegex(@"^\s*(//[^\n]*\n)+")]
    private static partial Regex LeadingLineCommentRegex();

    [GeneratedRegex(@"^(?:public|internal)\s+(?<text>[^\n{]*\b(?:class|record|struct|enum|interface)\b[^\n{]*)", RegexOptions.Multiline)]
    private static partial Regex TypeDeclarationRegex();

    [GeneratedRegex(@"\[(?<text>[^\]]+)\]\([^)]+\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
