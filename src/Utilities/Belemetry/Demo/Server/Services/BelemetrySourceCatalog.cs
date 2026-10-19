using System.Text;
using System.Reflection;
using System.Collections.Frozen;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// The text the MCP server answers from: the README (the reference guide), the demo's own sources and
/// the hosting samples - all embedded into this assembly by the csproj, so a published deployment has
/// them without any file next to it.
/// <para>
/// Every one of them is optional. The csproj embeds them with globs, so a README that has not been
/// written yet or a sample that does not exist is simply missing here, and every tool that reads them
/// answers with what there is instead of failing.
/// </para>
/// </summary>
public static class BelemetrySourceCatalog
{
    private const string DocsPrefix = "BelemetryDocs/";
    private const string SourcePrefix = "BelemetrySource/";

    private static readonly Assembly _assembly = typeof(BelemetrySourceCatalog).Assembly;

    private static readonly Lazy<FrozenDictionary<string, string>> _sources = new(() => Load(SourcePrefix));
    private static readonly Lazy<string> _readme = new(() => Read($"{DocsPrefix}README.md") ?? string.Empty);
    private static readonly Lazy<GuideSection[]> _sections = new(() => ParseSections(Readme));

    private static readonly Lazy<string> _contracts = new(() => Read($"{DocsPrefix}CLAUDE.md") ?? string.Empty);

    /// <summary>The README, or empty when it was not embedded.</summary>
    public static string Readme => _readme.Value;

    /// <summary>
    /// The tool folder's CLAUDE.md - the wire contracts between the C# and the TypeScript halves (feature bits,
    /// storage keys, the browser config, the conventions, the Azure mapping) - or empty when it was not embedded.
    /// </summary>
    public static string Contracts => _contracts.Value;

    /// <summary>The README split at its <c>##</c> and <c>###</c> headings, in document order.</summary>
    public static GuideSection[] Sections => _sections.Value;

    /// <summary>Every embedded source file by its path (e.g. <c>Demo/Client/Pages/ConsentPage.razor</c>, <c>Samples/Server/Program.cs</c>).</summary>
    public static FrozenDictionary<string, string> Sources => _sources.Value;

    /// <summary>The paths of <see cref="Sources"/>, sorted.</summary>
    public static string[] Paths => [.. Sources.Keys.Order(StringComparer.OrdinalIgnoreCase)];

    /// <summary>A source file by path, compared case-insensitively and with either slash; null when there is none.</summary>
    public static string? GetSource(string path)
    {
        var normalized = Normalize(path);

        if (Sources.TryGetValue(normalized, out var content)) return content;

        var match = Sources.Keys.FirstOrDefault(key => string.Equals(key, normalized, StringComparison.OrdinalIgnoreCase));

        return match is null ? null : Sources[match];
    }

    /// <summary>A README section by heading (case-insensitive, also matched as a prefix); null when there is none.</summary>
    public static GuideSection? GetSection(string heading)
    {
        var term = heading.Trim().TrimStart('#').Trim();

        return Sections.FirstOrDefault(s => string.Equals(s.Heading, term, StringComparison.OrdinalIgnoreCase))
            ?? Sections.FirstOrDefault(s => string.Equals(s.Anchor, term.TrimStart('#'), StringComparison.OrdinalIgnoreCase))
            ?? Sections.FirstOrDefault(s => s.Heading.StartsWith(term, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A path as the catalog keys it: forward slashes, no leading slash, no prefix.</summary>
    public static string Normalize(string path)
    {
        var normalized = path.Replace('\\', '/').Trim().TrimStart('/');

        return normalized.StartsWith(SourcePrefix, StringComparison.OrdinalIgnoreCase) ? normalized[SourcePrefix.Length..] : normalized;
    }

    /// <summary>The GitHub-style anchor of a heading: lower case, spaces to dashes, punctuation dropped.</summary>
    public static string ToAnchor(string heading)
    {
        var builder = new StringBuilder(heading.Length);

        foreach (var c in heading.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_') builder.Append(c);
            else if (c == ' ') builder.Append('-');
        }

        return builder.ToString();
    }

    private static FrozenDictionary<string, string> Load(string prefix)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in _assembly.GetManifestResourceNames())
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal) is false) continue;

            // MSBuild writes %(RecursiveDir) with backslashes on Windows.
            var path = name[prefix.Length..].Replace('\\', '/');

            if (Read(name) is { } content) files[path] = content;
        }

        return files.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static string? Read(string name)
    {
        using var stream = _assembly.GetManifestResourceStream(name);

        if (stream is null) return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static GuideSection[] ParseSections(string markdown)
    {
        if (markdown.Length == 0) return [];

        var sections = new List<GuideSection>();
        var lines = markdown.Split('\n');
        string? heading = null;
        string? parent = null;
        var level = 0;
        var body = new StringBuilder();
        var inFence = false;

        void Flush()
        {
            if (heading is not null) sections.Add(new(heading, level, level == 3 ? parent : null, body.ToString().Trim()));
            body.Clear();
        }

        foreach (var line in lines)
        {
            // A '#' inside a fenced code block is a comment in a shell or YAML sample, not a heading.
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) inFence = !inFence;

            var newLevel = inFence ? 0 : line.StartsWith("### ", StringComparison.Ordinal) ? 3 : line.StartsWith("## ", StringComparison.Ordinal) ? 2 : 0;

            if (newLevel > 0)
            {
                Flush();
                heading = line[(newLevel + 1)..].Trim();
                level = newLevel;
                if (newLevel == 2) parent = heading;
                continue;
            }

            if (heading is not null && line.Trim() != "---") body.Append(line).Append('\n');
        }

        Flush();

        return [.. sections];
    }
}

/// <summary>One section of the README.</summary>
/// <param name="Heading">The heading text, without the hashes.</param>
/// <param name="Level">2 for <c>##</c>, 3 for <c>###</c>.</param>
/// <param name="Parent">The <c>##</c> section a <c>###</c> one belongs to.</param>
/// <param name="Body">The markdown under the heading, up to the next heading of any level.</param>
public sealed record GuideSection(string Heading, int Level, string? Parent, string Body)
{
    /// <summary>The section's anchor in the README.</summary>
    public string Anchor => BelemetrySourceCatalog.ToAnchor(Heading);
}
