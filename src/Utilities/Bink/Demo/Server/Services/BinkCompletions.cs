using ModelContextProtocol.Protocol;

namespace Bit.Bink.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, a brush kind, an embedded source path.
/// Without completions a person picking <c>bink://api/{typeName}</c> in a client has to go and call
/// a listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BinkCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "the canvas renders but drawing on it does nothing",
        "the toolbar does not update after drawing",
        "ink lags behind the pen under Blazor Server",
        "touch scrolls the page instead of drawing",
        "a saved document does not load back",
        "custom brush strokes come back as a pen after loading",
        "the exported PNG is missing the text",
        "the exported SVG is cropped or blank",
    ];

    private static readonly string[] _features =
    [
        "a signature pad that exports a PNG",
        "a handwriting field with pressure-sensitive ink",
        "a whiteboard with pen, highlighter, eraser, shapes and undo",
        "annotate an image with arrows, highlights and callouts",
        "save the drawing to the database and load it back",
        "export the drawing as SVG on the server",
        "real-time collaborative drawing between two users",
        "a custom brush registered for serialization",
    ];

    /// <summary>
    /// The values matching what has been typed so far. The argument's name is enough to identify the
    /// set - no two arguments on this server mean different things by the same name - so one table
    /// serves a prompt argument and a resource template placeholder without either knowing about the
    /// other, and an argument it has nothing for is answered with an empty list rather than an error.
    /// </summary>
    public static Completion Complete(string? argumentName, string? typed)
    {
        var candidates = CandidatesFor(argumentName);
        if (candidates.Length == 0) return new Completion { Values = [] };

        var prefix = (typed ?? string.Empty).Trim();

        var matches = prefix.Length == 0
            ? candidates
            : [.. candidates
                .Where(value => value.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                // What was typed is a prefix far more often than it is a substring, so the values it
                // starts stay above the ones it merely appears inside.
                .OrderByDescending(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)];

        return new Completion
        {
            Values = [.. matches.Take(MaxValues)],
            Total = matches.Length,
            HasMore = matches.Length > MaxValues
        };
    }

    // "renderMode" is deliberately missing here: its values are declared with [AllowedValues], which
    // the SDK completes from on its own, and anything this returned would be merged with that rather
    // than replace it - the same value offered to someone twice.
    private static string[] CandidatesFor(string? argumentName) => argumentName switch
    {
        "heading" => [.. BinkSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BinkApiCatalog.Types.Select(type => type.Name)],
        "kind" => [.. BinkBrushCatalog.Brushes.Select(brush => brush.Kind)],
        "path" => [.. BinkSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
