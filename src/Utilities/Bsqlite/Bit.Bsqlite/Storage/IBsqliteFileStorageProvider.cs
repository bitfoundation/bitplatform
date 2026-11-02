namespace Bit.Bsqlite.Storage;

/// <summary>
/// A storage backend whose databases are files the native engine opens in the application's own
/// process, rather than a VFS the browser worker loads - what Bsqlite runs on where .NET runs natively,
/// in Blazor Hybrid.
/// </summary>
/// <remarks>
/// A backend implementing this declares <see cref="BsqliteExecutionContexts.NativeProcess"/> in its
/// <see cref="IBsqliteStorageProvider.Capabilities"/>; selection offers it only to hosts that run .NET
/// natively, and the session factory opens its databases with the in-process transport instead of
/// starting a worker.
/// </remarks>
public interface IBsqliteFileStorageProvider : IBsqliteStorageProvider
{
    /// <summary>
    /// The full path of the SQLite file that holds <paramref name="databaseName"/>. The file need not
    /// exist yet - opening creates it - but its directory must be creatable.
    /// </summary>
    string GetDatabasePath(string databaseName);
}
