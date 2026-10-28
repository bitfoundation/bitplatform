using ModelContextProtocol.Protocol;

namespace Bit.Bdnd.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, an embedded source path. Without
/// completions a person picking <c>bdnd://api/{typeName}</c> in a client has to go and call a
/// listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BdndCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "nothing can be dragged at all",
        "the item drags but the drop target never lights up",
        "OnDrop never runs when I drop on the target",
        "OnDrop runs on the outer target as well as the inner one",
        "the no-drop cursor shows in the gaps between list items",
        "OnEnd reports DidDrop = false although I dropped on the list",
        "InvalidOperationException: must be placed inside a BdndProvider",
        "There is no registered service of type 'Bit.Bdnd.BdndInterop'",
        "Could not load file or assembly 'Bit.Bdnd' after lazy loading it",
        "files dropped from the desktop open in the browser instead",
        "my custom drag preview flickers or blocks the drop",
    ];

    private static readonly string[] _features =
    [
        "a kanban board whose cards move between columns",
        "a sortable list reordered by dragging",
        "drop files from the desktop onto an upload zone",
        "a custom drag preview that follows the pointer",
        "drag items only by a grip handle",
        "nested drop targets that highlight only the innermost one",
        "drag tools from a palette onto a canvas, copying them",
        "a chessboard that highlights the legal squares while a piece is dragged",
        "snap an item back when it is dropped outside the list",
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
        "heading" => [.. BdndSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BdndApiCatalog.Types.Select(type => type.Name)],
        "path" => [.. BdndSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
