namespace Bit.Bsqlite.Storage;

/// <summary>
/// A storage backend for Bsqlite. Implement this to persist SQLite databases somewhere the
/// first-party providers do not reach - an encrypted store, a read-only HTTP range server, or a
/// browser API that does not exist yet.
/// </summary>
/// <remarks>
/// <para>
/// This contract is public and semver-stable from 1.0, and the core consumes nothing else: adding a
/// backend requires no change to Bsqlite itself. Every implementation, first- or third-party,
/// must pass the conformance kit shipped in <c>Bit.Bsqlite.Testing</c>, which is what makes the declared
/// <see cref="Capabilities"/> trustworthy.
/// </para>
/// <para>
/// Implementations are resolved from dependency injection and are expected to be safe to share, since
/// one provider instance serves every connection to every database it holds.
/// </para>
/// </remarks>
public interface IBsqliteStorageProvider
{
    /// <summary>
    /// Stable identifier used to select this backend, such as <c>opfs</c> or <c>indexeddb</c>.
    /// </summary>
    /// <remarks>
    /// Compared case-insensitively, and recorded as the sticky binding of every database this backend
    /// creates - so renaming it after release orphans existing data. Third parties should qualify
    /// their names to avoid colliding with a future first-party backend.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// What this backend can do. Read once and treated as constant, so it must not depend on the
    /// outcome of <see cref="ProbeAsync"/>.
    /// </summary>
    BsqliteStorageCapabilities Capabilities { get; }

    /// <summary>
    /// The ES module registering this backend's VFS, or <see langword="null"/> when it needs no
    /// JavaScript because its storage lives in the engine's own memory.
    /// </summary>
    BsqliteJsModule? VfsModule { get; }

    /// <summary>Database-level operations, used by diagnostics and cross-provider migration.</summary>
    IBsqliteStorageAdmin Admin { get; }

    /// <summary>
    /// Reports whether this backend works in the current browser, and the facts behind the verdict.
    /// </summary>
    /// <remarks>
    /// Called at most once per session by the core, which caches the result. Implementations should
    /// report unavailability through <see cref="BsqliteProbeResult.Unavailable"/> rather than by
    /// throwing, so that selection can move on to the next candidate and still explain itself.
    /// </remarks>
    ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default);
}
