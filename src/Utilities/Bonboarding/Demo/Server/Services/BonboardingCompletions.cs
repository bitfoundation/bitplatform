using ModelContextProtocol.Protocol;

namespace Bit.Bonboarding.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, an embedded source path. Without
/// completions a person picking <c>bonboarding://api/{typeName}</c> in a client has to go and call a
/// listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BonboardingCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "the tour starts but nothing is drawn",
        "the popover appears unstyled at the bottom of the page",
        "the tour starts twice after prerendering",
        "the step waits forever for an element that is on the page",
        "the popover points at the wrong place after the layout changes",
        "a finished tour never starts again even after a change",
        "the Back button goes to the wrong step in a branching tour",
        "clicking the highlighted button does nothing"
    ];

    private static readonly string[] _features =
    [
        "a welcome tour that runs once per user and can be replayed from the help menu",
        "a step that advances when the user clicks the highlighted button",
        "a tour that continues on the settings page",
        "different steps for admins and regular users",
        "a step that waits for a lazily loaded panel to appear",
        "a single contextual hint next to a new feature",
        "send every step's duration to our analytics",
        "a fully custom popover that matches our design system"
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
        "heading" => [.. BonboardingSourceCatalog.GuideSections.Select(section => section.Heading)],
        "typeName" => [.. BonboardingApiCatalog.Types.Select(type => type.Name)],
        "path" => [.. BonboardingSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
