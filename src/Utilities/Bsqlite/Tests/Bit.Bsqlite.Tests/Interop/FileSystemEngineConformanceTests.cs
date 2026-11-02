using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage.ConformanceTests;
using Bit.Bsqlite.Storage.FileSystem;
using Bit.Bsqlite.Storage.Tests;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// Runs the engine kit against the native transport on the file system backend: the engine a Blazor
/// Hybrid app runs, on a real file.
/// </summary>
[TestClass]
public sealed class FileSystemEngineConformanceTests : BsqliteStorageEngineConformanceTests
{
    private readonly TemporaryDirectory _root = new();

    /// <inheritdoc />
    protected override async ValueTask<IBsqliteTransport> CreateOpenTransportAsync()
    {
        var provider = new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path });
        var transport = new BsqliteNativeTransport(provider);
        await transport.OpenAsync($"conformance-{Guid.NewGuid():N}.db");
        return transport;
    }

    [TestCleanup]
    public void DeleteRoot() => _root.Dispose();
}
