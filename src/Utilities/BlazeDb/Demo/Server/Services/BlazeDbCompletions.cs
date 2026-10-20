using ModelContextProtocol.Protocol;

namespace Bit.BlazeDb.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, a diagnostic id, an embedded source path.
/// Without completions a person picking <c>blazedb://api/{typeName}</c> in a client has to go and call
/// a listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BlazeDbCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "the data is gone after a page reload",
        "BlazeDbDatabaseLockedException when a second tab opens",
        "'Todo' does not contain a definition for 'Table'",
        "a lookup by index does not find a row I just updated",
        "BlazeDbStaleRowException on UpdateInPlace",
        "DbUpdateConcurrencyException from SaveChanges",
        "rows come back with the wrong values after adding a property",
        "PlatformNotSupportedException during prerendering",
        "the UI freezes while scanning a large table",
        "BlazeDbStorageQuotaExceededException",
    ];

    private static readonly string[] _features =
    [
        "a todo list that survives a page reload",
        "an offline cache of server data with an index for lookups",
        "a unique index on an email address",
        "a range query over dates, newest first",
        "EF Core DbContext with LINQ over the browser database",
        "encrypt the database with a user's passphrase",
        "read-only replicas in other tabs that refresh on change",
        "fall back to IndexedDB where OPFS is not available",
        "stream a large result set without freezing the UI",
        "add a property to a table without losing stored rows",
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
        "heading" => [.. BlazeDbSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BlazeDbApiCatalog.Types.Select(type => type.Name)],
        "code" => [.. BlazeDbDiagnosticsReference.Entries.Select(entry => entry.Id)],
        "path" => [.. BlazeDbSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
