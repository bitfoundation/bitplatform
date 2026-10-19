using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite;

/// <summary>
/// The backend chosen for a database, together with enough context to explain the choice.
/// </summary>
public sealed record BsqliteStorageResolution
{
    /// <summary>The database this decision applies to.</summary>
    public required string DatabaseName { get; init; }

    /// <summary>The chosen backend.</summary>
    public required IBsqliteStorageProvider Provider { get; init; }

    /// <summary>The probe that cleared it.</summary>
    public required BsqliteProbeResult Probe { get; init; }

    /// <summary>Every candidate's outcome, in preference order.</summary>
    public required IReadOnlyList<BsqliteStorageCandidateOutcome> Attempts { get; init; }

    /// <summary>
    /// Whether the most preferred backend was chosen. False means a fallback was used, which is
    /// reported as a warning rather than passed over in silence.
    /// </summary>
    public bool IsFirstChoice { get; init; }

    /// <summary>
    /// Whether the choice was dictated by where the data already lives rather than by preference
    /// order.
    /// </summary>
    public bool WasDecidedByExistingData { get; init; }

    /// <summary>
    /// Whether the database had no binding and was found by asking each backend whether it holds
    /// it - after the binding store was cleared, typically. Committing the resolution binds it again.
    /// </summary>
    public bool WasFoundWithoutBinding { get; init; }

    /// <summary>
    /// A backend ranked above <see cref="Provider"/> that is available but not holding the data.
    /// Non-null only for an existing database, and what
    /// <see cref="BsqliteStorageMigrationMode"/> decides the response to.
    /// </summary>
    public IBsqliteStorageProvider? BetterProviderAvailable { get; init; }
}
