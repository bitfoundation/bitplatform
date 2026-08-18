using Bit.Brouter.Demo.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a docs slug, a guide heading, a public type name, an embedded source path.
/// Without completions a person picking <c>brouter://docs/{slug}</c> in a client has to go and call
/// a listing tool first, just to learn what a slug looks like. With them the client types ahead the
/// way an editor does, and a wrong value stops being reachable.
/// </para>
/// </summary>
public static class BrouterCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "a deep link 404s on refresh but works when navigated to in the app",
        "the route matches but its parameter arrives null",
        "a guard never fires when leaving the page",
        "the loader keeps returning stale data after a save",
        "two routes match the same URL and the wrong one wins",
        "the component is recreated on every navigation and loses its state",
        "/files/report.pdf renders the 404 route"
    ];

    private static readonly string[] _features =
    [
        "warn before leaving a half-filled form",
        "load the order before the page renders and cache it for 30 seconds",
        "redirect to the sign-in page and come back afterwards",
        "keep the search page alive so its scroll position survives navigation",
        "animate between two routes with a view transition",
        "nest a settings section under a shared layout",
        "bind a filter to the query string"
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
        // The overview page's slug is the empty string, which no one can type: the alias the tool
        // already accepts for it is what gets offered.
        "slug" => [.. DocsCatalog.AllPages.Select(page => page.Slug.Length == 0 ? "overview" : page.Slug)],
        "heading" => [.. BrouterSourceCatalog.GuideSections.Select(section => section.Heading)],
        "typeName" => [.. BrouterApiCatalog.Types.Select(type => type.Name)],
        "path" => [.. BrouterSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
