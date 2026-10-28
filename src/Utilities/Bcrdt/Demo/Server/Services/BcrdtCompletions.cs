using ModelContextProtocol.Protocol;

namespace Bit.Bcrdt.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, an embedded source path. Without
/// completions a person picking <c>bcrdt://api/{typeName}</c> in a client has to go and call a
/// listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BcrdtCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "two people typed into the same field and one of them lost everything",
        "the app fails while prerendering because IBcrdtCollaborationService is not registered",
        "documents never sync and the log warns that Bit.Bcrdt's services are not registered",
        "the SignalR connection fails with 'The uri must be absolute'",
        "clients disconnect once the document gets bigger",
        "the WebSocket relay answers 400",
        "edits made offline never reach the other tabs",
        "a deletion made offline comes back after reconnecting",
        "a user stays in the presence list long after closing the tab",
        "the caret jumps when someone else types earlier in the text",
        "undo removes other people's edits",
        "a document opened in Blazor Server shows nothing",
        "ApplyUpdate throws BcrdtFormatException",
    ];

    private static readonly string[] _features =
    [
        "a shared notes page where several people type at once",
        "a form several people fill in together, without overwriting each other",
        "a kanban board with columns and cards people reorder",
        "tags on a document that people add and remove",
        "show who else is on the page and where their cursor is",
        "keep working offline and sync when the connection returns",
        "persist documents on the server across restarts",
        "undo that only reverts my own edits",
        "a version history people can restore from",
        "a background worker that updates a shared document",
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

    // "hostingModel" is deliberately missing here: its values are declared with [AllowedValues], which
    // the SDK completes from on its own, and anything this returned would be merged with that rather
    // than replace it - the same value offered to someone twice.
    private static string[] CandidatesFor(string? argumentName) => argumentName switch
    {
        "heading" => [.. BcrdtSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BcrdtApiCatalog.Types.Select(type => type.Name)],
        "path" => [.. BcrdtSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
