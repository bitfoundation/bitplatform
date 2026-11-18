namespace Bit.BevTools;

/// <summary>
/// How much of the application's own values BevTools reads: component parameters, cascading values and fields, state
/// provider snapshots, authentication claims, HTTP header values and browser storage previews. Structure, counts,
/// timings, causes, events, requests and errors are recorded at every level; secrets are redacted at every level.
/// </summary>
public enum BevToolsValueAccess
{
    /// <summary>Values are read and shown, objects can be expanded member by member, with redaction.</summary>
    Full,

    /// <summary>
    /// Only scalar values are shown (numbers, booleans, enums, dates, strings that do not look like credentials);
    /// objects and collections show their type and size and are never walked or printed through <c>ToString</c>.
    /// Forced re-render detection needs <see cref="Full"/>, since it compares parameters member by member.
    /// </summary>
    Shallow,

    /// <summary>No application value is read: parameters, cascading values, fields and state show names and types only.</summary>
    None,
}
