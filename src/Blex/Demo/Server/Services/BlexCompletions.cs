using ModelContextProtocol.Protocol;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// Both of those are, on this server, keys into a closed set that nothing on the wire spells out: a docs
/// slug, a guide heading, a public type name, a diagnostic id, an embedded source path. Without
/// completions a person picking <c>blex://docs/{slug}</c> in a client has to call a listing tool first,
/// just to learn what a slug looks like.
/// </para>
/// </summary>
public static class BlexCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for.
    private static readonly string[] _symptoms =
    [
        "the component does not re-render when the store changes",
        "persisted state is lost after a reload",
        "the build fails with BLEX001 and dozens of 'does not contain a definition' errors",
        "the store resets when switching from server to WebAssembly",
        "an effect keeps running after the user navigated away",
        "undo restores the wrong state",
        "Redux DevTools shows nothing",
        "a store resolved during prerendering throws a missing-service exception"
    ];

    private static readonly string[] _features =
    [
        "a shopping cart with a computed total that survives a reload",
        "search-as-you-type that cancels the previous request",
        "a normalized list of contacts with add, update and remove",
        "undo and redo for a drawing canvas",
        "log every action to the console",
        "block every action while a read-only mode is on",
        "sync a setting across browser tabs",
        "react in one store to an action of another"
    ];

    /// <summary>The values matching what has been typed so far, for the argument with that name.</summary>
    public static Completion Complete(string? argumentName, string? typed)
    {
        var candidates = CandidatesFor(argumentName);
        if (candidates.Length == 0) return new Completion { Values = [] };

        var prefix = (typed ?? string.Empty).Trim();

        var matches = prefix.Length == 0
            ? candidates
            : [.. candidates
                .Where(value => value.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)];

        return new Completion
        {
            Values = [.. matches.Take(MaxValues)],
            Total = matches.Length,
            HasMore = matches.Length > MaxValues
        };
    }

    // "hostingModel" is deliberately missing: its values are declared with [AllowedValues], which the
    // SDK completes from on its own, and anything returned here would be offered twice.
    private static string[] CandidatesFor(string? argumentName) => argumentName switch
    {
        "slug" => [.. DocsPageRenderer.Pages.Select(page => page.Slug)],
        "heading" => [.. BlexSourceCatalog.GuideSections.Select(section => section.Heading)],
        "typeName" => [.. BlexApiCatalog.Types.Select(type => type.Name)],
        "code" => [.. BlexDiagnosticsReference.Entries.Select(entry => entry.Id)],
        "path" => [.. BlexSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
