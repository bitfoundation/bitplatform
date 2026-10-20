namespace Bit.Bsqlite;

/// <summary>
/// Thrown when no configured storage backend could be used, carrying what was tried and why each
/// candidate was rejected.
/// </summary>
/// <remarks>
/// Selection never substitutes a backend the application did not ask for, so this exception is the
/// only outcome when the configured choices do not work out. It reports every candidate's fate
/// because "storage unavailable" on its own is not something a developer can act on.
/// </remarks>
public sealed class BsqliteStorageUnavailableException : Exception
{
    /// <param name="databaseName">The database that could not be opened.</param>
    /// <param name="attempts">Every candidate considered, in preference order.</param>
    public BsqliteStorageUnavailableException(
        string databaseName,
        IReadOnlyList<BsqliteStorageCandidateOutcome> attempts)
        : base(BuildMessage(databaseName, attempts))
    {
        DatabaseName = databaseName;
        Attempts = attempts;
    }

    /// <summary>The database the application was trying to open.</summary>
    public string DatabaseName { get; }

    /// <summary>Every candidate considered, in preference order, with its outcome.</summary>
    public IReadOnlyList<BsqliteStorageCandidateOutcome> Attempts { get; }

    private static string BuildMessage(
        string databaseName,
        IReadOnlyList<BsqliteStorageCandidateOutcome> attempts)
    {
        var lines = attempts.Select(a => $"  - {a}");

        return $"""
            No storage provider could open database '{databaseName}'.

            Candidates, in preference order:
            {string.Join(Environment.NewLine, lines)}
            """;
    }
}
