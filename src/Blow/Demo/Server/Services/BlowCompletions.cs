using ModelContextProtocol.Protocol;

namespace Bit.Blow.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, an embedded source path. Without
/// completions a person picking <c>blow://api/{typeName}</c> in a client has to go and call a
/// listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BlowCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "the canvas renders nothing at all",
        "nodes are piled up in the top-left corner, unstyled",
        "nodes render but cannot be dragged or connected",
        "edges do not attach to the handles of my custom node",
        "an edge is missing although both nodes are there",
        "my page does not update after the user edits the flow",
        "child nodes are not positioned inside their group",
        "a saved flow loads back without its custom node data",
        "dragging feels laggy under Blazor Server",
    ];

    private static readonly string[] _features =
    [
        "a workflow editor with custom nodes and typed handles",
        "drag nodes from a sidebar palette onto the canvas",
        "save the flow to the database and restore it",
        "auto-layout an imported graph left to right",
        "undo and redo with keyboard shortcuts",
        "copy and paste a selection of nodes",
        "group nodes into a resizable sub-flow",
        "validate connections so only matching handle types connect",
        "a dark theme in the app's own colours",
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
        "heading" => [.. BlowSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BlowApiCatalog.Types.Select(type => type.Name)],
        "path" => [.. BlowSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
