namespace Bit.Bsqlite.Storage;

/// <summary>
/// The browser contexts a storage backend can operate in. Backends differ here: OPFS synchronous
/// access handles exist only inside workers, while IndexedDB is available everywhere.
/// </summary>
[Flags]
public enum BsqliteExecutionContexts
{
    /// <summary>No context - a backend declaring this can never be selected.</summary>
    None = 0,

    /// <summary>The main document thread.</summary>
    Window = 1 << 0,

    /// <summary>A dedicated worker, which is where Bsqlite runs the engine.</summary>
    DedicatedWorker = 1 << 1,

    /// <summary>A shared worker.</summary>
    SharedWorker = 1 << 2,

    /// <summary>A service worker.</summary>
    ServiceWorker = 1 << 3,
}
