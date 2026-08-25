using System.Text;
using System.Reflection;
using System.Collections.Frozen;
using Bit.Bswup.Demo.Server.Dtos;

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
public static class BswupSourceCatalog
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
                Description = DescribeSource(file.Key),
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
    /// What a file is, said by the role it plays rather than lifted out of its own text.
    /// <para>
    /// This used to be parsed out of whatever comment came first in the file, and on a curated set
    /// that reads badly: the sample splash was described by the markup someone had commented out
    /// inside it, a host document by an aside about screen readers, and every service-worker file
    /// by its version stamp. A guess that is wrong is worse than no description, because a caller
    /// picks the file to spend its next call on from exactly this line. The set these tools hand
    /// out is small and deliberate (see the EmbeddedResource items in the .csproj), so each entry
    /// can simply say what it is for.
    /// </para>
    /// </summary>
    private static string? DescribeSource(string path)
    {
        var name = Path.GetFileName(path);

        return path switch
        {
            "Library/Scripts/bit-bswup.ts" =>
                "The page script: registers the worker, owns Blazor's startup, dispatches every lifecycle message to the page's handler, installs the global BitBswup API.",
            "Library/Scripts/bit-bswup.sw.ts" =>
                "The service-worker engine: every self.* setting, the asset lists, precaching, serving and update staging. What every answer here about caching is read out of.",
            "Library/Scripts/bit-bswup.progress.ts" =>
                "The built-in splash script: the default bitBswupHandler, and the code driving the element ids BswupProgress renders.",
            "Library/Scripts/bit-bswup.sw-cleanup.ts" =>
                "The self-destructing cleanup worker a service-worker file is replaced with to back Bswup out.",
            "Library/BswupProgress.razor" =>
                "The BswupProgress component: the splash markup and the parameters it publishes as data-* attributes.",
            "Sample/BasicSample/wwwroot/index.html" =>
                "The standalone sample's host document: a complete hand-written splash and handler, which is what a first install needs - Blazor has not started yet, so no component can paint one.",
            "Sample/FullSample/Client/Shared/SampleBswupProgressBar.razor" =>
                "A custom splash: BswupProgress with the app's own ChildContent and handler.",
            "Sample/FullSample/Server/Bit.Bswup.FullSample.Server.csproj" =>
                "The host project's project file. The Bswup wiring lives in the client project.",
            _ when name.Equals("service-worker.js", StringComparison.OrdinalIgnoreCase) =>
                $"The development service-worker file of {ProjectOf(path)}.",
            _ when name.Equals("service-worker.published.js", StringComparison.OrdinalIgnoreCase) =>
                $"The published service-worker file of {ProjectOf(path)} - what deployed builds ship.",
            _ when name.Equals("App.razor", StringComparison.OrdinalIgnoreCase) && path.Contains("/Components/", StringComparison.OrdinalIgnoreCase) =>
                $"The host document of {ProjectOf(path)}: the Blazor script with autostart=\"false\", the bit-bswup.js tag and the splash.",
            _ when name.Equals("App.razor", StringComparison.OrdinalIgnoreCase) =>
                $"The root component of {ProjectOf(path)}; its host document is index.html.",
            _ when name.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) =>
                $"The startup of {ProjectPartOf(path)}. Bswup needs nothing here.",
            _ when path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) =>
                $"The project file of {ProjectPartOf(path)}: the ServiceWorker item, the assets manifest and the fingerprinting switch.",
            _ => null
        };
    }

    /// <summary>Which of the three projects a path belongs to, named the way a caller would name it.</summary>
    private static string ProjectOf(string path)
    {
        if (path.StartsWith("Demo/", StringComparison.OrdinalIgnoreCase)) return "this documentation site";
        if (path.StartsWith("Sample/BasicSample/", StringComparison.OrdinalIgnoreCase)) return "the standalone WebAssembly sample";
        if (path.StartsWith("Sample/FullSample/", StringComparison.OrdinalIgnoreCase)) return "the Blazor Web App sample";

        return "the library";
    }

    /// <summary>
    /// The same, but naming which half of a two-project sample the file belongs to. The Blazor Web
    /// App sample has a Program.cs and a .csproj on both sides, and only one of each carries the
    /// Bswup wiring, so for those files the sample's name alone would not say which is which.
    /// </summary>
    private static string ProjectPartOf(string path)
    {
        if (path.StartsWith("Sample/FullSample/Server/", StringComparison.OrdinalIgnoreCase)) return "the Blazor Web App sample's host project";
        if (path.StartsWith("Sample/FullSample/", StringComparison.OrdinalIgnoreCase)) return "the Blazor Web App sample's client project";

        return ProjectOf(path);
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

}
