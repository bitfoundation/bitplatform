using ModelContextProtocol.Protocol;

namespace Bit.Borm.Demo.Server.Services;

/// <summary>
/// Answers <c>completion/complete</c>: the values a client offers while someone fills in a prompt
/// argument or a resource template's placeholder.
/// <para>
/// The protocol allows this for two things only - a prompt's arguments and a resource template's
/// placeholders - and both of those are, on this server, keys into a closed set that nothing on the
/// wire spells out: a guide heading, a public type name, an embedded source path. Without
/// completions a person picking <c>borm://api/{typeName}</c> in a client has to go and call a
/// listing tool first, just to learn what a type name looks like.
/// </para>
/// </summary>
public static class BormCompletions
{
    /// <summary>The protocol caps one response at 100 values; anything past that is reported through <c>hasMore</c>.</summary>
    private const int MaxValues = 100;

    // Freeform arguments cannot be completed, only started off: these are the phrasings the matching
    // prompt was written for, so picking one lands in the workflow it was designed around instead of
    // in a sentence the tools have nothing to say about.
    private static readonly string[] _symptoms =
    [
        "there is no registered service of type IBormFieldRendererRegistry",
        "the form renders as plain unstyled HTML",
        "typing into a field does nothing",
        "a field that should appear never shows up",
        "the form refuses to submit and shows no error",
        "an error stays under the confirmation box after I fix the password",
        "a dropdown's options do not reload when the other field changes",
        "two forms on one page fight over the same field",
        "a JSON Schema imports with fields missing",
        "a custom renderer key throws at render time",
    ];

    private static readonly string[] _features =
    [
        "a sign-up form generated from a C# model",
        "a field that only shows when a checkbox is ticked",
        "load a dropdown's options from a service when another field changes",
        "a multi-step wizard that validates each step",
        "a repeater of order lines with a computed total",
        "render a form from a JSON Schema a user pastes in",
        "check a username is free with an async rule",
        "edit an existing record and warn about unsaved changes",
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

    // "hostingModel" is deliberately missing here: its values are declared with [AllowedValues], which
    // the SDK completes from on its own, and anything this returned would be merged with that rather
    // than replace it - the same value offered to someone twice.
    private static string[] CandidatesFor(string? argumentName) => argumentName switch
    {
        "heading" => [.. BormSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal)],
        "typeName" => [.. BormApiCatalog.Types.Select(type => type.Name)],
        "path" => [.. BormSourceCatalog.SourceFiles.Select(file => file.Path)],
        "symptom" => _symptoms,
        "feature" => _features,
        _ => []
    };
}
