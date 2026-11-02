namespace Bit.Bsqlite.Storage;

/// <summary>
/// The contexts a storage backend can operate in. Backends differ here: OPFS synchronous access
/// handles exist only inside workers, IndexedDB is available everywhere in the browser, and a file
/// system backend works only where .NET itself runs natively.
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

    /// <summary>
    /// The application's own process, where .NET runs natively rather than in the browser - a Blazor
    /// Hybrid app on Android, iOS, Mac Catalyst, Windows or macOS. The engine there is the native SQLite
    /// that ships with the package, opened in-process, so a backend declaring this must implement
    /// <see cref="IBsqliteFileStorageProvider"/>.
    /// </summary>
    NativeProcess = 1 << 4,
}
