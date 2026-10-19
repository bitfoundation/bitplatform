namespace Bit.Bsqlite.Storage;

/// <summary>
/// The SQLite/WASM build a storage backend needs, because a VFS whose reads are asynchronous cannot
/// run on an engine compiled for synchronous I/O.
/// </summary>
public enum BsqliteEngineBuild
{
    /// <summary>
    /// The engine may be the plain synchronous build. Only backends whose VFS can satisfy reads and
    /// writes without awaiting qualify - in practice, OPFS synchronous access handles and in-memory.
    /// </summary>
    /// <remarks>
    /// Listed first so it is not the default value: declaring <see cref="Synchronous"/> by accident
    /// would load an engine that cannot drive the VFS at all.
    /// </remarks>
    Synchronous,

    /// <summary>
    /// The engine must be able to suspend inside a VFS call - JSPI where available, Asyncify
    /// otherwise. Required by any backend built on an asynchronous browser API.
    /// </summary>
    AsyncCapable,
}
