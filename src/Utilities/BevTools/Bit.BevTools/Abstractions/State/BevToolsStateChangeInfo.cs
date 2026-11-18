namespace Bit.BevTools;

/// <summary>Optional metadata about a state change.</summary>
public sealed record BevToolsStateChangeInfo(string? Action = null, string? Detail = null)
{
    public static readonly BevToolsStateChangeInfo Empty = new();

    /// <summary>
    /// What the provider calls the state right after this change (a version, an action index), for a provider that
    /// implements <see cref="IBevToolsStateRestorer"/>: BevTools offers to restore the change and hands the token back.
    /// </summary>
    public string? RestoreToken { get; init; }

    /// <summary>
    /// The values the change was made with, by name: an action's arguments. BevTools reads them through its inspector, at
    /// the session's value access, when the change is raised, and shows them after <see cref="Detail"/>, which is shown
    /// as it is: put the application's values here, not there.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>>? Arguments { get; init; }
}
