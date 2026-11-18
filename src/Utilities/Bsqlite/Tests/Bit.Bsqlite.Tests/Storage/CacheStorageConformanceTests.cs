namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class CacheStorageConformanceTests : BsqliteStorageProviderConformanceTests
{
    /// <inheritdoc />
    protected override IBsqliteStorageProvider CreateProvider() => new BsqliteCacheStorageProvider();
}
