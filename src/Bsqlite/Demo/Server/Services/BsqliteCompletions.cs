using ModelContextProtocol.Protocol;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a document name, a public type name, a storage backend, a setup scenario, an
/// embedded source path. Without completions a person picking <c>bsqlite://api/{typeName}</c> in a
/// client has to go and call a listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BsqliteCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of in
    // a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "prerendering fails with a missing service",
        "the page throws during prerendering when it opens the database",
        "the database is empty after a reload",
        "the data is gone after OPFS became available",
        "BsqliteStorageUnavailableException on open",
        "SaveChanges throws BsqliteSynchronousApiNotSupportedException",
        "a live query does not refresh after a write",
        "PRAGMA journal_mode = WAL is rejected",
        "ATTACH is rejected on OPFS",
        "the worker or the engine fails to load under a sub-path",
    ];

    /// <summary>
    /// The values matching what has been typed so far. The argument's name is enough to identify the set -
    /// no two arguments on this server mean different things by the same name - so one table serves a
    /// prompt argument and a resource template placeholder without either knowing about the other, and an
    /// argument it has nothing for is answered with an empty list rather than an error.
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
                // What was typed is a prefix far more often than it is a substring, so the values it starts
                // stay above the ones it merely appears inside.
                .OrderByDescending(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)];

        return new Completion
        {
            Values = [.. matches.Take(MaxValues)],
            Total = matches.Length,
            HasMore = matches.Length > MaxValues
        };
    }

    // "hosting" is deliberately missing here: its values are declared with [AllowedValues] on the prompt,
    // which the SDK completes from on its own, and anything this returned would be merged with that rather
    // than replace it - the same value offered to someone twice. "scenario" is here because the setup
    // resource template has no attribute to declare its values with.
    private static string[] CandidatesFor(string? argumentName) => argumentName switch
    {
        "name" => [.. BsqliteSourceCatalog.Documents.Select(document => document.Name)],
        "typeName" => [.. BsqliteApiCatalog.Types.Select(type => type.Name)],
        "provider" => [.. BsqliteStorageCatalog.Providers.Select(provider => provider.Name)],
        "scenario" => BsqliteSetupGuide.Scenarios,
        "path" => [.. BsqliteSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        _ => []
    };
}
