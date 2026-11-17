namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// Runs the contract-layer kit against the IndexedDB provider. Admin tests skip on desktop because
/// there is no JavaScript runtime - the probe reports unavailable, which is the honest answer.
/// </summary>
[TestClass]
public sealed class IndexedDbStorageConformanceTests : BsqliteStorageProviderConformanceTests
{
    /// <inheritdoc />
    protected override IBsqliteStorageProvider CreateProvider() => new BsqliteIndexedDbStorageProvider();
}
