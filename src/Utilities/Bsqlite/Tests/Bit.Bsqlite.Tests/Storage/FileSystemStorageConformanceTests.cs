using Bit.Bsqlite.Storage.ConformanceTests;
using Bit.Bsqlite.Storage.FileSystem;

namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// Runs the conformance kit against the file system backend - on desktop .NET, which runs natively the
/// way a Blazor Hybrid app does, so every rule runs rather than being skipped.
/// </summary>
[TestClass]
public sealed class FileSystemStorageConformanceTests : BsqliteStorageProviderConformanceTests
{
    private readonly TemporaryDirectory _root = new();

    /// <inheritdoc />
    protected override IBsqliteStorageProvider CreateProvider()
        => new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path });

    [TestCleanup]
    public void DeleteRoot() => _root.Dispose();
}
