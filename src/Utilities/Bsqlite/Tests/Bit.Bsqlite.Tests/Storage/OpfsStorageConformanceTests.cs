using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.ConformanceTests;
using Bit.Bsqlite.Storage.Opfs;

namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// Runs the contract-layer kit against the OPFS provider. Admin tests skip on desktop because
/// there is no JavaScript runtime - the probe reports unavailable, which is the honest answer.
/// The engine-layer kit and admin operations run in the browser suite.
/// </summary>
[TestClass]
public sealed class OpfsStorageConformanceTests : BsqliteStorageProviderConformanceTests
{
    /// <inheritdoc />
    protected override IBsqliteStorageProvider CreateProvider() => new BsqliteOpfsStorageProvider();
}
