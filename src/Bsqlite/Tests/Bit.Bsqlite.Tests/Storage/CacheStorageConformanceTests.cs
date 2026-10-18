using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.CacheStorage;
using Bit.Bsqlite.Storage.ConformanceTests;

namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class CacheStorageConformanceTests : BsqliteStorageProviderConformanceTests
{
    /// <inheritdoc />
    protected override IBsqliteStorageProvider CreateProvider() => new BsqliteCacheStorageProvider();
}
