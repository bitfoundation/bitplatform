using Bit.Bsqlite.Storage.ConformanceTests;
using Bit.Bsqlite.Storage.InMemory;

namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// Runs the conformance kit against the in-memory backend, which is the contract's reference
/// implementation - so a kit rule the in-memory provider cannot satisfy is a bug in the rule.
/// </summary>
[TestClass]
public sealed class InMemoryStorageConformanceTests : BsqliteStorageProviderConformanceTests
{
    /// <inheritdoc />
    protected override IBsqliteStorageProvider CreateProvider() => new BsqliteInMemoryStorageProvider();
}
