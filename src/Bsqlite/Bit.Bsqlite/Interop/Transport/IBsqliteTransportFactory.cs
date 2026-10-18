using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Creates the transport that will talk to a chosen storage backend.
/// </summary>
/// <remarks>
/// Separated from selection so a test can resolve against real providers and still run the open
/// against an in-process engine, and so the Arch 2 tier can replace the worker transport without
/// touching the resolver.
/// </remarks>
public interface IBsqliteTransportFactory
{
    /// <summary>
    /// A transport configured for <paramref name="provider"/> - its required engine build and VFS
    /// module - but not yet opened.
    /// </summary>
    IBsqliteTransport Create(IBsqliteStorageProvider provider);
}
