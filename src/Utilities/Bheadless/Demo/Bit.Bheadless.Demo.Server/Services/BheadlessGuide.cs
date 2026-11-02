using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bheadless.Demo.Server.Services;

/// <summary>One heading of the guide with everything under it, sub-headings included.</summary>
public sealed record GuideSection(string Heading, int Level, string Markdown);

/// <summary>
/// The library's guide - its README, embedded into this assembly by the csproj - split at its
/// headings, so a question about positioning is answered with the positioning section rather than
/// with the whole document.
/// <para>
/// The README is embedded only when it exists at build time; without it the tools still answer
/// everything they read off the assembly and the demo pages, and say the guide is missing rather
/// than failing.
/// </para>
/// </summary>
public static partial class BheadlessGuide
{
    public const string ResourceName = "BitBheadlessDocs/README.md";

    private static readonly Lazy<string?> _readme = new(Load, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<GuideSection[]> _sections = new(Split, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>The README, or null when the build had none to embed.</summary>
    public static string? Readme => _readme.Value;

    /// <summary>Every ## and ### section in document order, the table of contents left out.</summary>
    public static GuideSection[] Sections => _sections.Value;

    public const string Missing =
        "_The library guide (README.md) was not part of this build, so there is no guide to answer from. " +
        "Everything read off the library itself still is: `GetBheadlessComponent` with no argument lists every component family._";

    /// <summary>The tool's answer: a section by topic, or with no topic the section list and the installation section.</summary>
    public static string Get(string? topic)
    {
        if (Readme is null) return Missing;

        if (string.IsNullOrWhiteSpace(topic)) return Overview();

        var section = Find(topic, out var exact);

        if (section is null)
        {
            return $"The guide has no section about '{topic}'. Its sections are: " +
                   string.Join(", ", Sections.Select(entry => $"'{entry.Heading}'")) + ".";
        }

        return exact
            ? section.Markdown
            : $"_No heading of the guide is '{topic}'; this is the section that covers it most._\n\n{section.Markdown}";
    }

    /// <summary>The section a topic names: by heading first, then by what the sections say.</summary>
    public static GuideSection? Find(string topic, out bool exact)
    {
        var key = Normalize(topic);
        exact = true;

        if (key.Length == 0) return null;

        var byHeading = Sections.FirstOrDefault(section => Normalize(section.Heading) == key)
                     ?? Sections.Where(section => Normalize(section.Heading).Contains(key, StringComparison.Ordinal)
                                               || (key.Length >= 4 && key.Contains(Normalize(section.Heading), StringComparison.Ordinal)))
                                .OrderBy(section => section.Heading.Length)
                                .FirstOrDefault();

        if (byHeading is not null) return byHeading;

        exact = false;

        var words = Words(topic);

        if (words.Length == 0) return null;

        // The innermost section that mentions the words most: a sub-section's text is part of its parent
        // too, so on a tie the deeper, shorter one is the more precise answer.
        return Sections.Select(section => (Section: section, Score: Score(section, words)))
                       .Where(hit => hit.Score > 0)
                       .OrderByDescending(hit => hit.Score)
                       .ThenByDescending(hit => hit.Section.Level)
                       .ThenBy(hit => hit.Section.Markdown.Length)
                       .Select(hit => hit.Section)
                       .FirstOrDefault();
    }

    /// <summary>How strongly a section is about the given words: a heading word counts more than one in the text.</summary>
    public static int Score(GuideSection section, string[] words)
    {
        var heading = Words(section.Heading);
        var text = Words(section.Markdown);

        return words.Sum(word => (heading.Contains(word) ? 5 : 0) + Math.Min(text.Count(candidate => candidate == word), 5));
    }

    private static string Overview()
    {
        var builder = new StringBuilder();

        var intro = IntroRegex().Match(Readme!);
        if (intro.Success) builder.AppendLine(intro.Groups["intro"].Value.Trim()).AppendLine();

        builder.AppendLine("## Sections of the guide").AppendLine();
        foreach (var section in Sections)
        {
            builder.Append(section.Level > 2 ? "  - " : "- ").AppendLine(section.Heading);
        }

        builder.AppendLine()
               .AppendLine("Pass one of these as `topic` for its text; sub-sections come with their parent.")
               .AppendLine();

        if (Find("Installation", out _) is { } installation) builder.AppendLine(installation.Markdown);

        return builder.ToString().TrimEnd();
    }

    private static string? Load()
    {
        using var stream = typeof(BheadlessGuide).Assembly.GetManifestResourceStream(ResourceName);

        if (stream is null) return null;

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static GuideSection[] Split()
    {
        if (Readme is null) return [];

        var lines = Readme.Split('\n');
        var headings = new List<(int Line, int Level, string Text)>();
        var fenced = false;

        for (var i = 0; i < lines.Length; i++)
        {
            // A '#' at the start of a line inside a code sample is a comment, not a heading.
            if (lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal)) fenced = !fenced;
            if (fenced) continue;

            if (HeadingRegex().Match(lines[i]) is { Success: true } match)
            {
                headings.Add((i, match.Groups["hashes"].Length, match.Groups["text"].Value.Trim()));
            }
        }

        var sections = new List<GuideSection>();

        for (var h = 0; h < headings.Count; h++)
        {
            var (line, level, text) = headings[h];

            if (level < 2 || level > 3 || Normalize(text) is "contents" or "tableofcontents") continue;

            var end = headings.Skip(h + 1).FirstOrDefault(next => next.Level <= level).Line;
            if (end == 0) end = lines.Length;

            sections.Add(new GuideSection(text, level, string.Join('\n', lines[line..end]).Trim().TrimEnd('-').TrimEnd()));
        }

        return [.. sections];
    }

    private static string Normalize(string text)
        => new([.. text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

    public static string[] Words(string text)
        => [.. WordRegex().Matches(text.ToLowerInvariant()).Select(match => match.Value).Where(word => word.Length > 1)];

    [GeneratedRegex(@"^(?<hashes>#{1,6})\s+(?<text>.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^#\s+.+?\n(?<intro>.*?)(?=\n---|\n##\s)", RegexOptions.Singleline)]
    private static partial Regex IntroRegex();

    [GeneratedRegex(@"[a-z0-9]+")]
    private static partial Regex WordRegex();
}
