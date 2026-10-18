using ModelContextProtocol.Protocol;

namespace Bit.Balette.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, an embedded source path. Without
/// completions a person picking <c>balette://api/{typeName}</c> in a client has to go and call a
/// listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BaletteCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "Ctrl+K does nothing",
        "the palette opens unstyled at the top of the page",
        "the app fails while prerendering because IBaletteService is not registered",
        "a toggle shows its old state after it was run",
        "recents and pins forget a command after its title changed",
        "a page's commands are still listed after leaving the page",
        "a command does not show up when I type its name",
        "a command ranks below a worse match",
        "a global shortcut does not fire while a text box has focus",
        "results from my API source never appear",
    ];

    private static readonly string[] _features =
    [
        "a command palette with navigation to every page and Ctrl+K to open it",
        "commands that only exist while one page is open",
        "a settings toggle that keeps the palette open and shows its new state",
        "a rename command that asks for the new name",
        "a multi-select page to set labels, applied with Ctrl+Enter",
        "a delete command that asks for confirmation",
        "search our API for documents as the user types",
        "a '>' mode that lists only commands, like VS Code",
        "G then S shortcuts that navigate with the palette closed",
        "restyle the palette to our brand colours, in light and dark",
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
        "heading" => [.. BaletteSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BaletteApiCatalog.Types.Select(type => type.Name)],
        "path" => [.. BaletteSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
